using MediaBrowser.Model.Plugins;

namespace Emby.Plugin.MistyTheme
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool EnableNewUI { get; set; } = true;
    }
}
