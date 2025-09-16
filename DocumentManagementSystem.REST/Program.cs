
using DocumentManagementService.DAL;
using DocumentManagementService.DAL.Repositories;
using DocumentManagementService.DAL.Repositories.Interfaces;
using DocumentManagementService.DAL.Services;
using DocumentManagementService.DAL.Services.Interfaces;
using DocumentManagementService.DAL.Mapper;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.REST
{
	public partial class Program
	{
		public const bool RECREATE_DATABASE = true;

		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			var conn = builder.Configuration.GetConnectionString("Default");

			builder.Services.AddDbContext<DocumentManagementServiceContext>(opts => opts.UseNpgsql(conn));
			builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
			builder.Services.AddScoped<IDocumentService, DocumentService>();

			// Add services to the container.
			builder.Services.AddAutoMapper(
				cfg =>
				{

				}, typeof(MappingProfile)
			);

			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			var app = builder.Build();

			using (var scope = app.Services.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagementServiceContext>();

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

			app.UseAuthorization();


			app.MapControllers();

			app.Run();
		}
	}
}
