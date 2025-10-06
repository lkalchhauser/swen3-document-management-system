using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.OcrWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
	.ConfigureServices((ctx, services) =>
	{
		services.AddOptions<RabbitMQOptions>()
			.Bind(ctx.Configuration.GetSection("RabbitMq"));
		services.AddHostedService<OcrWorkerService>();
	})
	.RunConsoleAsync();