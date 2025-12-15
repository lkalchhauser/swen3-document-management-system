using DocumentManagementSystem.Application.Configuration;
using DocumentManagementSystem.Application.Mapper;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Services.Gemini;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.DAL.Repositories;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Interfaces;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.OcrWorker.Configuration;
using DocumentManagementSystem.OcrWorker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

			services.AddOptions<MinioOptions>()
				.Bind(ctx.Configuration.GetSection(MinioOptions.SectionName));

			services.AddOptions<TesseractOptions>()
				.Bind(ctx.Configuration.GetSection(TesseractOptions.SectionName))
				.Configure(opts =>
				{
					if (string.IsNullOrEmpty(opts.TessdataPath))
						opts.TessdataPath = "/usr/share/tesseract-ocr/5/tessdata";
					if (string.IsNullOrEmpty(opts.Language))
						opts.Language = "eng";
				});

			services.AddOptions<ElasticSearchOptions>()
				.Bind(ctx.Configuration.GetSection(ElasticSearchOptions.SectionName));

			services.AddOptions<GeminiOptions>()
				.Bind(ctx.Configuration.GetSection(GeminiOptions.SectionName))
				.ValidateDataAnnotations()
				.Validate(opts => !string.IsNullOrWhiteSpace(opts.ApiKey), "Gemini API key is required");

			services.AddHttpClient<IGenAiService, GeminiAiService>();

			var connectionString = ctx.Configuration.GetConnectionString("Default");
			services.AddDbContext<DocumentManagementSystemContext>(opts =>
				opts.UseNpgsql(connectionString));

			services.AddSingleton<IMessagePublisherService>(sp =>
			{
				var options = sp.GetRequiredService<IOptions<RabbitMQOptions>>();
				var logger = sp.GetRequiredService<ILogger<MessagePublisherService>>();
				return MessagePublisherService.CreateAsync(options, logger).GetAwaiter().GetResult();
			});

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

			services.AddScoped<IDocumentRepository, DocumentRepository>();

			services.AddScoped<IDocumentUpdateService, DocumentUpdateService>();

			services.AddScoped<IStorageService, MinioStorageService>();
			services.AddScoped<IOcrService, TesseractOcrService>();
			services.AddScoped<ISearchService, ElasticSearchService>();
			services.AddScoped<INoteService, NoteService>();
			services.AddScoped<IDocumentService, DocumentService>();

			services.AddAutoMapper(
				cfg =>
				{

				}, typeof(MappingProfile)
			);

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