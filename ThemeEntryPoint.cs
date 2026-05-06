using System;
using System.IO;
using MediaBrowser.Controller.Plugins;

namespace Emby.Plugin.MistyTheme
{
    public class ThemeEntryPoint : IServerEntryPoint
    {
        public void Run()
        {
            try
            {
                var webPath = PathHelper.FindDashboardPath();
                var indexPath = Path.Combine(webPath, "index.html");

                Console.WriteLine($"[MistyTheme] dashboard path: {webPath}");
                Console.WriteLine($"[MistyTheme] index.html exists: {File.Exists(indexPath)}");
                Console.WriteLine($"[MistyTheme] Plugin.Instance: {(Plugin.Instance == null ? "NULL" : "OK")}");

                ThemeManager.CopyAssets(webPath);

                // Plugin.Instance 为 null 时默认启用
                var enabled = Plugin.Instance?.Configuration.EnableNewUI ?? true;
                Console.WriteLine($"[MistyTheme] EnableNewUI: {enabled}");

                if (enabled)
                    ThemeManager.Inject(webPath);
                else
                    ThemeManager.Remove(webPath);

                Console.WriteLine("[MistyTheme] Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MistyTheme] ERROR: {ex}");
            }
        }

        public void Dispose() { }
    }
}
