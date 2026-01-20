using Dapper;
using Microsoft.Extensions.Logging;
using Vantus.Engine.Models;

namespace Vantus.Engine.Services;

public class RulesEngineService
{
    private readonly TagService _tagService;
    private readonly DatabaseService _db;
    private readonly ILogger<RulesEngineService> _logger;
    private List<Rule>? _cachedRules;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RulesEngineService(TagService tagService, DatabaseService db, ILogger<RulesEngineService> logger)
    {
        _tagService = tagService;
        _db = db;
        _logger = logger;
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
            }

            if (match)
            {
                try
                {
                    switch (rule.ActionType.ToLowerInvariant())
                    {
                        case "tag":
                            await _tagService.TagFileAsync(filePath, rule.ActionValue, 1.0);
                            _logger.LogInformation("Applied rule '{Rule}' (Tag) to {Path}", rule.Name, filePath);
                            break;
                        case "move":
                            var destMove = Path.Combine(rule.ActionValue, fileName);
                            if (!Directory.Exists(rule.ActionValue)) Directory.CreateDirectory(rule.ActionValue);
                            File.Move(filePath, destMove);
                            _logger.LogInformation("Applied rule '{Rule}' (Move) to {Path} -> {Dest}", rule.Name, filePath, destMove);
                            break;
                        case "copy":
                            var destCopy = Path.Combine(rule.ActionValue, fileName);
                            if (!Directory.Exists(rule.ActionValue)) Directory.CreateDirectory(rule.ActionValue);
                            File.Copy(filePath, destCopy, true);
                            _logger.LogInformation("Applied rule '{Rule}' (Copy) to {Path} -> {Dest}", rule.Name, filePath, destCopy);
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
