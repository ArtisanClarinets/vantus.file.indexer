using Dapper;
using Microsoft.Extensions.Logging;
using Vantus.Engine.Models;

namespace Vantus.Engine.Services;

public class PartnerService
{
    private readonly DatabaseService _db;
    private readonly ILogger<PartnerService> _logger;
    private List<Partner>? _cachedPartners;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public PartnerService(DatabaseService db, ILogger<PartnerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LoadPartnersAsync()
    {
        if (_cachedPartners != null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_cachedPartners != null) return;

            using var conn = _db.GetConnection();
            var partners = await conn.QueryAsync<Partner>("SELECT * FROM partners");
            _cachedPartners = partners.AsList();

            if (!_cachedPartners.Any())
            {
                await conn.ExecuteAsync(@"
                    INSERT OR IGNORE INTO partners (name, domains, keywords) VALUES
                    ('Acme Corp', 'acme.com', 'Acme'),
                    ('Contoso', 'contoso.com', 'Contoso'),
                    ('Fabrikam', 'fabrikam.com', 'Fabrikam')
                ");
                _cachedPartners = (await conn.QueryAsync<Partner>("SELECT * FROM partners")).AsList();
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task DetectPartnersAsync(string filePath, string content)
    {
        if (_cachedPartners == null) await LoadPartnersAsync();

        var fileName = Path.GetFileName(filePath);
        var lowerContent = content.ToLowerInvariant();
        var lowerName = fileName.ToLowerInvariant();

        foreach (var partner in _cachedPartners!)
        {
            bool match = false;

            // Check keywords
            if (!string.IsNullOrEmpty(partner.Keywords))
            {
                foreach (var kw in partner.Keywords.Split(','))
                {
                    var term = kw.Trim().ToLowerInvariant();
                    if (!string.IsNullOrEmpty(term) && (lowerContent.Contains(term) || lowerName.Contains(term)))
                    {
                        match = true;
                        break;
                    }
                }
            }

            if (match)
            {
                await AssociatePartnerAsync(filePath, partner.Id);
                _logger.LogInformation("Detected Partner '{Partner}' in {Path}", partner.Name, filePath);
            }
        }
    }

    private async Task AssociatePartnerAsync(string filePath, int partnerId)
    {
        using var conn = _db.GetConnection();
        var fileId = await conn.ExecuteScalarAsync<long?>("SELECT id FROM files WHERE path = @Path", new { Path = filePath });
        if (fileId.HasValue)
        {
            await conn.ExecuteAsync(
                "INSERT OR IGNORE INTO file_partners (file_id, partner_id, confidence) VALUES (@FileId, @PartnerId, 1.0)",
                new { FileId = fileId.Value, PartnerId = partnerId });
        }
    }
}
