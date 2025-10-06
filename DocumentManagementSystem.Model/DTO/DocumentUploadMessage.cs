namespace DocumentManagementSystem.Model.DTO;

public record DocumentUploadMessage(
	Guid DocumentId,
	string FileName,
	string? StoragePath,
	DateTimeOffset UploadedAtUtc
);