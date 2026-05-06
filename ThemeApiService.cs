using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace Emby.Plugin.MistyTheme
{
    // POST /MistyTheme/Apply
    // 配置页保存后调用，立即生效无需重启 Emby
    [Route("/MistyTheme/Apply", "POST", Summary = "Apply or remove Misty Theme injection")]
    public class ApplyThemeRequest : IReturnVoid { }

    public class ThemeApiService : IService
    {
        private readonly IApplicationPaths _paths;

        public ThemeApiService(IApplicationPaths paths)
        {
            _paths = paths;
        }

        public void Post(ApplyThemeRequest _)
        {
            if (Plugin.Instance?.Configuration.EnableNewUI == true)
                ThemeManager.Inject(_paths.WebPath);
            else
                ThemeManager.Remove(_paths.WebPath);

            // 重新复制资源（以防更新了文件）
            ThemeManager.CopyAssets(_paths.WebPath);
        }
    }
}
