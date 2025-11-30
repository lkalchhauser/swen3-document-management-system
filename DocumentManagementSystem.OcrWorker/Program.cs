using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Messaging.Model;
using DocumentManagementSystem.OcrWorker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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