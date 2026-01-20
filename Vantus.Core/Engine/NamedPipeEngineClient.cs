using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using Vantus.Core.Models;

namespace Vantus.Core.Engine;

public class NamedPipeEngineClient : IEngineClient
{
    private const string PipeName = "VantusEnginePipe";
    private readonly ILogger<NamedPipeEngineClient> _logger;

    public NamedPipeEngineClient(ILogger<NamedPipeEngineClient> logger)
    {
        _logger = logger;
    }

    public async Task<EngineStatus> GetIndexStatusAsync()
    {
        var response = await SendCommandAsync("STATUS");
        if (string.IsNullOrEmpty(response) || response == "Unknown" || response == "Disconnected")
            return new EngineStatus { State = "Disconnected" };

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<EngineStatus>(response) ?? new EngineStatus { State = "Unknown" };
        }
        catch
        {
            return new EngineStatus { State = "Error" };
        }
    }

    public async Task<IEnumerable<FileResult>> SearchAsync(string query)
    {
        var response = await SendCommandAsync($"SEARCH {query}");
        if (string.IsNullOrEmpty(response) || response == "Unknown" || response == "Disconnected")
            return Enumerable.Empty<FileResult>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<FileResult>>(response) ?? Enumerable.Empty<FileResult>();
        }
        catch
        {
            return Enumerable.Empty<FileResult>();
        }
    }

    public Task PauseIndexingAsync() => SendCommandAsync("PAUSE");
    public Task ResumeIndexingAsync() => SendCommandAsync("RESUME");
    public Task RequestRebuildIndexAsync() => SendCommandAsync("REBUILD");

    private async Task<string> SendCommandAsync(string command)
    {
        const int MaxRetries = 3;
        const int BaseDelayMs = 200;

        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);
                await client.ConnectAsync(500); // Short timeout per attempt

                using var writer = new StreamWriter(client) { AutoFlush = true };
                using var reader = new StreamReader(client);

                await writer.WriteLineAsync(command);
                return await reader.ReadLineAsync() ?? "Unknown";
            }
            catch (Exception ex)
            {
                if (i == MaxRetries - 1)
                {
                    _logger.LogWarning(ex, "Failed to connect to engine IPC after {Retries} attempts", MaxRetries);
                    return "Disconnected";
                }
                await Task.Delay(BaseDelayMs * (i + 1));
            }
        }
        return "Disconnected";
    }
}
