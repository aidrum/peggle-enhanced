#!/usr/bin/env python3
"""
Builds the AltStore / SideStore source (the GitHub Pages site) from the repository's GitHub releases.

    make_source.py --repo OWNER/NAME --releases releases.json --out site/

releases.json is the GitHub API's release list (gh api repos/OWNER/NAME/releases --paginate --slurp,
or a plain list). Every release made by the build workflow is named
"Peggle Enhanced <version> (build <n>)" and carries the unsigned IPAs; each one becomes a version
of the apps in the source, newest first. The site gets:
    apps.json   the source (AltStore source format v2, plus the older top-level fields SideStore and
                older AltStore versions read)
    index.html  a page with "add source" links and the latest downloads
    *.png       the app icons
Only the standard library is used, so it runs on any CI runner.
"""
import argparse
import html
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAX_VERSIONS = 15

APPS = [
    {
        "edition": "HD",
        "name": "Peggle HD",
        "bundleIdentifier": "com.aidrum.peggle.hd",
        "ipa": "PeggleHD-unsigned.ipa",
        "subtitle": "Peggle Classic HD, ported to MonoGame.",
        "description": (
            "The iPad version of Peggle (Peggle Classic HD 2.0.6), decompiled and ported to MonoGame. "
            "Includes the complete Peggle, Peggle Nights, Peggle Extreme and Peggle World of Warcraft "
            "adventures."
        ),
        "icon": ROOT / "Peggle.iOS/Icons/HD/Assets.xcassets/AppIcon.appiconset/icon_1024.png",
        "iconFile": "icon-hd.png",
    },
    {
        "edition": "iPhone",
        "name": "Peggle",
        "bundleIdentifier": "com.aidrum.peggle",
        "ipa": "Peggle-unsigned.ipa",
        "subtitle": "Peggle Classic, ported to MonoGame.",
        "description": (
            "The iPhone version of Peggle (Peggle Classic 1.7.2), decompiled and ported to MonoGame, "
            "with ultrawide support. Includes the complete Peggle, Peggle Nights, Peggle Extreme and "
            "Peggle World of Warcraft adventures."
        ),
        "icon": ROOT / "Peggle.iOS/Icons/iPhone/Assets.xcassets/AppIcon.appiconset/icon_1024.png",
        "iconFile": "icon.png",
    },
]

TINT = "E8652B"
MIN_OS = "15.0"
NAME_RE = re.compile(r"Peggle Enhanced (\S+) \(build (\d+)\)")


def load_releases(path):
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    # gh api --paginate --slurp gives a list of pages.
    if data and isinstance(data[0], list):
        data = [r for page in data for r in page]
    return data


def plain_notes(body):
    """Release notes as plain text for the app's version history."""
    text = body or ""
    text = re.sub(r"<!--.*?-->", "", text, flags=re.S)
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)  # [label](url) -> label
    text = re.sub(r"^#+\s*", "", text, flags=re.M)
    text = re.sub(r"\*\*|__|`", "", text)
    text = re.sub(r"\n{3,}", "\n\n", text).strip()
    return text[:1500]


def versions_for(app, releases):
    out = []
    for rel in releases:
        if rel.get("draft"):
            continue
        m = NAME_RE.search(rel.get("name") or "")
        if not m:
            continue
        asset = next((a for a in rel.get("assets", []) if a.get("name") == app["ipa"]), None)
        if asset is None:
            continue
        out.append({
            "version": m.group(1),
            "buildVersion": m.group(2),
            "date": rel.get("published_at") or rel.get("created_at"),
            "localizedDescription": plain_notes(rel.get("body")) or f"Build {m.group(2)}.",
            "downloadURL": asset["browser_download_url"],
            "size": asset.get("size", 0),
            "minOSVersion": MIN_OS,
        })
    out.sort(key=lambda v: int(v["buildVersion"]), reverse=True)
    return out[:MAX_VERSIONS]


def build(repo, releases, out_dir):
    owner, name = repo.split("/", 1)
    site = f"https://{owner.lower()}.github.io/{name}"
    out_dir.mkdir(parents=True, exist_ok=True)

    apps = []
    for app in APPS:
        shutil.copyfile(app["icon"], out_dir / app["iconFile"])
        versions = versions_for(app, releases)
        entry = {
            "name": app["name"],
            "bundleIdentifier": app["bundleIdentifier"],
            "developerName": "aidrum & TheAwesomeBlue",
            "subtitle": app["subtitle"],
            "localizedDescription": app["description"],
            "iconURL": f"{site}/{app['iconFile']}",
            "tintColor": TINT,
            "category": "games",
            "screenshots": [],
            "versions": versions,
            "appPermissions": {"entitlements": [], "privacy": {}},
        }
        if versions:
            # Older AltStore versions and some SideStore builds read the latest version from these.
            latest = versions[0]
            entry.update({
                "version": latest["version"],
                "versionDate": latest["date"],
                "versionDescription": latest["localizedDescription"],
                "downloadURL": latest["downloadURL"],
                "size": latest["size"],
            })
        apps.append(entry)

    source = {
        "name": "Peggle Enhanced",
        "identifier": f"io.github.{owner.lower()}.{name.lower()}",
        "subtitle": "Peggle, natively ported to modern iPhones and iPads.",
        "description": "Unsigned builds of Peggle Enhanced straight from GitHub Actions.",
        "iconURL": f"{site}/icon-hd.png",
        "website": f"https://github.com/{repo}",
        "tintColor": TINT,
        "featuredApps": [a["bundleIdentifier"] for a in APPS],
        "apps": apps,
        "news": [],
    }
    (out_dir / "apps.json").write_text(json.dumps(source, indent=2) + "\n", encoding="utf-8")
    (out_dir / "index.html").write_text(index_html(repo, site, apps), encoding="utf-8")
    (out_dir / ".nojekyll").write_text("", encoding="utf-8")
    return source


def index_html(repo, site, apps):
    src = f"{site}/apps.json"
    rows = []
    for app in apps:
        v = app["versions"][0] if app["versions"] else None
        if v:
            dl = (f'<a href="{html.escape(v["downloadURL"])}">Download IPA</a> '
                  f'· {html.escape(v["version"])} (build {html.escape(v["buildVersion"])}) '
                  f'· {v["size"] / 1048576:.0f} MB')
        else:
            dl = "No build yet."
        rows.append(
            f'<div class="app"><img src="{html.escape(app["iconURL"].rsplit("/", 1)[-1])}" alt="">'
            f'<div><h2>{html.escape(app["name"])}</h2><p>{html.escape(app["subtitle"])}</p>'
            f'<p>{dl}</p></div></div>'
        )
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Peggle Enhanced for iOS</title>
<style>
  body {{ font-family: -apple-system, system-ui, sans-serif; max-width: 40rem; margin: 2rem auto; padding: 0 1rem; line-height: 1.5; }}
  .buttons a {{ display: inline-block; margin: 0 .5rem .5rem 0; padding: .6rem 1rem; border-radius: .6rem;
               background: #{TINT}; color: #fff; text-decoration: none; font-weight: 600; }}
  .app {{ display: flex; gap: 1rem; align-items: center; margin: 1.5rem 0; }}
  .app img {{ width: 72px; height: 72px; border-radius: 16px; }}
  .app h2 {{ margin: 0; font-size: 1.15rem; }} .app p {{ margin: .2rem 0; }}
  code {{ word-break: break-all; }}
</style>
</head>
<body>
<h1>Peggle Enhanced for iOS</h1>
<p>Add this source to AltStore or SideStore to install Peggle Enhanced and get new builds as updates.</p>
<p class="buttons">
  <a href="altstore://source?url={html.escape(src)}">Add to AltStore</a>
  <a href="sidestore://source?url={html.escape(src)}">Add to SideStore</a>
</p>
<p>Or add the source by hand: <code>{html.escape(src)}</code></p>
{''.join(rows)}
<p>Other platforms (Android, Windows, macOS, Linux): <a href="https://github.com/{html.escape(repo)}/releases/latest">latest release</a>.</p>
</body>
</html>
"""


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--repo", required=True, help="OWNER/NAME")
    ap.add_argument("--releases", required=True, help="JSON release list from the GitHub API")
    ap.add_argument("--out", required=True, type=Path, help="output folder")
    args = ap.parse_args()
    source = build(args.repo, load_releases(args.releases), args.out)
    for app in source["apps"]:
        print(f"{app['name']}: {len(app['versions'])} version(s)")


if __name__ == "__main__":
    main()
