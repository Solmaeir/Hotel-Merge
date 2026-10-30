#!/usr/bin/env python3
"""Hotel Merge UI kit: the cosy cream-and-plum look (glossy chunky buttons, cream cards, dark currency pills,
purple tabs, navy bottom bar) plus its icons and the guest portraits.

Run from anywhere:  python3 Tools/art/make_kit.py
Output: Assets/Resources/Art/Kit (9-slice frames, 256 px) and Assets/Resources/Art/Kit/Icons (256 px).
9-slice borders live in Art.KitBorder (Assets/Scripts/Art.cs); keep them in sync with the corner radii here.
"""
import math
import os
import subprocess
import tempfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.environ.get("KIT_OUT") or os.path.join(ROOT, "Assets", "Resources", "Art", "Kit")
LINE = "#4a2c3a"   # soft plum-brown outline used by every icon

_ids = [0]


def uid(prefix="g"):
    _ids[0] += 1
    return f"{prefix}{_ids[0]}"


def svg(body, defs="", w=256, h=256):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">'
            f"<defs>{defs}</defs>{body}</svg>")


def lin(*cols, x1=0, y1=0, x2=0, y2=1):
    """Returns (defs, url) of a linear gradient through the colours."""
    i = uid()
    n = max(1, len(cols) - 1)
    stops = "".join(f'<stop offset="{k / n:.2f}" stop-color="{c}"/>' for k, c in enumerate(cols))
    return f'<linearGradient id="{i}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{stops}</linearGradient>', f"url(#{i})"


def rad(*cols, cx=0.38, cy=0.32, r=0.8):
    i = uid()
    n = max(1, len(cols) - 1)
    stops = "".join(f'<stop offset="{k / n:.2f}" stop-color="{c}"/>' for k, c in enumerate(cols))
    return f'<radialGradient id="{i}" cx="{cx}" cy="{cy}" r="{r}">{stops}</radialGradient>', f"url(#{i})"


def blur(sd):
    i = uid("b")
    return f'<filter id="{i}" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="{sd}"/></filter>', f"url(#{i})"


def st(w=7, col=LINE):
    return f'stroke="{col}" stroke-width="{w}" stroke-linejoin="round" stroke-linecap="round"'


def star_d(cx, cy, ro, ri, n=5, rot=-90, round_=0.0):
    pts = []
    for k in range(n * 2):
        r = ro if k % 2 == 0 else ri
        a = math.radians(rot + k * 180 / n)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    if round_ <= 0:
        return "M" + " L".join(f"{x:.1f} {y:.1f}" for x, y in pts) + " Z"
    # Rounded corners: cut each corner and curve through it.
    d = ""
    m = len(pts)
    for k in range(m):
        p0, p1, p2 = pts[k - 1], pts[k], pts[(k + 1) % m]
        a = (p1[0] + (p0[0] - p1[0]) * round_, p1[1] + (p0[1] - p1[1]) * round_)
        b = (p1[0] + (p2[0] - p1[0]) * round_, p1[1] + (p2[1] - p1[1]) * round_)
        d += ("M" if k == 0 else "L") + f"{a[0]:.1f} {a[1]:.1f} Q{p1[0]:.1f} {p1[1]:.1f} {b[0]:.1f} {b[1]:.1f} "
    return d + "Z"


def gloss(x, y, w, h, op=0.45, rx=None):
    """The soft white highlight band near the top of glossy shapes."""
    rx = rx if rx is not None else h / 2
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" fill="#fff" opacity="{op}"/>'


def ground(cx=128, cy=236, rx=78, ry=11, op=0.16):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#3a1f30" opacity="{op}"/>'


def sparkle(cx, cy, s, col="#fff8d6", op=1.0):
    d = (f"M{cx} {cy - s} Q{cx + s * .16} {cy - s * .16} {cx + s} {cy} Q{cx + s * .16} {cy + s * .16} {cx} {cy + s} "
         f"Q{cx - s * .16} {cy + s * .16} {cx - s} {cy} Q{cx - s * .16} {cy - s * .16} {cx} {cy - s} Z")
    return f'<path d="{d}" fill="{col}" opacity="{op}"/>'


# ============================================================================ 9-slice frames (256 x 256)
FRAMES = {}


def chunky(top, bottom, lip, edge, rx=72, gloss_op=0.42):
    """Glossy pill button: outline, darker lip underneath, gradient face, highlight band, faint bottom glow."""
    gd, g = lin(top, bottom)
    sd, s = blur(5)
    return svg(
        f'<rect x="10" y="22" width="236" height="226" rx="{rx}" fill="#2a1424" opacity=".28" filter="{s}"/>'
        f'<rect x="8" y="14" width="240" height="228" rx="{rx}" fill="{lip}" {st(8, edge)}/>'
        f'<rect x="8" y="8" width="240" height="214" rx="{rx}" fill="{g}" {st(8, edge)}/>'
        + gloss(34, 22, 188, 58, gloss_op, 29)
        + f'<rect x="30" y="170" width="196" height="30" rx="15" fill="#fff" opacity=".10"/>',
        gd + sd)


FRAMES["btn_green"] = chunky("#8de865", "#3fae3c", "#2b8a2c", "#22662a")
FRAMES["btn_purple"] = chunky("#b49bff", "#7a55e8", "#5a3cc0", "#43308a")
FRAMES["btn_orange"] = chunky("#ffd75a", "#ffa21f", "#e27812", "#b4560e")
FRAMES["btn_blue"] = chunky("#7cc0ff", "#3d7fe6", "#2b5ec0", "#22468e")
FRAMES["btn_red"] = chunky("#ff8c86", "#e8454f", "#b82b3a", "#8e2232")
FRAMES["btn_gray"] = chunky("#efe9e3", "#d8cec4", "#c0b3a6", "#a89a8c", gloss_op=0.55)
FRAMES["btn_cream"] = chunky("#fbf3ea", "#ece0d2", "#d9c8b6", "#c9b49f", gloss_op=0.6)


def square_btn(top, bottom, lip, edge):
    return chunky(top, bottom, lip, edge, rx=54)


FRAMES["sq_purple"] = square_btn("#9d93bf", "#71679a", "#554c7c", "#3e365c")
FRAMES["sq_cream"] = square_btn("#fffaf3", "#f3e6d7", "#dfccb8", "#cdb59d")
FRAMES["sq_orange"] = square_btn("#ffd86a", "#ffaa2c", "#e0800f", "#b35a0c")


def sheet():
    """Big cream panel inside a dark plum frame (popups, screens)."""
    gd, g = lin("#fffaf2", "#f8ecdd")
    sd, s = blur(6)
    return svg(
        f'<rect x="6" y="14" width="244" height="236" rx="64" fill="#1e0f1a" opacity=".35" filter="{s}"/>'
        '<rect x="6" y="6" width="244" height="240" rx="64" fill="#4b3a48"/>'
        '<rect x="6" y="6" width="244" height="240" rx="64" fill="none" stroke="#3a2b38" stroke-width="4"/>'
        f'<rect x="22" y="22" width="212" height="206" rx="50" fill="{g}"/>'
        '<rect x="22" y="22" width="212" height="206" rx="50" fill="none" stroke="#e9d6c1" stroke-width="4"/>'
        '<rect x="30" y="28" width="196" height="10" rx="5" fill="#fff" opacity=".7"/>',
        gd + sd)


FRAMES["sheet"] = sheet()


def card(fill1, fill2, border, shadow, rx=44):
    gd, g = lin(fill1, fill2)
    sd, s = blur(4)
    return svg(
        f'<rect x="10" y="18" width="236" height="230" rx="{rx}" fill="{shadow}" opacity=".55" filter="{s}"/>'
        f'<rect x="8" y="8" width="240" height="234" rx="{rx}" fill="{g}" stroke="{border}" stroke-width="4"/>'
        f'<rect x="20" y="14" width="216" height="8" rx="4" fill="#fff" opacity=".8"/>',
        gd + sd)


FRAMES["card"] = card("#fffcf7", "#fbf2e6", "#efdfcc", "#cdb197")
FRAMES["card_lilac"] = card("#f7f3ff", "#ece5fb", "#ddd2f2", "#b2a3d4")
FRAMES["card_peach"] = card("#fff3e3", "#fde4c8", "#efcfa9", "#d4a983")


def inset(fill1, fill2, rim, rx=40):
    """Sunken well (item tiles, list icons)."""
    gd, g = lin(fill1, fill2)
    return svg(
        f'<rect x="6" y="6" width="244" height="244" rx="{rx}" fill="{rim}"/>'
        f'<rect x="6" y="12" width="244" height="238" rx="{rx}" fill="{g}"/>'
        f'<rect x="6" y="6" width="244" height="244" rx="{rx}" fill="none" stroke="#fff" stroke-width="3" opacity=".6"/>',
        gd)


FRAMES["inset"] = inset("#f8ece0", "#f3e1cf", "#e3cbb3")
FRAMES["inset_lilac"] = inset("#efe8fb", "#e3d9f6", "#c9b9e8")


def cell(top, bottom, edge):
    gd, g = lin(top, bottom)
    return svg(
        f'<rect x="8" y="12" width="240" height="238" rx="34" fill="{edge}"/>'
        f'<rect x="8" y="6" width="240" height="236" rx="34" fill="{g}"/>'
        '<rect x="22" y="16" width="212" height="12" rx="6" fill="#fff" opacity=".55"/>',
        gd)


FRAMES["cell_a"] = cell("#fdf0e2", "#f9e6d2", "#ead0b3")
FRAMES["cell_b"] = cell("#f8dcc0", "#f3cfae", "#e2b791")
FRAMES["cell_locked"] = cell("#e9dfd6", "#ddd0c4", "#c8b8a8")


def board():
    gd, g = lin("#fbe4cc", "#f5d4b2")
    sd, s = blur(5)
    return svg(
        f'<rect x="8" y="16" width="240" height="232" rx="48" fill="#3a2030" opacity=".3" filter="{s}"/>'
        f'<rect x="8" y="8" width="240" height="236" rx="48" fill="{g}" stroke="#e5bf98" stroke-width="5"/>'
        '<rect x="20" y="16" width="216" height="8" rx="4" fill="#fff" opacity=".7"/>',
        gd + sd)


FRAMES["board"] = board()


def pill_dark():
    gd, g = lin("#4d3f4b", "#64555f")
    return svg(
        f'<rect x="4" y="10" width="248" height="236" rx="118" fill="{g}" opacity=".9"/>'
        '<rect x="4" y="10" width="248" height="236" rx="118" fill="none" stroke="#2f2430" stroke-width="5" opacity=".7"/>'
        '<rect x="24" y="22" width="208" height="30" rx="15" fill="#000" opacity=".14"/>',
        gd)


FRAMES["pill_dark"] = pill_dark()


def pill_blue():
    gd, g = lin("#6cb3ff", "#3d7fe6")
    return svg(
        f'<rect x="4" y="8" width="248" height="240" rx="118" fill="{g}" stroke="#2a4f9c" stroke-width="6"/>'
        + gloss(30, 18, 196, 50, 0.35, 25), gd)


FRAMES["pill_blue"] = pill_blue()


def track():
    gd, g = lin("#cfc4ba", "#e2d9d0")
    return svg(
        f'<rect x="4" y="4" width="248" height="248" rx="124" fill="{g}" stroke="#b9ab9e" stroke-width="5"/>'
        '<rect x="20" y="14" width="216" height="34" rx="17" fill="#000" opacity=".08"/>', gd)


FRAMES["track"] = track()


def fill(top, bottom, edge):
    gd, g = lin(top, bottom)
    return svg(
        f'<rect x="4" y="4" width="248" height="248" rx="124" fill="{g}" stroke="{edge}" stroke-width="4"/>'
        + gloss(26, 16, 204, 64, 0.45, 32), gd)


FRAMES["fill_blue"] = fill("#8be6f7", "#34a6de", "#2a86b8")
FRAMES["fill_green"] = fill("#a4ea7c", "#48b544", "#338a32")
FRAMES["fill_gold"] = fill("#ffe27a", "#ffb31f", "#d98a12")
FRAMES["fill_purple"] = fill("#c4b0ff", "#7a55e8", "#5a3cc0")


def tag(top, bottom, edge, rx=60):
    """Section header ribbon ("Görevler", "Mağaza")."""
    gd, g = lin(top, bottom)
    return svg(
        f'<rect x="8" y="14" width="240" height="232" rx="{rx}" fill="{edge}"/>'
        f'<rect x="8" y="8" width="240" height="226" rx="{rx}" fill="{g}" stroke="{edge}" stroke-width="6"/>'
        + gloss(30, 18, 196, 40, 0.25, 20), gd)


FRAMES["tag_plum"] = tag("#7b6a92", "#5f4f78", "#463a5c")
FRAMES["tag_pink"] = tag("#ff9cbc", "#f0628f", "#b83d68")


def nav_bar():
    gd, g = lin("#4a4874", "#363458")
    return svg(
        f'<rect x="4" y="6" width="248" height="246" rx="56" fill="{g}" stroke="#262444" stroke-width="6"/>'
        '<rect x="22" y="16" width="212" height="10" rx="5" fill="#fff" opacity=".18"/>', gd)


FRAMES["nav_bar"] = nav_bar()
FRAMES["nav_sel"] = square_btn("#ffd86a", "#ffab2c", "#e0800f", "#b35a0c")


def bubble():
    gd, g = lin("#fffdf9", "#fbf1e4")
    sd, s = blur(4)
    return svg(
        f'<rect x="10" y="16" width="236" height="232" rx="52" fill="#b99a80" opacity=".5" filter="{s}"/>'
        f'<rect x="8" y="8" width="240" height="234" rx="52" fill="{g}" stroke="#ead7c2" stroke-width="4"/>', gd + sd)


FRAMES["bubble"] = bubble()


def badge():
    gd, g = lin("#ff7a6e", "#e8343e")
    return svg(
        f'<circle cx="128" cy="128" r="116" fill="{g}" stroke="#fff" stroke-width="16"/>'
        '<circle cx="128" cy="128" r="116" fill="none" stroke="#a4202c" stroke-width="4" opacity=".6"/>'
        + gloss(66, 40, 124, 46, 0.35, 23), gd)


FRAMES["badge"] = badge()


def avatar_ring():
    gd, g = lin("#7ec4ff", "#3a7fe0")
    return svg(
        f'<circle cx="128" cy="128" r="118" fill="{g}" stroke="#244c96" stroke-width="6"/>'
        '<circle cx="128" cy="128" r="98" fill="#fff4e8" stroke="#244c96" stroke-width="5"/>'
        + gloss(70, 18, 116, 18, 0.35, 9), gd)


FRAMES["avatar_ring"] = avatar_ring()


def select_frame():
    """Golden selection with corner brackets (merge board)."""
    c = ""
    for sx, sy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        x0, y0 = 128 - sx * 112, 128 - sy * 112
        c += (f'<path d="M{x0} {y0 + sy * 46} L{x0} {y0 + sy * 10} Q{x0} {y0} {x0 + sx * 10} {y0} L{x0 + sx * 46} {y0}" '
              f'fill="none" stroke="#fff6c4" stroke-width="18" stroke-linecap="round"/>'
              f'<path d="M{x0} {y0 + sy * 46} L{x0} {y0 + sy * 10} Q{x0} {y0} {x0 + sx * 10} {y0} L{x0 + sx * 46} {y0}" '
              f'fill="none" stroke="#ffc21f" stroke-width="10" stroke-linecap="round"/>')
    gd, g = rad("#fff3b0", "#ffd84a", cx=0.5, cy=0.5, r=0.6)
    return svg(f'<rect x="20" y="20" width="216" height="216" rx="34" fill="{g}" opacity=".35"/>' + c, gd)


FRAMES["select"] = select_frame()

FRAMES["circle"] = svg('<circle cx="128" cy="128" r="124" fill="#fff"/>')
FRAMES["rounded"] = svg('<rect x="0" y="0" width="256" height="256" rx="72" fill="#fff"/>')
FRAMES["divider"] = svg('<rect x="0" y="118" width="256" height="20" rx="10" fill="#e8d6c2"/>'
                        '<rect x="0" y="118" width="256" height="8" rx="4" fill="#fff" opacity=".7"/>')


def awning():
    """Red and white shop awning strip with scalloped edge (tiles horizontally)."""
    body = ""
    for k in range(4):
        col = "#f05a5a" if k % 2 == 0 else "#fff6ee"
        x = k * 64
        body += f'<rect x="{x}" y="0" width="64" height="150" fill="{col}"/>'
        body += f'<path d="M{x} 150 Q{x + 32} 200 {x + 64} 150 Z" fill="{col}"/>'
    body += '<rect x="0" y="0" width="256" height="24" fill="#000" opacity=".08"/>'
    return svg(body)


FRAMES["awning"] = awning()

def backdrop(top, mid, bottom):
    """Screen background (stretched): soft vertical gradient with a faint diamond pattern."""
    gd, g = lin(top, mid, bottom)
    dots = "".join(f'<path d="M{x} {y - 6} L{x + 6} {y} L{x} {y + 6} L{x - 6} {y} Z" fill="#fff" opacity=".05"/>'
                   for y in range(16, 256, 32) for x in range(16 + (y // 32 % 2) * 16, 256, 32))
    return svg(f'<rect width="256" height="256" fill="{g}"/>' + dots, gd)


FRAMES["bg_kitchen"] = backdrop("#6a5470", "#54405a", "#3e2f44")
FRAMES["bg_services"] = backdrop("#5f5684", "#4a4270", "#36305a")
FRAMES["bg_pool"] = backdrop("#4f6f84", "#3e5872", "#2f4058")

# ============================================================================ icons (256 x 256)
ICONS = {}


def coin_body(cx=128, cy=128, r=100, outline=8):
    gd, g = rad("#fff6a8", "#ffd23a", "#f4a20f", cx=0.36, cy=0.3, r=0.85)
    ed, e = lin("#ffcf3a", "#d8860c")
    return (f'<circle cx="{cx}" cy="{cy + 8}" r="{r}" fill="#b8690a" {st(outline)}/>'
            f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{g}" {st(outline)}/>'
            f'<circle cx="{cx}" cy="{cy}" r="{r * 0.72:.0f}" fill="{e}" stroke="#d98a12" stroke-width="{r * 0.06:.0f}"/>'
            f'<circle cx="{cx}" cy="{cy}" r="{r * 0.66:.0f}" fill="none" stroke="#fff3a6" stroke-width="{r * 0.04:.0f}" opacity=".7"/>'
            f'<path d="{star_d(cx, cy + 2, r * 0.42, r * 0.19, round_=0.18)}" fill="#ffe680" stroke="#d98a12" stroke-width="{r * 0.06:.0f}" stroke-linejoin="round"/>'
            f'<path d="M{cx - r * 0.62:.0f} {cy - r * 0.3:.0f} Q{cx - r * 0.5:.0f} {cy - r * 0.66:.0f} {cx - r * 0.12:.0f} {cy - r * 0.76:.0f}" '
            f'fill="none" stroke="#fff" stroke-width="{r * 0.12:.0f}" stroke-linecap="round" opacity=".8"/>'), gd + ed


b, d = coin_body()
ICONS["coin"] = svg(b, d)


def bolt(cx=128, cy=128, s=1.0, top="#ffe45a", bottom="#ffa21f", edge=LINE, rot=8):
    gd, g = lin(top, bottom)
    p = "M146 18 L60 140 L118 140 L96 238 L196 100 L136 100 L170 18 Z"
    return (f'<g transform="translate({cx} {cy}) rotate({rot}) scale({s}) translate(-128 -128)">'
            f'<path d="{p}" transform="translate(0 8)" fill="#c46a0c" {st(10, edge)}/>'
            f'<path d="{p}" fill="{g}" {st(10, edge)}/>'
            '<path d="M140 36 L84 124" stroke="#fff" stroke-width="12" stroke-linecap="round" opacity=".75"/>'
            '</g>'), gd


b, d = bolt()
ICONS["energy"] = svg(b, d)
b, d = bolt(top="#c9a8ff", bottom="#7a49e8", edge="#3d2470")
ICONS["energy_purple"] = svg(b, d)


def round_btn(top, bottom, edge, inner):
    gd, g = lin(top, bottom)
    return svg(
        f'<circle cx="128" cy="136" r="110" fill="{edge}"/>'
        f'<circle cx="128" cy="128" r="110" fill="{g}" {st(9, edge)}/>'
        + gloss(66, 36, 124, 44, 0.4, 22) + inner, gd)


PLUS = ('<path d="M128 72 L128 184 M72 128 L184 128" stroke="#2a5a22" stroke-width="44" stroke-linecap="round"/>'
        '<path d="M128 72 L128 184 M72 128 L184 128" stroke="#fff" stroke-width="28" stroke-linecap="round"/>')
ICONS["plus"] = round_btn("#8de865", "#3fae3c", "#22662a", PLUS)


def gear_d(cx, cy, ro, ri, teeth=8):
    pts = []
    for k in range(teeth * 4):
        a = math.radians(k * 360 / (teeth * 4) - 90)
        r = ro if (k % 4) in (1, 2) else ri
        pts.append(f"{cx + r * math.cos(a):.1f} {cy + r * math.sin(a):.1f}")
    return "M" + " L".join(pts) + " Z"


def gear():
    gd, g = lin("#8cc8ff", "#3a7fe0")
    d = gear_d(128, 124, 112, 86, 8)
    return svg(
        f'<path d="{d}" transform="translate(0 8)" fill="#244c96" {st(10)}/>'
        f'<path d="{d}" fill="{g}" {st(10)}/>'
        f'<circle cx="128" cy="124" r="38" fill="#eaf4ff" {st(10)}/>'
        '<path d="M64 86 Q84 52 122 44" stroke="#fff" stroke-width="12" fill="none" stroke-linecap="round" opacity=".7"/>', gd)


ICONS["settings"] = gear()


def clipboard(check_cols=("#3fae3c", "#e8454f", "#3fae3c")):
    bd, bgr = lin("#7cc0ff", "#3d7fe6")
    pd, pg = lin("#ffffff", "#f1ece6")
    rows = ""
    for k, col in enumerate(check_cols):
        y = 106 + k * 40
        rows += (f'<path d="M78 {y} l10 10 l18 -20" fill="none" stroke="{col}" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>'
                 f'<rect x="118" y="{y - 4}" width="{60 - k * 8}" height="12" rx="6" fill="#c9bfb4"/>')
    return svg(
        ground(128, 238, 70, 9)
        + f'<rect x="40" y="38" width="176" height="200" rx="28" fill="#244c96" {st(9)}/>'
        f'<rect x="40" y="30" width="176" height="200" rx="28" fill="{bgr}" {st(9)}/>'
        f'<rect x="58" y="62" width="140" height="152" rx="14" fill="{pg}" {st(6)}/>'
        + rows
        + f'<rect x="88" y="16" width="80" height="40" rx="14" fill="#c9cfd8" {st(8)}/>'
        '<rect x="100" y="24" width="56" height="8" rx="4" fill="#fff" opacity=".8"/>'
        '<path d="M52 52 L52 120" stroke="#fff" stroke-width="10" stroke-linecap="round" opacity=".45"/>',
        bd + pd)


ICONS["tasks"] = clipboard()


def gift():
    bd, bx = lin("#ffd75a", "#f4a20f")
    ld, lid = lin("#ffe680", "#ffbd2a")
    rd, rb = lin("#ff6b6b", "#d72e3d")
    return svg(
        ground(128, 238, 84, 10)
        + f'<rect x="44" y="118" width="168" height="112" rx="16" fill="{bx}" {st(9)}/>'
        f'<rect x="32" y="84" width="192" height="50" rx="16" fill="{lid}" {st(9)}/>'
        f'<rect x="112" y="84" width="32" height="146" fill="{rb}" {st(7)}/>'
        f'<path d="M128 84 C96 40 54 46 66 72 C74 88 108 86 128 84 Z" fill="{rb}" {st(8)}/>'
        f'<path d="M128 84 C160 40 202 46 190 72 C182 88 148 86 128 84 Z" fill="{rb}" {st(8)}/>'
        f'<circle cx="128" cy="84" r="14" fill="#ff8a8a" {st(7)}/>'
        + gloss(46, 92, 60, 12, 0.6, 6)
        + '<path d="M60 140 L60 210" stroke="#fff" stroke-width="10" stroke-linecap="round" opacity=".4"/>'
        + sparkle(212, 56, 16) + sparkle(44, 54, 10),
        bd + ld + rd)


ICONS["gift"] = gift()


def shop():
    wd, wall = lin("#8cc8ff", "#4d8fe6")
    awn = ""
    for k in range(5):
        col = "#f05a5a" if k % 2 == 0 else "#fff6ee"
        x = 34 + k * 37.6
        awn += (f'<path d="M{x:.1f} 70 L{x + 37.6:.1f} 70 L{x + 37.6:.1f} 108 Q{x + 18.8:.1f} 130 {x:.1f} 108 Z" fill="{col}"/>')
    return svg(
        ground(128, 238, 92, 10)
        + f'<rect x="44" y="100" width="168" height="130" rx="14" fill="{wall}" {st(9)}/>'
        f'<rect x="70" y="134" width="56" height="96" rx="10" fill="#fff4e6" {st(7)}/>'
        f'<rect x="140" y="134" width="50" height="44" rx="10" fill="#bfe6ff" {st(7)}/>'
        '<path d="M148 142 L162 142" stroke="#fff" stroke-width="7" stroke-linecap="round"/>'
        f'<circle cx="114" cy="184" r="5" fill="{LINE}"/>'
        f'<path d="M34 70 L222 70 L222 108 L34 108 Z" fill="#fff6ee" {st(9)}/>' + awn
        + f'<path d="M34 70 L222 70 L222 108 Q203 130 184 108 Q165 130 147 108 Q128 130 109 108 Q90 130 72 108 Q53 130 34 108 Z" fill="none" {st(9)}/>'
        f'<path d="M48 40 L208 40 L222 70 L34 70 Z" fill="#e8454f" {st(9)}/>'
        '<path d="M62 48 L196 48" stroke="#fff" stroke-width="8" stroke-linecap="round" opacity=".5"/>',
        wd)


ICONS["shop"] = shop()


def hammer():
    hd, hh = lin("#eef2f8", "#9aa6b8", x2=0, y2=1)
    wd, wood = lin("#e7a565", "#a8622c", x2=1, y2=0)
    return svg(
        ground(128, 238, 64, 8)
        + f'<g transform="rotate(-38 128 128)">'
        f'<rect x="112" y="80" width="34" height="168" rx="14" fill="{wood}" {st(9)}/>'
        '<path d="M122 100 L122 220" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".45"/>'
        f'<path d="M52 38 L176 38 Q196 38 204 50 L212 64 L212 104 L52 104 Q40 104 40 92 L40 50 Q40 38 52 38 Z" fill="{hh}" {st(9)}/>'
        '<path d="M56 52 L184 52" stroke="#fff" stroke-width="10" stroke-linecap="round" opacity=".8"/>'
        '</g>', hd + wd)


ICONS["build"] = hammer()


def hotel_icon():
    wd, wall = lin("#ffd9e8", "#f2a9c8")
    rd, roof = lin("#b9a2ff", "#7a55e8")
    win = ""
    for row in range(2):
        for col in range(3):
            x, y = 70 + col * 42, 96 + row * 46
            win += f'<rect x="{x}" y="{y}" width="28" height="30" rx="6" fill="#bfe6ff" {st(6)}/>'
    return svg(
        ground(128, 238, 90, 10)
        + f'<rect x="50" y="76" width="156" height="156" rx="12" fill="{wall}" {st(9)}/>'
        + win
        + f'<path d="M108 232 L108 196 Q108 182 128 182 Q148 182 148 196 L148 232 Z" fill="#a8622c" {st(7)}/>'
        f'<path d="M36 80 L128 26 L220 80 Z" fill="{roof}" {st(9)}/>'
        f'<rect x="100" y="48" width="56" height="20" rx="8" fill="#ffd23a" {st(6)}/>'
        '<path d="M62 92 L62 210" stroke="#fff" stroke-width="9" stroke-linecap="round" opacity=".45"/>',
        wd + rd)


ICONS["hotel"] = hotel_icon()


def up_arrow(top="#f2d3b0", bottom="#c98a52", edge=LINE):
    gd, g = lin(top, bottom)
    p = "M128 22 L222 124 L164 124 L164 226 L92 226 L92 124 L34 124 Z"
    return svg(
        f'<path d="{p}" transform="translate(0 8)" fill="#8f5530" {st(10, edge)}/>'
        f'<path d="{p}" fill="{g}" {st(10, edge)}/>'
        '<path d="M126 46 L64 112" stroke="#fff" stroke-width="12" stroke-linecap="round" opacity=".6"/>', gd)


ICONS["upgrade"] = up_arrow()
ICONS["upgrade_green"] = up_arrow("#a4ea7c", "#3fae3c", "#22662a")

ICONS["back"] = svg(
    '<path d="M162 40 L78 128 L162 216" fill="none" stroke="#3e365c" stroke-width="58" stroke-linecap="round" stroke-linejoin="round"/>'
    '<path d="M162 40 L78 128 L162 216" fill="none" stroke="#fff" stroke-width="34" stroke-linecap="round" stroke-linejoin="round"/>')
ICONS["close"] = svg(
    '<path d="M64 64 L192 192 M192 64 L64 192" stroke="#4a2c22" stroke-width="40" stroke-linecap="round"/>'
    '<path d="M64 64 L192 192 M192 64 L64 192" stroke="#6b3f2e" stroke-width="22" stroke-linecap="round"/>')
ICONS["check"] = svg(
    '<path d="M44 132 L102 190 L214 70" fill="none" stroke="#22662a" stroke-width="56" stroke-linecap="round" stroke-linejoin="round"/>'
    '<path d="M44 132 L102 190 L214 70" fill="none" stroke="#5cc94a" stroke-width="32" stroke-linecap="round" stroke-linejoin="round"/>'
    '<path d="M52 124 L98 170" stroke="#fff" stroke-width="10" stroke-linecap="round" opacity=".6"/>')


def play():
    return svg('<path d="M84 46 Q72 40 72 54 L72 202 Q72 216 84 210 L206 140 Q218 128 206 116 Z" fill="#fff" '
               'stroke="#c46a0c" stroke-width="14" stroke-linejoin="round"/>')


ICONS["play"] = play()


def star(fill_top, fill_bottom, edge, glossy=True):
    gd, g = lin(fill_top, fill_bottom)
    d = star_d(128, 132, 116, 56, round_=0.16)
    return svg(
        f'<path d="{d}" transform="translate(0 8)" fill="{edge}" {st(9, edge)}/>'
        f'<path d="{d}" fill="{g}" {st(9, edge)}/>'
        + ('<path d="M98 84 Q112 56 126 40" stroke="#fff" stroke-width="12" fill="none" stroke-linecap="round" opacity=".75"/>' if glossy else ""),
        gd)


ICONS["star"] = star("#ffef8a", "#ffb81f", "#c47a0c")
ICONS["star_off"] = star("#e6e2ea", "#c6c0cc", "#a59eae", glossy=False)
ICONS["star_blue"] = star("#c9ecff", "#7cc4f2", "#4b8fc0")


def bell():
    gd, g = lin("#ffe680", "#f4a20f")
    return svg(
        ground(128, 236, 84, 10)
        + f'<rect x="40" y="196" width="176" height="28" rx="14" fill="#8a5a3c" {st(8)}/>'
        f'<path d="M50 196 Q50 96 128 92 Q206 96 206 196 Z" fill="{g}" {st(9)}/>'
        f'<circle cx="128" cy="78" r="16" fill="#ffd23a" {st(8)}/>'
        '<path d="M80 176 Q78 128 108 110" stroke="#fff" stroke-width="12" fill="none" stroke-linecap="round" opacity=".7"/>'
        + sparkle(214, 84, 14) + sparkle(42, 100, 10), gd)


ICONS["bell"] = bell()


def bell_alert():
    gd, g = lin("#ffcf9a", "#f29a4a")
    return svg(
        f'<path d="M64 182 Q64 76 128 70 Q192 76 192 182 Z" fill="{g}" {st(10)}/>'
        f'<rect x="44" y="176" width="168" height="28" rx="14" fill="#f29a4a" {st(10)}/>'
        f'<circle cx="128" cy="222" r="18" fill="#c46a0c" {st(9)}/>'
        f'<circle cx="128" cy="58" r="14" fill="#f29a4a" {st(8)}/>'
        '<path d="M88 160 Q86 116 112 98" stroke="#fff" stroke-width="12" fill="none" stroke-linecap="round" opacity=".6"/>', gd)


ICONS["notify"] = bell_alert()


def map_icon():
    gd, g = lin("#a8e07a", "#5cb84a")
    panels = ("M30 64 L94 40 L94 196 L30 220 Z", "M94 40 L162 64 L162 220 L94 196 Z", "M162 64 L226 40 L226 196 L162 220 Z")
    cols = (g, "#8fd06a", g)
    body = "".join(f'<path d="{p}" fill="{c}" {st(9)}/>' for p, c in zip(panels, cols))
    body += '<path d="M44 150 Q94 120 130 160 Q170 196 212 140" fill="none" stroke="#fff3c4" stroke-width="10" stroke-dasharray="14 12" stroke-linecap="round"/>'
    pd, pg = lin("#ff7a6e", "#e8343e")
    body += (f'<path d="M150 18 Q190 18 190 58 Q190 88 150 128 Q110 88 110 58 Q110 18 150 18 Z" fill="{pg}" {st(9)}/>'
             f'<circle cx="150" cy="58" r="16" fill="#fff" {st(6)}/>')
    return svg(ground(128, 236, 92, 10) + body, gd + pd)


ICONS["map"] = map_icon()


def lock():
    gd, g = lin("#ffd75a", "#f0a01f")
    return svg(
        f'<path d="M78 118 L78 84 Q78 34 128 34 Q178 34 178 84 L178 118" fill="none" stroke="{LINE}" stroke-width="40" stroke-linecap="round"/>'
        '<path d="M78 118 L78 84 Q78 34 128 34 Q178 34 178 84 L178 118" fill="none" stroke="#c9cfd8" stroke-width="22" stroke-linecap="round"/>'
        f'<rect x="44" y="106" width="168" height="128" rx="26" fill="{g}" {st(10)}/>'
        f'<circle cx="128" cy="160" r="18" fill="{LINE}"/><rect x="120" y="166" width="16" height="36" rx="8" fill="{LINE}"/>'
        + gloss(62, 118, 132, 20, 0.5, 10), gd)


ICONS["lock"] = lock()


def clock():
    gd, g = lin("#fffdf8", "#eee4d8")
    rd, rg = lin("#7cc0ff", "#3d7fe6")
    ticks = "".join(
        f'<circle cx="{128 + 76 * math.cos(math.radians(a)):.1f}" cy="{132 + 76 * math.sin(math.radians(a)):.1f}" r="7" fill="#c9bfb4"/>'
        for a in range(0, 360, 90))
    return svg(
        f'<circle cx="128" cy="140" r="108" fill="#2b5ec0" {st(9)}/>'
        f'<circle cx="128" cy="132" r="108" fill="{rg}" {st(9)}/>'
        f'<circle cx="128" cy="132" r="88" fill="{g}" {st(7)}/>' + ticks
        + f'<path d="M128 132 L128 78 M128 132 L166 150" stroke="{LINE}" stroke-width="16" stroke-linecap="round"/>'
        f'<circle cx="128" cy="132" r="12" fill="#e8454f" {st(5)}/>'
        + gloss(74, 36, 108, 22, 0.4, 11), gd + rd)


ICONS["clock"] = clock()


def speaker(on=True):
    gd, g = lin("#fffdf8", "#e5dccf")
    body = (f'<path d="M40 100 L84 100 L140 48 L140 208 L84 156 L40 156 Z" fill="{g}" {st(12, "#3e365c")}/>')
    if on:
        body += ('<path d="M168 92 Q190 128 168 164" fill="none" stroke="#3e365c" stroke-width="28" stroke-linecap="round"/>'
                 '<path d="M168 92 Q190 128 168 164" fill="none" stroke="#fff" stroke-width="12" stroke-linecap="round"/>'
                 '<path d="M196 64 Q236 128 196 192" fill="none" stroke="#3e365c" stroke-width="28" stroke-linecap="round"/>'
                 '<path d="M196 64 Q236 128 196 192" fill="none" stroke="#fff" stroke-width="12" stroke-linecap="round"/>')
    else:
        body += ('<path d="M168 94 L224 162 M224 94 L168 162" stroke="#8e2232" stroke-width="34" stroke-linecap="round"/>'
                 '<path d="M168 94 L224 162 M224 94 L168 162" stroke="#ff6b6b" stroke-width="18" stroke-linecap="round"/>')
    return svg(body, gd)


ICONS["sound_on"] = speaker(True)
ICONS["sound_off"] = speaker(False)


def heart():
    gd, g = lin("#ff8a9a", "#e8344f")
    d = "M128 222 C60 170 24 132 24 88 C24 54 50 30 82 30 C104 30 120 42 128 58 C136 42 152 30 174 30 C206 30 232 54 232 88 C232 132 196 170 128 222 Z"
    return svg(
        f'<path d="{d}" transform="translate(0 8)" fill="#a4202c" {st(10)}/>'
        f'<path d="{d}" fill="{g}" {st(10)}/>'
        '<path d="M58 90 Q60 60 88 56" stroke="#fff" stroke-width="14" fill="none" stroke-linecap="round" opacity=".75"/>', gd)


ICONS["heart"] = heart()


def guest_icon():
    gd, g = lin("#8cc8ff", "#3a7fe0")
    return svg(
        f'<path d="M48 228 Q48 156 128 156 Q208 156 208 228 Z" fill="{g}" {st(10)}/>'
        f'<circle cx="128" cy="98" r="58" fill="#ffd6b8" {st(10)}/>'
        f'<path d="M72 92 Q76 38 128 38 Q182 38 184 92 Q160 70 128 72 Q96 70 72 92 Z" fill="#7a4a2a" {st(8)}/>', gd)


ICONS["guest"] = guest_icon()


def sparkle_icon():
    return svg(sparkle(128, 128, 112, "#fff") + sparkle(128, 128, 70, "#fff7c4"))


ICONS["sparkle"] = sparkle_icon()


def glow_icon():
    gd, g = rad("#ffffff", "#fff3b0", "#fff3b000", cx=0.5, cy=0.5, r=0.5)
    gd = gd.replace('stop-color="#fff3b000"', 'stop-color="#fff3b0" stop-opacity="0"')
    return svg(f'<circle cx="128" cy="128" r="128" fill="{g}"/>', gd)


ICONS["glow"] = glow_icon()


def bed_icon():
    fd, fr = lin("#d9905a", "#9a5a30")
    bd, bl = lin("#b9a2ff", "#7a55e8")
    return svg(
        ground(128, 236, 96, 10)
        + f'<rect x="30" y="70" width="40" height="150" rx="12" fill="{fr}" {st(8)}/>'
        f'<rect x="54" y="136" width="176" height="56" rx="14" fill="{bl}" {st(8)}/>'
        f'<rect x="64" y="110" width="58" height="34" rx="14" fill="#fff6ee" {st(7)}/>'
        f'<rect x="36" y="186" width="200" height="22" rx="8" fill="{fr}" {st(8)}/>'
        f'<rect x="210" y="160" width="24" height="64" rx="8" fill="{fr}" {st(8)}/>'
        '<path d="M72 148 L210 148" stroke="#fff" stroke-width="8" stroke-linecap="round" opacity=".45"/>', fd + bd)


ICONS["bed"] = bed_icon()

# ---------------------------------------------------------------------------- portraits (order givers, level avatar)
SKIN = "#ffd9bd"
SKIN_SHADE = "#f5b996"


def face(hair, hair_dark, back_hair, front_hair, outfit, outfit_dark, extra="", skin=SKIN, eyes="#4a2c22", age=""):
    """Chibi bust: big head, large shiny eyes, blush, small smile. Drawn in a 256 box, centred for round frames."""
    od, og = lin(outfit, outfit_dark)
    hd, hg = lin(hair, hair_dark)
    body = ""
    body += back_hair.replace("HAIR", hg)
    # shoulders / outfit
    body += f'<path d="M34 262 Q36 196 128 190 Q220 196 222 262 Z" fill="{og}" {st(8)}/>'
    body += f'<path d="M104 192 L128 222 L152 192" fill="none" stroke="#fff" stroke-width="12" stroke-linejoin="round"/>'
    # neck + head
    body += f'<rect x="108" y="160" width="40" height="40" rx="14" fill="{SKIN_SHADE}" {st(7)}/>'
    body += f'<ellipse cx="128" cy="116" rx="78" ry="74" fill="{skin}" {st(8)}/>'
    # ears
    body += f'<ellipse cx="52" cy="124" rx="14" ry="18" fill="{skin}" {st(7)}/><ellipse cx="204" cy="124" rx="14" ry="18" fill="{skin}" {st(7)}/>'
    body += f'<ellipse cx="128" cy="116" rx="72" ry="68" fill="{skin}"/>'
    # eyes
    for ex in (98, 158):
        body += (f'<ellipse cx="{ex}" cy="128" rx="15" ry="19" fill="{eyes}"/>'
                 f'<ellipse cx="{ex}" cy="134" rx="10" ry="11" fill="#7a4a3a" opacity=".6"/>'
                 f'<circle cx="{ex + 5}" cy="120" r="6.5" fill="#fff"/><circle cx="{ex - 5}" cy="138" r="3" fill="#fff"/>')
        body += f'<path d="M{ex - 15} 106 Q{ex} 98 {ex + 15} 106" fill="none" stroke="{hair_dark}" stroke-width="6" stroke-linecap="round"/>'
    body += age
    # blush, nose, smile
    body += '<ellipse cx="80" cy="156" rx="15" ry="9" fill="#ff8a8a" opacity=".45"/><ellipse cx="176" cy="156" rx="15" ry="9" fill="#ff8a8a" opacity=".45"/>'
    body += f'<path d="M124 146 Q128 150 132 146" fill="none" stroke="{SKIN_SHADE}" stroke-width="5" stroke-linecap="round"/>'
    body += f'<path d="M112 160 Q128 178 144 160 Q128 168 112 160 Z" fill="#c8484f" stroke="{LINE}" stroke-width="5" stroke-linejoin="round"/>'
    body += front_hair.replace("HAIR", hg)
    body += extra
    return svg(f'<g>{body}</g>', od + hd)


def woman():
    back = (f'<path d="M44 120 Q40 34 128 30 Q216 34 212 120 L218 214 Q192 228 168 214 L88 214 Q64 228 38 214 Z" fill="HAIR" {st(8)}/>')
    front = (f'<path d="M52 124 Q46 46 128 42 Q210 46 204 124 Q190 92 160 80 Q140 104 96 98 Q72 96 52 124 Z" fill="HAIR" {st(8)}/>'
             '<path d="M70 70 Q100 46 140 50" stroke="#fff" stroke-width="9" fill="none" stroke-linecap="round" opacity=".35"/>'
             # side locks over the ears
             f'<path d="M54 100 Q40 150 58 200 Q74 196 76 150 Q74 118 70 100 Z" fill="HAIR" {st(8)}/>'
             f'<path d="M202 100 Q216 150 198 200 Q182 196 180 150 Q182 118 186 100 Z" fill="HAIR" {st(8)}/>'
             # headband
             f'<path d="M58 84 Q128 30 198 84" fill="none" stroke="{LINE}" stroke-width="20" stroke-linecap="round"/>'
             '<path d="M58 84 Q128 30 198 84" fill="none" stroke="#f0628f" stroke-width="11" stroke-linecap="round"/>')
    return face("#9a5a34", "#5e321c", back, front, "#ffffff", "#e6dccf",
                extra=f'<path d="M84 206 L128 236 L172 206" fill="none" stroke="#f0628f" stroke-width="10" stroke-linejoin="round"/>')


def man():
    back = ""
    front = (f'<path d="M50 122 Q44 42 128 38 Q212 42 206 122 Q196 84 168 76 Q150 96 112 86 Q86 80 72 96 Q58 104 50 122 Z" fill="HAIR" {st(8)}/>'
             '<path d="M78 64 Q110 46 150 52" stroke="#fff" stroke-width="9" fill="none" stroke-linecap="round" opacity=".3"/>')
    return face("#4a3a3a", "#231a1c", back, front, "#6cb3ff", "#3d7fe6")


def granny():
    back = (f'<circle cx="128" cy="34" r="30" fill="HAIR" {st(8)}/>'
            f'<path d="M48 126 Q40 44 128 40 Q216 44 208 126 L204 150 L52 150 Z" fill="HAIR" {st(8)}/>')
    front = (f'<path d="M54 120 Q52 56 128 52 Q204 56 202 120 Q186 82 128 80 Q70 82 54 120 Z" fill="HAIR" {st(8)}/>'
             '<path d="M82 66 Q112 54 150 58" stroke="#fff" stroke-width="9" fill="none" stroke-linecap="round" opacity=".5"/>')
    glasses = (f'<circle cx="98" cy="128" r="24" fill="#fff" fill-opacity=".25" {st(6, "#8a5a3c")}/>'
               f'<circle cx="158" cy="128" r="24" fill="#fff" fill-opacity=".25" {st(6, "#8a5a3c")}/>'
               '<path d="M122 126 L134 126" stroke="#8a5a3c" stroke-width="6"/>')
    return face("#eeeaf2", "#b9b2c4", back, front, "#b49bff", "#7a55e8", extra=glasses)


def kid():
    back = ""
    front = (f'<path d="M48 126 Q38 36 128 34 Q222 36 208 126 Q200 96 178 86 Q170 100 150 92 Q136 104 116 92 Q96 104 82 90 Q62 98 48 126 Z" fill="HAIR" {st(8)}/>'
             f'<path d="M120 40 Q130 10 150 18 Q136 24 134 40 Z" fill="HAIR" {st(7)}/>'
             '<path d="M76 66 Q106 46 146 50" stroke="#fff" stroke-width="9" fill="none" stroke-linecap="round" opacity=".35"/>')
    return face("#ffa64d", "#d9661c", back, front, "#8de865", "#3fae3c")


def blonde():
    back = (f'<path d="M42 124 Q36 30 128 28 Q220 30 214 124 Q226 180 206 230 Q190 238 176 226 L80 226 Q66 238 50 230 Q30 180 42 124 Z" fill="HAIR" {st(8)}/>')
    front = (f'<path d="M52 126 Q44 44 128 40 Q212 44 204 126 Q196 84 150 74 Q118 100 74 96 Q60 104 52 126 Z" fill="HAIR" {st(8)}/>'
             '<path d="M72 66 Q104 46 144 50" stroke="#fff" stroke-width="9" fill="none" stroke-linecap="round" opacity=".45"/>'
             f'<circle cx="190" cy="80" r="16" fill="#ff8ab0" {st(6)}/><circle cx="190" cy="80" r="6" fill="#ffd23a"/>')
    return face("#ffe07a", "#e0a82a", back, front, "#ff9cbc", "#f0628f")


PORTRAITS = {"portrait_host": woman(), "portrait_man": man(), "portrait_granny": granny(),
             "portrait_kid": kid(), "portrait_girl": blonde()}


# ============================================================================ render
def render(folder, items, size):
    os.makedirs(folder, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        actions = []
        for name, content in items.items():
            src = os.path.join(tmp, name + ".svg")
            with open(src, "w") as f:
                f.write(content)
            dst = os.path.join(folder, name + ".png")
            actions.append(f"file-open:{src};export-width:{size};export-height:{size};export-filename:{dst};export-do;file-close")
        subprocess.run(["inkscape", "--shell"], input="\n".join(actions) + "\nquit\n", text=True, check=True, capture_output=True)
    print(f"{os.path.relpath(folder, ROOT)}: {len(items)} files")


if __name__ == "__main__":
    render(OUT, FRAMES, 256)
    render(os.path.join(OUT, "Icons"), ICONS, 256)
    render(os.path.join(OUT, "Portraits"), PORTRAITS, 256)
