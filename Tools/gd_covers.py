"""Makes the GameDistribution image set from the "art" cover set (already cleaned by prepare_covers.py).
Sizes and format are what the GameDistribution Assets tab asks for: .jpg only.

Usage: python Tools/gd_covers.py
Output: Submission/covers/gamedistribution/blobby-stack-<w>x<h>.jpg
"""
import glob
import os

from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COVERS = os.path.join(ROOT, "Submission", "covers")
OUT = os.path.join(COVERS, "gamedistribution")

# (width, height, source, crop box in source pixels). The landscape art is 1920x1080 with the
# title on the left and Blobby in the middle; each crop keeps both and only trims the right side
# (towers, coins) or the empty space above and below.
SIZES = [
    (512, 384, "art-landscape-1920x1080.png", (0, 0, 1440, 1080)),   # required
    (512, 512, "art-square-800x800.png", None),                      # required
    (200, 120, "art-landscape-1920x1080.png", (0, 0, 1800, 1080)),   # required
    (1280, 720, "art-landscape-1920x1080.png", (0, 0, 1920, 1080)),  # marketing
    (1280, 550, "art-landscape-1920x1080.png", (0, 40, 1920, 865)),  # marketing
]


def main():
    os.makedirs(OUT, exist_ok=True)
    for old in glob.glob(os.path.join(OUT, "blobby-stack-*")):
        os.remove(old)
    for w, h, src, box in SIZES:
        im = Image.open(os.path.join(COVERS, src)).convert("RGB")
        if box:
            im = im.crop(box)
        out = im.resize((w, h), Image.LANCZOS)
        if w <= 512:
            out = out.filter(ImageFilter.UnsharpMask(radius=1.0, percent=40, threshold=2))
        path = os.path.join(OUT, f"blobby-stack-{w}x{h}.jpg")
        out.save(path, quality=92, optimize=True, progressive=False)
        print("wrote", path, out.size, os.path.getsize(path) // 1024, "KB")


if __name__ == "__main__":
    main()
