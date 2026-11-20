"""Makes the itch.io image set from the "art" covers and the untitled in-engine renders.

Usage: python Tools/itch_images.py
Output: Submission/itch/
  cover-630x500.png        game cover (itch recommends 630x500, minimum 315x250)
  banner-1920x1080.jpg     page banner / background art
  screenshot-1..4.jpg      1920x1080 gameplay renders (itch shows 3-5)
"""
import os

from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COVERS = os.path.join(ROOT, "Submission", "covers")
OUT = os.path.join(ROOT, "Submission", "itch")


def main():
    os.makedirs(OUT, exist_ok=True)

    art = Image.open(os.path.join(COVERS, "art-landscape-1920x1080.png")).convert("RGB")
    # 630x500 is nearly square: keep the title and Blobby, trim the towers on the right.
    cover = art.crop((0, 0, 1361, 1080)).resize((630, 500), Image.LANCZOS)
    cover = cover.filter(ImageFilter.UnsharpMask(radius=1.0, percent=40, threshold=2))
    cover.save(os.path.join(OUT, "cover-630x500.png"), optimize=True)

    art.save(os.path.join(OUT, "banner-1920x1080.jpg"), quality=90, optimize=True)

    for i, world in enumerate(("cosmos", "farm", "neon", "beanstalk"), 1):
        shot = Image.open(os.path.join(COVERS, f"raw-{world}-landscape.png")).convert("RGB")
        shot = shot.resize((1920, 1080), Image.LANCZOS)
        shot.save(os.path.join(OUT, f"screenshot-{i}.jpg"), quality=90, optimize=True)

    for name in sorted(os.listdir(OUT)):
        path = os.path.join(OUT, name)
        with Image.open(path) as im:
            print(f"{name}: {im.size[0]}x{im.size[1]}, {os.path.getsize(path) // 1024} KB")


if __name__ == "__main__":
    main()
