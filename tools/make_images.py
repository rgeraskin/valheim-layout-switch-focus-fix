# /// script
# requires-python = ">=3.11"
# dependencies = ["pillow>=11"]
# ///
"""Makes the mod page images from in-game screenshots (tools/harness/mac-game.sh + the companion's shot commands):
package/icon.png (256, Thunderstore) and package/logo.png (1024): a text field with Q ⇄ Й (one key in QWERTY and ЙЦУКЕН, letters only one alphabet has) and a caret over a meadow;
package/nexus-header.jpg (1300x372, Nexus Mods' size; content in the top ~200 px, Nexus covers the rest): the title and a word typed in the wrong layout, erased, retyped after Alt+Shift, the caret still there.
Titles are set in Cinzel (OFL) like the other mods; Cyrillic in Forum (OFL), which Cinzel lacks.

    uv run tools/make_images.py <shots dir> <fonts dir with Cinzel.ttf and Forum-Regular.ttf>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

GOLD = (255, 214, 140)
GOLD_DARK = (201, 160, 82)
GLOW = (255, 140, 40)
INK = (24, 18, 12)
PAPER = (232, 220, 196)

shots, fonts = Path(sys.argv[1]), Path(sys.argv[2])
out = Path(__file__).resolve().parent.parent / "package"


def cinzel(size: int, weight: str = "Bold") -> ImageFont.FreeTypeFont:
    f = ImageFont.truetype(str(fonts / "Cinzel.ttf"), size)
    f.set_variation_by_name(weight)
    return f


def forum(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(fonts / "Forum-Regular.ttf"), size)


def glow_text(img, xy, text, f, anchor="lm", radius=10, fill=GOLD, stroke=2):
    """Gold letters with a dark outline and a warm glow behind them."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).text(xy, text, font=f, anchor=anchor, fill=GLOW + (255,), stroke_width=stroke + 3, stroke_fill=GLOW + (255,))
    glow = layer.filter(ImageFilter.GaussianBlur(radius))
    img.alpha_composite(glow)
    img.alpha_composite(glow)
    ImageDraw.Draw(img).text(xy, text, font=f, anchor=anchor, fill=fill + (255,), stroke_width=stroke, stroke_fill=(40, 18, 6, 255))


def glow_rect(img, box, radius=8):
    """The caret: a gold bar with a glow, the sign that the field still has focus."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rectangle(box, fill=GLOW + (255,))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(radius)))
    ImageDraw.Draw(img).rectangle(box, fill=GOLD + (255,))


def panel(img, box, r):
    """A Valheim-like text field: dark, slightly see-through, with a worn gold rim."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0 + 6, y0 + 10, x1 + 6, y1 + 10), radius=r, fill=(0, 0, 0, 150))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(12)))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, radius=r, fill=(14, 11, 8, 225), outline=GOLD_DARK + (255,), width=max(2, r // 5))
    d.rounded_rectangle((x0 + r // 3, y0 + r // 3, x1 - r // 3, y1 - r // 3), radius=r * 2 // 3, outline=(90, 70, 40, 200), width=max(1, r // 12))


def swap_arrows(img, cx, cy, w, color):
    """⇄ drawn as two arrows: the layout switch."""
    d = ImageDraw.Draw(img)
    t, h, gap = max(3, w // 12), w // 4, w // 5
    d.line((cx - w // 2, cy - gap, cx + w // 2 - h, cy - gap), fill=color, width=t)
    d.polygon([(cx + w // 2 - h, cy - gap - h // 2 - t // 2), (cx + w // 2, cy - gap), (cx + w // 2 - h, cy - gap + h // 2 + t // 2)], fill=color)
    d.line((cx - w // 2 + h, cy + gap, cx + w // 2, cy + gap), fill=color, width=t)
    d.polygon([(cx - w // 2 + h, cy + gap - h // 2 - t // 2), (cx - w // 2, cy + gap), (cx - w // 2 + h, cy + gap + h // 2 + t // 2)], fill=color)


def arrow(img, points, color, t=4, head=13):
    """A path of straight segments ending in an arrowhead: the next step."""
    d = ImageDraw.Draw(img)
    (x0, y0), (x1, y1) = points[-2], points[-1]
    dx, dy = (x1 > x0) - (x1 < x0), (y1 > y0) - (y1 < y0)
    tip = points[:-1] + [(x1 - dx * head, y1 - dy * head)]
    d.line(tip, fill=color, width=t, joint="curve")
    px, py = -dy, dx  # perpendicular
    d.polygon([(x1, y1), (x1 - dx * head + px * head * 0.6, y1 - dy * head + py * head * 0.6),
               (x1 - dx * head - px * head * 0.6, y1 - dy * head - py * head * 0.6)], fill=color)


def shade(img, strength=150, left=None):
    """Darkens the picture evenly, or from the left edge fading out (left = fraction of the width)."""
    w, h = img.size
    if left is None:
        img.alpha_composite(Image.new("RGBA", img.size, (0, 0, 0, strength)))
        return
    mask = Image.new("L", (w, 1))
    for x in range(w):
        mask.putpixel((x, 0), int(strength * max(0.0, 1 - x / (w * left)) ** 1.3) + 40)
    img.alpha_composite(Image.merge("RGBA", (*Image.new("RGB", (w, h)).split(), mask.resize((w, h)))))


def vignette(img, strength=200):
    w, h = img.size
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).ellipse((-w * 0.25, -h * 0.25, w * 1.25, h * 1.25), fill=255)
    mask = Image.eval(mask.filter(ImageFilter.GaussianBlur(w // 6)), lambda v: int(strength * (1 - v / 255)))
    img.alpha_composite(Image.merge("RGBA", (*Image.new("RGB", (w, h)).split(), mask)))


def cover(path: Path, size, focus=(0.5, 0.5), zoom=1.0) -> Image.Image:
    """The shot scaled to fill size (times zoom), cropped around focus (fractions of the picture)."""
    im = Image.open(path).convert("RGB")
    s = max(size[0] / im.width, size[1] / im.height) * zoom
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    x = min(max(0, round(im.width * focus[0] - size[0] / 2)), im.width - size[0])
    y = min(max(0, round(im.height * focus[1] - size[1] / 2)), im.height - size[1])
    return im.crop((x, y, x + size[0], y + size[1])).convert("RGBA")


def logo():
    S = 1024
    img = cover(shots / "w1.png", (S, S), (0.45, 0.45)).filter(ImageFilter.GaussianBlur(5))
    shade(img, 120)
    vignette(img, 210)
    # the tile: rounded, with a gold rim
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle((16, 16, S - 16, S - 16), radius=150, fill=255)
    tile = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    tile.paste(img, (0, 0), mask)
    d = ImageDraw.Draw(tile)
    d.rounded_rectangle((16, 16, S - 16, S - 16), radius=150, outline=GOLD_DARK + (255,), width=22)
    d.rounded_rectangle((44, 44, S - 44, S - 44), radius=124, outline=(90, 66, 34, 255), width=6)

    panel(tile, (110, 300, S - 110, 650), 46)
    glow_text(tile, (278, 468), "Q", cinzel(250), anchor="mm", radius=14, stroke=4)
    swap_arrows(tile, 512, 478, 150, GOLD_DARK + (255,))
    glow_text(tile, (712, 492), "Й", forum(270), anchor="mm", radius=14, stroke=4)
    glow_rect(tile, (848, 352, 868, 600), radius=14)

    glow_text(tile, (S // 2, 820), "KEEP TYPING", cinzel(118), anchor="mm", radius=12, stroke=4)
    tile.save(out / "logo.png")
    tile.resize((256, 256), Image.LANCZOS).save(out / "icon.png")


def keycap(img, x, y, label, f, h=64):
    d = ImageDraw.Draw(img)
    w = round(d.textlength(label, font=f)) + 44
    d.rounded_rectangle((x + 3, y + 6, x + w + 3, y + h + 6), radius=12, fill=(0, 0, 0, 170))
    d.rounded_rectangle((x, y, x + w, y + h), radius=12, fill=(48, 38, 26, 245), outline=GOLD_DARK + (255,), width=3)
    d.text((x + w / 2, y + h / 2), label, font=f, anchor="mm", fill=PAPER + (255,))
    return x + w


def header():
    W, H = 1300, 372  # Nexus Mods' recommended header size
    # zoomed in so the player stays below the frame
    img = cover(shots / "w1.png", (W, H), (0.5, 0.2), zoom=1.3)
    shade(img, 225, left=0.6)
    vignette(img, 110)
    # Nexus lays the mod's name and buttons over the lower part: everything stays in the top ~200 px
    glow_text(img, (56, 62), "LAYOUT SWITCH", cinzel(58), radius=10, stroke=3)
    glow_text(img, (56, 130), "FOCUS FIX", cinzel(58), radius=10, stroke=3)
    d = ImageDraw.Draw(img)
    d.text((59, 190), "Switch the keyboard layout and keep typing", font=forum(28), anchor="lm", fill=PAPER + (255,))

    # what a player sees, step by step: a word typed in the wrong layout, erased, the layout switched, the word
    # typed again, and the caret still in the field
    f = forum(26)
    color = GOLD_DARK + (255,)
    d = ImageDraw.Draw(img)
    x, y0, y1 = 752, 36, 80
    cy = (y0 + y1) // 2
    w = round(d.textlength("Ujhs", font=forum(30)))
    panel(img, (x, y0, x + w + 36, y1), 12)
    d = ImageDraw.Draw(img)
    d.text((x + 18, cy), "Ujhs", font=forum(30), anchor="lm", fill=(170, 150, 120, 255))
    d.line((x + 14, cy + 1, x + w + 22, cy + 1), fill=(220, 120, 90, 255), width=3)
    x += w + 36
    for k in ["Backspace ×4", "Alt+Shift"]:
        arrow(img, [(x + 8, cy), (x + 40, cy)], color)
        left = x + 48
        x = keycap(img, left, y0, k, f, h=y1 - y0)
    right, shift_mid = x, (left + x) // 2
    line = forum(34)
    caret = 776 + round(d.textlength("Горы", font=line)) + 6
    # from Alt+Shift down into the field, to where the typing goes on
    arrow(img, [(shift_mid, y1 + 12), (shift_mid, 104), (caret + 2, 104), (caret + 2, 130)], color)
    panel(img, (752, 134, right, 198), 16)
    d = ImageDraw.Draw(img)
    d.text((776, 166), "Горы", font=line, anchor="lm", fill=PAPER + (255,))
    glow_rect(img, (caret, 148, caret + 4, 184), radius=5)
    img.convert("RGB").save(out / "nexus-header.jpg", quality=92)


logo()
header()
print("package/icon.png, logo.png, nexus-header.jpg")
