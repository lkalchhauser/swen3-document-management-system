namespace DocumentManagementSystem.OcrWorker.Configuration;

public class TesseractOptions
{
	public const string SectionName = "Tesseract";

	public string TessdataPath { get; set; } = "/usr/share/tesseract-ocr/5/tessdata";

	public string Language { get; set; } = "eng";

	public int EngineMode { get; set; } = 3;

	public int PageSegmentationMode { get; set; } = 3;
}
