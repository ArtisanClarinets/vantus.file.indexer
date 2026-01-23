using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using Vantus.Core.Interfaces;

namespace Vantus.App.ViewModels;

public partial class PartnersPageViewModel : ObservableObject
{
    private readonly ISettingsStore _store;
    private const string PartnersKey = "partners.custom_list";

    [ObservableProperty]
    private ObservableCollection<PartnerItem> _partners = new();

    [ObservableProperty]
    private string _newName = "";

    [ObservableProperty]
    private string _newDomains = "";

    public PartnersPageViewModel(ISettingsStore store)
    {
        _store = store;
        LoadPartners();
    }

    private void LoadPartners()
    {
        var json = _store.GetValue<string>(PartnersKey);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<PartnerItem>>(json);
                if (list != null)
                {
                    Partners = new ObservableCollection<PartnerItem>(list);
                }
            }
            catch { }
        }
    }

    private void SavePartners()
    {
        var json = JsonSerializer.Serialize(Partners.ToList());
        _store.SetValue(PartnersKey, json);
        _store.SaveAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    public void AddPartner()
    {
        if (!string.IsNullOrWhiteSpace(NewName))
        {
            Partners.Add(new PartnerItem { Name = NewName, Domains = NewDomains });
            NewName = "";
            NewDomains = "";
            SavePartners();
        }
    }

    [RelayCommand]
    public void RemovePartner(PartnerItem item)
    {
        if (Partners.Contains(item))
        {
            Partners.Remove(item);
            SavePartners();
        }
    }
}

public class PartnerItem
{
    public string Name { get; set; } = "";
    public string Domains { get; set; } = "";
    public string Keywords { get; set; } = "";
}
