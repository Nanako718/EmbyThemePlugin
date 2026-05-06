#!/bin/bash
# 用法: ./build.sh [Emby安装目录]
# 示例: ./build.sh /Applications/EmbyServer/system/

EMBY_PATH="${1:-/Applications/EmbyServer/system/}"
OUT="./bin/plugin"

echo "→ 复制主题文件..."
cp ../EmbyTheme/Theme/main.js     web/main.js
cp ../EmbyTheme/Theme/styles.css  web/styles.css
cp ../EmbyTheme/AgibotDisplay-Regular.woff2 web/AgibotDisplay-Regular.woff2

echo "→ 编译插件..."
dotnet publish -c Release \
  -p:EmbyPath="$EMBY_PATH" \
  -o "$OUT" \
  --no-self-contained

if [ $? -ne 0 ]; then echo "✗ 编译失败"; exit 1; fi

# 只保留插件自身的 DLL，依赖库 Emby 已有
mkdir -p "$OUT/clean"
cp "$OUT/Emby.Plugin.MistyTheme.dll" "$OUT/clean/"

echo ""
echo "✓ 构建完成：$OUT/clean/Emby.Plugin.MistyTheme.dll"
echo ""
echo "部署步骤："
echo "  1. 停止 Emby Server"
echo "  2. 将 Emby.Plugin.MistyTheme.dll 复制到 Emby 插件目录："
echo "     macOS:   ~/.emby-server/plugins/MistyTheme/"
echo "     Linux:   /var/lib/emby/plugins/MistyTheme/"
echo "     Windows: %APPDATA%\\Emby-Server\\plugins\\MistyTheme\\"
echo "  3. 启动 Emby Server"
echo "  4. 进入 控制台 → 插件 → Misty Theme → 点击「立即应用」"
