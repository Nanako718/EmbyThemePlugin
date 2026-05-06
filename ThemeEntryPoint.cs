using System;
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
                ThemeManager.CopyAssets(webPath);

                if (Plugin.Instance?.Configuration.EnableNewUI == true)
                    ThemeManager.Inject(webPath);
                else
                    ThemeManager.Remove(webPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MistyTheme] 启动失败: {ex.Message}");
            }
        }

        public void Dispose() { }
    }
}
