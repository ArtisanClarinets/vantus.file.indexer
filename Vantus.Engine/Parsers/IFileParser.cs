using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Vantus.Engine.Parsers;

public interface IFileParser
{
    bool CanParse(string extension);
    Task<string> ParseAsync(string filePath);
}

public class TextParser : IFileParser
{
    public bool CanParse(string extension) => extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) || extension.Equals(".md", StringComparison.OrdinalIgnoreCase) || extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ParseAsync(string filePath)
    {
        try { return await File.ReadAllTextAsync(filePath); }
        catch { return string.Empty; }
    }
}

public class PdfParser : IFileParser
{
    public bool CanParse(string extension) => extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ParseAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                using var document = PdfDocument.Open(filePath);
                var pages = document.GetPages().Select(p => p.Text);
                return string.Join(Environment.NewLine, pages);
            }
            catch
            {
                return string.Empty;
            }
        });
    }
}

public class OfficeParser : IFileParser
{
    public bool CanParse(string extension) => extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ParseAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document.Body;
                return body?.InnerText ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        });
    }
}

public class ImageParser : IFileParser
{
    public bool CanParse(string extension) =>
        extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".png", StringComparison.OrdinalIgnoreCase);

    public Task<string> ParseAsync(string filePath)
    {
        // Placeholder for OCR / EXIF extraction
        return Task.FromResult($"[Image Metadata Placeholder for {Path.GetFileName(filePath)}]");
    }
}
