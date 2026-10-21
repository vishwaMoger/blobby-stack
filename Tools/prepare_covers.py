"""Prepares supplied cover art for CrazyGames: removes small corner marks, resizes to the exact
cover sizes and saves PNGs.

Usage: python Tools/prepare_covers.py <square> <portrait> <landscape> <set-name>
Output: Submission/covers/<set>-square-800x800.png, -portrait-800x1200.png, -landscape-1920x1080.png
        plus the untouched originals in Submission/covers/source/
"""
import os
import sys

from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COVERS = os.path.join(ROOT, "Submission", "covers")

# Image-generator watermark glyph in the landscape art's top-left corner (source pixel coordinates).
LANDSCAPE_MARKS = [(16, 8, 70, 62)]


def paint_out(im, box, iterations=60):
    """Fills `box` from its surroundings by repeated blurring (diffusion), then feathers the seam.
    Works well on soft, out-of-focus backgrounds like the blurred planet behind the mark."""
    x0, y0, x1, y1 = box
    pad = 24
    region = (max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad), min(im.height, y1 + pad))
    patch = im.crop(region)
    inner = (x0 - region[0], y0 - region[1], x1 - region[0], y1 - region[1])

    mask = Image.new("L", patch.size, 0)
    mask.paste(255, inner)
    work = patch.copy()
    for _ in range(iterations):
        blurred = work.filter(ImageFilter.GaussianBlur(4))
        work = Image.composite(blurred, patch, mask)

    feather = mask.filter(ImageFilter.GaussianBlur(5))
    result = Image.composite(work, patch, feather)
    out = im.copy()
    out.paste(result, region[:2])
    return out


def save(im, size, name, sharpen=False):
    out = im.resize(size, Image.LANCZOS)
    if sharpen:
        out = out.filter(ImageFilter.UnsharpMask(radius=1.2, percent=60, threshold=2))
    path = os.path.join(COVERS, name)
    out.save(path, optimize=True)
    print("wrote", path, out.size)


def main(square_path, portrait_path, landscape_path, set_name):
    os.makedirs(os.path.join(COVERS, "source"), exist_ok=True)
    square = Image.open(square_path).convert("RGB")
    portrait = Image.open(portrait_path).convert("RGB")
    landscape = Image.open(landscape_path).convert("RGB")
    for im, label in ((square, "square"), (portrait, "portrait"), (landscape, "landscape")):
        im.save(os.path.join(COVERS, "source", f"{set_name}-{label}-original.png"))

    for box in LANDSCAPE_MARKS:
        landscape = paint_out(landscape, box)

    save(square, (800, 800), f"{set_name}-square-800x800.png")
    save(portrait, (800, 1200), f"{set_name}-portrait-800x1200.png")
    # 1672x941 -> 1920x1080 is a ~15% upscale; a light unsharp mask keeps edges crisp.
    save(landscape, (1920, 1080), f"{set_name}-landscape-1920x1080.png", sharpen=True)


if __name__ == "__main__":
    main(*sys.argv[1:5])
