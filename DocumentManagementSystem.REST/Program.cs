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

namespace DocumentManagementSystem.REST
{
	public partial class Program
	{
		public const bool RECREATE_DATABASE = true;

		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Logging.ClearProviders();
			builder.Logging.AddConsole();
			builder.Logging.AddDebug();

			var conn = builder.Configuration.GetConnectionString("Default");

			builder.Services.AddOptions<RabbitMQOptions>()
				.Bind(builder.Configuration.GetSection("RabbitMq"))
				.ValidateDataAnnotations()
				.Validate(o => !string.IsNullOrWhiteSpace(o.QueueName), "QueueName required");

			builder.Services.AddSingleton<IMessagePublisherService>(sp =>
			{
				var options = sp.GetRequiredService<IOptions<RabbitMQOptions>>();
				var logger = sp.GetRequiredService<ILogger<MessagePublisherService>>();
				return MessagePublisherService.CreateAsync(options, logger).GetAwaiter().GetResult();
			});

			builder.Services.AddDbContext<DocumentManagementSystemContext>(opts => opts.UseNpgsql(conn));
			builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
			builder.Services.AddScoped<IDocumentService, DocumentService>();

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

				if (RECREATE_DATABASE)
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

			app.Run();
		}
	}
}
