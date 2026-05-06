using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugin.MistyTheme
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin? Instance { get; private set; }

        public override string Name => "Misty Theme";
        public override Guid Id => new Guid("4a8b2c6d-1e3f-4a7b-9c2d-8e5f1a0b3c7d");
        public override string Description => "Cinematic banner theme for Emby";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo
            {
                Name = "MistyTheme",
                EmbeddedResourcePath = GetType().Namespace + ".web.configurationpage.html"
            }
        };
    }
}
