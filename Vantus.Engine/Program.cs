using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vantus.Core.Interfaces;
using Vantus.Core.Services;
using Vantus.Engine;
using Vantus.Engine.Services;

var builder = Host.CreateApplicationBuilder(args);

// Core Services
builder.Services.AddSingleton<ISettingsStore, SettingsStore>();
// We don't need Registry in Engine necessarily unless we validate settings, but good to have
builder.Services.AddSingleton<ISettingsRegistry, SettingsRegistry>();

// Engine Services
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<IndexerService>();
builder.Services.AddSingleton<TagService>();
builder.Services.AddSingleton<PartnerService>();
builder.Services.AddSingleton<AiService>();
builder.Services.AddSingleton<RulesEngineService>();
builder.Services.AddSingleton<ActionLogService>();
builder.Services.AddSingleton<SearchService>();
builder.Services.AddSingleton<FileCrawlerService>();
builder.Services.AddSingleton<IpcServer>();
builder.Services.AddHostedService<EngineWorker>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var host = builder.Build();

// Ensure Settings are loaded
var store = host.Services.GetRequiredService<ISettingsStore>();
await store.LoadAsync();

await host.RunAsync();
