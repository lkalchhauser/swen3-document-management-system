using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace DocumentManagementSystem.OcrWorker.Services;

public class PdfConverterService : IPdfConverterService
{
	private readonly ILogger<PdfConverterService> _logger;
	private const int DefaultDpi = 300; 

	public PdfConverterService(ILogger<PdfConverterService> logger)
	{
		_logger = logger;
	}

	public async Task<IReadOnlyList<byte[]>> ConvertToImagesAsync(Stream pdfStream, CancellationToken ct = default)
	{
		_logger.LogInformation("Starting PDF to image conversion");

		var images = new List<byte[]>();

		try
		{
			await Task.Run(() =>
			{
				using var memoryStream = new MemoryStream();
				pdfStream.CopyTo(memoryStream);
				var pdfBytes = memoryStream.ToArray();

				using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(DefaultDpi, DefaultDpi));
				var pageCount = docReader.GetPageCount();

				_logger.LogInformation("PDF contains {PageCount} pages", pageCount);

				for (int i = 0; i < pageCount; i++)
				{
					ct.ThrowIfCancellationRequested();

					_logger.LogDebug("Converting page {PageNumber} of {PageCount}", i + 1, pageCount);

					using var pageReader = docReader.GetPageReader(i);
					var width = pageReader.GetPageWidth();
					var height = pageReader.GetPageHeight();

					var rawBytes = pageReader.GetImage();
					var imageBytes = ConvertBgraToImage(rawBytes, width, height);
					images.Add(imageBytes);

					_logger.LogDebug("Completed conversion of page {PageNumber}", i + 1);
				}

				_logger.LogInformation("Successfully converted {PageCount} pages to images", pageCount);
			}, ct);

			return images;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to convert PDF to images");
			throw;
		}
	}

	private byte[] ConvertBgraToImage(byte[] rawBytes, int width, int height)
	{
		_logger.LogDebug("Converting BGRA image: {Width}x{Height}, Raw bytes length: {Length}",
			width, height, rawBytes.Length);

		int expectedLength = width * height * 4;
		if (rawBytes.Length != expectedLength)
		{
			_logger.LogWarning("Raw bytes length mismatch. Expected: {Expected}, Actual: {Actual}",
				expectedLength, rawBytes.Length);
		}

		var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

		using var bitmap = new SKBitmap(imageInfo);

		IntPtr pixelsAddr = bitmap.GetPixels();
		System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, pixelsAddr, rawBytes.Length);

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);

		var result = data.ToArray();
		_logger.LogDebug("Successfully converted to PNG image: {Size} bytes", result.Length);

		return result;
	}
}
