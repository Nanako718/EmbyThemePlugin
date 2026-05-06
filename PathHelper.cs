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
            // 插件 DLL 位置: /plugins/MistyTheme/
            var pluginDir  = Path.GetDirectoryName(typeof(PathHelper).Assembly.Location) ?? "";
            // /plugins/
            var pluginsDir = Path.GetDirectoryName(pluginDir) ?? "";
            // 上一级（容器根 / 或 Emby 数据根）
            var rootDir    = Path.GetDirectoryName(pluginsDir) ?? "";

            string[] candidates =
            {
                Path.Combine(rootDir,    "system",   "dashboard-ui"),
                Path.Combine(rootDir,                "dashboard-ui"),
                Path.Combine(pluginsDir, "..",       "dashboard-ui"),
                Path.Combine(pluginsDir, "..", "system", "dashboard-ui"),
            };

            foreach (var p in candidates)
            {
                if (File.Exists(Path.Combine(p, "index.html")))
                    return p;
            }

            // 找不到就返回最常见路径，ThemeManager 会在日志里报错
            return Path.Combine(rootDir, "system", "dashboard-ui");
        }
    }
}
