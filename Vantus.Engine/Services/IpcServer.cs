
using System.IO.Pipes;
using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using Vantus.Core.Models;

namespace Vantus.Engine.Services;

public class IpcServer
{
    private readonly ILogger<IpcServer> _logger;
    private readonly SearchService _searchService;
    private readonly DatabaseService _db;
    private readonly FileCrawlerService _crawler;
    private const string PipeName = "VantusEnginePipe";

    public IpcServer(ILogger<IpcServer> logger, SearchService searchService, DatabaseService db, FileCrawlerService crawler)
    {
        _logger = logger;
        _searchService = searchService;
        _db = db;
        _crawler = crawler;
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
                    long count = 0;
                    try
                    {
                        using var conn = _db.GetConnection();
                        count = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM files");
                    }
                    catch { }

                    var status = new EngineStatus
                    {
                        State = _crawler.IsCrawling ? "Indexing" : "Idle",
                        IsCrawling = _crawler.IsCrawling,
                        IndexedCount = count
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(status);
                    await writer.WriteLineAsync(json);
                }
                else if (line?.StartsWith("SEARCH ") == true)
                {
                    var query = line.Substring(7);
                    var results = await _searchService.SearchAsync(query);
                    // Use simple JSON serialization or custom delimiter that handles paths
                    // JSON is safer.
                    var json = System.Text.Json.JsonSerializer.Serialize(results);
                    await writer.WriteLineAsync(json);
                }
                else if (line == "REBUILD")
                {
                    await _db.RebuildAsync();
                    await _crawler.UpdateLocationsAsync(ct);
                    await writer.WriteLineAsync("OK");
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
