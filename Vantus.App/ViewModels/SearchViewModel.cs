using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Vantus.Core.Engine;
using Vantus.Core.Models;

namespace Vantus.App.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly IEngineClient _engine;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<FileResult> Results { get; } = new();

    public SearchViewModel(IEngineClient engine)
    {
        _engine = engine;
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsLoading = true;
        Results.Clear();
        try
        {
            var results = await _engine.SearchAsync(SearchQuery);
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var r in results) Results.Add(r);
            });
        }
        finally
        {
            IsLoading = false;
        }
    }
}
