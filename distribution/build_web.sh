#!/usr/bin/env bash
# Builds the web version (WebAssembly, played in a browser) as a static site.
#   distribution/build_web.sh [HD|iPhone]
# Output: distribution/out/web/<Exe>/ (the site: open it through a web server, e.g.
#         "python3 -m http.server -d distribution/out/web/<Exe>") and distribution/out/<Exe>-web.zip.
# Needs the wasm-tools workload (dotnet workload install wasm-tools) for the Release build, which is
# compiled ahead of time; CONFIGURATION=Debug builds a quicker, much slower-running one without it.
# Saves are kept in the browser (IndexedDB), per edition.
set -euo pipefail
cd "$(dirname "$0")/.."
source distribution/edition.sh

EDITION="${1:-HD}"
edition_info "$EDITION"
CONFIGURATION="${CONFIGURATION:-Release}"

OUT="distribution/out"
PUBLISH="$OUT/web-publish/$EXE"
SITE="$OUT/web/$EXE"
rm -rf "$PUBLISH" "$SITE" "$OUT/$EXE-web.zip"
mkdir -p "$OUT/web"

dotnet publish Peggle.Web/Peggle.Web.csproj -c "$CONFIGURATION" \
  -p:PeggleWeb=true -p:PeggleEdition="$EDITION" -o "$PUBLISH"
mv "$PUBLISH/wwwroot" "$SITE"
rm -rf "$PUBLISH"

# The page's edition: its title, and which saves it uses (js/peggle.js).
sed -i.bak -e "s|data-edition=\"HD\"|data-edition=\"$EDITION\"|" -e "s|<title>Peggle HD</title>|<title>$APP_NAME</title>|" "$SITE/index.html"
rm -f "$SITE/index.html.bak"
cp "Peggle.Desktop/Icons/$EDITION/icon.png" "$SITE/icon.png"
cp logo.png "$SITE/logo.png"
# GitHub Pages: serve the _framework and _content folders as they are.
touch "$SITE/.nojekyll"

# The game data, the shared files first so the edition's own replace them (as in the desktop builds), and
# the list of files WebContentSource reads (a browser can't list a folder).
mkdir -p "$SITE/Content"
cp -R Peggle/Content.Shared/. "$SITE/Content/"
cp -R "Peggle/Content.$EDITION/." "$SITE/Content/"
(cd "$SITE/Content" && find . -type f ! -name files.txt | sed 's|^\./||' | LC_ALL=C sort > files.txt)

(cd "$SITE" && zip -qr -X "../../$EXE-web.zip" .)
echo "Built $SITE and $OUT/$EXE-web.zip"
