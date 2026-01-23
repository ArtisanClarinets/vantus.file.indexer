namespace Vantus.Core.Engine;

using Vantus.Core.Models;

public interface IEngineClient
{
    Task<EngineStatus> GetIndexStatusAsync();
    Task<IEnumerable<FileResult>> SearchAsync(string query);
    Task PauseIndexingAsync();
    Task ResumeIndexingAsync();
    Task RequestRebuildIndexAsync();
    Task UndoLastActionAsync();
    Task<IEnumerable<Rule>> GetRulesAsync();
}

public class StubEngineClient : IEngineClient
{
    public Task<EngineStatus> GetIndexStatusAsync() => Task.FromResult(new EngineStatus { State = "Idle", IndexedCount = 1234 });
    public Task<IEnumerable<FileResult>> SearchAsync(string query) => Task.FromResult(Enumerable.Empty<FileResult>());
    public Task PauseIndexingAsync() => Task.CompletedTask;
    public Task ResumeIndexingAsync() => Task.CompletedTask;
    public Task RequestRebuildIndexAsync() => Task.CompletedTask;
    public Task UndoLastActionAsync() => Task.CompletedTask;
    public Task<IEnumerable<Rule>> GetRulesAsync() => Task.FromResult(Enumerable.Empty<Rule>());
}
