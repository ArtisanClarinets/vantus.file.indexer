
using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Logging;
using Dapper;

namespace Vantus.Engine.Services;

public class IpcServer
{
    private readonly ILogger<IpcServer> _logger;
    private readonly SearchService _searchService;
    private readonly DatabaseService _db;
    private readonly FileCrawlerService _crawler;
    private readonly ActionLogService _actionLog;
    private readonly RulesEngineService _rulesEngine;
    private const string PipeName = "VantusEnginePipe";

    public IpcServer(ILogger<IpcServer> logger, SearchService searchService, DatabaseService db, FileCrawlerService crawler, ActionLogService actionLog, RulesEngineService rulesEngine)
    {
        _logger = logger;
        _searchService = searchService;
        _db = db;
        _crawler = crawler;
        _actionLog = actionLog;
        _rulesEngine = rulesEngine;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        // Monitor for orphaned process state (if parent dies without killing us)
        _ = Task.Run(async () =>
        {
            while(!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);
                // If we haven't received a connection in X minutes, or some other heartbeat, we could exit.
                // For now, relies on explicit kill or pipe broken signals if we were doing persistent connections.
            }
        }, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct);
                _ = HandleConnectionAsync(server, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "IPC Server Error");
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken ct)
    {
        try
        {
            using (server)
            using (var reader = new StreamReader(server))
            using (var writer = new StreamWriter(server) { AutoFlush = true })
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == "STATUS")
                {
                    using var conn = _db.GetConnection();
                    var count = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM files");
                    var status = new Vantus.Core.Models.EngineStatus
                    {
                         State = _crawler.IsCrawling ? "Crawling" : "Idle",
                         IndexedCount = count,
                         IsCrawling = _crawler.IsCrawling
                    };
                    await writer.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(status));
                }
                else if (line?.StartsWith("SEARCH ") == true)
                {
                    var query = line.Substring(7);
                    var results = await _searchService.SearchAsync(query);
                    var json = System.Text.Json.JsonSerializer.Serialize(results);
                    await writer.WriteLineAsync(json);
                }
                else if (line == "REBUILD")
                {
                    await _db.RebuildAsync();
                    await _crawler.UpdateLocationsAsync(ct);
                    await writer.WriteLineAsync("OK");
                }
                else if (line == "UNDO")
                {
                    await _actionLog.UndoLastActionAsync();
                    await writer.WriteLineAsync("OK");
                }
                else if (line == "GET_RULES")
                {
                    var rules = await _rulesEngine.GetRulesAsync();
                    var json = System.Text.Json.JsonSerializer.Serialize(rules);
                    await writer.WriteLineAsync(json);
                }
                else
                {
                    await writer.WriteLineAsync("OK");
                }
            }
        }
        catch { }
    }
}
