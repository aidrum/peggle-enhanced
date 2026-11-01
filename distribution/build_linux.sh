#!/usr/bin/env bash
# Builds the Linux (x64) desktop version as a self-contained folder, packed as a .tar.gz, and as an
# AppImage when appimagetool is available (in PATH, or set APPIMAGETOOL=/path/to/appimagetool).
#   distribution/build_linux.sh [HD|iPhone]
# Output: distribution/out/<Name>-linux-x64.tar.gz (+ <Name>-x86_64.AppImage).
# Saves go to ~/.local/share/Peggle Enhanced/<edition> ($XDG_DATA_HOME).
set -euo pipefail
cd "$(dirname "$0")/.."

EDITION="${1:-HD}"
RID=linux-x64
case "$EDITION" in
  HD)     APP_NAME="Peggle HD"; EXE="PeggleHD"; APP_ID="com.aidrum.peggle.hd" ;;
  iPhone) APP_NAME="Peggle";    EXE="Peggle";   APP_ID="com.aidrum.peggle" ;;
  *) echo "Edition must be HD or iPhone (got '$EDITION')" >&2; exit 1 ;;
esac
VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n1)"

OUT="distribution/out"
DIR="$OUT/$EXE-linux-x64"
rm -rf "$DIR" "$OUT/$EXE.AppDir"
mkdir -p "$OUT"

dotnet publish Peggle.Desktop -c Release -r "$RID" --self-contained \
  -p:PeggleEdition="$EDITION" -p:DebugType=none -o "$DIR"
chmod +x "$DIR/$EXE"
cp "Peggle.Desktop/Icons/$EDITION/icon.png" "$DIR/$APP_ID.png"

desktop_entry() { # $1 = Exec line, $2 = Icon
  cat <<DESKTOP
[Desktop Entry]
Type=Application
Name=$APP_NAME
Comment=Peggle Enhanced $VERSION
Exec=$1
Icon=$2
Terminal=false
Categories=Game;ArcadeGame;
StartupWMClass=$EXE
DESKTOP
}
# For the tarball: a desktop entry to copy to ~/.local/share/applications (edit the paths) if wanted.
desktop_entry "/path/to/$EXE-linux-x64/$EXE" "/path/to/$EXE-linux-x64/$APP_ID.png" > "$DIR/$APP_ID.desktop"

tar -C "$OUT" -czf "$OUT/$EXE-linux-x64.tar.gz" "$EXE-linux-x64"
echo "Built $OUT/$EXE-linux-x64.tar.gz"

TOOL="${APPIMAGETOOL:-$(command -v appimagetool || true)}"
if [[ -n "$TOOL" ]]; then
  APPDIR="$OUT/$EXE.AppDir"
  mkdir -p "$APPDIR/usr/lib"
  cp -R "$DIR" "$APPDIR/usr/lib/$EXE"
  rm -f "$APPDIR/usr/lib/$EXE/$APP_ID.desktop"
  cat > "$APPDIR/AppRun" <<APPRUN
#!/bin/sh
HERE="\$(dirname "\$(readlink -f "\$0")")"
exec "\$HERE/usr/lib/$EXE/$EXE" "\$@"
APPRUN
  chmod +x "$APPDIR/AppRun"
  desktop_entry "$EXE" "$APP_ID" > "$APPDIR/$APP_ID.desktop"
  cp "Peggle.Desktop/Icons/$EDITION/icon.png" "$APPDIR/$APP_ID.png"
  ARCH=x86_64 "$TOOL" --no-appstream "$APPDIR" "$OUT/$EXE-x86_64.AppImage"
  rm -rf "$APPDIR"
  echo "Built $OUT/$EXE-x86_64.AppImage"
else
  echo "appimagetool not found: skipped the AppImage."
fi
