using Dapper;
using Microsoft.Extensions.Logging;
using Vantus.Core.Models;

namespace Vantus.Engine.Services;

public class RulesEngineService
{
    private readonly TagService _tagService;
    private readonly DatabaseService _db;
    private readonly ActionLogService _actionLog;
    private readonly ILogger<RulesEngineService> _logger;
    private List<Rule>? _cachedRules;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RulesEngineService(TagService tagService, DatabaseService db, ActionLogService actionLog, ILogger<RulesEngineService> logger)
    {
        _tagService = tagService;
        _db = db;
        _actionLog = actionLog;
        _logger = logger;
    }

    public async Task<IEnumerable<Rule>> GetRulesAsync()
    {
        if (_cachedRules == null) await LoadRulesAsync();
        return _cachedRules ?? Enumerable.Empty<Rule>();
    }

    public async Task LoadRulesAsync()
    {
        if (_cachedRules != null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_cachedRules != null) return;

            using var conn = _db.GetConnection();
            var rules = await conn.QueryAsync<Rule>("SELECT * FROM rules WHERE is_active = 1");
            _cachedRules = rules.AsList();

            // Seed default rules if empty
            if (!_cachedRules.Any())
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO rules (name, condition_type, condition_value, action_type, action_value)
                    VALUES
                    ('PDF Documents', 'extension', '.pdf', 'tag', 'Document'),
                    ('Images', 'extension', '.jpg', 'tag', 'Image'),
                    ('Images PNG', 'extension', '.png', 'tag', 'Image'),
                    ('Source Code', 'extension', '.cs', 'tag', 'Code')
                ");
                _cachedRules = (await conn.QueryAsync<Rule>("SELECT * FROM rules WHERE is_active = 1")).AsList();
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task ApplyRulesAsync(string filePath)
    {
        if (_cachedRules == null) await LoadRulesAsync();

        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath);

        foreach (var rule in _cachedRules!)
        {
            bool match = false;
            switch (rule.ConditionType.ToLowerInvariant())
            {
                case "extension":
                    match = extension.Equals(rule.ConditionValue, StringComparison.OrdinalIgnoreCase);
                    break;
                case "name":
                    match = fileName.Contains(rule.ConditionValue, StringComparison.OrdinalIgnoreCase);
                    break;
                case "tag":
                    using (var conn = _db.GetConnection())
                    {
                        var hasTag = await conn.ExecuteScalarAsync<int>(@"
                            SELECT 1
                            FROM file_tags ft
                            JOIN tags t ON ft.tag_id = t.id
                            JOIN files f ON ft.file_id = f.id
                            WHERE f.path = @Path AND t.name = @Tag",
                            new { Path = filePath, Tag = rule.ConditionValue });
                        match = hasTag == 1;
                    }
                    break;
            }

            if (match)
            {
                try
                {
                    switch (rule.ActionType.ToLowerInvariant())
                    {
                        case "tag":
                            await _tagService.TagFileAsync(filePath, rule.ActionValue, 1.0);
                            await _actionLog.LogActionAsync(filePath, "Tag", $"Applied tag '{rule.ActionValue}' via rule '{rule.Name}'");
                            _logger.LogInformation("Applied rule '{Rule}' (Tag) to {Path}", rule.Name, filePath);
                            break;
                        case "move":
                            var destMove = Path.Combine(rule.ActionValue, fileName);
                            if (!Directory.Exists(rule.ActionValue)) Directory.CreateDirectory(rule.ActionValue);
                            File.Move(filePath, destMove);
                            await _actionLog.LogActionAsync(filePath, "Move", $"Moved to {destMove} via rule '{rule.Name}'");
                            _logger.LogInformation("Applied rule '{Rule}' (Move) to {Path} -> {Dest}", rule.Name, filePath, destMove);
                            break;
                        case "copy":
                            var destCopy = Path.Combine(rule.ActionValue, fileName);
                            if (!Directory.Exists(rule.ActionValue)) Directory.CreateDirectory(rule.ActionValue);
                            File.Copy(filePath, destCopy, true);
                            await _actionLog.LogActionAsync(filePath, "Copy", $"Copied to {destCopy} via rule '{rule.Name}'");
                            _logger.LogInformation("Applied rule '{Rule}' (Copy) to {Path} -> {Dest}", rule.Name, filePath, destCopy);
                            break;
                        case "rename":
                             var newName = rule.ActionValue + extension;
                             var dir = Path.GetDirectoryName(filePath) ?? string.Empty;
                             var destRename = Path.Combine(dir, newName);
                             // Fix collision by appending timestamp
                             if (File.Exists(destRename))
                             {
                                 destRename = Path.Combine(dir, $"{rule.ActionValue}_{DateTime.UtcNow.Ticks}{extension}");
                             }
                             File.Move(filePath, destRename);
                             await _actionLog.LogActionAsync(filePath, "Rename", $"Renamed to {Path.GetFileName(destRename)} via rule '{rule.Name}'");
                             _logger.LogInformation("Applied rule '{Rule}' (Rename) to {Path} -> {Dest}", rule.Name, filePath, destRename);
                             break;
                        case "quarantine":
                             var quarantineDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vantus", "Quarantine");
                             if (!Directory.Exists(quarantineDir)) Directory.CreateDirectory(quarantineDir);
                             var destQuarantine = Path.Combine(quarantineDir, fileName);
                             File.Move(filePath, destQuarantine);
                             await _actionLog.LogActionAsync(filePath, "Quarantine", $"Quarantined to {destQuarantine} via rule '{rule.Name}'");
                             _logger.LogInformation("Applied rule '{Rule}' (Quarantine) to {Path} -> {Dest}", rule.Name, filePath, destQuarantine);
                             break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply rule '{Rule}' to {Path}", rule.Name, filePath);
                }
            }
        }
    }
}
