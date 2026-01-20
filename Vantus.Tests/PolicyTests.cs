using System.Text.Json;
using Vantus.Core.Services;
using Vantus.Core.Models;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;

namespace Vantus.Tests;

public class PolicyTests : IDisposable
{
    private const string PolicyFileName = "policies.json";

    public PolicyTests()
    {
        if (File.Exists(PolicyFileName)) File.Delete(PolicyFileName);
    }

    public void Dispose()
    {
        if (File.Exists(PolicyFileName)) File.Delete(PolicyFileName);
    }

    [Fact]
    public async Task InitializeAsync_LoadsLocks_WhenFileExists()
    {
        var policy = new PolicyFile
        {
            Managed = true,
            Locks = new List<PolicyLock>
            {
                new PolicyLock { SettingId = "locked.setting", LockedValue = true, Reason = "Test" }
            }
        };
        await File.WriteAllTextAsync(PolicyFileName, JsonSerializer.Serialize(policy));

        var loggerMock = new Mock<ILogger<PolicyEngine>>();
        var engine = new PolicyEngine(loggerMock.Object);
        await engine.InitializeAsync();

        Assert.True(engine.IsLocked("locked.setting"));
        var lockInfo = engine.GetLock("locked.setting");
        Assert.NotNull(lockInfo);
        Assert.Equal("Test", lockInfo!.Reason);
        Assert.True(engine.IsManaged);
    }
}
