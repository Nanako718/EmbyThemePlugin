#!/bin/bash
# 运行一次，把 Emby DLL 和主题文件准备好，然后 git push，GitHub Actions 自动编译
# 用法: ./setup.sh [Emby安装目录]
# 示例: ./setup.sh /Applications/EmbyServer/system/

EMBY="${1:-/Applications/EmbyServer/system/}"

# ── 1. 复制 Emby DLL ────────────────────────────────────────────
echo "→ 从 $EMBY 复制 Emby DLL..."
mkdir -p lib

DLLS=(
    "MediaBrowser.Common.dll"
    "MediaBrowser.Model.dll"
    "MediaBrowser.Controller.dll"
    "Microsoft.Extensions.Logging.Abstractions.dll"
)

ALL_OK=true
for dll in "${DLLS[@]}"; do
    src="$EMBY$dll"
    if [ -f "$src" ]; then
        cp "$src" "lib/$dll"
        echo "  ✓ $dll"
    else
        echo "  ✗ 找不到: $src"
        ALL_OK=false
    fi
done

if [ "$ALL_OK" = false ]; then
    echo ""
    echo "部分 DLL 未找到，请确认 Emby 安装路径正确。"
    echo "用法: ./setup.sh /path/to/emby/system/"
    exit 1
fi

# ── 2. 复制主题文件 ──────────────────────────────────────────────
echo "→ 复制主题文件..."
THEME="../EmbyTheme"

cp "$THEME/Theme/main.js"    web/main.js    && echo "  ✓ main.js"
cp "$THEME/Theme/styles.css" web/styles.css && echo "  ✓ styles.css"

# 字体（可选）
FONT=$(find "$THEME" -name "*.woff2" 2>/dev/null | head -1)
if [ -n "$FONT" ]; then
    cp "$FONT" web/AgibotDisplay-Regular.woff2
    echo "  ✓ 字体: $(basename $FONT)"
else
    echo "  ℹ 未找到 .woff2 字体，如需字体请手动复制到 web/ 目录"
fi

# ── 3. 提示后续步骤 ──────────────────────────────────────────────
echo ""
echo "✓ 准备完成！"
echo ""
echo "接下来："
echo "  git add lib/ web/"
echo "  git commit -m 'Add Emby DLLs and theme assets'"
echo "  git push"
echo ""
echo "GitHub Actions 会自动编译，在 Actions 页面下载 DLL，"
echo "或推一个 tag（git tag v1.0.0 && git push --tags）自动发布 Release。"
