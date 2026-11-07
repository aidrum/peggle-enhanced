#!/usr/bin/env python3
"""
Lays out the web version's game data (distribution/build_web.sh):

    web_content.py --edition HD|iPhone --out <site>/Content [--cache DIR] [--jobs N]

The edition's data goes in --out, the shared files first so the edition's own replace them (as in the desktop
builds). Images are converted to WebP, which the web version decodes with SkiaSharp: smaller downloads,
faster decoding. Content/files.txt lists every file by the path the game asks for (a browser can't list a
folder); a converted file's line also gives its url ("images/x.png<TAB>Content/images/x.webp"), so the game
keeps asking for the original names (Peggle.Web/WebContentSource.cs).

PNGs follow cuttherope-dx: lossy at quality 80 when that's at most 60% of the PNG's size, otherwise whichever
of lossy and lossless is smaller. Alpha masks ("name_.png", "_name.png", which become an image's alpha
channel) are always lossless, so edges stay exact; so is every image's alpha channel. JPEGs are already
lossy, so they're re-encoded at quality 90 and only kept as WebP when that's at most 85% of the JPEG's size.
GIFs and everything else are copied as they are.

--cache keeps each encoding by the image's contents, so unchanged images aren't encoded again (CI keeps it
between runs). Needs Pillow with WebP support (pip install pillow).
"""

import argparse
import hashlib
import io
import os
import shutil
import sys
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path
from urllib.parse import quote

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
QUALITY = 80
LOSSLESS_FALLBACK_RATIO = 0.60
JPEG_QUALITY = 90
JPEG_KEEP_RATIO = 0.85
METHOD = 4
# Part of the cache key: change it when the encoding rules change.
SETTINGS = b"webp:q80+ll60+mask-lossless+exact+jpg-q90-r85:v2"
FILE_LIST = "files.txt"
TAB = "\t"


def is_alpha_mask(rel):
    stem = Path(rel).stem
    return stem.endswith("_") or stem.startswith("_")


def is_jpeg(rel):
    return rel.lower().endswith((".jpg", ".jpeg"))


def is_image(rel):
    return rel.lower().endswith(".png") or is_jpeg(rel)


def encode_png(source, mask):
    image = Image.open(source)
    if mask:
        image = image.convert("RGB")
    else:
        image = image.convert("RGBA")
        if image.getchannel("A").getextrema()[0] == 255:
            image = image.convert("RGB")

    def save(**options):
        buffer = io.BytesIO()
        image.save(buffer, "WEBP", method=METHOD, exact=True, **options)
        return buffer.getvalue()

    if mask:
        return save(lossless=True)
    lossy = save(quality=QUALITY, alpha_quality=100)
    if len(lossy) <= LOSSLESS_FALLBACK_RATIO * os.path.getsize(source):
        return lossy
    lossless = save(lossless=True)
    return lossless if len(lossless) < len(lossy) else lossy


def encode_jpeg(source):
    """A JPEG as WebP, or empty when that isn't worth a second lossy encoding."""
    buffer = io.BytesIO()
    Image.open(source).convert("RGB").save(buffer, "WEBP", method=METHOD, quality=JPEG_QUALITY)
    data = buffer.getvalue()
    return data if len(data) <= JPEG_KEEP_RATIO * os.path.getsize(source) else b""


def convert(job):
    """
    Encodes one image (in a worker process) and writes the WebP. Returns its size (0 when the original is
    kept) and whether the cache had it; an empty cache entry means "keep the original".
    """
    rel, source, target, cache = job
    mask = is_alpha_mask(rel)
    cached = None
    data = None
    hit = False
    if cache:
        key = hashlib.sha256(SETTINGS + (b"mask" if mask else b"") + Path(source).read_bytes()).hexdigest()
        cached = Path(cache) / key[:2] / (key + ".webp")
        if cached.exists():
            data = cached.read_bytes()
            hit = True
    if data is None:
        data = encode_jpeg(source) if is_jpeg(rel) else encode_png(source, mask)
        if cached is not None:
            cached.parent.mkdir(parents=True, exist_ok=True)
            tmp = cached.with_suffix(".tmp%d" % os.getpid())
            tmp.write_bytes(data)
            os.replace(tmp, cached)
    if data:
        Path(target).write_bytes(data)
    return len(data), hit


def url_of(rel):
    return "Content/" + "/".join(quote(part) for part in rel.split("/"))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--edition", required=True, choices=["HD", "iPhone"])
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--cache", type=Path)
    parser.add_argument("--jobs", type=int, default=os.cpu_count())
    args = parser.parse_args()

    files = {}
    for folder in ("Content.Shared", "Content." + args.edition):
        base = ROOT / "Peggle" / folder
        for path in base.rglob("*"):
            if path.is_file():
                files[path.relative_to(base).as_posix()] = path

    out = args.out
    if out.exists():
        shutil.rmtree(out)
    out.mkdir(parents=True)
    cache = str(args.cache) if args.cache else None
    jobs = []
    for rel in sorted(files):
        target = out / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        if is_image(rel):
            jobs.append((rel, str(files[rel]), str(target.with_suffix(".webp")), cache))
        else:
            shutil.copyfile(files[rel], target)

    with ProcessPoolExecutor(max_workers=args.jobs) as pool:
        results = dict(zip((job[0] for job in jobs), pool.map(convert, jobs, chunksize=4)))

    lines = []
    for rel in sorted(files):
        size = results.get(rel, (0, False))[0]
        if size:
            lines.append(rel + TAB + url_of(rel[:rel.rindex(".")] + ".webp"))
        else:
            if rel in results:
                # An image kept as it was (a JPEG not worth converting).
                shutil.copyfile(files[rel], out / rel)
            lines.append(rel)
    (out / FILE_LIST).write_text("\n".join(lines) + "\n", encoding="utf-8")

    before = sum(os.path.getsize(job[1]) for job in jobs)
    after = sum(size if size else os.path.getsize(files[rel]) for rel, (size, _) in results.items())
    converted = sum(1 for size, _ in results.values() if size)
    hits = sum(1 for _, hit in results.values() if hit)
    print(f"{len(files)} files; {converted} of {len(jobs)} images as WebP ({hits} from the cache): "
          f"{before / 1e6:.1f} MB -> {after / 1e6:.1f} MB")


if __name__ == "__main__":
    sys.exit(main())
