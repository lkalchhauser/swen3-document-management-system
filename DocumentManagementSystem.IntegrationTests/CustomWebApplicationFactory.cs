using DocumentManagementSystem.DAL;
using DocumentManagementSystem.Messaging.Interfaces;
using DocumentManagementSystem.REST;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace DocumentManagementSystem.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<DocumentManagementSystem.REST.Program>
{
    public Mock<IMessagePublisherService> MockMessagePublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove DbContext
            services.RemoveAll<DocumentManagementSystemContext>();
            services.RemoveAll<DbContextOptions<DocumentManagementSystemContext>>();
            services.RemoveAll<DbContextOptions>();

            // Add InMemory database for tests with unique name per factory instance
            var uniqueDbName = $"InMemoryTestDb_{Guid.NewGuid()}";
            services.AddDbContext<DocumentManagementSystemContext>(options =>
            {
                options.UseInMemoryDatabase(uniqueDbName)
                       .EnableSensitiveDataLogging();
            });

            // Replace MessagePublisher with mock
            services.RemoveAll<IMessagePublisherService>();
            services.AddSingleton(MockMessagePublisher.Object);
        });

        builder.UseEnvironment("Testing");
    }
}
