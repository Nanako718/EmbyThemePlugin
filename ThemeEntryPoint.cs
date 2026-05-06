using System;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Emby.Plugin.MistyTheme
{
    /// <summary>
    /// Emby 服务器启动时自动执行：复制静态资源，按配置注入/移除 index.html
    /// </summary>
    public class ThemeEntryPoint : IServerEntryPoint
    {
        private readonly IApplicationPaths _paths;
        private readonly ILogger<ThemeEntryPoint> _logger;

        public ThemeEntryPoint(IApplicationPaths paths, ILogger<ThemeEntryPoint> logger)
        {
            _paths  = paths;
            _logger = logger;
        }

        public Task RunAsync()
        {
            try
            {
                // 1. 把 main.js / styles.css / 字体 复制到 {WebPath}/misty/
                ThemeManager.CopyAssets(_paths.WebPath);

                // 2. 根据开关决定是否注入 index.html
                if (Plugin.Instance?.Configuration.EnableNewUI == true)
                    ThemeManager.Inject(_paths.WebPath);
                else
                    ThemeManager.Remove(_paths.WebPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MistyTheme] 启动时初始化失败");
            }

            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
