using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Vantus.Core.Interfaces;
using Vantus.Core.Services;
using Vantus.Core.Engine;
using Vantus.App.ViewModels;

namespace Vantus.App;

public partial class App : Application
{
    public IHost Host { get; }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }
        return service;
    }

    public App()
    {
        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                // Core Services
                services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
                services.AddSingleton<ISettingsStore, SettingsStore>();
                services.AddSingleton<IPresetManager, PresetManager>();
                services.AddSingleton<IPolicyEngine, PolicyEngine>();
                services.AddSingleton<IEngineClient, NamedPipeEngineClient>();
                services.AddSingleton<IImportExportService, ImportExportService>();

                // ViewModels
                services.AddTransient<ShellViewModel>();
                services.AddTransient<SettingsPageViewModel>();
                services.AddTransient<ModesPageViewModel>();
                services.AddTransient<ImportExportPageViewModel>();
            }).
            Build();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        await Host.StartAsync();
        StartEngine();

        try
        {
            // Init Registry & Policy & Store
            var registry = GetService<ISettingsRegistry>();
            await registry.InitializeAsync();

            var policy = GetService<IPolicyEngine>();
            await policy.InitializeAsync();

            var store = GetService<ISettingsStore>();
            await store.LoadAsync();
        }
        catch (Exception ex)
        {
             // Log error or show message?
             // Proceed to activate window anyway to allow app to start (maybe in error state or defaults)
             System.Diagnostics.Debug.WriteLine($"Initialization failed: {ex}");
        }

        m_window = new MainWindow();
        m_window.Activate();
    }

    private void StartEngine()
    {
        // Try to find the engine executable relative to the app
        // In dev: ../../../Vantus.Engine/bin/Debug/net8.0/Vantus.Engine.exe
        // In prod: ./Vantus.Engine.exe
        var engineName = "Vantus.Engine.exe";
        var devPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Vantus.Engine", "bin", "Debug", "net8.0", engineName);
        var prodPath = Path.Combine(AppContext.BaseDirectory, engineName);

        string? path = null;
        if (File.Exists(prodPath)) path = prodPath;
        else if (File.Exists(devPath)) path = devPath;

        if (path != null)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start engine: {ex}");
            }
        }
    }

    private Window m_window;
}
