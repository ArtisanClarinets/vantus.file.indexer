namespace Vantus.Core.Engine;

public interface IEngineClient
{
    Task<string> GetIndexStatusAsync();
    Task PauseIndexingAsync();
    Task ResumeIndexingAsync();
    Task RequestRebuildIndexAsync();
}

public class StubEngineClient : IEngineClient
{
    public Task<string> GetIndexStatusAsync() => Task.FromResult("Idle");
    public Task PauseIndexingAsync() => Task.CompletedTask;
    public Task ResumeIndexingAsync() => Task.CompletedTask;
    public Task RequestRebuildIndexAsync() => Task.CompletedTask;
}
