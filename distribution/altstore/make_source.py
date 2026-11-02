#!/usr/bin/env python3
"""
Builds the AltStore / SideStore source (the GitHub Pages site) from the repository's GitHub releases.

    make_source.py --repo OWNER/NAME --releases releases.json --out site/

releases.json is a release list from the GitHub API (gh api repos/OWNER/NAME/releases). Every release
the Build workflow publishes carries the unsigned IPAs and, in its notes, a hidden
"<!-- peggle-build version=<v> build=<n> -->" line with the IPAs' CFBundleShortVersionString and
CFBundleVersion; each such release becomes a version of the apps in the source, newest build first.
Releases from before that line existed are read from their old "Peggle Enhanced <v> (build <n>)" title.
The output folder gets:

    apps.json   the source (AltStore source format v2, plus the older top-level version fields that
                SideStore and older AltStore versions read)
    index.html  a page with "add source" links and the latest downloads (from index.html here)
    *.png       the app icons

Only the standard library is used, so it runs on any CI runner.
"""

import argparse
import html
import json
import re
import shutil
from pathlib import Path
from string import Template

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]

MAX_VERSIONS = 15
MIN_OS_VERSION = "15.0"
TINT_COLOR = "E8652B"
DEVELOPER = "aidrum & TheAwesomeBlue"
# The Build workflow's marker in each release's notes, and the older releases' title format.
BUILD_MARKER = re.compile(r"<!-- peggle-build version=(?P<version>\S+) build=(?P<build>\d+) -->")
LEGACY_RELEASE_NAME = re.compile(r"Peggle Enhanced (?P<version>\S+) \(build (?P<build>\d+)\)")

ADVENTURES = "the complete Peggle, Peggle Nights, Peggle Extreme and Peggle World of Warcraft adventures"
APPS = [
    {
        "name": "Peggle HD",
        "bundleIdentifier": "com.aidrum.peggle.hd",
        "ipa": "PeggleHD-unsigned.ipa",
        "icon": "Peggle.iOS/Icons/HD/Assets.xcassets/AppIcon.appiconset/icon_1024.png",
        "iconFile": "icon-hd.png",
        "subtitle": "Peggle Classic HD, ported to MonoGame.",
        "description": (
            "The iPad version of Peggle (Peggle Classic HD 2.0.6), decompiled and ported to MonoGame. "
            f"Includes {ADVENTURES}."
        ),
    },
    {
        "name": "Peggle",
        "bundleIdentifier": "com.aidrum.peggle",
        "ipa": "Peggle-unsigned.ipa",
        "icon": "Peggle.iOS/Icons/iPhone/Assets.xcassets/AppIcon.appiconset/icon_1024.png",
        "iconFile": "icon.png",
        "subtitle": "Peggle Classic, ported to MonoGame.",
        "description": (
            "The iPhone version of Peggle (Peggle Classic 1.7.2), decompiled and ported to MonoGame, "
            f"with ultrawide support. Includes {ADVENTURES}."
        ),
    },
]


def load_releases(path):
    """Reads a release list; `gh api --paginate --slurp` output (a list of pages) is flattened."""
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    if data and isinstance(data[0], list):
        data = [release for page in data for release in page]
    return data


def plain_text(markdown):
    """GitHub's generated release notes, reduced to plain text for the app's version history."""
    text = markdown or ""
    text = re.sub(r"<!--.*?-->", "", text, flags=re.DOTALL)
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)  # [label](url) -> label
    text = re.sub(r"^#+\s*", "", text, flags=re.MULTILINE)
    text = re.sub(r"\*\*|__|`", "", text)
    text = re.sub(r"\n{3,}", "\n\n", text).strip()
    return text[:1500]


def app_versions(app, releases):
    """The app's versions, one per release that has its IPA, newest build first."""
    versions = []
    for release in releases:
        name = BUILD_MARKER.search(release.get("body") or "") or LEGACY_RELEASE_NAME.search(release.get("name") or "")
        if release.get("draft") or not name:
            continue
        ipa = next((a for a in release.get("assets", []) if a.get("name") == app["ipa"]), None)
        if ipa is None:
            continue
        versions.append(
            {
                "version": name["version"],
                "buildVersion": name["build"],
                "date": release.get("published_at") or release.get("created_at"),
                "localizedDescription": plain_text(release.get("body")) or f"Build {name['build']}.",
                "downloadURL": ipa["browser_download_url"],
                "size": ipa.get("size", 0),
                "minOSVersion": MIN_OS_VERSION,
            }
        )
    versions.sort(key=lambda v: int(v["buildVersion"]), reverse=True)
    return versions[:MAX_VERSIONS]


def source_app(app, site_url, releases):
    """One app entry of the source."""
    versions = app_versions(app, releases)
    entry = {
        "name": app["name"],
        "bundleIdentifier": app["bundleIdentifier"],
        "developerName": DEVELOPER,
        "subtitle": app["subtitle"],
        "localizedDescription": app["description"],
        "iconURL": f"{site_url}/{app['iconFile']}",
        "tintColor": TINT_COLOR,
        "category": "games",
        "screenshots": [],
        "versions": versions,
        "appPermissions": {"entitlements": [], "privacy": {}},
    }
    if versions:
        # The pre-v2 format: SideStore and older AltStore versions read the latest version from these.
        latest = versions[0]
        entry.update(
            {
                "version": latest["version"],
                "versionDate": latest["date"],
                "versionDescription": latest["localizedDescription"],
                "downloadURL": latest["downloadURL"],
                "size": latest["size"],
            }
        )
    return entry


def app_html(app, entry):
    """The install page's block for one app: icon, name and the latest IPA."""
    if entry["versions"]:
        latest = entry["versions"][0]
        download = (
            f'<a href="{html.escape(latest["downloadURL"])}">Download IPA</a>'
            f" · {html.escape(latest['version'])} (build {html.escape(latest['buildVersion'])})"
            f" · {latest['size'] / 1048576:.0f} MB"
        )
    else:
        download = "No build yet."
    return (
        f'<div class="app"><img src="{html.escape(app["iconFile"])}" alt="">'
        f"<div><h2>{html.escape(app['name'])}</h2><p>{html.escape(app['subtitle'])}</p>"
        f"<p>{download}</p></div></div>"
    )


def build_site(repo, releases, out_dir):
    """Writes apps.json, index.html and the icons to out_dir; returns the source."""
    owner, name = repo.split("/", 1)
    site_url = f"https://{owner.lower()}.github.io/{name}"
    out_dir.mkdir(parents=True, exist_ok=True)

    entries = [source_app(app, site_url, releases) for app in APPS]
    source = {
        "name": "Peggle Enhanced",
        "identifier": f"io.github.{owner.lower()}.{name.lower()}",
        "subtitle": "Peggle, natively ported to modern iPhones and iPads.",
        "description": "Unsigned builds of Peggle Enhanced straight from GitHub Actions.",
        "iconURL": f"{site_url}/{APPS[0]['iconFile']}",
        "website": f"https://github.com/{repo}",
        "tintColor": TINT_COLOR,
        "featuredApps": [app["bundleIdentifier"] for app in APPS],
        "apps": entries,
        "news": [],
    }
    (out_dir / "apps.json").write_text(json.dumps(source, indent=2) + "\n", encoding="utf-8")

    page = Template((HERE / "index.html").read_text(encoding="utf-8")).substitute(
        tint=TINT_COLOR,
        source_url=html.escape(f"{site_url}/apps.json"),
        apps="\n".join(app_html(app, entry) for app, entry in zip(APPS, entries, strict=True)),
        releases_url=html.escape(f"https://github.com/{repo}/releases/latest"),
    )
    (out_dir / "index.html").write_text(page, encoding="utf-8")

    for app in APPS:
        shutil.copyfile(ROOT / app["icon"], out_dir / app["iconFile"])
    # Serve the files as they are (no Jekyll processing).
    (out_dir / ".nojekyll").touch()
    return source


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--repo", required=True, help="OWNER/NAME")
    parser.add_argument("--releases", required=True, help="release list (JSON) from the GitHub API")
    parser.add_argument("--out", required=True, type=Path, help="output folder")
    args = parser.parse_args()

    source = build_site(args.repo, load_releases(args.releases), args.out)
    for app in source["apps"]:
        print(f"{app['name']}: {len(app['versions'])} version(s)")


if __name__ == "__main__":
    main()
