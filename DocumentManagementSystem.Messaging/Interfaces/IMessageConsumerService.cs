namespace DocumentManagementSystem.Messaging.Interfaces;

public interface IMessageConsumerService
{
	Task StopAsync(CancellationToken ct = default);
}