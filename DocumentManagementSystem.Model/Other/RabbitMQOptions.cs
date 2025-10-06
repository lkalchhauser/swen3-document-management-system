namespace DocumentManagementSystem.Model.Other;

public class RabbitMQOptions
{
	public string HostName { get; init; } = "localhost";
	public int Port { get; init; } = 5672;
	public string Username { get; init; } = "guest";
	public string Password { get; init; } = "guest";
	public string QueueName { get; init; } = "ocr_queue";
}