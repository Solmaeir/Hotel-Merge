"""itch.io page art: cover (1260x1000, shown at 630x500), banner (1920x540) and page background (1920x1080).
Reuses the app icon's hotel and sunburst plus the game's logo and item sprites. Writes PNGs to Marketing/itchio."""
import base64
import math
import os
import random
import subprocess
import tempfile

from make_icon import DEFS, INK, hotel, star

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ART = os.path.join(ROOT, "Assets", "Resources", "Art")
OUT = os.path.join(ROOT, "Marketing", "itchio")


def png(path):
    with open(os.path.join(ART, path), "rb") as f:
        return "data:image/png;base64," + base64.b64encode(f.read()).decode()


def image(path, x, y, w, h, rot=0, opacity=1):
    cx, cy = x + w / 2, y + h / 2
    return (f'<image href="{png(path)}" x="{x:.0f}" y="{y:.0f}" width="{w:.0f}" height="{h:.0f}" opacity="{opacity}" '
            f'transform="rotate({rot} {cx:.0f} {cy:.0f})" preserveAspectRatio="xMidYMid meet"/>')


def item(name, cx, cy, size, rot=0):
    """Item sprite on a soft white glow, like on the merge board."""
    return (f'<circle cx="{cx}" cy="{cy}" r="{size * 0.46:.0f}" fill="#fff" opacity=".35"/>'
            + image(f"Items/{name}.png", cx - size / 2, cy - size / 2, size, size, rot))


def sunburst(w, h, cx, cy):
    reach = math.hypot(w, h)
    rays = "".join(
        f'<path d="M{cx} {cy} L{cx + reach * math.cos(math.radians(a - 6)):.0f} {cy + reach * math.sin(math.radians(a - 6)):.0f} '
        f'L{cx + reach * math.cos(math.radians(a + 6)):.0f} {cy + reach * math.sin(math.radians(a + 6)):.0f} Z" fill="#fff" opacity=".13"/>'
        for a in range(0, 360, 20))
    grad = (f'<radialGradient id="sky" gradientUnits="userSpaceOnUse" cx="{cx}" cy="{cy}" r="{max(w, h) * 0.75:.0f}">'
            '<stop offset="0" stop-color="#ffe6a8"/><stop offset="0.45" stop-color="#ff9fc3"/><stop offset="1" stop-color="#e2508f"/>'
            '</radialGradient>')
    return f'<defs>{grad}</defs><rect width="{w}" height="{h}" fill="url(#sky)"/>{rays}'


def sparkle(x, y, s, color="#fffbe0", opacity=1):
    return (f'<path d="M{x} {y - s} Q{x + s * .13} {y - s * .13} {x + s} {y} Q{x + s * .13} {y + s * .13} {x} {y + s} '
            f'Q{x - s * .13} {y + s * .13} {x - s} {y} Q{x - s * .13} {y - s * .13} {x} {y - s} Z" fill="{color}" opacity="{opacity}"/>')


def gold_star(x, y, r):
    return f'<path d="{star(x, y, r, r * 0.48)}" fill="url(#gold)" stroke="{INK}" stroke-width="{r * 0.2:.0f}" stroke-linejoin="round"/>'


def placed_hotel(x, y, scale):
    """The icon's hotel (drawn in a 1024 box) with its top-left corner at x, y."""
    return f'<g transform="translate({x:.0f} {y:.0f}) scale({scale})">{hotel(1.0)}</g>'


def logo(cx, cy, width):
    h = width * 480 / 1024
    return image("UI/logo.png", cx - width / 2, cy - h / 2, width, h)


def cover():
    w, h = 1260, 1000
    body = [sunburst(w, h, 630, 600)]
    body.append(placed_hotel(630 - 512 * 0.74, 300, 0.74))
    body += [item("food_2", 170, 830, 210, -10), item("drink_3", 330, 905, 170, 8),
             item("beach_4", 1090, 830, 210, 10), item("care_3", 930, 905, 170, -8)]
    body += [sparkle(120, 560, 34), sparkle(1150, 540, 30), sparkle(250, 420, 20), sparkle(1010, 410, 22)]
    body.append(logo(630, 205, 860))
    return w, h, body


def banner():
    w, h = 1920, 540
    body = [sunburst(w, h, 960, 300)]
    body.append(placed_hotel(150, 30, 0.48))
    # A little merge: two of the same item make the next one.
    body += [item("food_2", 1470, 210, 170, -8), item("food_2", 1470, 400, 170, 8),
             f'<path d="M1585 305 L1650 305" stroke="{INK}" stroke-width="18" stroke-linecap="round"/>',
             f'<path d="M1630 280 L1662 305 L1630 330" fill="none" stroke="{INK}" stroke-width="18" stroke-linecap="round" stroke-linejoin="round"/>',
             item("food_3", 1770, 305, 240, 0), gold_star(1840, 175, 34)]
    body += [sparkle(80, 120, 26), sparkle(1880, 470, 24), sparkle(700, 470, 20), sparkle(1240, 80, 22)]
    body.append(logo(960, 270, 830))
    return w, h, body


def background():
    w, h = 1920, 1080
    rnd = random.Random(7)
    body = ['<defs><linearGradient id="page" x1="0" y1="0" x2="0" y2="1">'
            '<stop offset="0" stop-color="#ffd3e4"/><stop offset="1" stop-color="#f39bbf"/></linearGradient></defs>',
            f'<rect width="{w}" height="{h}" fill="url(#page)"/>']
    for gy in range(0, h + 120, 120):
        for gx in range(0, w + 120, 120):
            x, y = gx + (60 if (gy // 120) % 2 else 0) + rnd.uniform(-14, 14), gy + rnd.uniform(-14, 14)
            if rnd.random() < 0.5:
                body.append(sparkle(x, y, rnd.uniform(10, 18), "#fff", 0.45))
            else:
                body.append(f'<path d="{star(x, y, rnd.uniform(9, 14), 5)}" fill="#fff" opacity=".35"/>')
    return w, h, body


def main():
    os.makedirs(OUT, exist_ok=True)
    files = {"cover": cover(), "banner": banner(), "background": background()}
    with tempfile.TemporaryDirectory() as tmp:
        actions = []
        for name, (w, h, body) in files.items():
            src = os.path.join(tmp, name + ".svg")
            with open(src, "w") as f:
                f.write(f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">'
                        f'<defs>{DEFS}</defs>{"".join(body)}</svg>')
            dst = os.path.join(OUT, name + ".png")
            actions.append(f"file-open:{src};export-width:{w};export-height:{h};export-filename:{dst};export-do;file-close")
        subprocess.run(["inkscape", "--shell"], input="\n".join(actions) + "\nquit\n", text=True, check=True, capture_output=True)
    print("itch.io art ->", OUT)


if __name__ == "__main__":
    main()
