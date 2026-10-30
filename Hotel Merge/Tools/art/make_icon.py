"""App icon: a cute pink hotel with gold stars on a warm sunburst. Writes three 1024 px PNGs to Assets/HotelMerge/Icon:
AppIcon.png (full icon), AppIcon_Background.png and AppIcon_Foreground.png (Android adaptive icon layers)."""
import math
import os
import subprocess
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "HotelMerge", "Icon")
INK = "#3a2438"


def star(cx, cy, ro, ri, rot=-90):
    pts = []
    for i in range(10):
        r = ro if i % 2 == 0 else ri
        a = math.radians(rot + i * 36)
        pts.append(f"{cx + r * math.cos(a):.1f},{cy + r * math.sin(a):.1f}")
    return "M" + " L".join(pts) + " Z"


DEFS = """
<radialGradient id="bg" cx="0.5" cy="0.42" r="0.75">
  <stop offset="0" stop-color="#ffe6a8"/><stop offset="0.45" stop-color="#ff9fc3"/><stop offset="1" stop-color="#e2508f"/>
</radialGradient>
<linearGradient id="wall" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff6ec"/><stop offset="1" stop-color="#ffd9c2"/></linearGradient>
<linearGradient id="roof" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#6fc3ff"/><stop offset="1" stop-color="#3a86e0"/></linearGradient>
<linearGradient id="sign" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ff8fb8"/><stop offset="1" stop-color="#ef4f8f"/></linearGradient>
<linearGradient id="win" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#bfe9ff"/><stop offset="1" stop-color="#6cc4f5"/></linearGradient>
<linearGradient id="gold" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff07a"/><stop offset="1" stop-color="#ffb21f"/></linearGradient>
<linearGradient id="door" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#c98a52"/><stop offset="1" stop-color="#8f5530"/></linearGradient>
"""


def background():
    rays = "".join(
        f'<path d="M512 470 L{512 + 900 * math.cos(math.radians(a - 6)):.0f} {470 + 900 * math.sin(math.radians(a - 6)):.0f} '
        f'L{512 + 900 * math.cos(math.radians(a + 6)):.0f} {470 + 900 * math.sin(math.radians(a + 6)):.0f} Z" fill="#fff" opacity=".13"/>'
        for a in range(0, 360, 24))
    return f'<rect width="1024" height="1024" fill="url(#bg)"/>{rays}'


def hotel(scale=1.0):
    o = 'stroke="%s" stroke-width="22" stroke-linejoin="round"' % INK
    g = []
    # Building
    g.append(f'<rect x="262" y="400" width="500" height="420" rx="40" fill="url(#wall)" {o}/>')
    # Windows 3x2
    for row in range(2):
        for col in range(3):
            x, y = 312 + col * 145, 450 + row * 125
            g.append(f'<rect x="{x}" y="{y}" width="110" height="92" rx="20" fill="url(#win)" stroke="{INK}" stroke-width="16"/>')
            g.append(f'<path d="M{x + 18} {y + 70} L{x + 40} {y + 22}" stroke="#fff" stroke-width="12" stroke-linecap="round" opacity=".8"/>')
            g.append(f'<rect x="{x - 8}" y="{y + 80}" width="126" height="20" rx="10" fill="#ff8fb8" stroke="{INK}" stroke-width="12"/>')
    # Door + awning
    g.append(f'<rect x="452" y="690" width="120" height="130" rx="22" fill="url(#door)" stroke="{INK}" stroke-width="16"/>')
    g.append(f'<circle cx="548" cy="760" r="10" fill="#ffd23a" stroke="{INK}" stroke-width="6"/>')
    stripes = "".join(f'<path d="M{402 + i * 44} 650 L{446 + i * 44} 650 L{446 + i * 44} 690 Q{424 + i * 44} 708 {402 + i * 44} 690 Z" '
                      f'fill="{"#ff5f7f" if i % 2 == 0 else "#fff"}"/>' for i in range(5))
    g.append(stripes)
    g.append(f'<path d="M402 650 L622 650 L622 690 Q512 712 402 690 Z" fill="none" stroke="{INK}" stroke-width="14" stroke-linejoin="round"/>')
    # Roof
    g.append(f'<rect x="232" y="352" width="560" height="74" rx="30" fill="url(#roof)" {o}/>')
    g.append('<rect x="262" y="364" width="500" height="16" rx="8" fill="#fff" opacity=".45"/>')
    # Sign with a heart
    g.append(f'<rect x="352" y="232" width="320" height="128" rx="36" fill="url(#sign)" {o}/>')
    g.append('<rect x="374" y="246" width="276" height="26" rx="13" fill="#fff" opacity=".45"/>')
    g.append(f'<path d="M512 336 C470 306 440 284 452 262 C462 244 492 246 512 270 C532 246 562 244 572 262 C584 284 554 306 512 336 Z" '
             f'fill="#fff" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>')
    # Stars arching above
    for cx, cy, r in [(512, 150, 74), (350, 192, 52), (674, 192, 52)]:
        g.append(f'<path d="{star(cx, cy, r, r * 0.48)}" fill="url(#gold)" stroke="{INK}" stroke-width="16" stroke-linejoin="round"/>')
    # Sparkles
    for x, y, s in [(205, 300, 30), (822, 290, 26), (180, 640, 22), (850, 620, 24)]:
        g.append(f'<path d="M{x} {y - s} Q{x + 4} {y - 4} {x + s} {y} Q{x + 4} {y + 4} {x} {y + s} Q{x - 4} {y + 4} {x - s} {y} Q{x - 4} {y - 4} {x} {y - s} Z" fill="#fffbe0"/>')
    body = "".join(g)
    if scale != 1.0:
        t = 512 * (1 - scale)
        body = f'<g transform="translate({t:.1f} {t:.1f}) scale({scale})">{body}</g>'
    return body


def svg(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024"><defs>{DEFS}</defs>{body}</svg>'


def main():
    os.makedirs(OUT, exist_ok=True)
    files = {
        "AppIcon": svg(background() + hotel(0.95)),
        "AppIcon_Background": svg(background()),
        # Adaptive icons are cropped to a circle/squircle by the launcher: keep the art inside the middle ~66%.
        "AppIcon_Foreground": svg(hotel(0.72)),
    }
    with tempfile.TemporaryDirectory() as tmp:
        actions = []
        for name, content in files.items():
            src = os.path.join(tmp, name + ".svg")
            with open(src, "w") as f:
                f.write(content)
            dst = os.path.join(OUT, name + ".png")
            actions.append(f"file-open:{src};export-width:1024;export-height:1024;export-filename:{dst};export-do;file-close")
        subprocess.run(["inkscape", "--shell"], input="\n".join(actions) + "\nquit\n", text=True, check=True, capture_output=True)
    print("icons ->", OUT)


if __name__ == "__main__":
    main()
