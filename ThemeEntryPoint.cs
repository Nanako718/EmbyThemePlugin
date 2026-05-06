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
                var webPath   = PathHelper.FindDashboardPath();
                var indexPath = Path.Combine(webPath, "index.html");

                Console.WriteLine($"[MistyTheme] path={webPath} exists={File.Exists(indexPath)}");

                var enabled = Plugin.Instance?.Configuration.EnableNewUI ?? true;
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
