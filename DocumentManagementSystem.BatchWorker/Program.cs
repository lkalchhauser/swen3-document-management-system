using DocumentManagementSystem.BatchWorker.Configuration;
using DocumentManagementSystem.BatchWorker.Jobs;
using DocumentManagementSystem.BatchWorker.Services;
using DocumentManagementSystem.BatchWorker.Services.Interfaces;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.DAL.Repositories;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;
using Quartz;

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
	logger.Info("Starting Batch Worker");

	var builder = Host.CreateApplicationBuilder(args);

	builder.Logging.ClearProviders();
	builder.Logging.AddNLog(new NLogProviderOptions());

	builder.Services.AddOptions<BatchProcessingOptions>()
		.Bind(builder.Configuration.GetSection(BatchProcessingOptions.SectionName))
		.ValidateDataAnnotations()
		.ValidateOnStart();

	var connectionString = builder.Configuration.GetConnectionString("Default");
	builder.Services.AddDbContext<DocumentManagementSystemContext>(opts =>
		opts.UseNpgsql(connectionString));

	builder.Services.AddScoped<IDocumentAccessLogRepository, DocumentAccessLogRepository>();
	builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
	builder.Services.AddScoped<IXmlParserService, XmlParserService>();
	builder.Services.AddScoped<IAccessLogPersistenceService, AccessLogPersistenceService>();
	builder.Services.AddScoped<IFileProcessorService, FileProcessorService>();

	builder.Services.AddQuartz(q =>
	{
		var jobKey = new JobKey("AccessLogProcessingJob");
		q.AddJob<AccessLogProcessingJob>(opts => opts.WithIdentity(jobKey));

		q.AddTrigger(opts =>
		{
			var cronSchedule = builder.Configuration
				.GetSection(BatchProcessingOptions.SectionName)
				.GetValue<string>("CronSchedule") ?? "0 0 1 * * ?";

			opts.ForJob(jobKey)
				.WithIdentity("AccessLogProcessingTrigger")
				.WithCronSchedule(cronSchedule);
		});
	});

	builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

	var host = builder.Build();

	using (var scope = host.Services.CreateScope())
	{
		var context = scope.ServiceProvider.GetRequiredService<DocumentManagementSystemContext>();
		await context.Database.MigrateAsync();
		logger.Info("Database migration completed");
	}

	await host.RunAsync();
}
catch (Exception ex)
{
	logger.Error(ex, "Batch Worker stopped due to exception");
	throw;
}
finally
{
	LogManager.Shutdown();
}
