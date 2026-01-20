
using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Vantus.Engine.Services;

public class IpcServer
{
    private readonly ILogger<IpcServer> _logger;
    private readonly SearchService _searchService;
    private const string PipeName = "VantusEnginePipe";

    public IpcServer(ILogger<IpcServer> logger, SearchService searchService)
    {
        _logger = logger;
        _searchService = searchService;
    }

    public async Task StartAsync(CancellationToken ct)
    {
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
                    await writer.WriteLineAsync("Indexing");
                }
                else if (line?.StartsWith("SEARCH ") == true)
                {
                    var query = line.Substring(7);
                    var results = await _searchService.SearchAsync(query);
                    // Use simple JSON serialization or custom delimiter that handles paths
                    // JSON is safer.
                    var paths = results.Select(r => r.Path).ToList();
                    var json = System.Text.Json.JsonSerializer.Serialize(paths);
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
