using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Emby.Plugin.MistyTheme
{
    /// <summary>
    /// 负责将主题内容注入 / 移除 Emby 的 index.html
    /// 用首尾标记保证幂等：多次 Inject 不会重复注入
    /// </summary>
    public static class ThemeManager
    {
        private const string HeadStart = "<!-- misty:head:start -->";
        private const string HeadEnd   = "<!-- misty:head:end -->";
        private const string BodyStart = "<!-- misty:body:start -->";
        private const string BodyEnd   = "<!-- misty:body:end -->";

        // ── 公开 API ─────────────────────────────────────────────

        public static void Inject(string webPath)
        {
            var indexPath  = Path.Combine(webPath, "index.html");
            if (!File.Exists(indexPath)) return;

            var html = File.ReadAllText(indexPath, Encoding.UTF8);
            if (html.Contains(HeadStart)) return;   // 已注入，跳过

            // 备份原始文件（只备份一次）
            var backup = indexPath + ".misty-bak";
            if (!File.Exists(backup)) File.Copy(indexPath, backup);

            html = html.Replace("</head>", $"\n{HeadStart}\n{HeadContent()}\n{HeadEnd}\n</head>");
            html = html.Replace("</body>", $"\n{BodyStart}\n{BodyContent()}\n{BodyEnd}\n</body>");

            File.WriteAllText(indexPath, html, Encoding.UTF8);
        }

        public static void Remove(string webPath)
        {
            var indexPath = Path.Combine(webPath, "index.html");
            var backup    = indexPath + ".misty-bak";

            // 优先从备份恢复
            if (File.Exists(backup))
            {
                File.Copy(backup, indexPath, overwrite: true);
                return;
            }

            if (!File.Exists(indexPath)) return;
            var html = File.ReadAllText(indexPath, Encoding.UTF8);
            html = Strip(html, HeadStart, HeadEnd);
            html = Strip(html, BodyStart, BodyEnd);
            File.WriteAllText(indexPath, html, Encoding.UTF8);
        }

        /// <summary>将 web/ 下的静态文件复制到 Emby web 目录的 misty/ 子目录</summary>
        public static void CopyAssets(string webPath)
        {
            var targetDir = Path.Combine(webPath, "misty");
            Directory.CreateDirectory(targetDir);

            var asm = typeof(ThemeManager).Assembly;
            var ns  = typeof(ThemeManager).Namespace!;

            foreach (var filename in new[] { "main.js", "styles.css", "AgibotDisplay-Regular.woff2" })
            {
                var resourceName = $"{ns}.web.{filename}";
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null) continue;
                using var fs = File.Create(Path.Combine(targetDir, filename));
                stream.CopyTo(fs);
            }
        }

        // ── 私有辅助 ─────────────────────────────────────────────

        private static string Strip(string html, string start, string end)
        {
            var s = html.IndexOf(start, StringComparison.Ordinal);
            var e = html.IndexOf(end,   StringComparison.Ordinal);
            if (s < 0 || e < 0) return html;
            return html.Remove(s, e + end.Length - s);
        }

        // ── 注入内容（从 index.html 提取，URL 改为 /misty/ 前缀）──

        private static string HeadContent() => @"<link rel=""preload"" href=""/misty/AgibotDisplay-Regular.woff2"" as=""font"" type=""font/woff2"" crossorigin>
<style>
    @font-face {
        font-family: 'Agibot Display';
        src: url('/misty/AgibotDisplay-Regular.woff2') format('woff2');
        font-weight: 400;
        font-style: normal;
        font-display: optional;
    }
    .app-splash-container { display: none !important; }
    .app-splash { display: none !important; }
    .misty-loading {
        position: fixed; inset: 0; z-index: 999999; pointer-events: none;
    }
    .misty-loading-top,
    .misty-loading-bottom {
        position: absolute; inset: 0; background: #000;
        display: flex; align-items: center; justify-content: center;
        overflow: hidden; pointer-events: auto;
    }
    .misty-loading-top    { clip-path: inset(0 0 50% 0); }
    .misty-loading-bottom { clip-path: inset(50% 0 0 0); }
    .misty-loading-top::before,
    .misty-loading-bottom::before {
        content: ""; position: absolute; inset: 0; pointer-events: none; z-index: 1;
        background: radial-gradient(ellipse 100% 100% at 50% 50%, transparent 28%, rgba(0,0,0,.72) 100%);
    }
    .misty-loading .misty-loading-bg {
        position: absolute; inset: 0;
        background:
            radial-gradient(ellipse 62% 50% at 50% 52%, rgba(88,48,168,.26) 0%, rgba(55,28,120,.08) 55%, transparent 70%),
            radial-gradient(ellipse 36% 28% at 16% 84%, rgba(52,28,108,.16) 0%, transparent 55%),
            radial-gradient(ellipse 30% 24% at 84% 16%, rgba(44,22,96,.14) 0%, transparent 55%),
            #000;
        animation: mistyLoadingBreathe 9s ease-in-out infinite alternate;
    }
    .misty-loading .misty-loading-bg::before {
        content: ""; position: absolute; inset: -10%;
        background: radial-gradient(42rem 28rem at 22% 72%, rgba(110,72,170,.14), transparent 62%);
        filter: blur(28px); animation: mistyLoadingPulse 10s ease-in-out infinite;
    }
    .misty-loading .misty-loading-bg::after {
        content: ""; position: absolute; inset: -10%;
        background: radial-gradient(36rem 24rem at 78% 28%, rgba(90,58,150,.11), transparent 62%);
        filter: blur(24px); animation: mistyLoadingPulse 13s ease-in-out infinite reverse;
    }
    .misty-loading .misty-loading-content {
        position: relative; z-index: 2; display: flex;
        flex-direction: column; align-items: center; gap: 2rem;
    }
    .misty-loading .misty-loading-mark {
        display: flex; align-items: center; gap: 10px;
        opacity: 0; animation: mistyLoadingFadeIn 1.4s cubic-bezier(.16,1,.3,1) .2s forwards;
    }
    .misty-loading .misty-loading-mark::before,
    .misty-loading .misty-loading-mark::after {
        content: ""; display: block; width: 26px; height: 1px; background: rgba(255,255,255,.26);
    }
    .misty-loading h1 {
        margin: 0 !important; font-family: 'Agibot Display' !important;
        font-size: clamp(38px,5.2vw,80px) !important; font-weight: 400 !important;
        line-height: 1.1 !important; letter-spacing: .26em; text-transform: uppercase;
        color: rgba(255,252,248,.95);
        text-shadow: 0 0 60px rgba(140,90,220,.28), 0 2px 18px rgba(0,0,0,.55);
        opacity: 0; transform: translateY(12px);
        transition: opacity .9s cubic-bezier(.16,1,.3,1), transform .9s cubic-bezier(.16,1,.3,1);
    }
    .misty-loading h1.active { opacity: 1; transform: translateY(0); }
    .misty-loading .misty-loading-progress {
        width: 180px; height: 1px; background: rgba(255,255,255,.09);
        position: relative; overflow: hidden;
        opacity: 0; animation: mistyLoadingFadeIn 1s cubic-bezier(.16,1,.3,1) .8s forwards;
    }
    .misty-loading .misty-loading-progress-bar {
        position: absolute; inset: 0 auto 0 -32%; width: 32%;
        background: linear-gradient(90deg, transparent 0%, rgba(190,158,255,.45) 25%, rgba(225,205,255,.92) 50%, rgba(190,158,255,.45) 75%, transparent 100%);
        filter: blur(.4px); animation: mistyLoadingScan 2.6s cubic-bezier(.4,0,.6,1) .8s infinite;
    }
    .misty-loading .misty-loading-sub {
        margin: 0; font-size: .6rem; letter-spacing: .5em; text-transform: uppercase;
        color: rgba(255,255,255,.24); animation: mistyLoadingBreathText 2.8s ease-in-out infinite;
    }
    @keyframes mistyLoadingFadeIn { to { opacity: 1; } }
    @keyframes mistyLoadingScan { 0%{transform:translateX(0)} 100%{transform:translateX(413%)} }
    @keyframes mistyLoadingBreathText { 0%,100%{opacity:.16} 50%{opacity:.48} }
    @keyframes mistyLoadingBreathe { 0%{transform:scale(1)} 100%{transform:scale(1.04)} }
    @keyframes mistyLoadingPulse { 0%,100%{opacity:.35;transform:translateY(0)} 50%{opacity:.62;transform:translateY(-1.2%)} }
</style>
<link rel=""stylesheet"" href=""/misty/styles.css"">";

        private static string BodyContent() => @"<div class=""misty-loading"">
    <div class=""misty-loading-top"">
        <div class=""misty-loading-bg""></div>
        <div class=""misty-loading-content"">
            <div class=""misty-loading-mark""></div>
            <h1 class=""misty-loading-title active"">DTZSGHNR MEDIA</h1>
            <div class=""misty-loading-progress""><div class=""misty-loading-progress-bar""></div></div>
            <p class=""misty-loading-sub"">Loading</p>
        </div>
    </div>
    <div class=""misty-loading-bottom"">
        <div class=""misty-loading-bg""></div>
        <div class=""misty-loading-content"">
            <div class=""misty-loading-mark""></div>
            <h1 class=""misty-loading-title active"">DTZSGHNR MEDIA</h1>
            <div class=""misty-loading-progress""><div class=""misty-loading-progress-bar""></div></div>
            <p class=""misty-loading-sub"">Loading</p>
        </div>
    </div>
</div>
<script>
(function(){
    function half(){
        return '<div class=""misty-loading-bg""></div>'
             + '<div class=""misty-loading-content"">'
             + '<div class=""misty-loading-mark""></div>'
             + '<h1 class=""misty-loading-title active"">DTZSGHNR MEDIA</h1>'
             + '<div class=""misty-loading-progress""><div class=""misty-loading-progress-bar""></div></div>'
             + '<p class=""misty-loading-sub"">Loading</p></div>';
    }
    window.MistyLoading={
        ensureDom:function(){
            if(document.querySelector('.misty-loading'))return;
            document.body.insertAdjacentHTML('beforeend',
                '<div class=""misty-loading""><div class=""misty-loading-top"">'+half()+'</div>'
               +'<div class=""misty-loading-bottom"">'+half()+'</div></div>');
        },
        fadeRemove:function(){
            var el=document.querySelector('.misty-loading');
            if(!el)return;
            var top=el.querySelector('.misty-loading-top');
            var bot=el.querySelector('.misty-loading-bottom');
            var ease='cubic-bezier(0.76,0,0.24,1)';
            el.style.pointerEvents='none';
            if(top){top.style.transition='transform 1.4s '+ease;top.style.transform='translateY(-100%)';}
            if(bot){bot.style.transition='transform 1.4s '+ease;bot.style.transform='translateY(100%)';}
            setTimeout(function(){if(el.parentNode)el.parentNode.removeChild(el);},1500);
        }
    };
})();
</script>
<script src=""/misty/main.js""></script>";
    }
}
