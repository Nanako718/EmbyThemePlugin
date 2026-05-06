using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace Emby.Plugin.MistyTheme
{
    [Route("/MistyTheme/Apply", "POST", Summary = "Apply or remove Misty Theme injection")]
    public class ApplyThemeRequest : IReturnVoid { }

    public class ThemeApiService : IService
    {
        public void Post(ApplyThemeRequest _)
        {
            var webPath = PathHelper.FindDashboardPath();
            if (Plugin.Instance?.Configuration.EnableNewUI == true)
                ThemeManager.Inject(webPath);
            else
                ThemeManager.Remove(webPath);
        }
    }
}
