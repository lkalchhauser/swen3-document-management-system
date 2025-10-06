namespace DocumentManagementSystem.Model.DTO;

public record DocumentUploadMessageDTO(
	Guid DocumentId,
	string FileName,
	string? StoragePath,
	DateTimeOffset UploadedAtUtc
);