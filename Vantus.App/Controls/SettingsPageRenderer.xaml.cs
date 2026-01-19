using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;
using Vantus.App.ViewModels;
using System.Text.Json;

namespace Vantus.App.Controls;

public sealed partial class SettingsPageRenderer : UserControl
{
    public List<SettingGroup> Groups
    {
        get => (List<SettingGroup>)GetValue(GroupsProperty);
        set => SetValue(GroupsProperty, value);
    }

    public static readonly DependencyProperty GroupsProperty =
        DependencyProperty.Register("Groups", typeof(List<SettingGroup>), typeof(SettingsPageRenderer), new PropertyMetadata(null, OnGroupsChanged));

    private static void OnGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SettingsPageRenderer)d).Render();
    }

    public SettingsPageRenderer()
    {
        this.InitializeComponent();
    }

    private void Render()
    {
        RootPanel.Children.Clear();
        if (Groups == null) return;

        foreach (var group in Groups)
        {
            if (!string.IsNullOrEmpty(group.Header))
            {
                RootPanel.Children.Add(new TextBlock {
                    Text = group.Header,
                    Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
                    Margin = new Thickness(0,0,0,8)
                });
            }

            var groupPanel = new StackPanel { Spacing = 4 };
            foreach(var setting in group.Settings)
            {
                 groupPanel.Children.Add(CreateControl(setting));
            }
            RootPanel.Children.Add(groupPanel);
        }
    }

    private FrameworkElement CreateControl(SettingViewModel vm)
    {
        var card = new CommunityToolkit.WinUI.Controls.SettingsCard();
        card.Header = vm.Definition.Label;
        card.Description = vm.Definition.HelperText;

        if (vm.IsLocked)
        {
            card.HeaderIcon = new SymbolIcon(Symbol.Lock);
            ToolTipService.SetToolTip(card, $"Managed by your organization. {vm.LockReason}");
        }

        FrameworkElement? content = null;

        try {
            switch (vm.Definition.ControlType)
            {
                case "toggle":
                    var ts = new ToggleSwitch();
                    try { ts.IsOn = Convert.ToBoolean(vm.Value); } catch {}
                    ts.Toggled += (s, e) => vm.Value = ts.IsOn;
                    content = ts;
                    break;
                case "slider":
                    var sl = new Slider();
                    sl.Width = 200;
                    if (vm.Definition.AllowedValues is JsonElement je && je.ValueKind == JsonValueKind.Object)
                    {
                        if(je.TryGetProperty("min", out var min)) sl.Minimum = min.GetDouble();
                        if(je.TryGetProperty("max", out var max)) sl.Maximum = max.GetDouble();
                        if(je.TryGetProperty("step", out var step)) sl.StepFrequency = step.GetDouble();
                    }
                    try { sl.Value = Convert.ToDouble(vm.Value); } catch {}
                    sl.ValueChanged += (s, e) => vm.Value = sl.Value;
                    content = sl;
                    break;
                case "dropdown":
                    var cb = new ComboBox();
                    cb.Width = 200;
                    if (vm.Definition.AllowedValues is JsonElement jeArr && jeArr.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<string>();
                        foreach(var item in jeArr.EnumerateArray()) list.Add(item.ToString());
                        cb.ItemsSource = list;
                    }
                    else if (vm.Definition.AllowedValues is string[] arr)
                    {
                        cb.ItemsSource = arr;
                    }

                    try { cb.SelectedItem = vm.Value?.ToString(); } catch {}
                    cb.SelectionChanged += (s, e) => vm.Value = cb.SelectedItem;
                    content = cb;
                    break;
                case "button":
                    var btn = new Button();
                    btn.Content = "Action";
                    content = btn;
                    break;
                case "status":
                    var tb = new TextBlock();
                    tb.Text = vm.Value?.ToString() ?? "";
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    content = tb;
                    break;
            }
        }
        catch { }

        if (content != null)
        {
            if (vm.IsLocked) content.IsEnabled = false;
            card.Content = content;
        }

        return card;
    }
}
