namespace DocumentManagementSystem.Application.Services.Interfaces;

public interface IMessagePublisherService
{
	Task PublishAsync<T>(T message, CancellationToken ct = default);
}