using Dapper;
using Microsoft.Extensions.Logging;

namespace Vantus.Engine.Services;

public class ActionLogService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ActionLogService> _logger;

    public ActionLogService(DatabaseService db, ILogger<ActionLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogActionAsync(string filePath, string actionType, string description, string status = "Success")
    {
        try
        {
            using var conn = _db.GetConnection();
            await conn.ExecuteAsync(
                "INSERT INTO action_log (file_path, action_type, description, timestamp, status) VALUES (@FilePath, @ActionType, @Description, @Timestamp, @Status)",
                new
                {
                    FilePath = filePath,
                    ActionType = actionType,
                    Description = description,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Status = status
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log action");
        }
    }
}
