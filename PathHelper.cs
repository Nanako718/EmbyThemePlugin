using System.IO;

namespace Emby.Plugin.MistyTheme
{
    public static class PathHelper
    {
        /// <summary>
        /// 推算 Emby dashboard-ui 目录。
        /// 典型结构（Docker）:
        ///   /system/          ← Emby 系统文件 + dashboard-ui
        ///   /plugins/Name/    ← 插件 DLL 所在
        /// </summary>
        public static string FindDashboardPath()
        {
            // 先尝试绝对路径的常见位置（Docker 标准结构）
            string[] absolute =
            {
                "/system/dashboard-ui",
                "/app/dashboard-ui",
                "/emby/dashboard-ui",
            };

            foreach (var p in absolute)
            {
                if (File.Exists(Path.Combine(p, "index.html")))
                    return p;
            }

            // 再尝试从 DLL 位置推算（确保绝对路径）
            var location   = Path.GetFullPath(typeof(PathHelper).Assembly.Location ?? ".");
            var pluginDir  = Path.GetDirectoryName(location) ?? "/";
            var pluginsDir = Path.GetDirectoryName(pluginDir) ?? "/";
            var rootDir    = Path.GetDirectoryName(pluginsDir) ?? "/";

            string[] derived =
            {
                Path.Combine(rootDir, "system", "dashboard-ui"),
                Path.Combine(rootDir, "dashboard-ui"),
            };

            foreach (var p in derived)
            {
                if (File.Exists(Path.Combine(p, "index.html")))
                    return p;
            }

            return "/system/dashboard-ui";
        }
    }
}
