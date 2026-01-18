using DocumentManagementSystem.Application.Configuration;
using DocumentManagementSystem.Application.Mapper;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.DAL;
using DocumentManagementSystem.DAL.Repositories;
using DocumentManagementSystem.DAL.Repositories.Interfaces;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Interfaces;
using DocumentManagementSystem.Messaging.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using NLog;
using NLog.Web;

namespace DocumentManagementSystem.REST
{
	public partial class Program
	{
		public const bool RECREATE_DATABASE = true;

		public static void Main(string[] args)
		{
			var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

			try
			{
				var builder = WebApplication.CreateBuilder(args);

				// Configure NLog
				builder.Logging.ClearProviders();
				builder.Host.UseNLog();

				var conn = builder.Configuration.GetConnectionString("Default");

				builder.Services.AddOptions<RabbitMQOptions>()
					.Bind(builder.Configuration.GetSection("RabbitMq"))
					.ValidateDataAnnotations()
					.Validate(o => !string.IsNullOrWhiteSpace(o.QueueName), "QueueName required");

				builder.Services.AddOptions<MinioOptions>()
					.Bind(builder.Configuration.GetSection(MinioOptions.SectionName));

				builder.Services.AddOptions<ElasticSearchOptions>()
					.Bind(builder.Configuration.GetSection(ElasticSearchOptions.SectionName));

				builder.Services.AddSingleton<IMessagePublisherService>(sp =>
			{
				var options = sp.GetRequiredService<IOptions<RabbitMQOptions>>();
				var logger = sp.GetRequiredService<ILogger<MessagePublisherService>>();
				return MessagePublisherService.CreateAsync(options, logger).GetAwaiter().GetResult();
			});

				builder.Services.AddSingleton<IMinioClient>(sp =>
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

				builder.Services.AddScoped<IStorageService, MinioStorageService>();

				// Only configure PostgreSQL if not in Testing environment
				if (builder.Environment.EnvironmentName != "Testing")
				{
					builder.Services.AddDbContext<DocumentManagementSystemContext>(opts => opts.UseNpgsql(conn));
				}

				builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
				builder.Services.AddScoped<IDocumentAccessLogRepository, DocumentAccessLogRepository>();
				builder.Services.AddScoped<IDocumentService, DocumentService>();
				builder.Services.AddScoped<ISearchService, ElasticSearchService>();
				builder.Services.AddScoped<INoteService, NoteService>();
				builder.Services.AddScoped<IBatchMonitoringService, BatchMonitoringService>();
				builder.Services.AddScoped<IAccessTrackingService, AccessTrackingService>();

				// Add services to the container.
				builder.Services.AddAutoMapper(
				cfg =>
				{

				}, typeof(MappingProfile)
			);

				// Configure CORS for frontend communication
				builder.Services.AddCors(options =>
				{
					options.AddPolicy("AllowUI", policy =>
					{
						policy.WithOrigins("http://localhost", "http://localhost:80", "http://ui")
							  .AllowAnyMethod()
							  .AllowAnyHeader()
							  .AllowCredentials();
					});
				});

				builder.Services.AddControllers();
				// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
				builder.Services.AddEndpointsApiExplorer();
				builder.Services.AddSwaggerGen();

				var app = builder.Build();

				using (var scope = app.Services.CreateScope())
				{
					var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagementSystemContext>();
					var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

					// Only recreate database if not in Testing environment
					if (RECREATE_DATABASE && env.EnvironmentName != "Testing")
					{
						dbContext.Database.ExecuteSqlRaw(@"
            DO $$
            DECLARE
                r RECORD;
            BEGIN
                FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
                    EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
                END LOOP;
            END $$;
        ");
						dbContext.Database.EnsureCreated();
					}
				}

				// Configure the HTTP request pipeline.
				if (app.Environment.IsDevelopment())
				{
					app.UseSwagger();
					app.UseSwaggerUI();
				}

				app.UseCors("AllowUI");
				app.UseAuthorization();


				app.MapControllers();

				logger.Info("Starting application");
				app.Run();
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Application stopped due to exception");
				throw;
			}
			finally
			{
				LogManager.Shutdown();
			}
		}
	}
}
