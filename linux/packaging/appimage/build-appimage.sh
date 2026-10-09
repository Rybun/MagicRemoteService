#!/usr/bin/env bash
# Builds MagicRemoteService.AppImage: a self-contained, unsandboxed bundle of the GUI (no .NET
# runtime required on the target machine). Downloads appimagetool on first run (cached under
# packaging/appimage/build/tools/) — everything else is produced locally, no other network access.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"
APPDIR="$BUILD_DIR/MagicRemoteService.AppDir"
TOOLS_DIR="$BUILD_DIR/tools"
APPIMAGETOOL="$TOOLS_DIR/appimagetool-x86_64.AppImage"

rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$TOOLS_DIR"

echo "==> Publishing MagicRemote.Gui (self-contained, linux-x64)"
dotnet publish "$REPO_ROOT/src/MagicRemote.Gui" \
	--configuration Release \
	--runtime linux-x64 \
	--self-contained true \
	--output "$APPDIR/usr/bin"

echo "==> Assembling AppDir"
cp "$SCRIPT_DIR/AppDir/AppRun" "$APPDIR/AppRun"
chmod +x "$APPDIR/AppRun"
cp "$SCRIPT_DIR/AppDir/magicremoteservice.desktop" "$APPDIR/magicremoteservice.desktop"

# Same teardrop template TrayIconFactory recolors for the tray/window icon at runtime (see
# MagicRemote.Cli/TvTemplates) — used here as-is (its baked-in neutral gray border), upscaled to
# 256x256, so the app-menu icon matches the actual product instead of a generic placeholder shape.
ICON_TEMPLATE="$REPO_ROOT/src/MagicRemote.Cli/TvTemplates/MagicRemoteService/largeIcon.png"
if [[ -f "$ICON_TEMPLATE" ]] && command -v convert > /dev/null; then
	convert "$ICON_TEMPLATE" -resize 256x256 "$APPDIR/magicremoteservice.png"
elif command -v convert > /dev/null; then
	echo "    Teardrop icon template not found at $ICON_TEMPLATE — using placeholder circle instead." >&2
	convert -size 256x256 xc:none -fill "#1E6EC8" -draw "circle 128,128 128,10" "$APPDIR/magicremoteservice.png"
else
	echo "    ImageMagick's 'convert' not found — placing a 1x1 placeholder icon instead." >&2
	printf '\x89PNG\r\n\x1a\n' > "$APPDIR/magicremoteservice.png" # not a valid PNG; replace before shipping
fi
# Also left as a sibling of the built AppImage (not just inside it) — install.sh's system-wide
# desktop-menu entry copies this file rather than extracting the AppImage just for its icon.
cp "$APPDIR/magicremoteservice.png" "$BUILD_DIR/magicremoteservice.png"

if [[ ! -x "$APPIMAGETOOL" ]]; then
	echo "==> Downloading appimagetool"
	curl -fL -o "$APPIMAGETOOL" \
		"https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
	chmod +x "$APPIMAGETOOL"
fi

echo "==> Building AppImage"
ARCH=x86_64 "$APPIMAGETOOL" "$APPDIR" "$BUILD_DIR/MagicRemoteService-x86_64.AppImage"

echo "Built $BUILD_DIR/MagicRemoteService-x86_64.AppImage"
