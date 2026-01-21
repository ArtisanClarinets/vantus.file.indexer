using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace Vantus.App.Services;

public class ShellIntegrationService
{
    private const string MenuName = "Index with Vantus";
    private const string CommandName = "Vantus.Index";

    public void RegisterContextMenu(bool enable)
    {
        // This only works on Windows
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            return;

        try
        {
            // HKCU\Software\Classes\*\shell\Vantus
            using var classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell", true);
            if (classesKey == null) return;

            if (enable)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;

                // Create Key
                using var key = classesKey.CreateSubKey("Vantus");
                if (key != null)
                {
                    key.SetValue("", MenuName);
                    key.SetValue("Icon", exePath);

                    using var commandKey = key.CreateSubKey("command");
                    if (commandKey != null)
                    {
                        commandKey.SetValue("", $"\"{exePath}\" --index \"%1\"");
                    }
                }
            }
            else
            {
                classesKey.DeleteSubKeyTree("Vantus", false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error registering context menu: {ex}");
        }
    }
}
