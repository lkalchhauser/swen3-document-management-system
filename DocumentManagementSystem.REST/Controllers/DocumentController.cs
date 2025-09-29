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
		private readonly ILogger<DocumentController> _logger;

		public DocumentController(IDocumentService service, ILogger<DocumentController> logger)
		{
			_service = service;
			_logger = logger;
		}

		// GET api/document
		[HttpGet]
		public async Task<ActionResult<IReadOnlyList<DocumentDTO>>> GetAll(CancellationToken ct)
		{
			var docs = await _service.GetAllAsync(ct);
			return Ok(docs);
		}

		// GET api/document/{id}
		[HttpGet("{id:guid}")]
		public async Task<ActionResult<DocumentDTO>> GetById(Guid id, CancellationToken ct)
		{
			var doc = await _service.GetByIdAsync(id, ct);
			if (doc is null)
			{
				_logger.LogInformation("Document {DocumentId} not found", id);
				return NotFound();
			}

			return Ok(doc);
		}

		// POST api/document
		[HttpPost]
		public async Task<ActionResult<DocumentDTO>> Create([FromBody] DocumentCreateDTO dto, CancellationToken ct)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var created = await _service.CreateAsync(dto, ct);
			return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
		}

		// PUT api/document/{id}
		[HttpPut("{id:guid}")]
		public async Task<ActionResult<DocumentDTO>> Update(Guid id, [FromBody] DocumentCreateDTO dto,
			CancellationToken ct)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var updated = await _service.UpdateAsync(id, dto, ct);
			if (updated is null)
			{
				_logger.LogInformation("Document {DocumentId} not found for update", id);
				return NotFound();
			}

			return Ok(updated);
		}

		// POST api/document/upload
		[HttpPost("upload")]
		public async Task<ActionResult<DocumentDTO>> UploadFile(IFormFile file, [FromForm] string? tags, CancellationToken ct)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file provided");
			}

			// Parse tags if provided
			var tagList = new List<string>();
			if (!string.IsNullOrWhiteSpace(tags))
			{
				tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim())
					.Where(t => !string.IsNullOrEmpty(t))
					.ToList();
			}

			// Create DTO from uploaded file
			var dto = new DocumentCreateDTO
			{
				FileName = file.FileName,
				FileSize = file.Length,
				ContentType = file.ContentType,
				Tags = tagList
			};

			// For now, we'll create the document record
			// In a real implementation, you'd also save the file to storage
			var created = await _service.CreateAsync(dto, ct);
			return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
		}

		// DELETE api/document/{id}
		[HttpDelete("{id:guid}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
		{
			var success = await _service.DeleteAsync(id, ct);
			if (!success)
			{
				_logger.LogInformation("Document {DocumentId} not found for delete", id);
				return NotFound();
			}

			return NoContent();
		}
	}
}