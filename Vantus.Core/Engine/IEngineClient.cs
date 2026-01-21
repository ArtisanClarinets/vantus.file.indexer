using Vantus.Core.Models;

namespace Vantus.Core.Engine;

public interface IEngineClient
{
    Task<EngineStatus> GetIndexStatusAsync();
    Task<IEnumerable<FileResult>> SearchAsync(string query);
    Task PauseIndexingAsync();
    Task ResumeIndexingAsync();
    Task RequestRebuildIndexAsync();
}

public class StubEngineClient : IEngineClient
{
    public Task<EngineStatus> GetIndexStatusAsync() => Task.FromResult(new EngineStatus { State = "Idle" });
    public Task<IEnumerable<FileResult>> SearchAsync(string query) => Task.FromResult(Enumerable.Empty<FileResult>());
    public Task PauseIndexingAsync() => Task.CompletedTask;
    public Task ResumeIndexingAsync() => Task.CompletedTask;
    public Task RequestRebuildIndexAsync() => Task.CompletedTask;
}
