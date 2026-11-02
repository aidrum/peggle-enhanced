#!/usr/bin/env bash
# Builds the Linux (x64) desktop version as a self-contained folder packed into a .tar.gz, plus an
# AppImage when appimagetool is available (in PATH, or APPIMAGETOOL=/path/to/appimagetool).
#   distribution/build_linux.sh [HD|iPhone]
# Output: distribution/out/<Exe>-linux-x64.tar.gz and <Exe>-x86_64.AppImage.
# Saves go to $XDG_DATA_HOME/Peggle Enhanced/<edition> (Program.GetDefaultStorageDir).
set -euo pipefail
cd "$(dirname "$0")/.."
source distribution/edition.sh

EDITION="${1:-HD}"
edition_info "$EDITION"

OUT="distribution/out"
DIR="$OUT/$EXE-linux-x64"
APPDIR="$OUT/$EXE.AppDir"
rm -rf "$DIR" "$APPDIR"
mkdir -p "$OUT"

dotnet publish Peggle.Desktop -c Release -r linux-x64 --self-contained \
  -p:PeggleEdition="$EDITION" -p:DebugType=none -o "$DIR"
chmod +x "$DIR/$EXE"

tar -C "$OUT" -czf "$OUT/$EXE-linux-x64.tar.gz" "$EXE-linux-x64"
echo "Built $OUT/$EXE-linux-x64.tar.gz"

TOOL="${APPIMAGETOOL:-$(command -v appimagetool || true)}"
if [[ -z "$TOOL" ]]; then
  echo "appimagetool not found: skipped the AppImage."
  exit 0
fi

# AppDir layout: the published folder under usr/lib, an AppRun launcher, and the desktop entry and
# icon at the top level.
mkdir -p "$APPDIR/usr/lib"
cp -R "$DIR" "$APPDIR/usr/lib/$EXE"
cat > "$APPDIR/AppRun" <<APPRUN
#!/bin/sh
HERE="\$(dirname "\$(readlink -f "\$0")")"
exec "\$HERE/usr/lib/$EXE/$EXE" "\$@"
APPRUN
chmod +x "$APPDIR/AppRun"
cat > "$APPDIR/$APP_ID.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=$APP_NAME
Comment=Peggle Enhanced $VERSION
Exec=$EXE
Icon=$APP_ID
Terminal=false
Categories=Game;ArcadeGame;
StartupWMClass=$EXE
DESKTOP
cp "Peggle.Desktop/Icons/$EDITION/icon.png" "$APPDIR/$APP_ID.png"

ARCH=x86_64 "$TOOL" --no-appstream "$APPDIR" "$OUT/$EXE-x86_64.AppImage"
rm -rf "$APPDIR"
echo "Built $OUT/$EXE-x86_64.AppImage"
