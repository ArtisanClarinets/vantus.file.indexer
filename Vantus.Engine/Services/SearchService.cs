using Dapper;
using Microsoft.Extensions.Logging;
using Vantus.Engine.Models;

namespace Vantus.Engine.Services;

public class SearchService
{
    private readonly DatabaseService _db;
    private readonly ILogger<SearchService> _logger;

    public SearchService(DatabaseService db, ILogger<SearchService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<FileResult>> SearchAsync(string query)
    {
        using var conn = _db.GetConnection();
        // FTS5 query
        // "SELECT * FROM files WHERE id IN (SELECT rowid FROM files_fts WHERE files_fts MATCH @Query)"

        var sql = @"
            SELECT f.*
            FROM files f
            JOIN files_fts fts ON f.id = fts.rowid
            WHERE fts MATCH @Query
            ORDER BY rank
            LIMIT 50;
        ";

        try
        {
            return await conn.QueryAsync<FileResult>(sql, new { Query = query });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query {Query}", query);
            return Enumerable.Empty<FileResult>();
        }
    }
}

public class FileResult
{
    public long Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
}
