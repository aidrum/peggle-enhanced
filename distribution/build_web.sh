#!/usr/bin/env bash
# Builds the web version (WebAssembly, played in a browser) as a static site.
#   distribution/build_web.sh [HD|iPhone]
# Output: distribution/out/web/<Exe>/ (the site: open it through a web server, e.g.
#         "python3 -m http.server -d distribution/out/web/<Exe>") and distribution/out/<Exe>-web.zip.
# Needs Python 3 with Pillow (pip install pillow) and the wasm-tools workload (dotnet workload install
# wasm-tools), which links SkiaSharp into the runtime. Release is compiled ahead of time, which takes a while;
# CONFIGURATION=Debug builds quicker but runs much slower.
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

# The game data, with the PNGs as WebP, and the list of files WebContentSource reads (a browser can't list
# a folder). WEBP_CACHE keeps the encoded images between builds.
python3 distribution/web_content.py --edition "$EDITION" --out "$SITE/Content" ${WEBP_CACHE:+--cache "$WEBP_CACHE"}

(cd "$SITE" && zip -qr -X "../../$EXE-web.zip" .)
echo "Built $SITE and $OUT/$EXE-web.zip"
