using DocumentManagementSystem.OcrWorker.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace DocumentManagementSystem.OcrWorker.Services;

public class TesseractOcrService : IOcrService
{
	private readonly ILogger<TesseractOcrService> _logger;
	private readonly TesseractOptions _options;

	private const int MinimumEmbeddedTextLength = 50;

	public TesseractOcrService(
		IOptions<TesseractOptions> options,
		ILogger<TesseractOcrService> logger)
	{
		_options = options.Value;
		_logger = logger;
	}

	public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream, CancellationToken ct = default)
	{
		try
		{
			var directText = await TryExtractTextDirectlyAsync(pdfStream, ct);

			if (!string.IsNullOrWhiteSpace(directText) && directText.Length > MinimumEmbeddedTextLength)
			{
				_logger.LogInformation("Successfully extracted text directly from PDF (embedded text): {Length} characters", directText.Length);
				return directText;
			}

			_logger.LogInformation("PDF has no embedded text or insufficient text. Falling back to OCR");

			pdfStream.Position = 0;

			_logger.LogInformation("Starting OCR text extraction from PDF");

			var ocrText = await ExtractTextUsingOcrAsync(pdfStream, ct);

			_logger.LogInformation("OCR text extraction completed. Total extracted: {Length} characters", ocrText.Length);

			return ocrText;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to extract text from PDF");
			throw;
		}
	}

	private async Task<string> TryExtractTextDirectlyAsync(Stream pdfStream, CancellationToken ct)
	{
		try
		{
			return await Task.Run(() =>
			{
				using var memoryStream = new MemoryStream();
				pdfStream.CopyTo(memoryStream);
				var pdfBytes = memoryStream.ToArray();

				using var docReader = Docnet.Core.DocLib.Instance.GetDocReader(pdfBytes, new Docnet.Core.Models.PageDimensions(72, 72));
				var pageCount = docReader.GetPageCount();

				var extractedTexts = new List<string>();

				for (int i = 0; i < pageCount; i++)
				{
					ct.ThrowIfCancellationRequested();

					using var pageReader = docReader.GetPageReader(i);
					var pageText = pageReader.GetText();

					if (!string.IsNullOrWhiteSpace(pageText))
					{
						extractedTexts.Add(pageText);
					}
				}

				return string.Join("\n\n--- Page Break ---\n\n", extractedTexts);
			}, ct);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Direct text extraction failed, will fall back to OCR");
			return string.Empty;
		}
	}

	private async Task<string> ExtractTextUsingOcrAsync(Stream pdfStream, CancellationToken ct)
	{
		var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			var pdfPath = await SavePdfToTempFileAsync(pdfStream, tempDir, ct);
			var imageFiles = await ConvertPdfToImagesAsync(pdfPath, tempDir, ct);

			if (imageFiles.Count == 0)
			{
				_logger.LogWarning("No images were generated from PDF");
				return string.Empty;
			}

			var extractedTexts = await ExtractTextFromImagesAsync(imageFiles, tempDir, ct);

			var combinedText = string.Join("\n\n--- Page Break ---\n\n", extractedTexts);
			_logger.LogInformation("OCR completed. Total extracted: {Length} characters from {PageCount} pages",
				combinedText.Length, imageFiles.Count);

			return combinedText;
		}
		finally
		{
			CleanupTempDirectory(tempDir);
		}
	}

	private async Task<string> SavePdfToTempFileAsync(Stream pdfStream, string tempDir, CancellationToken ct)
	{
		var pdfPath = Path.Combine(tempDir, "input.pdf");
		using (var fileStream = File.Create(pdfPath))
		{
			await pdfStream.CopyToAsync(fileStream, ct);
		}
		_logger.LogDebug("Saved PDF to temp file: {Path}, Size: {Size} bytes", pdfPath, new FileInfo(pdfPath).Length);
		return pdfPath;
	}

	private async Task<List<string>> ConvertPdfToImagesAsync(string pdfPath, string tempDir, CancellationToken ct)
	{
		var pdfToPpmStartInfo = new ProcessStartInfo
		{
			FileName = "pdftoppm",
			Arguments = $"-png -r 300 \"{pdfPath}\" \"{Path.Combine(tempDir, "page")}\"",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using (var pdfToPpmProcess = new Process { StartInfo = pdfToPpmStartInfo })
		{
			pdfToPpmProcess.Start();
			var pdfToPpmError = await pdfToPpmProcess.StandardError.ReadToEndAsync(ct);
			await pdfToPpmProcess.WaitForExitAsync(ct);

			if (pdfToPpmProcess.ExitCode != 0)
			{
				_logger.LogError("pdftoppm failed with exit code {ExitCode}. Error: {Error}", pdfToPpmProcess.ExitCode, pdfToPpmError);
				return new List<string>();
			}

			_logger.LogDebug("Successfully converted PDF to images using pdftoppm");
		}

		var imageFiles = Directory.GetFiles(tempDir, "page-*.png").OrderBy(f => f).ToList();
		_logger.LogInformation("Converted PDF to {PageCount} image(s)", imageFiles.Count);

		return imageFiles;
	}

	private async Task<List<string>> ExtractTextFromImagesAsync(List<string> imageFiles, string tempDir, CancellationToken ct)
	{
		var extractedTexts = new List<string>();

		for (int i = 0; i < imageFiles.Count; i++)
		{
			ct.ThrowIfCancellationRequested();

			var imagePath = imageFiles[i];
			var outputPath = Path.Combine(tempDir, $"output_{i}");

			_logger.LogDebug("Processing OCR for page {PageNumber} of {TotalPages}", i + 1, imageFiles.Count);

			var pageText = await RunTesseractOnImageAsync(imagePath, outputPath, i + 1, ct);
			extractedTexts.Add(pageText);
		}

		return extractedTexts;
	}

	private async Task<string> RunTesseractOnImageAsync(string imagePath, string outputPath, int pageNumber, CancellationToken ct)
	{
		var tesseractStartInfo = new ProcessStartInfo
		{
			FileName = "tesseract",
			Arguments = $"\"{imagePath}\" \"{outputPath}\" -l {_options.Language}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var tesseractProcess = new Process { StartInfo = tesseractStartInfo };
		tesseractProcess.Start();

		var tesseractError = await tesseractProcess.StandardError.ReadToEndAsync(ct);
		await tesseractProcess.WaitForExitAsync(ct);

		if (tesseractProcess.ExitCode != 0)
		{
			_logger.LogWarning("Tesseract exited with code {ExitCode} for page {PageNumber}. Error: {Error}",
				tesseractProcess.ExitCode, pageNumber, tesseractError);
		}

		var textOutputPath = outputPath + ".txt";
		if (File.Exists(textOutputPath))
		{
			var pageText = await File.ReadAllTextAsync(textOutputPath, ct);
			_logger.LogDebug("Extracted {CharCount} characters from page {PageNumber}", pageText?.Length ?? 0, pageNumber);
			return pageText ?? string.Empty;
		}

		_logger.LogWarning("Tesseract did not produce output file for page {PageNumber}", pageNumber);
		return string.Empty;
	}

	private void CleanupTempDirectory(string tempDir)
	{
		if (Directory.Exists(tempDir))
		{
			try
			{
				Directory.Delete(tempDir, recursive: true);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to clean up temp directory: {TempDir}", tempDir);
			}
		}
	}
}
