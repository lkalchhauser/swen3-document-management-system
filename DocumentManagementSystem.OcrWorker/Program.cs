using DocumentManagementSystem.DAL;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.OcrWorker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Minio;
using NLog;
using NLog.Extensions.Hosting;

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
	logger.Info("Starting OCR Worker");

	await Host.CreateDefaultBuilder(args)
		.ConfigureLogging(logging =>
		{
			logging.ClearProviders();
		})
		.UseNLog()
		.ConfigureServices((ctx, services) =>
		{
			services.AddOptions<RabbitMQOptions>()
				.Bind(ctx.Configuration.GetSection("RabbitMq"));

			// Configure PostgreSQL DbContext
			var connectionString = ctx.Configuration.GetConnectionString("Default");
			services.AddDbContext<DocumentManagementSystemContext>(opts =>
				opts.UseNpgsql(connectionString));

			// Configure MinIO client (following demo pattern)
			services.AddSingleton<IMinioClient>(sp =>
			{
				var config = sp.GetRequiredService<IConfiguration>();
				var endpoint = config["MinIO:Endpoint"] ?? "localhost:9000";
				var accessKey = config["MinIO:AccessKey"] ?? "minioadmin";
				var secretKey = config["MinIO:SecretKey"] ?? "minioadmin";
				var useSSL = bool.Parse(config["MinIO:UseSSL"] ?? "false");

				return new MinioClient()
					.WithEndpoint(endpoint)
					.WithCredentials(accessKey, secretKey)
					.WithSSL(useSSL)
					.Build();
			});

			services.AddScoped<IStorageService, MinioStorageService>();
			services.AddScoped<IOcrService, TesseractOcrService>();
			services.AddHostedService<OcrWorkerService>();
		})
		.RunConsoleAsync();
}
catch (Exception ex)
{
	logger.Error(ex, "OCR Worker stopped due to exception");
	throw;
}
finally
{
	LogManager.Shutdown();
}