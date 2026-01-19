using Moq;
using Vantus.Core.Interfaces;
using Vantus.Core.Models;
using Vantus.Core.Services;
using Xunit;

namespace Vantus.Tests;

public class PresetManagerTests
{
    private readonly Mock<ISettingsRegistry> _registryMock;
    private readonly Mock<ISettingsStore> _storeMock;
    private readonly PresetManager _presetManager;

    public PresetManagerTests()
    {
        _registryMock = new Mock<ISettingsRegistry>();
        _storeMock = new Mock<ISettingsStore>();
        _presetManager = new PresetManager(_registryMock.Object, _storeMock.Object);
    }

    [Fact]
    public void GetAvailablePresets_ReturnsDefaults_WhenRegistryEmpty()
    {
        _registryMock.Setup(r => r.GetDefinition("modes.active_preset")).Returns((SettingDefinition?)null);

        var result = _presetManager.GetAvailablePresets();

        Assert.Contains("Personal", result);
        Assert.Contains("Pro", result);
    }

    [Fact]
    public void ApplyPreset_SetsValues_FromRegistryDefaults()
    {
        var def = new SettingDefinition
        {
            SettingId = "test.setting",
            Defaults = new Dictionary<string, object> { { "personal", "value_personal" } }
        };
        _registryMock.Setup(r => r.GetAllDefinitions()).Returns(new[] { def });
        _registryMock.Setup(r => r.GetDefinition("test.setting")).Returns(def);

        _presetManager.ApplyPreset("Personal");

        _storeMock.Verify(s => s.SetValue("test.setting", (object)"value_personal"), Times.Once);
        _storeMock.Verify(s => s.SetValue("modes.active_preset", "Personal"), Times.Once);
    }

    [Fact]
    public void GetDiff_IdentifiesDifferences()
    {
        var def = new SettingDefinition
        {
            SettingId = "test.setting",
            Defaults = new Dictionary<string, object> { { "pro", "value_pro" } }
        };
        _registryMock.Setup(r => r.GetAllDefinitions()).Returns(new[] { def });

        var currentSettings = new Dictionary<string, object> { { "test.setting", "value_personal" } };

        var diff = _presetManager.GetDiff("Pro", currentSettings);

        Assert.True(diff.ContainsKey("test.setting"));
        Assert.Equal("value_personal", diff["test.setting"].OldValue);
        Assert.Equal("value_pro", diff["test.setting"].NewValue);
    }
}
