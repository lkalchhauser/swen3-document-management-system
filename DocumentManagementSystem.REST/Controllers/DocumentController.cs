using DocumentManagementSystem.Application.Services.Enums;
using DocumentManagementSystem.Application.Services.Interfaces;
using DocumentManagementSystem.Model.DTO;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.REST.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public sealed class DocumentController : ControllerBase
	{
		private readonly IDocumentService _service;
		private readonly IStorageService _storageService;
		private readonly ISearchService _searchService;
		private readonly IAccessTrackingService _accessTrackingService;
		private readonly ILogger<DocumentController> _logger;

		public DocumentController(IDocumentService service, IStorageService storageService, ISearchService searchService,
			IAccessTrackingService accessTrackingService, ILogger<DocumentController> logger)
		{
			_service = service;
			_storageService = storageService;
			_searchService = searchService;
			_accessTrackingService = accessTrackingService;
			_logger = logger;
		}

		// GET api/document
		[HttpGet]
		public async Task<ActionResult<IReadOnlyList<DocumentDTO>>> GetAll(CancellationToken ct)
		{
			_logger.LogDebug("Getting all documents");
			var docs = await _service.GetAllAsync(ct);
			_logger.LogInformation("Retrieved {Count} documents", docs.Count);
			return Ok(docs);
		}

		// GET api/document/{id}
		[HttpGet("{id:guid}")]
		public async Task<ActionResult<DocumentDTO>> GetById(Guid id, CancellationToken ct)
		{
			_logger.LogDebug("Getting document by ID: {DocumentId}", id);
			var doc = await _service.GetByIdAsync(id, ct);
			if (doc is null)
			{
				_logger.LogWarning("Document {DocumentId} not found", id);
				return NotFound();
			}

		await _accessTrackingService.TrackAccessAsync(id, ct);

			_logger.LogInformation("Retrieved document {DocumentId}", id);
			return Ok(doc);
		}

		// POST api/document
		[HttpPost]
		public async Task<ActionResult<DocumentDTO>> Create([FromBody] DocumentCreateDTO dto, CancellationToken ct)
		{
			if (!ModelState.IsValid)
			{
				_logger.LogWarning("Invalid model state for document creation");
				return BadRequest(ModelState);
			}

			_logger.LogInformation("Creating document: {FileName}", dto.FileName);
			var created = await _service.CreateAsync(dto, null, ct);
			_logger.LogInformation("Document created successfully with ID: {DocumentId}", created.Id);
			return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
		}

		// PUT api/document/{id}
		[HttpPut("{id:guid}")]
		public async Task<ActionResult<DocumentDTO>> Update(Guid id, [FromBody] DocumentCreateDTO dto,
			CancellationToken ct)
		{
			if (!ModelState.IsValid)
			{
				_logger.LogWarning("Invalid model state for document update: {DocumentId}", id);
				return BadRequest(ModelState);
			}

			_logger.LogInformation("Updating document: {DocumentId}", id);
			var updated = await _service.UpdateAsync(id, dto, ct);
			if (updated is null)
			{
				_logger.LogWarning("Document {DocumentId} not found for update", id);
				return NotFound();
			}

			_logger.LogInformation("Document {DocumentId} updated successfully", id);
			return Ok(updated);
		}

		// POST api/document/upload
		[HttpPost("upload")]
		public async Task<ActionResult<DocumentDTO>> UploadFile(IFormFile file, [FromForm] string? tags,
			CancellationToken ct)
		{
			if (file == null || file.Length == 0)
			{
				_logger.LogWarning("Upload attempt with no file provided");
				return BadRequest("No file provided");
			}

			_logger.LogInformation("Uploading file: {FileName}, Size: {FileSize} bytes", file.FileName, file.Length);

			string storagePath;
			await using (var fileStream = file.OpenReadStream())
			{
				storagePath = await _storageService.UploadFileAsync(fileStream, file.FileName, file.ContentType, ct);
			}

			_logger.LogInformation("File saved to MinIO: {StoragePath}", storagePath);

			// Parse tags if provided
			var tagList = new List<string>();
			if (!string.IsNullOrWhiteSpace(tags))
			{
				tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim())
					.Where(t => !string.IsNullOrEmpty(t))
					.ToList();
				_logger.LogDebug("Parsed {TagCount} tags for file upload", tagList.Count);
			}

			// Create DTO from uploaded file
			var dto = new DocumentCreateDTO
			{
				FileName = file.FileName,
				FileSize = file.Length,
				ContentType = file.ContentType,
				Tags = tagList
			};

			// Create document record with storage path
			var created = await _service.CreateAsync(dto, storagePath, ct);
			_logger.LogInformation("File uploaded successfully with ID: {DocumentId}", created.Id);
			return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
		}

		// DELETE api/document/{id}
		[HttpDelete("{id:guid}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
		{
			_logger.LogInformation("Deleting document: {DocumentId}", id);
			var success = await _service.DeleteAsync(id, ct);
			if (!success)
			{
				_logger.LogWarning("Document {DocumentId} not found for delete", id);
				return NotFound();
			}

			_logger.LogInformation("Document {DocumentId} deleted successfully", id);
			return NoContent();
		}

		[HttpGet("search")]
		public async Task<ActionResult<IReadOnlyList<DocumentDTO>>> Search([FromQuery] string query, string mode, CancellationToken ct)
		{
			_logger.LogInformation("Searching for documents with query: {Query}", query);
			var searchMode = mode.ToLower() == "notes" ? SearchMode.Notes : SearchMode.Content;
			var results = await _searchService.SearchAsync(query, searchMode, ct);
			return Ok(results);
		}

		// GET api/document/{id}/preview
		/// <summary>
		/// Stream document file for preview purposes with range request support.
		/// Following Single Responsibility Principle - handles HTTP concerns only.
		/// </summary>
		/// <param name="id">Document unique identifier</param>
		/// <param name="ct">Cancellation token</param>
		/// <returns>File stream with appropriate content type for inline display</returns>
		[HttpGet("{id:guid}/preview")]
		public async Task<IActionResult> PreviewFile(Guid id, CancellationToken ct)
		{
			_logger.LogDebug("Preview request for document: {DocumentId}", id);

			// Retrieve document metadata (Dependency Inversion: depend on IDocumentService abstraction)
			var document = await _service.GetByIdAsync(id, ct);
			if (document is null)
			{
				_logger.LogWarning("Document {DocumentId} not found for preview", id);
				return NotFound(new { message = "Document not found" });
			}

			// Validate storage path exists (defensive programming)
			if (string.IsNullOrWhiteSpace(document.Metadata.StoragePath))
			{
				_logger.LogError("Document {DocumentId} has no storage path", id);
				return NotFound(new { message = "Document file not found in storage" });
			}

			try
			{
				// Download file from storage (Dependency Inversion: depend on IStorageService abstraction)
				var fileStream = await _storageService.DownloadFileAsync(document.Metadata.StoragePath, ct);

				_logger.LogInformation("Streaming preview for document {DocumentId}, Type: {ContentType}",
					id, document.Metadata.ContentType);

				// Return file stream with proper content type for inline display
				// enableRangeProcessing allows browser to seek through PDFs and videos
				return File(fileStream, document.Metadata.ContentType, enableRangeProcessing: true);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error streaming preview for document {DocumentId}", id);
				return StatusCode(500, new { message = "Error retrieving document file" });
			}
		}

		// GET api/document/{id}/download
		/// <summary>
		/// Download document file with proper attachment headers.
		/// Following Open/Closed Principle - extensible through configuration, closed for modification.
		/// </summary>
		/// <param name="id">Document unique identifier</param>
		/// <param name="ct">Cancellation token</param>
		/// <returns>File stream as downloadable attachment</returns>
		[HttpGet("{id:guid}/download")]
		public async Task<IActionResult> DownloadFile(Guid id, CancellationToken ct)
		{
			_logger.LogDebug("Download request for document: {DocumentId}", id);

			// Retrieve document metadata
			var document = await _service.GetByIdAsync(id, ct);
			if (document is null)
			{
				_logger.LogWarning("Document {DocumentId} not found for download", id);
				return NotFound(new { message = "Document not found" });
			}

			// Validate storage path exists
			if (string.IsNullOrWhiteSpace(document.Metadata.StoragePath))
			{
				_logger.LogError("Document {DocumentId} has no storage path", id);
				return NotFound(new { message = "Document file not found in storage" });
			}

			try
			{
				// Download file from storage
				var fileStream = await _storageService.DownloadFileAsync(document.Metadata.StoragePath, ct);

				_logger.LogInformation("Downloading document {DocumentId}, FileName: {FileName}",
					id, document.FileName);

				// Return file as downloadable attachment with original filename
				return File(fileStream, document.Metadata.ContentType, document.FileName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error downloading document {DocumentId}", id);
				return StatusCode(500, new { message = "Error retrieving document file" });
			}
		}
	}
}
