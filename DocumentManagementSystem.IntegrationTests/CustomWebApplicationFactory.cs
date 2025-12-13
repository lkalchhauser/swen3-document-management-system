using DocumentManagementSystem.Application.Services.Interfaces;
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
    public Mock<IStorageService> MockStorageService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DocumentManagementSystemContext>();
            services.RemoveAll<DbContextOptions<DocumentManagementSystemContext>>();
            services.RemoveAll<DbContextOptions>();

            var uniqueDbName = $"InMemoryTestDb_{Guid.NewGuid()}";
            services.AddDbContext<DocumentManagementSystemContext>(options =>
            {
                options.UseInMemoryDatabase(uniqueDbName)
                       .EnableSensitiveDataLogging();
            });

            services.RemoveAll<IMessagePublisherService>();
            services.AddSingleton(MockMessagePublisher.Object);

            services.RemoveAll<IStorageService>();
            MockStorageService
                .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Stream stream, string fileName, string contentType, CancellationToken ct) =>
                    $"documents/test-{Guid.NewGuid()}_{fileName}");
            services.AddSingleton(MockStorageService.Object);
        });

        builder.UseEnvironment("Testing");
    }
}
