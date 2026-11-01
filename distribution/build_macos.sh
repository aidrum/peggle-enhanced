#!/usr/bin/env bash
# Builds a macOS .app bundle of the desktop (DesktopGL) version.
#   distribution/build_macos.sh [HD|iPhone] [osx-x64|osx-arm64]
# Defaults: HD, osx-x64 (Intel). Output: distribution/out/<App Name>.app (+ a .zip on macOS).
# The publish step works on any OS; the ad-hoc signature and zip need macOS (codesign, ditto).
set -euo pipefail
cd "$(dirname "$0")/.."

EDITION="${1:-HD}"
RID="${2:-osx-x64}"
case "$EDITION" in
  HD)     APP_NAME="Peggle HD"; EXE="PeggleHD"; BUNDLE_ID="com.aidrum.peggle.hd" ;;
  iPhone) APP_NAME="Peggle";    EXE="Peggle";   BUNDLE_ID="com.aidrum.peggle" ;;
  *) echo "Edition must be HD or iPhone (got '$EDITION')" >&2; exit 1 ;;
esac
VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n1)"

OUT="distribution/out"
APP="$OUT/$APP_NAME.app"
PUBLISH="$OUT/publish-$EDITION-$RID"
rm -rf "$APP" "$PUBLISH"

dotnet publish Peggle.Desktop -c Release -r "$RID" --self-contained \
  -p:PeggleEdition="$EDITION" -p:DebugType=none -o "$PUBLISH"

mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBLISH"/. "$APP/Contents/MacOS/"
cp "Peggle.Desktop/Icons/$EDITION/icon.icns" "$APP/Contents/Resources/icon.icns"
chmod +x "$APP/Contents/MacOS/$EXE"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$APP_NAME</string>
  <key>CFBundleDisplayName</key><string>$APP_NAME</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
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
  # Ad-hoc signature: no Apple account needed (required to run at all on Apple Silicon). Gatekeeper still
  # warns on first launch of a downloaded copy: right-click > Open, or xattr -dr com.apple.quarantine.
  codesign --force --deep --sign - "$APP"
  ZIP="$OUT/${APP_NAME// /}-macOS-$RID.zip"
  rm -f "$ZIP"
  ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"
  echo "Built $ZIP"
fi
rm -rf "$PUBLISH"
echo "Built $APP"
