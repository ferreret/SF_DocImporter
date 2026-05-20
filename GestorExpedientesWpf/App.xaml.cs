using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using LibUtil;
using Velopack;

namespace GestorExpedientesWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _ = CheckForUpdatesAsync();
        }

        private static async Task CheckForUpdatesAsync()
        {
            try
            {
                string updateUrl = ReadUpdateUrlFromIni();
                if (string.IsNullOrWhiteSpace(updateUrl))
                {
                    return;
                }

                var mgr = new UpdateManager(updateUrl);

                // En modo desarrollo (sin instalar) IsInstalled es false: no aplicamos updates.
                if (!mgr.IsInstalled)
                {
                    return;
                }

                var nuevaVersion = await mgr.CheckForUpdatesAsync();
                if (nuevaVersion == null)
                {
                    return;
                }

                MessageBoxResult resultado = MessageBoxResult.No;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    resultado = MessageBox.Show(
                        $"Hay una nueva versión disponible ({nuevaVersion.TargetFullRelease.Version}).\n\n" +
                        "¿Desea actualizar ahora? La aplicación se reiniciará al terminar.",
                        "Actualización disponible",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                });

                if (resultado != MessageBoxResult.Yes)
                {
                    return;
                }

                await mgr.DownloadUpdatesAsync(nuevaVersion);
                mgr.ApplyUpdatesAndRestart(nuevaVersion);
            }
            catch
            {
                // Un fallo de red o un share inaccesible no debe romper el arranque de la app.
            }
        }

        private static string ReadUpdateUrlFromIni()
        {
            try
            {
                string iniPath = Path.Combine(AppContext.BaseDirectory, "GestorExpedientesWpf.ini");
                if (!File.Exists(iniPath))
                {
                    return string.Empty;
                }

                var ini = new IniFile(iniPath);
                if (!string.Equals(SafeReadValue(ini, "Updates", "Enabled"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                return SafeReadValue(ini, "Updates", "UpdateUrl");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeReadValue(IniFile ini, string section, string key)
        {
            try
            {
                return ini.ReadValue(section, key);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

}
