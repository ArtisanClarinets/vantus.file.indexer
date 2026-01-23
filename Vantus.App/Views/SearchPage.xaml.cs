using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using Vantus.App.ViewModels;
using Vantus.Core.Models;

namespace Vantus.App.Views;

public partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<SearchViewModel>();
        DataContext = ViewModel;
    }

    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is FileResult file)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file.Path,
                    UseShellExecute = true
                };
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Vantus.App.ViewModels;
using Vantus.Core.Models;

namespace Vantus.App.Views;

public partial class SearchPage : Page
{
    // Define allowed directories and file extensions
    private static readonly string[] AllowedExtensions = { ".txt", ".pdf" };
    private static readonly string[] AllowedDirectories = { "C:\\Users\\Public\\Documents", "C:\\SomeOtherSafePath" };

    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<SearchViewModel>();
        DataContext = ViewModel;
    }

    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is FileResult file)
        {
            try
            {
                string filePath = file.Path;

                // Validate extension
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                    return;

                // Validate path is rooted and in allowed directory
                string fullPath = Path.GetFullPath(filePath);
                if (!AllowedDirectories.Any(allowedDir => fullPath.StartsWith(allowedDir, System.StringComparison.OrdinalIgnoreCase)))
                    return;

                // Additional sanitization: Check for dangerous characters
                if (filePath.Contains(";") || filePath.Contains("&") || filePath.Contains("|"))
                    return;

                // Hardcoded executable for opening files based on extension
                string viewerExe;
                string arguments;
                if (ext == ".txt")
                {
                    viewerExe = @"C:\Windows\System32\notepad.exe";
                    arguments = $"\"{fullPath}\"";
                }
                else if (ext == ".pdf")
                {
                    // Using default app via shell, validate only the sanitized path is used
                    viewerExe = fullPath;
                    arguments = "";
                }
                else
                {
                    return; // Extension not supported
                }

                var psi = new ProcessStartInfo
                {
                    FileName = viewerExe,
                    Arguments = arguments,
                    UseShellExecute = ext != ".txt", // UseShellExecute is true for PDFs, false for notepad
                    Verb = ext == ".pdf" ? "open" : null // Use "open" verb for PDFs
                };
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Handle file not found, access denied, etc. (log or show message as needed)
            }
            catch (System.Exception ex)
            {
                // Handle other specific exceptions if necessary (logging, user notification, etc.)
            }
        }
    }
}
            }
            catch { }
        }
    }
}
