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
                services.AddSingleton<IEngineClient, StubEngineClient>();
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

    private Window m_window;
}
