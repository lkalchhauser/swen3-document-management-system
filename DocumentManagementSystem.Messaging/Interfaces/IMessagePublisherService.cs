namespace DocumentManagementSystem.Messaging.Interfaces;

public interface IMessagePublisherService
{
	Task PublishAsync<T>(T message, CancellationToken ct = default);
}