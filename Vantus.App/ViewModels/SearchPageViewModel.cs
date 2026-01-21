using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vantus.Core.Engine;
using Vantus.Core.Models;

namespace Vantus.App.ViewModels;

public partial class SearchPageViewModel : ObservableObject
{
    private readonly IEngineClient _engine;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<FileResult> Results { get; } = new();

    public SearchPageViewModel(IEngineClient engine)
    {
        _engine = engine;
    }

    [RelayCommand]
    public async Task SearchAsync(string? query = null)
    {
        if (query != null) Query = query;
        if (string.IsNullOrWhiteSpace(Query)) return;

        IsLoading = true;
        Results.Clear();

        try
        {
            var results = await _engine.SearchAsync(Query);
            foreach (var r in results)
            {
                Results.Add(r);
            }
        }
        catch
        {
            // Handle error
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public bool OpenFile(FileResult file)
    {
        if (file == null || string.IsNullOrEmpty(file.Path)) return false;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = file.Path,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
