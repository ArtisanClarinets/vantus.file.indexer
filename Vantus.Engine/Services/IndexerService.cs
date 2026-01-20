using Dapper;
using Microsoft.Extensions.Logging;
using Vantus.Engine.Parsers;

namespace Vantus.Engine.Services;

public class IndexerService
{
    private readonly DatabaseService _db;
    private readonly RulesEngineService _rules;
    private readonly AiService _ai;
    private readonly PartnerService _partners;
    private readonly ActionLogService _actionLog;
    private readonly ILogger<IndexerService> _logger;
    private readonly List<IFileParser> _parsers;

    public IndexerService(DatabaseService db, RulesEngineService rules, AiService ai, PartnerService partners, ActionLogService actionLog, ILogger<IndexerService> logger)
    {
        _db = db;
        _rules = rules;
        _ai = ai;
        _partners = partners;
        _actionLog = actionLog;
        _logger = logger;
        _parsers = new List<IFileParser>
        {
            new TextParser(),
            new PdfParser(),
            new OfficeParser(),
            new ImageParser()
        };
    }

    public async Task IndexFileAsync(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists) return;

        // Retry logic for parsing locked files
        string content = "";
        var parser = _parsers.FirstOrDefault(p => p.CanParse(fileInfo.Extension));
        if (parser != null)
        {
            const int MaxRetries = 3;
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    content = await parser.ParseAsync(filePath);
                    break;
                }
                catch (IOException) // File locked
                {
                    if (i == MaxRetries - 1)
                        _logger.LogWarning("Failed to parse content for {Path} after retries (Locked)", filePath);
                    else
                        await Task.Delay(500 * (i + 1));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse content for {Path}", filePath);
                    break;
                }
            }
        }

        using var conn = _db.GetConnection();
        var sql = @"
            INSERT INTO files (path, name, extension, size, last_modified, content)
            VALUES (@Path, @Name, @Extension, @Size, @LastModified, @Content)
            ON CONFLICT(path) DO UPDATE SET
                size = excluded.size,
                last_modified = excluded.last_modified,
                content = excluded.content;
        ";

        await conn.ExecuteAsync(sql, new
        {
            Path = filePath,
            Name = fileInfo.Name,
            Extension = fileInfo.Extension,
            Size = fileInfo.Length,
            LastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds(),
            Content = content
        });

        // Trigger rules
        await _rules.ApplyRulesAsync(filePath);

        // Trigger AI
        await _ai.ProcessFileAsync(filePath, content);

        // Trigger Partners
        await _partners.DetectPartnersAsync(filePath, content);

        // Log
        await _actionLog.LogActionAsync(filePath, "Index", "Indexed file with metadata extraction");

        _logger.LogDebug("Indexed {Path}", filePath);
    }
}
