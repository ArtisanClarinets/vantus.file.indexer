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
    private int _activeCrawls = 0;

    public bool IsCrawling => _activeCrawls > 0;

    public FileCrawlerService(ILogger<FileCrawlerService> logger, IndexerService indexer, ISettingsStore settingsStore)
    {
        _logger = logger;
        _indexer = indexer;
        _settingsStore = settingsStore;
    }

    public async Task StartCrawlingAsync(CancellationToken cancellationToken)
    {
        await UpdateLocationsAsync(cancellationToken);
    }

    public async Task UpdateLocationsAsync(CancellationToken cancellationToken)
    {
        // Stop existing watchers
        Dispose();

        // Get included folders from settings
        // Force reload settings if needed (SettingsStore handles reload if notified, but here we just read)
        await _settingsStore.LoadAsync();
        var locations = _settingsStore.GetValue<List<string>>("locations.included") ?? new List<string>();

        var resolvedLocations = ResolveLocations(locations);

        foreach (var path in resolvedLocations)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (Directory.Exists(path))
            {
                _logger.LogInformation("Crawling/Watching {Path}", path);

                StartWatcher(path);
                // We crawl in background to not block the caller/updates
                _ = Task.Run(() => CrawlDirectoryAsync(path, cancellationToken), cancellationToken);
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
        Interlocked.Increment(ref _activeCrawls);
        try
        {
            await CrawlDirectoryInternalAsync(path, ct);
        }
        finally
        {
            Interlocked.Decrement(ref _activeCrawls);
        }
    }

    private async Task CrawlDirectoryInternalAsync(string path, CancellationToken ct)
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
                await CrawlDirectoryInternalAsync(dir, ct);
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
