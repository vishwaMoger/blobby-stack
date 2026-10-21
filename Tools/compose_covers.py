"""Adds the game title to the raw cover renders and writes the three CrazyGames cover sizes.

Raw renders come from Unity (StackJumpSetup.CaptureCovers):
    Submission/covers/raw-<world>-landscape.png  1920x1080
    Submission/covers/raw-<world>-portrait.png   1600x2400 (2x)
    Submission/covers/raw-<world>-square.png     1600x1600 (2x)

Usage: python Tools/compose_covers.py cosmos farm
Output: Submission/covers/<world>-landscape-1920x1080.png, -portrait-800x1200.png, -square-800x800.png
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONT = os.path.join(ROOT, "Assets", "StackJump", "Resources", "Fonts", "LilitaOne-Regular.ttf")
COVERS = os.path.join(ROOT, "Submission", "covers")

WHITE = (255, 255, 255, 255)
GOLD = (255, 210, 58, 255)
OUTLINE = (42, 15, 79, 255)
SHADOW = (20, 5, 40, 150)

# (raw suffix, output name, final size, title lines, font size at render scale, title center y as a fraction)
LAYOUTS = [
    ("landscape", "landscape-1920x1080", (1920, 1080), [["BLOBBY ", "STACK"]], 200, 0.17),
    ("portrait", "portrait-800x1200", (800, 1200), [["BLOBBY"], ["STACK"]], 330, 0.15),
    ("square", "square-800x800", (800, 800), [["BLOBBY ", "STACK"]], 190, 0.14),
]


def title_layer(size, lines, font_size, center_y):
    """Renders the title (white BLOBBY, gold STACK) with a thick outline and soft drop shadow."""
    w, h = size
    font = ImageFont.truetype(FONT, font_size)
    stroke = max(6, font_size // 13)
    text = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(text)
    line_h = int(font_size * 1.0)
    total_h = line_h * len(lines)
    y = int(h * center_y - total_h / 2)
    colors = [WHITE, GOLD]
    color_index = 0
    for line in lines:
        widths = [draw.textlength(part, font=font) for part in line]
        x = (w - sum(widths)) / 2
        for part, pw in zip(line, widths):
            draw.text((x, y), part, font=font, fill=colors[color_index % 2], stroke_width=stroke, stroke_fill=OUTLINE)
            x += pw
            color_index += 1
        y += line_h

    # Shadow: the alpha of the title, offset down and blurred.
    alpha = text.split()[3]
    shadow = Image.new("RGBA", size, SHADOW)
    shadow.putalpha(alpha.point(lambda a: a * SHADOW[3] // 255))
    shadow = shadow.transform(size, Image.AFFINE, (1, 0, 0, 0, 1, -font_size // 9)).filter(ImageFilter.GaussianBlur(font_size // 30))

    layer = Image.alpha_composite(shadow, text)
    return layer.rotate(-3, resample=Image.BICUBIC, center=(w / 2, h * center_y))


def compose(world):
    for raw_suffix, out_name, final_size, lines, font_size, center_y in LAYOUTS:
        raw_path = os.path.join(COVERS, f"raw-{world}-{raw_suffix}.png")
        base = Image.open(raw_path).convert("RGBA")
        base = Image.alpha_composite(base, title_layer(base.size, lines, font_size, center_y))
        if base.size != final_size:
            base = base.resize(final_size, Image.LANCZOS)
        out = os.path.join(COVERS, f"{world}-{out_name}.png")
        base.convert("RGB").save(out, optimize=True)
        print("wrote", out, base.size)


if __name__ == "__main__":
    for world in sys.argv[1:] or ["cosmos"]:
        compose(world)
