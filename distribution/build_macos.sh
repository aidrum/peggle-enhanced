#!/usr/bin/env bash
# Builds a macOS .app bundle of the desktop (DesktopGL) version.
#   distribution/build_macos.sh [HD|iPhone] [osx-x64|osx-arm64]
# Defaults: HD, osx-x64 (Intel). Output: distribution/out/<App Name>.app, plus a .zip on macOS.
# The publish step works on any OS; the ad-hoc signature and the zip need macOS (codesign, ditto).
set -euo pipefail
cd "$(dirname "$0")/.."
source distribution/edition.sh

EDITION="${1:-HD}"
RID="${2:-osx-x64}"
edition_info "$EDITION"

OUT="distribution/out"
APP="$OUT/$APP_NAME.app"
PUBLISH="$OUT/publish-$EDITION-$RID"
rm -rf "$APP" "$PUBLISH"

dotnet publish Peggle.Desktop -c Release -r "$RID" --self-contained \
  -p:PeggleEdition="$EDITION" -p:DebugType=none -o "$PUBLISH"

mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBLISH"/. "$APP/Contents/MacOS/"
rm -rf "$PUBLISH"
# Game data goes in Resources (Program.UseAppBundleContent): codesign rejects data folders such as
# en.lproj in Contents/MacOS.
mv "$APP/Contents/MacOS/Content" "$APP/Contents/Resources/Content"
cp "Peggle.Desktop/Icons/$EDITION/icon.icns" "$APP/Contents/Resources/icon.icns"
chmod +x "$APP/Contents/MacOS/$EXE"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$APP_NAME</string>
  <key>CFBundleDisplayName</key><string>$APP_NAME</string>
  <key>CFBundleIdentifier</key><string>$APP_ID</string>
  <key>CFBundleExecutable</key><string>$EXE</string>
  <key>CFBundleIconFile</key><string>icon</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>LSApplicationCategoryType</key><string>public.app-category.arcade-games</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST

if [[ "$(uname)" == "Darwin" ]]; then
  # Ad-hoc signature: no Apple account needed, and required to run at all on Apple Silicon. Gatekeeper
  # still warns on the first launch of a downloaded copy (right-click > Open, or xattr -dr
  # com.apple.quarantine).
  codesign --force --deep --sign - "$APP"
  ZIP="$OUT/$EXE-macOS-$RID.zip"
  rm -f "$ZIP"
  ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"
  echo "Built $ZIP"
fi
echo "Built $APP"
