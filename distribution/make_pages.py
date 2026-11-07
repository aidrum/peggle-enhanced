#!/usr/bin/env python3
"""
Assembles the GitHub Pages site from the repository's GitHub releases (the Pages workflow).

    make_pages.py --repo OWNER/NAME --out site/

    /                  the web version of the latest full release, iPhone edition (Peggle-web.zip)
    /hd/               the same, HD edition (PeggleHD-web.zip)
    /pre-release/      the web version of the newest pre-release, iPhone edition
    /pre-release/hd/   the same, HD edition
    /ios/              the AltStore / SideStore source (altstore/make_source.py)

Each Pages deployment replaces the whole site, so every part is rebuilt here from the releases, which keep
each build's web zips (distribution/build_web.sh). Without a release (or pre-release) that has them, its
paths lead to the other one.

The four web builds would exceed Pages' 1 GB limit together, so their game data is moved into one folder,
/data/, with each file named after its contents; a build's Content/files.txt then maps every game path to
its file there ("path<TAB>url", read by Peggle.Web/WebContentSource.cs). Files the builds share (the data
both editions use, and whatever didn't change between the release and the pre-release) are kept once, and
stay in the browser's cache from one version to the next.

Needs the gh CLI (authenticated, e.g. GH_TOKEN) and Python 3's standard library.
"""

import argparse
import hashlib
import html
import json
import shutil
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path
from urllib.parse import unquote

HERE = Path(__file__).resolve().parent

# (release asset, folder under the release's path)
EDITIONS = [("Peggle-web.zip", ""), ("PeggleHD-web.zip", "hd")]
PRE_RELEASE_DIR = "pre-release"
DATA_DIR = "data"
FILE_LIST = "files.txt"


def gh_json(*args):
    return json.loads(subprocess.run(["gh", *args], check=True, capture_output=True, text=True).stdout)


def has_web(release):
    names = {asset["name"] for asset in release.get("assets", [])}
    return all(name in names for name, _ in EDITIONS)


def file_hash(path):
    digest = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()[:32]


def install(repo, tag, asset, dest, site, tmp):
    """Unpacks a release's web zip into dest, moving its game data into the shared data folder."""
    download = tmp / tag
    download.mkdir(parents=True, exist_ok=True)
    subprocess.run(["gh", "release", "download", tag, "-R", repo, "-p", asset, "-D", str(download), "--clobber"], check=True)
    dest.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(download / asset) as archive:
        archive.extractall(dest)
    (download / asset).unlink()

    content = dest / "Content"
    to_site = "../" * len(dest.relative_to(site).parts)
    data = site / DATA_DIR
    data.mkdir(exist_ok=True)
    lines = []
    for line in (content / FILE_LIST).read_text(encoding="utf-8").splitlines():
        fields = line.split("\t")
        rel = fields[0].strip()
        if not rel:
            continue
        # A file stored under another name (a PNG converted to WebP) gives its url, relative to the page.
        if len(fields) > 1 and fields[1]:
            source = dest.joinpath(*unquote(fields[1]).split("/"))
        else:
            source = content.joinpath(*rel.split("/"))
        name = file_hash(source) + source.suffix.lower()
        target = data / name
        if target.exists():
            source.unlink()
        else:
            shutil.move(str(source), str(target))
        lines.append(f"{rel}\t{to_site}{DATA_DIR}/{name}")
    shutil.rmtree(content)
    content.mkdir()
    (content / FILE_LIST).write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"{tag}: {asset} -> /{dest.relative_to(site).as_posix()} ({len(lines)} files)")


def redirect(dest, target, title):
    """A page that sends the visitor on to target (a path relative to dest)."""
    dest.mkdir(parents=True, exist_ok=True)
    url = html.escape(target, quote=True)
    (dest / "index.html").write_text(
        "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\" />\n"
        f"<meta http-equiv=\"refresh\" content=\"0; url={url}\" />\n<title>{html.escape(title)}</title>\n"
        f"</head>\n<body>\n<p><a href=\"{url}\">{html.escape(title)}</a></p>\n</body>\n</html>\n",
        encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--repo", required=True, help="OWNER/NAME")
    parser.add_argument("--out", required=True, type=Path, help="the site folder (replaced)")
    args = parser.parse_args()

    site = args.out
    if site.exists():
        shutil.rmtree(site)
    site.mkdir(parents=True)
    (site / ".nojekyll").touch()

    with tempfile.TemporaryDirectory() as tmp_name:
        tmp = Path(tmp_name)
        releases = gh_json("api", f"repos/{args.repo}/releases?per_page=100")
        releases_file = tmp / "releases.json"
        releases_file.write_text(json.dumps(releases), encoding="utf-8")

        # The API lists the newest first.
        published = [r for r in releases if not r.get("draft")]
        release = next((r for r in published if not r.get("prerelease") and has_web(r)), None)
        pre_release = next((r for r in published if r.get("prerelease") and has_web(r)), None)
        if release is None and pre_release is None:
            print("No release has the web version yet.", file=sys.stderr)

        for asset, folder in EDITIONS:
            dest = site / folder if folder else site
            pre_dest = site / PRE_RELEASE_DIR / folder if folder else site / PRE_RELEASE_DIR
            name = "Peggle HD" if folder else "Peggle"
            if release is not None:
                install(args.repo, release["tag_name"], asset, dest, site, tmp)
            elif pre_release is not None:
                redirect(dest, "../" * len(dest.relative_to(site).parts) + f"{PRE_RELEASE_DIR}/{folder}", name)
            if pre_release is not None:
                install(args.repo, pre_release["tag_name"], asset, pre_dest, site, tmp)
            elif release is not None:
                redirect(pre_dest, "../" * len(pre_dest.relative_to(site).parts) + folder, name)

        subprocess.run([sys.executable, str(HERE / "altstore" / "make_source.py"), "--repo", args.repo,
                        "--releases", str(releases_file), "--out", str(site / "ios")], check=True)


if __name__ == "__main__":
    main()
