using Microsoft.Extensions.Logging;
using Vantus.Core.Interfaces;
using Vantus.Core.Services;

namespace Vantus.Engine.Services;

public class FileCrawlerService : IDisposable
{
    private readonly ILogger<FileCrawlerService> _logger;
    private readonly IndexerService _indexer;
    private readonly ISettingsStore _settingsStore;
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> _debounceTokens = new();

    public FileCrawlerService(ILogger<FileCrawlerService> logger, IndexerService indexer, ISettingsStore settingsStore)
    {
        _logger = logger;
        _indexer = indexer;
        _settingsStore = settingsStore;
    }

    public async Task StartCrawlingAsync(CancellationToken cancellationToken)
    {
        // Get included folders from settings
        var locations = _settingsStore.GetValue<List<string>>("locations.included") ?? new List<string>();
        // Resolve special folders if needed (e.g. "Documents" -> path)
        var resolvedLocations = ResolveLocations(locations);

        foreach (var path in resolvedLocations)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (Directory.Exists(path))
            {
                _logger.LogInformation("Crawling {Path}", path);

                // Start watcher
                StartWatcher(path);

                await CrawlDirectoryAsync(path, cancellationToken);
            }
        }
    }

    private void StartWatcher(string path)
    {
        try
        {
            var watcher = new FileSystemWatcher(path);
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName;

            watcher.Created += OnFileChanged;
            watcher.Changed += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.Deleted += OnFileDeleted;

            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
            _logger.LogInformation("Started watching {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start watcher for {Path}", path);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        DebounceIndex(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Delete old, index new
        // For now just index new. Ideally we remove the old entry from DB.
        DebounceIndex(e.FullPath);
    }

    private void DebounceIndex(string filePath)
    {
        var cts = new CancellationTokenSource();
        _debounceTokens.AddOrUpdate(filePath, cts, (key, oldCts) =>
        {
            oldCts.Cancel();
            oldCts.Dispose();
            return cts;
        });

        _ = Task.Delay(500, cts.Token).ContinueWith(async t =>
        {
            if (t.IsCanceled) return;
            _debounceTokens.TryRemove(filePath, out _);
            await _indexer.IndexFileAsync(filePath);
        }, TaskScheduler.Default);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        // TODO: Call indexer delete
    }

    public void Dispose()
    {
        foreach(var w in _watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
        _watchers.Clear();
    }

    private List<string> ResolveLocations(List<string> locations)
    {
        var result = new List<string>();
        foreach (var loc in locations)
        {
            if (loc.Equals("Documents", StringComparison.OrdinalIgnoreCase))
                result.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            else if (loc.Equals("Pictures", StringComparison.OrdinalIgnoreCase))
                result.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            else if (loc.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
                result.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            else if (Directory.Exists(loc)) // Absolute path
                result.Add(loc);
        }
        return result;
    }

    private async Task CrawlDirectoryAsync(string path, CancellationToken ct)
    {
        // Files
        try
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                if (ct.IsCancellationRequested) return;
                try
                {
                    await _indexer.IndexFileAsync(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error indexing file {File}", file);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Access denied to files in {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files in {Path}", path);
        }

        // Subdirectories
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                if (ct.IsCancellationRequested) return;
                // Recurse
                await CrawlDirectoryAsync(dir, ct);
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Access denied to directories in {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directories in {Path}", path);
        }
    }
}
