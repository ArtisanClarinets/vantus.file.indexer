
using Vantus.Engine.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.Data.Sqlite;
using Dapper;

namespace Vantus.Tests;

public class EngineTests : IDisposable
{
    private readonly Mock<ILogger<DatabaseService>> _dbLoggerMock;
    private readonly Mock<ILogger<IndexerService>> _indexerLoggerMock;
    private readonly Mock<ILogger<TagService>> _tagLoggerMock;
    private readonly Mock<ILogger<RulesEngineService>> _rulesLoggerMock;
    private readonly Mock<ILogger<AiService>> _aiLoggerMock;
    private readonly Mock<ILogger<PartnerService>> _partnerLoggerMock;
    private readonly Mock<ILogger<ActionLogService>> _actionLogLoggerMock;
    private readonly string _testDbPath;

    public EngineTests()
    {
        _dbLoggerMock = new Mock<ILogger<DatabaseService>>();
        _indexerLoggerMock = new Mock<ILogger<IndexerService>>();
        _tagLoggerMock = new Mock<ILogger<TagService>>();
        _rulesLoggerMock = new Mock<ILogger<RulesEngineService>>();
        _aiLoggerMock = new Mock<ILogger<AiService>>();
        _partnerLoggerMock = new Mock<ILogger<PartnerService>>();
        _actionLogLoggerMock = new Mock<ILogger<ActionLogService>>();
        _testDbPath = Path.Combine(Path.GetTempPath(), $"vantus_test_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
         if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
    }

    [Fact]
    public async Task DatabaseService_Initialize_CreatesTables()
    {
        var db = new DatabaseService(_dbLoggerMock.Object, _testDbPath);
        await db.InitializeAsync();

        using var conn = db.GetConnection();
        var tableCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='files'");
        Assert.Equal(1, tableCount);

        var tagCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='tags'");
        Assert.Equal(1, tagCount);
    }

    [Fact]
    public async Task IndexerService_IndexFile_FullPipelineTest()
    {
        var db = new DatabaseService(_dbLoggerMock.Object, _testDbPath);
        await db.InitializeAsync();

        var tagService = new TagService(db, _tagLoggerMock.Object);
        var rulesService = new RulesEngineService(tagService, db, _rulesLoggerMock.Object);
        var aiService = new AiService(tagService, _aiLoggerMock.Object);
        var partnerService = new PartnerService(db, _partnerLoggerMock.Object);
        var actionLog = new ActionLogService(db, _actionLogLoggerMock.Object);

        var indexer = new IndexerService(db, rulesService, aiService, partnerService, actionLog, _indexerLoggerMock.Object);

        // Ensure we have a rule for .txt files
        using (var conn = db.GetConnection())
        {
            await conn.ExecuteAsync("INSERT INTO rules (name, condition_type, condition_value, action_type, action_value) VALUES ('Text Files', 'extension', '.txt', 'tag', 'Document')");
        }

        // Create dummy text file with content matching AI and Partner
        var dummyFile = Path.Combine(Path.GetTempPath(), "invoice_acme.txt");
        await File.WriteAllTextAsync(dummyFile, "This is an Invoice from Acme Corp. Total due: $500.");

        // Create dummy docx (empty, just to verify parser selection doesn't crash)
        var dummyDocx = Path.Combine(Path.GetTempPath(), "test.docx");
        // We can't easily create a valid docx without the library, so we just create a file to test the extension logic.
        // The parser will fail gracefully (which we implemented).
        await File.WriteAllBytesAsync(dummyDocx, new byte[100]);

        try
        {
            await indexer.IndexFileAsync(dummyFile);
            await indexer.IndexFileAsync(dummyDocx);

            using var conn = db.GetConnection();
            var row = await conn.QuerySingleAsync("SELECT * FROM files WHERE path = @Path", new { Path = dummyFile });

            // 1. Verify Rules (.txt -> Document)
            var ruleTag = await conn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM file_tags ft JOIN tags t ON ft.tag_id = t.id WHERE t.name = 'Document' AND ft.file_id = @FileId",
                new { FileId = row.id });
            Assert.Equal(1, ruleTag);

            // 2. Verify AI (Invoice -> Finance)
            var aiTag = await conn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM file_tags ft JOIN tags t ON ft.tag_id = t.id WHERE t.name = 'Finance' AND ft.file_id = @FileId",
                new { FileId = row.id });
            Assert.Equal(1, aiTag);

            // 3. Verify Partner (Acme Corp)
            var partner = await conn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM file_partners fp JOIN partners p ON fp.partner_id = p.id WHERE p.name = 'Acme Corp' AND fp.file_id = @FileId",
                new { FileId = row.id });
            Assert.Equal(1, partner);

            // 4. Verify Log (Should be 2 because we indexed 2 files)
            var logCount = await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM action_log");
            Assert.Equal(2, logCount);
        }
        finally
        {
            if (File.Exists(dummyFile)) File.Delete(dummyFile);
            if (File.Exists(dummyDocx)) File.Delete(dummyDocx);
        }
    }

    [Fact]
    public async Task RulesEngineService_ApplyRulesAsync_ExecutesMoveAction()
    {
        var db = new DatabaseService(_dbLoggerMock.Object, _testDbPath);
        await db.InitializeAsync();

        // Seed Move Rule
        using (var conn = db.GetConnection())
        {
            await conn.ExecuteAsync(@"
                INSERT INTO rules (name, condition_type, condition_value, action_type, action_value)
                VALUES ('Move Test', 'extension', '.moveme', 'move', @Dest)", new { Dest = Path.GetTempPath() });
        }

        var tagService = new TagService(db, _tagLoggerMock.Object);
        var rulesService = new RulesEngineService(tagService, db, _rulesLoggerMock.Object);

        var sourceFile = Path.Combine(Path.GetTempPath(), "source.moveme");
        // Destination is same folder (temp), so it's a no-op move if filename same?
        // Let's make a subfolder
        var destFolder = Path.Combine(Path.GetTempPath(), "MoveDest");
        Directory.CreateDirectory(destFolder);

        // Update rule to use subfolder
        using (var conn = db.GetConnection())
        {
            await conn.ExecuteAsync("UPDATE rules SET action_value = @Dest WHERE name = 'Move Test'", new { Dest = destFolder });
        }

        await File.WriteAllTextAsync(sourceFile, "Move content");

        try
        {
            await rulesService.LoadRulesAsync(); // Refresh cache
            await rulesService.ApplyRulesAsync(sourceFile);

            var destFile = Path.Combine(destFolder, "source.moveme");
            Assert.True(File.Exists(destFile));
            Assert.False(File.Exists(sourceFile));
        }
        finally
        {
            if (File.Exists(sourceFile)) File.Delete(sourceFile);
            if (Directory.Exists(destFolder)) Directory.Delete(destFolder, true);
        }
    }
}
