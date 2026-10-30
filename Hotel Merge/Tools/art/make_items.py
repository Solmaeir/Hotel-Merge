#!/usr/bin/env python3
"""Merge items in the UI kit's painted style: soft plum-brown outlines, gradient bodies with an inner rim shadow and
a blurred highlight, a little sparkle. Drawn in a 256 box, rendered at 512 px.

Run from anywhere:  python3 Tools/art/make_items.py
Output: Assets/Resources/Art/Items/{drink,food,bath,care,pool,beach}_{1..6}.png, generator.png, gen_services.png, gen_pool.png
"""
import math
import os
import subprocess
import tempfile

from make_kit import LINE, star_d

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.environ.get("ITEMS_OUT") or os.path.join(ROOT, "Assets", "Resources", "Art", "Items")
SIZE = 512


class Pen:
    """Collects the defs and shapes of one item."""

    def __init__(self):
        self.defs = []
        self.body = []
        self.n = 0

    def id(self, p="d"):
        self.n += 1
        return f"{p}{self.n}"

    def lin(self, *cols, x2=0, y2=1, x1=0, y1=0):
        i = self.id("g")
        n = max(1, len(cols) - 1)
        stops = "".join(f'<stop offset="{k / n:.2f}" stop-color="{c}"/>' for k, c in enumerate(cols))
        self.defs.append(f'<linearGradient id="{i}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{stops}</linearGradient>')
        return f"url(#{i})"

    def rad(self, *cols, cx=0.38, cy=0.3, r=0.8):
        i = self.id("r")
        n = max(1, len(cols) - 1)
        stops = "".join(f'<stop offset="{k / n:.2f}" stop-color="{c}"/>' for k, c in enumerate(cols))
        self.defs.append(f'<radialGradient id="{i}" cx="{cx}" cy="{cy}" r="{r}">{stops}</radialGradient>')
        return f"url(#{i})"

    def blur(self, sd):
        i = self.id("f")
        self.defs.append(f'<filter id="{i}" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="{sd}"/></filter>')
        return f"url(#{i})"

    def add(self, s):
        self.body.append(s)
        return self

    def ground(self, cx=128, cy=232, rx=74, ry=11, op=0.18):
        return self.add(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#3a1f30" opacity="{op}"/>')

    def vol(self, d, light, base, dark, w=7, hl=None, rim=True, hl_op=0.55, rim_op=0.35, flat=False, line=LINE, extra=""):
        """A painted volume: gradient fill, darker rim along the bottom edge, soft highlight, outline."""
        fill = base if flat else self.lin(light, base, dark)
        cid = self.id("c")
        self.defs.append(f'<clipPath id="{cid}"><path d="{d}"/></clipPath>')
        s = f'<path d="{d}" fill="{fill}"/>'
        inner = ""
        if rim:
            inner += (f'<path d="{d}" fill="none" stroke="{dark}" stroke-width="22" opacity="{rim_op}" '
                      f'transform="translate(0 -9)" filter="{self.blur(5)}"/>')
        if hl:
            cx, cy, rx, ry = hl
            inner += f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#fff" opacity="{hl_op}" filter="{self.blur(6)}"/>'
        inner += extra
        if inner:
            s += f'<g clip-path="url(#{cid})">{inner}</g>'
        if w:
            s += f'<path d="{d}" fill="none" stroke="{line}" stroke-width="{w}" stroke-linejoin="round" stroke-linecap="round"/>'
        return self.add(s)

    def line(self, d, col, w, cap="round", op=1.0):
        return self.add(f'<path d="{d}" fill="none" stroke="{col}" stroke-width="{w}" stroke-linecap="{cap}" stroke-linejoin="round" opacity="{op}"/>')

    def shine(self, d, w=9, op=0.85):
        """Crisp white highlight stroke."""
        return self.line(d, "#fff", w, op=op)

    def spark(self, cx, cy, s, col="#fffbe0"):
        d = (f"M{cx} {cy - s} Q{cx + s * .16} {cy - s * .16} {cx + s} {cy} Q{cx + s * .16} {cy + s * .16} {cx} {cy + s} "
             f"Q{cx - s * .16} {cy + s * .16} {cx - s} {cy} Q{cx - s * .16} {cy - s * .16} {cx} {cy - s} Z")
        return self.add(f'<path d="{d}" fill="{col}"/>')

    def svg(self):
        return (f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">'
                f'<defs>{"".join(self.defs)}</defs>{"".join(self.body)}</svg>')


def rect_d(x, y, w, h, r):
    return (f"M{x + r} {y} L{x + w - r} {y} Q{x + w} {y} {x + w} {y + r} L{x + w} {y + h - r} Q{x + w} {y + h} {x + w - r} {y + h} "
            f"L{x + r} {y + h} Q{x} {y + h} {x} {y + h - r} L{x} {y + r} Q{x} {y} {x + r} {y} Z")


def ell_d(cx, cy, rx, ry):
    return f"M{cx - rx} {cy} A{rx} {ry} 0 1 0 {cx + rx} {cy} A{rx} {ry} 0 1 0 {cx - rx} {cy} Z"


ITEMS = {}


def item(name):
    def wrap(fn):
        p = Pen()
        fn(p)
        ITEMS[name] = p.svg()
        return fn
    return wrap


# ============================================================================ drinks
def glass(p, x0, x1, top, bottom, inset=8):
    """A drinking glass (trapezoid) — returns the inner path for the liquid."""
    d = f"M{x0} {top} L{256 - x0} {top} L{256 - x1} {bottom - 10} Q{256 - x1} {bottom} {256 - x1 - 10} {bottom} L{x1 + 10} {bottom} Q{x1} {bottom} {x1} {bottom - 10} Z"
    p.add(f'<path d="{d}" fill="#eaf8ff" opacity=".55"/>')
    return d


@item("drink_1")
def water(p):
    p.ground()
    d = glass(p, 62, 78, 46, 226)
    liquid = "M70 96 L186 96 L178 216 Q178 222 170 222 L86 222 Q78 222 78 216 Z"
    p.vol(liquid, "#8fdcff", "#45aef0", "#2a7fd0", w=0, hl=(104, 130, 16, 50))
    p.add('<ellipse cx="128" cy="96" rx="58" ry="8" fill="#bfeaff"/>')
    for x, y, r in ((110, 170, 6), (146, 140, 5), (130, 196, 4), (154, 186, 7)):
        p.add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#fff" opacity=".75"/>')
    p.add(f'<path d="{d}" fill="none" stroke="{LINE}" stroke-width="7" stroke-linejoin="round"/>')
    p.add(f'<ellipse cx="128" cy="46" rx="66" ry="9" fill="#f4fbff" stroke="{LINE}" stroke-width="6"/>')
    p.shine("M78 64 L88 196", 9, 0.9)
    p.spark(204, 70, 12)


@item("drink_2")
def milk(p):
    p.ground()
    body = "M92 92 Q92 76 104 70 L104 54 L152 54 L152 70 Q164 76 164 92 L170 112 Q176 124 176 140 L176 214 Q176 228 162 228 L94 228 Q80 228 80 214 L80 140 Q80 124 86 112 Z"
    p.vol(body, "#ffffff", "#f1f4f8", "#c9d4e2", hl=(104, 150, 14, 52))
    p.vol(rect_d(96, 28, 64, 32, 10), "#7cc0ff", "#3d7fe6", "#2b5ec0", hl=(116, 36, 18, 6))
    p.vol(rect_d(80, 140, 96, 62, 10), "#8fd0ff", "#4fa0f0", "#2f78d0", w=6, rim=False, hl=(110, 150, 30, 8))
    p.add('<path d="M128 152 Q142 170 142 178 Q142 192 128 192 Q114 192 114 178 Q114 170 128 152 Z" fill="#fff"/>')
    p.shine("M92 118 L92 132", 8)
    p.spark(200, 80, 12)


@item("drink_3")
def tea(p):
    p.ground(128, 234, 100, 12)
    # Saucer: a shallow dish seen from the side.
    p.vol("M20 206 Q20 190 128 188 Q236 190 236 206 Q236 226 128 228 Q20 226 20 206 Z", "#ff8a7a", "#e04848", "#a82a34", hl=(84, 198, 40, 6))
    p.add(f'<ellipse cx="128" cy="202" rx="96" ry="13" fill="#c8303a" stroke="{LINE}" stroke-width="5"/>')
    p.add('<ellipse cx="128" cy="200" rx="70" ry="8" fill="#ff8a7a" opacity=".6"/>')
    # Turkish tea glass: wide rim, gentle waist, round belly, small foot.
    glass = ("M74 40 L182 40 Q180 74 166 98 Q158 110 160 122 Q186 142 184 174 Q182 202 152 204 L104 204 "
             "Q74 202 72 174 Q70 142 96 122 Q98 110 90 98 Q76 74 74 40 Z")
    p.add(f'<path d="{glass}" fill="#f2fbff" opacity=".55"/>')
    tea = ("M80 58 L176 58 Q174 80 162 100 Q152 114 154 126 Q178 144 176 172 Q174 194 150 196 L106 196 "
           "Q82 194 80 172 Q78 144 102 126 Q104 114 94 100 Q82 80 80 58 Z")
    p.vol(tea, "#ffa04a", "#d8461e", "#7a1a0c", w=0, rim_op=0.35, hl=(104, 150, 12, 26))
    p.add('<ellipse cx="128" cy="58" rx="48" ry="7" fill="#ffb36a"/>')
    p.add(f'<path d="{glass}" fill="none" stroke="{LINE}" stroke-width="7" stroke-linejoin="round"/>')
    p.add(f'<ellipse cx="128" cy="40" rx="54" ry="8" fill="#f4fbff" fill-opacity=".8" stroke="{LINE}" stroke-width="6"/>')
    # Glass shine: a long streak on the left, a short one on the belly.
    p.shine("M86 52 Q90 80 102 98", 8, 0.9)
    p.shine("M86 150 Q84 168 92 182", 7, 0.75)
    # Spoon resting in the glass, sugar cube on the saucer.
    p.line("M150 30 L172 4", LINE, 12)
    p.line("M150 30 L172 4", "#d6dde8", 5)
    p.vol(rect_d(186, 176, 26, 24, 5), "#ffffff", "#f6f2ee", "#d6cfc6", w=5, rim=False)
    for x in (110, 146):
        p.line(f"M{x} 24 Q{x - 8} 12 {x} 0", "#fff", 6, op=0.7)


@item("drink_4")
def coffee(p):
    p.ground(128, 230, 96, 12)
    p.vol(ell_d(128, 208, 98, 22), "#ffffff", "#eef2f6", "#c4cede", hl=(100, 200, 40, 6))
    p.vol("M196 120 Q232 120 230 150 Q228 176 192 176", "#fff", "#eef2f6", "#c4cede", w=0, rim=False)
    p.line("M192 124 Q226 124 224 150 Q222 172 192 172", LINE, 26)
    p.line("M192 124 Q226 124 224 150 Q222 172 192 172", "#f4f6fa", 12)
    cup = "M50 96 L206 96 L198 170 Q192 204 158 206 L98 206 Q64 204 58 170 Z"
    p.vol(cup, "#ffffff", "#f1f4f8", "#c4cede", hl=(84, 130, 14, 34))
    p.add(f'<path d="M60 150 L196 150" stroke="#e8504f" stroke-width="12"/>')
    p.add(f'<ellipse cx="128" cy="96" rx="78" ry="16" fill="#8a5232" stroke="{LINE}" stroke-width="7"/>')
    p.add('<ellipse cx="128" cy="96" rx="62" ry="10" fill="#c98a52"/>')
    p.add('<path d="M128 104 Q112 94 116 88 Q122 84 128 92 Q134 84 140 88 Q144 94 128 104 Z" fill="#fff4e6"/>')
    for x in (104, 128, 152):
        p.line(f"M{x} 70 Q{x - 10} 54 {x} 40 Q{x + 10} 28 {x} 16", "#fff", 7, op=0.7)


@item("drink_5")
def lemonade(p):
    p.ground()
    p.line("M150 22 L136 120", LINE, 18)
    p.line("M150 22 L136 120", "#fff", 9)
    p.line("M150 22 L136 120", "#ff4f6d", 9, cap="butt", op=1).body[-1] = p.body[-1].replace('stroke-linecap="butt"', 'stroke-dasharray="10 10"')
    d = glass(p, 66, 80, 52, 228)
    p.vol("M74 84 L182 84 L176 216 Q176 222 168 222 L88 222 Q80 222 80 216 Z", "#fff6a0", "#ffd23a", "#f0a01f", w=0, hl=(104, 120, 14, 50))
    for x, y in ((100, 130), (140, 170), (112, 190)):
        p.add(f'<rect x="{x}" y="{y}" width="26" height="24" rx="6" fill="#fff" opacity=".7" stroke="#fff6c4" stroke-width="3" transform="rotate(12 {x + 13} {y + 12})"/>')
    p.add(f'<path d="{d}" fill="none" stroke="{LINE}" stroke-width="7" stroke-linejoin="round"/>')
    p.add(f'<ellipse cx="128" cy="52" rx="62" ry="9" fill="#f4fbff" stroke="{LINE}" stroke-width="6"/>')
    # lemon slice on the rim
    p.vol(ell_d(186, 52, 34, 34), "#fff6a0", "#ffd23a", "#e8a01f", hl=(176, 40, 10, 8))
    p.add('<circle cx="186" cy="52" r="24" fill="#fff4b8"/>')
    for a in range(0, 360, 60):
        x, y = 186 + 20 * math.cos(math.radians(a)), 52 + 20 * math.sin(math.radians(a))
        p.line(f"M186 52 L{x:.0f} {y:.0f}", "#f0c020", 4)
    p.shine("M82 70 L92 200", 9, 0.9)


@item("drink_6")
def milkshake(p):
    p.ground()
    p.line("M164 14 L148 96", LINE, 18)
    p.line("M164 14 L148 96", "#fff", 9)
    cup = "M70 96 L186 96 L170 210 Q168 226 152 226 L104 226 Q88 226 86 210 Z"
    p.vol(cup, "#ffc6dc", "#ff8ab0", "#e05a8a", hl=(96, 140, 12, 40))
    p.add('<path d="M78 130 L178 130" stroke="#fff" stroke-width="10" opacity=".5"/>')
    cream = "M60 104 Q52 84 74 78 Q74 54 100 56 Q108 36 132 42 Q156 36 164 58 Q190 58 186 80 Q206 86 194 106 Z"
    p.vol(cream, "#ffffff", "#fff4f8", "#f2cddc", hl=(104, 64, 24, 10))
    for x, y, c in ((96, 76, "#ff6b9a"), (140, 70, "#7cc0ff"), (118, 92, "#ffd23a"), (160, 92, "#8de865")):
        p.add(f'<rect x="{x}" y="{y}" width="12" height="5" rx="2.5" fill="{c}" transform="rotate(30 {x} {y})"/>')
    p.vol(ell_d(128, 34, 18, 18), "#ff8a8a", "#e8344f", "#a4202c", hl=(122, 28, 6, 5))
    p.line("M132 18 Q140 4 152 8", "#3e9c3a", 6)
    p.spark(206, 60, 12)


# ============================================================================ food
@item("food_1")
def egg(p):
    p.ground(128, 230, 64, 11)
    d = "M128 24 Q194 30 200 136 Q202 222 128 222 Q54 222 56 136 Q62 30 128 24 Z"
    p.vol(d, "#fffdf8", "#f6ead8", "#d6bea0", hl=(100, 84, 22, 40))
    for x, y, r in ((150, 150, 5), (104, 172, 4), (164, 112, 3), (124, 196, 4)):
        p.add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#d9bb94" opacity=".7"/>')
    p.spark(196, 54, 12)


@item("food_2")
def toast(p):
    p.ground(128, 232, 82, 11)
    crust = "M50 116 Q36 66 76 50 Q100 30 128 44 Q156 30 180 50 Q220 66 206 116 L200 214 Q200 226 188 226 L68 226 Q56 226 56 214 Z"
    p.vol(crust, "#e7a565", "#c47a3a", "#8f4f20", hl=(96, 60, 30, 10))
    inner = "M68 116 Q58 76 86 66 Q106 52 128 62 Q150 52 170 66 Q198 76 188 116 L184 206 Q184 212 178 212 L78 212 Q72 212 72 206 Z"
    p.vol(inner, "#fff2c8", "#ffd98a", "#e8b060", w=0, hl=(110, 100, 30, 24), rim_op=0.25)
    p.add(f'<path d="M98 150 Q100 126 124 128 L160 132 Q170 136 166 160 L158 176 Q150 186 132 182 L106 176 Q96 170 98 150 Z" fill="#ffe680" stroke="#e8b020" stroke-width="5" transform="rotate(-8 128 150)"/>')
    p.add('<path d="M108 144 L126 140" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".8"/>')


@item("food_3")
def sandwich(p):
    p.ground(128, 232, 92, 11)
    p.vol("M30 200 L128 50 L226 200 Q226 214 212 214 L44 214 Q30 214 30 200 Z", "#ffe0a8", "#f2b468", "#c47a3a", hl=(110, 100, 18, 30))
    p.add(f'<path d="M48 176 L208 176" stroke="{LINE}" stroke-width="22" stroke-linecap="round"/>')
    p.add('<path d="M48 176 Q68 160 88 176 Q108 160 128 176 Q148 160 168 176 Q188 160 208 176" stroke="#7ad04a" stroke-width="14" fill="none" stroke-linecap="round"/>')
    p.add('<path d="M58 192 L198 192" stroke="#e8504f" stroke-width="14" stroke-linecap="round"/>')
    p.add('<path d="M64 160 L192 160" stroke="#ffd23a" stroke-width="12" stroke-linecap="round"/>')
    p.vol("M30 200 L128 50 L226 200", "#fff", "#fff", "#fff", w=7, rim=False, flat=True).body[-1] = p.body[-1].replace('fill="#fff"', 'fill="none"', 1)
    p.add('<rect x="122" y="22" width="12" height="44" rx="6" fill="#c98a52" stroke="' + LINE + '" stroke-width="5"/>')
    p.add('<circle cx="128" cy="22" r="9" fill="#8de865" stroke="' + LINE + '" stroke-width="5"/>')
    p.shine("M96 116 L118 82", 8)


@item("food_4")
def burger(p):
    p.ground(128, 232, 90, 11)
    p.vol(rect_d(44, 186, 168, 38, 18), "#f2b468", "#d98a3a", "#a8622c", hl=(90, 192, 30, 5))
    p.vol(rect_d(38, 160, 180, 32, 16), "#9a5a34", "#6e3a20", "#4a2414", hl=(90, 166, 30, 4))
    p.add('<path d="M36 152 Q56 170 76 152 Q96 170 116 152 Q136 170 156 152 Q176 170 196 152 Q212 166 222 152 L222 146 L36 146 Z" fill="#ffd23a" stroke="' + LINE + '" stroke-width="5" stroke-linejoin="round"/>')
    p.add('<path d="M30 140 Q50 128 70 140 Q90 128 110 140 Q130 128 150 140 Q170 128 190 140 Q210 128 226 140" fill="none" stroke="' + LINE + '" stroke-width="20" stroke-linecap="round"/>')
    p.add('<path d="M30 140 Q50 128 70 140 Q90 128 110 140 Q130 128 150 140 Q170 128 190 140 Q210 128 226 140" fill="none" stroke="#7ad04a" stroke-width="10" stroke-linecap="round"/>')
    bun = "M36 128 Q36 40 128 38 Q220 40 220 128 Q220 136 210 136 L46 136 Q36 136 36 128 Z"
    p.vol(bun, "#ffd08a", "#f0a050", "#c4702a", hl=(96, 64, 34, 14))
    for x, y in ((90, 80), (128, 66), (164, 84), (110, 102), (150, 108), (184, 112), (72, 110)):
        p.add(f'<ellipse cx="{x}" cy="{y}" rx="6" ry="3.5" fill="#fff6e0" transform="rotate(-20 {x} {y})"/>')


@item("food_5")
def pizza(p):
    p.ground(128, 230, 100, 12)
    p.vol(ell_d(128, 134, 104, 92), "#f2c070", "#d98a3a", "#a8622c", hl=(90, 70, 40, 14))
    p.vol(ell_d(128, 132, 88, 76), "#ffe08a", "#ffc23a", "#e8902a", w=0, hl=(100, 96, 30, 16), rim_op=0.3)
    p.add('<ellipse cx="128" cy="132" rx="76" ry="64" fill="#e8504f" opacity=".9"/>')
    for x, y, r in ((104, 112, 16), (150, 124, 18), (120, 160, 15), (160, 160, 12), (84, 146, 12)):
        p.add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#fff4c8" opacity=".95"/>')
    for x, y in ((96, 120), (150, 112), (124, 150), (166, 150), (100, 168), (136, 92)):
        p.add(f'<circle cx="{x}" cy="{y}" r="12" fill="#c8303a" stroke="#8e2232" stroke-width="3"/><circle cx="{x - 3}" cy="{y - 3}" r="3" fill="#ff8a8a"/>')
    for x, y in ((114, 128), (146, 136), (84, 144), (172, 122)):
        p.add(f'<path d="M{x} {y} q6 -8 12 0 q-6 8 -12 0 Z" fill="#5cb84a"/>')
    for a in (30, 90, 150):
        x, y = 128 + 86 * math.cos(math.radians(a)), 132 + 74 * math.sin(math.radians(a))
        p.line(f"M{256 - x:.0f} {264 - y:.0f} L{x:.0f} {y:.0f}", "#c47a3a", 4, op=0.6)


@item("food_6")
def cake(p):
    p.ground(128, 232, 96, 12)
    p.vol(ell_d(128, 214, 100, 16), "#ffffff", "#eef2f6", "#c4cede")
    p.vol(rect_d(44, 140, 168, 74, 18), "#ffd6e6", "#ff9cbc", "#e05a8a", hl=(80, 160, 20, 20))
    p.add('<path d="M44 158 Q60 176 76 158 Q92 176 108 158 Q124 176 140 158 Q156 176 172 158 Q188 176 212 158 L212 150 L44 150 Z" fill="#fff6f8"/>')
    p.vol(rect_d(72, 84, 112, 62, 16), "#fff4f8", "#ffe2ec", "#f2b8cc", hl=(96, 96, 18, 14))
    p.add('<path d="M72 98 Q86 114 100 98 Q114 114 128 98 Q142 114 156 98 Q170 114 184 98 L184 92 L72 92 Z" fill="#ff9cbc"/>')
    for x in (60, 100, 140, 180):
        p.vol(ell_d(x + 8, 136, 13, 12), "#ff8a8a", "#e8344f", "#a4202c", w=5, hl=(x + 4, 132, 4, 3))
    p.vol(rect_d(120, 36, 16, 50, 6), "#c9f0ff", "#7cc4f2", "#4b8fc0", w=5)
    p.add('<path d="M128 6 Q142 22 136 32 Q128 40 120 32 Q114 22 128 6 Z" fill="#ffd23a" stroke="#f08a1f" stroke-width="4"/>')
    p.spark(200, 60, 12)
    p.spark(56, 70, 9)


# ============================================================================ bath
def bubble(p, x, y, r):
    p.add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#eaf8ff" fill-opacity=".6" stroke="{LINE}" stroke-width="5"/>')
    p.add(f'<path d="M{x - r * .5} {y - r * .2} Q{x - r * .45} {y - r * .6} {x - r * .1} {y - r * .6}" stroke="#fff" stroke-width="{max(3, r * .22):.0f}" fill="none" stroke-linecap="round"/>')


@item("bath_1")
def soap(p):
    p.ground(128, 226, 90, 12)
    p.vol("M40 150 Q40 122 68 118 L188 110 Q216 110 218 138 L220 176 Q220 204 192 206 L68 212 Q40 212 40 186 Z", "#ffd6e6", "#ff9cbc", "#e05a8a", hl=(90, 132, 34, 10))
    p.add('<path d="M58 132 Q58 124 72 122 L182 116 Q200 116 200 132 Q200 146 182 148 L72 154 Q58 154 58 142 Z" fill="#ffc6dc"/>')
    p.add('<path d="M128 142 Q112 126 120 120 Q128 116 130 124 Q134 114 142 120 Q148 128 128 142 Z" fill="#fff"/>')
    for x, y, r in ((84, 84, 22), (124, 62, 15), (164, 80, 26), (196, 46, 12), (60, 50, 10)):
        bubble(p, x, y, r)


@item("bath_2")
def shampoo(p):
    p.ground()
    p.vol(rect_d(110, 40, 36, 30, 6), "#eef2f8", "#b4bfd0", "#7a869a", w=6)
    p.vol("M100 40 L146 40 L176 30 Q186 28 186 38 L186 46 L146 52 L110 52 Z", "#eef2f8", "#b4bfd0", "#7a869a", w=6)
    p.vol("M86 96 Q86 70 112 68 L144 68 Q170 70 170 96 L176 210 Q176 228 158 228 L98 228 Q80 228 80 210 Z", "#a8f0e4", "#3ec8b4", "#1f8a80", hl=(104, 120, 14, 44))
    p.vol(rect_d(94, 128, 68, 70, 14), "#ffffff", "#f6faff", "#d6e2ee", w=6, rim=False)
    p.add('<path d="M128 140 Q144 160 144 170 Q144 186 128 186 Q112 186 112 170 Q112 160 128 140 Z" fill="#3ec8b4"/>')
    bubble(p, 196, 92, 14)
    bubble(p, 210, 132, 9)


@item("bath_3")
def towel(p):
    p.ground(128, 226, 96, 12)
    p.vol(rect_d(36, 110, 184, 104, 26), "#9ad8ff", "#4fa8f0", "#2a76c8", hl=(80, 130, 40, 12))
    p.vol(rect_d(36, 82, 184, 58, 26), "#bfe6ff", "#7cc4f8", "#3a8ee0", hl=(90, 92, 50, 8))
    for x in range(52, 210, 16):
        p.add(f'<rect x="{x}" y="186" width="7" height="22" rx="3.5" fill="#fff" opacity=".55"/>')
    p.add('<path d="M44 168 L212 168" stroke="#fff" stroke-width="10" opacity=".7"/>')
    p.spark(206, 56, 12)


@item("bath_4")
def towel_set(p):
    p.ground(128, 228, 98, 12)
    for y, c in ((166, ("#ffd6e6", "#ff9cbc", "#e05a8a")), (118, ("#fff6c4", "#ffd75a", "#e8a01f")), (70, ("#c9f4ea", "#5cd2bc", "#2a9a88"))):
        p.vol(rect_d(44, y, 168, 56, 22), *c, hl=(90, y + 12, 40, 8))
        p.add(f'<path d="M52 {y + 40} L204 {y + 40}" stroke="#fff" stroke-width="7" opacity=".55"/>')
    p.vol(rect_d(116, 62, 24, 164, 6), "#ff8a8a", "#e8344f", "#a4202c", w=6, rim=False)
    p.vol("M128 66 C100 24 64 34 76 56 C84 70 112 70 128 66 Z", "#ff8a8a", "#e8344f", "#a4202c", w=6, rim=False)
    p.vol("M128 66 C156 24 192 34 180 56 C172 70 144 70 128 66 Z", "#ff8a8a", "#e8344f", "#a4202c", w=6, rim=False)
    p.vol(ell_d(128, 64, 12, 10), "#ff8a8a", "#e8344f", "#a4202c", w=6, rim=False)


@item("bath_5")
def bathrobe(p):
    p.ground(128, 234, 86, 11)
    # Wooden hanger with a hook.
    p.add(f'<path d="M128 30 L128 18 Q128 6 140 8 Q150 12 146 22" fill="none" stroke="{LINE}" stroke-width="10" stroke-linecap="round"/>')
    p.add('<path d="M128 30 L128 18 Q128 6 140 8 Q150 12 146 22" fill="none" stroke="#c9cfd8" stroke-width="4" stroke-linecap="round"/>')
    p.add(f'<path d="M50 62 Q128 18 206 62" fill="none" stroke="{LINE}" stroke-width="18" stroke-linecap="round"/>')
    p.add('<path d="M50 62 Q128 18 206 62" fill="none" stroke="#d9964a" stroke-width="9" stroke-linecap="round"/>')
    # Sleeves hanging down at the sides.
    for s in (-1, 1):
        x = 128 + s * 74
        sleeve = f"M{128 + s * 52} 56 L{x + s * 20} 74 Q{x + s * 34} 120 {x + s * 26} 170 L{x - s * 6} 172 Q{x - s * 4} 120 {128 + s * 46} 96 Z"
        p.vol(sleeve, "#ffffff", "#f3eee8", "#cfc2b4", w=7, hl=(x, 100, 8, 26))
        p.add(f'<path d="M{x + s * 26} 160 L{x - s * 6} 162" stroke="#ff9cbc" stroke-width="16" stroke-linecap="round"/>')
    # Body of the robe.
    body = "M78 56 L178 56 Q186 140 190 222 Q190 230 182 230 L74 230 Q66 230 66 222 Q70 140 78 56 Z"
    p.vol(body, "#ffffff", "#f3eee8", "#cfc2b4", hl=(100, 110, 18, 50))
    # Wrap: the right front lies over the left; shawl collar in pink.
    p.add(f'<path d="M150 56 L104 150 L114 230" fill="none" stroke="{LINE}" stroke-width="5" stroke-linejoin="round"/>')
    p.add(f'<path d="M90 54 Q104 54 112 60 L148 140 L136 150 L98 70 Z" fill="#ff9cbc" stroke="{LINE}" stroke-width="5" stroke-linejoin="round"/>')
    p.add(f'<path d="M166 54 Q152 54 144 60 L106 150 L118 152 L158 70 Z" fill="#ffb8d0" stroke="{LINE}" stroke-width="5" stroke-linejoin="round"/>')
    # Belt and knot with hanging ends.
    p.add(f'<path d="M70 150 L186 150" stroke="{LINE}" stroke-width="22"/><path d="M71 150 L185 150" stroke="#ff9cbc" stroke-width="14"/>')
    p.add(f'<path d="M126 152 L112 200 L124 202 L132 156 Z M132 152 L150 196 L160 190 L138 150 Z" fill="#ff9cbc" stroke="{LINE}" stroke-width="5" stroke-linejoin="round"/>')
    p.vol(rect_d(122, 140, 20, 22, 7), "#ffc6dc", "#ff9cbc", "#e05a8a", w=5, rim=False)
    # Pockets.
    p.add(f'<path d="M78 176 L100 176 L100 202 Q100 208 94 208 L84 208 Q78 208 78 202 Z" fill="none" stroke="#d9cfc4" stroke-width="5"/>')
    p.add(f'<path d="M150 176 L174 176 L174 202 Q174 208 168 208 L156 208 Q150 208 150 202 Z" fill="none" stroke="#d9cfc4" stroke-width="5"/>')


@item("bath_6")
def spa_basket(p):
    p.ground(128, 232, 100, 12)
    p.add(f'<path d="M58 120 Q128 20 198 120" fill="none" stroke="{LINE}" stroke-width="18" stroke-linecap="round"/>')
    p.add('<path d="M58 120 Q128 20 198 120" fill="none" stroke="#d9a060" stroke-width="9" stroke-linecap="round"/>')
    p.vol(rect_d(150, 64, 30, 70, 10), "#a8f0e4", "#3ec8b4", "#1f8a80", w=6)
    p.vol(rect_d(156, 50, 18, 18, 4), "#eef2f8", "#b4bfd0", "#7a869a", w=5)
    p.vol(rect_d(64, 92, 82, 44, 18), "#ffd6e6", "#ff9cbc", "#e05a8a", w=6)
    for a in range(0, 360, 72):
        x, y = 112 + 16 * math.cos(math.radians(a)), 84 + 16 * math.sin(math.radians(a))
        p.add(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="11" fill="#fff" stroke="{LINE}" stroke-width="4"/>')
    p.add(f'<circle cx="112" cy="84" r="9" fill="#ffd23a" stroke="{LINE}" stroke-width="4"/>')
    basket = "M36 124 L220 124 L204 210 Q200 226 184 226 L72 226 Q56 226 52 210 Z"
    weave = "".join(f'<path d="M{x} 124 L{x + (x - 128) * 0.1:.0f} 226" stroke="#a8622c" stroke-width="4" opacity=".6"/>' for x in range(56, 210, 20))
    weave += "".join(f'<path d="M30 {y} L226 {y}" stroke="#a8622c" stroke-width="4" opacity=".6"/>' for y in (150, 176, 202))
    p.vol(basket, "#f2c27a", "#d9964a", "#a8622c", hl=(80, 140, 30, 10), extra=weave)
    p.vol(rect_d(28, 116, 200, 20, 10), "#f2c27a", "#d9964a", "#a8622c", w=7, rim=False)
    p.spark(214, 60, 12)


# ============================================================================ care
@item("care_1")
def comb(p):
    p.ground(128, 214, 90, 10)
    teeth = "".join(f'<rect x="{x}" y="128" width="10" height="64" rx="5" fill="#9b7bf5" stroke="{LINE}" stroke-width="5"/>' for x in range(52, 206, 18))
    p.add(f'<g transform="rotate(-14 128 128)">{teeth}</g>')
    p.add('<g transform="rotate(-14 128 128)">')
    p.vol(rect_d(40, 92, 180, 48, 22), "#d4c2ff", "#9b7bf5", "#6a49d6", hl=(90, 102, 50, 8))
    p.add('</g>')
    p.spark(206, 62, 12)


@item("care_2")
def toothbrush(p):
    p.ground(128, 214, 96, 10)
    p.add('<g transform="rotate(-24 128 128)">')
    p.vol(rect_d(20, 120, 158, 30, 15), "#9ad8ff", "#4fa8f0", "#2a76c8", hl=(60, 126, 30, 4))
    p.vol(rect_d(170, 116, 66, 38, 14), "#9ad8ff", "#4fa8f0", "#2a76c8")
    bristles = "".join(f'<rect x="{x}" y="80" width="11" height="40" rx="4" fill="{c}" stroke="{LINE}" stroke-width="4"/>'
                       for x, c in zip(range(176, 236, 13), ("#fff", "#bfeaff", "#fff", "#bfeaff", "#fff")))
    p.add(bristles)
    p.vol("M172 82 Q174 56 200 60 Q214 46 228 60 Q244 62 238 82 Z", "#ffffff", "#eafcff", "#9adfe8", w=6, rim=False)
    p.add('<path d="M184 74 Q204 66 226 72" stroke="#5cd2bc" stroke-width="6" fill="none" stroke-linecap="round"/>')
    p.add('</g>')


def slipper(p, dx, dy, back=False):
    """A hotel slipper in side view: flat thick sole, open heel, puffy closed toe with a pompom (toe to the right)."""
    p.add(f'<g transform="translate({dx} {dy})">')
    dark = back
    sole = "M22 176 Q20 158 44 156 L196 154 Q234 154 234 180 Q232 204 200 204 L42 204 Q22 202 22 186 Z"
    p.vol(sole, "#f4f0f2", "#d9ccd2", "#a8949e" if not dark else "#8e7a84", hl=(70, 166, 40, 4))
    # soft pink insole on the open heel part
    p.add(f'<path d="M34 164 Q34 158 46 158 L104 157 L104 170 L46 171 Q34 171 34 164 Z" fill="#ffc6dc" stroke="{LINE}" stroke-width="4"/>')
    upper = "M96 168 Q96 104 158 98 Q222 96 230 168 Z"
    p.vol(upper, "#ffd6e6", "#ff8ab0" if not dark else "#e8709a", "#c04476", hl=(140, 112, 26, 12))
    # stitched seam where the cover meets the sole
    p.add('<path d="M102 160 Q160 150 224 160" stroke="#fff" stroke-width="4" fill="none" stroke-dasharray="8 8" opacity=".75"/>')
    p.vol(ell_d(150, 104, 18, 17), "#ffffff", "#fff4f8", "#f2cddc", w=5, hl=(144, 98, 6, 5))
    p.add('</g>')


@item("care_3")
def slippers(p):
    p.ground(128, 226, 110, 12)
    p.add('<g transform="translate(128 128) scale(0.94) translate(-128 -128)">')
    slipper(p, 6, -50, back=True)
    slipper(p, -14, 4)
    p.add('</g>')
    p.spark(44, 70, 12)


@item("care_4")
def hair_dryer(p):
    p.ground(128, 232, 92, 11)
    # Cord curling from the handle.
    p.line("M104 222 Q90 238 70 228 Q52 218 66 206 Q80 196 70 186", LINE, 9)
    p.line("M104 222 Q90 238 70 228 Q52 218 66 206 Q80 196 70 186", "#ff8ab0", 4)
    # Handle, slanted back under the body.
    handle = "M96 124 L140 124 L130 214 Q128 226 114 226 L104 226 Q90 224 92 210 Z"
    p.vol(handle, "#ffc6dc", "#ff8ab0", "#d0507e", hl=(110, 150, 8, 30))
    p.add(f'<rect x="116" y="146" width="12" height="28" rx="6" fill="#fff" stroke="{LINE}" stroke-width="4"/>')
    # Body: round back and a barrel that narrows to the nozzle.
    body = "M66 46 Q114 46 130 70 L196 78 Q206 80 206 92 L206 116 Q206 128 196 130 L130 138 Q114 160 66 160 Q12 160 12 104 Q12 46 66 46 Z"
    p.vol(body, "#ffd6e6", "#ff9cbc", "#e05a8a", hl=(56, 66, 34, 12))
    # Nozzle.
    p.vol(rect_d(198, 74, 34, 60, 10), "#8a94a6", "#5a6474", "#353d4a", w=6, hl=(206, 80, 10, 4))
    p.add(f'<rect x="222" y="82" width="4" height="44" rx="2" fill="#2a2f3a"/>')
    # Air intake at the back: a round grille.
    p.add(f'<circle cx="64" cy="104" r="36" fill="#fff4f8" stroke="{LINE}" stroke-width="6"/>')
    for r in (26, 16, 6):
        p.add(f'<circle cx="64" cy="104" r="{r}" fill="none" stroke="#e05a8a" stroke-width="4"/>')
    p.line("M38 104 L90 104 M64 78 L64 130", "#e05a8a", 4)
    p.shine("M112 60 Q150 64 178 70", 8, 0.8)
    for y in (82, 104, 126):
        p.line(f"M240 {y} Q248 {y - 6} 256 {y}", "#7cc4f2", 5, op=0.9)


@item("care_5")
def iron(p):
    p.ground(128, 226, 100, 12)
    p.add(f'<path d="M92 120 L92 84 Q92 62 116 62 L184 62 Q206 62 206 86 L206 120" fill="none" stroke="{LINE}" stroke-width="30" stroke-linecap="round"/>')
    p.add('<path d="M92 120 L92 84 Q92 62 116 62 L184 62 Q206 62 206 86 L206 120" fill="none" stroke="#3a3a4a" stroke-width="16" stroke-linecap="round"/>')
    body = "M24 196 Q30 112 120 108 L214 108 Q232 108 232 128 L232 196 Z"
    p.vol(body, "#a8f0e4", "#3ec8b4", "#1f8a80", hl=(90, 128, 40, 12))
    p.vol(rect_d(18, 190, 220, 26, 10), "#f4f6fa", "#c4ccd8", "#8a94a6", hl=(80, 194, 60, 4))
    p.add(f'<circle cx="190" cy="152" r="16" fill="#fff" stroke="{LINE}" stroke-width="5"/><path d="M190 152 L190 142" stroke="#e8454f" stroke-width="5" stroke-linecap="round"/>')
    for x in (40, 64, 88):
        p.line(f"M{x} 92 Q{x - 10} 74 {x} 58 Q{x + 10} 44 {x} 30", "#bfeaff", 7, op=0.85)


@item("care_6")
def suitcase(p):
    p.ground(128, 232, 92, 12)
    p.add(f'<rect x="96" y="30" width="64" height="48" rx="16" fill="none" stroke="{LINE}" stroke-width="22"/>')
    p.add('<rect x="96" y="30" width="64" height="48" rx="16" fill="none" stroke="#7a4a2a" stroke-width="10"/>')
    p.vol(rect_d(40, 66, 176, 152, 26), "#ffc27a", "#ff9420", "#d0640a", hl=(84, 90, 34, 16))
    for x in (84, 172):
        p.add(f'<rect x="{x - 8}" y="66" width="16" height="152" fill="#c4600a" opacity=".55"/>')
    p.add(f'<circle cx="78" cy="226" r="10" fill="#4a3a48" stroke="{LINE}" stroke-width="4"/><circle cx="178" cy="226" r="10" fill="#4a3a48" stroke="{LINE}" stroke-width="4"/>')
    p.add(f'<g transform="rotate(-12 120 120)"><rect x="98" y="100" width="56" height="40" rx="8" fill="#fff4e6" stroke="{LINE}" stroke-width="5"/><text x="126" y="130" font-family="Noto Sans" font-weight="900" font-size="26" text-anchor="middle" fill="#3d7fe6">H</text></g>')
    p.add(f'<path d="{star_d(166, 168, 22, 10, round_=0.15)}" fill="#ffd23a" stroke="{LINE}" stroke-width="5" stroke-linejoin="round"/>')
    p.add(f'<rect x="68" y="168" width="40" height="28" rx="6" fill="#7cc0ff" stroke="{LINE}" stroke-width="5" transform="rotate(8 88 182)"/>')


# ============================================================================ pool
@item("pool_1")
def beach_ball(p):
    p.ground(128, 232, 80, 12)
    cols = ("#ff5a5a", "#fff6ee", "#ffd23a", "#fff6ee", "#3d9ef0", "#fff6ee")
    p.add('<defs/>')
    clip = p.id("c")
    p.defs.append(f'<clipPath id="{clip}"><circle cx="128" cy="124" r="98"/></clipPath>')
    seg = ""
    for k, c in enumerate(cols):
        a0, a1 = math.radians(-90 + k * 60), math.radians(-90 + (k + 1) * 60)
        seg += (f'<path d="M128 124 L{128 + 200 * math.cos(a0):.0f} {124 + 200 * math.sin(a0):.0f} '
                f'L{128 + 200 * math.cos(a1):.0f} {124 + 200 * math.sin(a1):.0f} Z" fill="{c}"/>')
    shade = (f'<circle cx="128" cy="124" r="98" fill="none" stroke="#2a1424" stroke-width="30" opacity=".22" transform="translate(0 -10)" filter="{p.blur(6)}"/>'
             f'<ellipse cx="94" cy="76" rx="34" ry="22" fill="#fff" opacity=".6" filter="{p.blur(7)}"/>')
    p.add(f'<g clip-path="url(#{clip})">{seg}{shade}</g>')
    p.add(f'<circle cx="128" cy="124" r="98" fill="none" stroke="{LINE}" stroke-width="7"/>')
    p.add(f'<circle cx="128" cy="124" r="16" fill="#fff6ee" stroke="{LINE}" stroke-width="5"/>')


@item("pool_2")
def armbands(p):
    p.ground(128, 226, 104, 12)
    for x in (78, 178):
        p.vol(ell_d(x, 132, 54, 74), "#ffc27a", "#ff8a2a", "#d0560a", hl=(x - 20, 90, 14, 22))
        p.add(f'<ellipse cx="{x}" cy="132" rx="22" ry="40" fill="#fff4e6" stroke="{LINE}" stroke-width="6"/>')
        p.add(f'<path d="M{x - 52} 132 L{x - 22} 132 M{x + 22} 132 L{x + 52} 132" stroke="#fff" stroke-width="12" opacity=".8"/>')
        p.add(f'<rect x="{x + 34}" y="78" width="12" height="20" rx="5" fill="#ffd23a" stroke="{LINE}" stroke-width="4"/>')


@item("pool_3")
def duck(p):
    p.ground(128, 230, 96, 12)
    p.add('<ellipse cx="128" cy="220" rx="104" ry="14" fill="#7fe6ff" opacity=".7"/>')
    body = "M40 150 Q36 108 84 112 Q120 116 150 132 Q190 120 214 140 Q222 196 156 214 L100 214 Q42 206 40 150 Z"
    p.vol(body, "#fff3a0", "#ffd23a", "#e8a01f", hl=(90, 140, 30, 14))
    p.vol(ell_d(88, 82, 50, 48), "#fff3a0", "#ffd23a", "#e8a01f", hl=(72, 62, 16, 12))
    p.vol("M30 86 Q14 90 18 104 Q30 112 48 100 Z", "#ffb070", "#ff8a2a", "#d0560a", w=6)
    p.add(f'<circle cx="90" cy="76" r="10" fill="{LINE}"/><circle cx="93" cy="72" r="3.5" fill="#fff"/>')
    p.add('<ellipse cx="106" cy="98" rx="10" ry="6" fill="#ff8a8a" opacity=".5"/>')
    p.add(f'<path d="M150 150 Q176 140 196 150 Q182 176 156 170 Z" fill="#ffc21f" stroke="{LINE}" stroke-width="5" stroke-linejoin="round"/>')


@item("pool_4")
def swim_ring(p):
    p.ground(128, 230, 104, 12)
    ring = f"{ell_d(128, 140, 106, 74)} {ell_d(128, 136, 44, 26)}"
    p.add(f'<path d="{ring}" fill-rule="evenodd" fill="#e05a8a" transform="translate(0 8)" stroke="{LINE}" stroke-width="7"/>')
    p.add(f'<path d="{ring}" fill-rule="evenodd" fill="{p.lin("#ffd6e6", "#ff9cbc", "#e05a8a")}" stroke="{LINE}" stroke-width="7"/>')
    icing = "M30 128 Q34 80 128 72 Q222 80 226 128 Q214 146 196 132 Q184 150 166 136 Q150 150 128 128 Q106 150 90 136 Q72 150 60 132 Q42 146 30 128 Z"
    p.add(f'<path d="{icing}" fill="#fff6f8" opacity=".75"/>')
    p.add(f'<ellipse cx="128" cy="136" rx="44" ry="26" fill="#7fe6ff" stroke="{LINE}" stroke-width="7"/>')
    for x, y, c, r in ((64, 104, "#3d9ef0", 20), (100, 86, "#ffd23a", -30), (156, 88, "#8de865", 40), (190, 108, "#ff6b6b", -10), (78, 160, "#ffd23a", 60), (178, 166, "#3d9ef0", 10), (128, 186, "#ff6b6b", -40)):
        p.add(f'<rect x="{x - 9}" y="{y - 3}" width="18" height="6" rx="3" fill="{c}" transform="rotate({r} {x} {y})"/>')
    p.shine("M48 116 Q62 90 96 80", 9, 0.8)


@item("pool_5")
def air_mattress(p):
    p.ground(128, 226, 112, 12)
    p.add('<g transform="rotate(-10 128 140)">')
    p.vol(rect_d(18, 96, 220, 108, 36), "#8ff0f0", "#2ec4d4", "#178a9a", hl=(70, 110, 50, 10))
    for x in (78, 128, 178):
        p.add(f'<path d="M{x} 104 L{x} 196" stroke="#178a9a" stroke-width="7" stroke-linecap="round" opacity=".6"/>')
        p.add(f'<path d="M{x + 6} 110 L{x + 6} 190" stroke="#fff" stroke-width="4" stroke-linecap="round" opacity=".5"/>')
    p.vol(rect_d(186, 104, 44, 92, 22), "#c9fbff", "#7fe6f0", "#2ec4d4", w=6, rim=False)
    p.add('</g>')
    p.spark(200, 60, 12)


@item("pool_6")
def flamingo(p):
    p.ground(128, 232, 104, 12)
    ring = f"{ell_d(128, 182, 104, 44)} {ell_d(128, 178, 50, 18)}"
    p.add(f'<path d="{ring}" fill-rule="evenodd" fill="{p.lin("#ffd6e6", "#ff8ab0", "#d0507e")}" stroke="{LINE}" stroke-width="7"/>')
    p.add(f'<path d="M154 160 Q170 96 150 64 Q132 36 156 22 Q182 12 196 34" fill="none" stroke="{LINE}" stroke-width="36" stroke-linecap="round"/>')
    p.add(f'<path d="M154 160 Q170 96 150 64 Q132 36 156 22 Q182 12 196 34" fill="none" stroke="#ff8ab0" stroke-width="22" stroke-linecap="round"/>')
    p.add('<path d="M160 140 Q170 96 152 66" fill="none" stroke="#fff" stroke-width="5" stroke-linecap="round" opacity=".5"/>')
    p.vol("M192 26 Q216 24 222 42 Q212 52 196 46 Z", "#ffffff", "#f4f0ea", "#c9bcae", w=6, rim=False)
    p.add(f'<path d="M212 40 Q222 52 214 60 Q206 54 206 46 Z" fill="{LINE}"/>')
    p.add(f'<circle cx="174" cy="26" r="5" fill="{LINE}"/>')
    p.add(f'<path d="M68 166 Q60 140 84 136 Q110 132 120 160" fill="#ffb8d0" stroke="{LINE}" stroke-width="6"/>')
    p.shine("M44 170 Q58 150 88 146", 8, 0.8)


# ============================================================================ beach
@item("beach_1")
def sunglasses(p):
    p.ground(128, 196, 100, 10)
    p.add(f'<path d="M24 96 L8 84 M232 96 L248 84" stroke="{LINE}" stroke-width="16" stroke-linecap="round"/>')
    p.add('<path d="M24 96 L8 84 M232 96 L248 84" stroke="#ff6b9a" stroke-width="7" stroke-linecap="round"/>')
    for x in (74, 182):
        p.vol(rect_d(x - 56, 76, 112, 88, 36), "#ff9cbc", "#ff6b9a", "#c83d6e", hl=(x - 30, 84, 20, 6))
        p.vol(rect_d(x - 44, 88, 88, 64, 26), "#c4a8ff", "#6a49d6", "#2f1e70", w=5, hl=(x - 20, 100, 16, 10), hl_op=0.75)
        p.shine(f"M{x - 28} 136 L{x - 8} 104", 6, 0.6)
    p.add(f'<path d="M116 100 Q128 86 140 100" stroke="{LINE}" stroke-width="18" fill="none" stroke-linecap="round"/>')
    p.add('<path d="M116 100 Q128 86 140 100" stroke="#ff6b9a" stroke-width="8" fill="none" stroke-linecap="round"/>')
    p.spark(222, 52, 14)


@item("beach_2")
def sunscreen(p):
    p.ground(128, 232, 76, 11)
    tube = "M80 46 L176 46 L168 200 Q166 214 152 214 L104 214 Q90 214 88 200 Z"
    p.vol(tube, "#ffffff", "#fff4e6", "#e6cdb0", hl=(100, 90, 12, 40))
    p.vol(rect_d(78, 30, 100, 24, 6), "#ffd75a", "#ffa21f", "#d0640a", w=6)
    p.vol(rect_d(96, 212, 64, 22, 8), "#ffd75a", "#ffa21f", "#d0640a", w=6)
    p.add(f'<circle cx="128" cy="110" r="26" fill="#ffd23a" stroke="#f08a1f" stroke-width="5"/>')
    for a in range(0, 360, 45):
        x0, y0 = 128 + 32 * math.cos(math.radians(a)), 110 + 32 * math.sin(math.radians(a))
        x1, y1 = 128 + 42 * math.cos(math.radians(a)), 110 + 42 * math.sin(math.radians(a))
        p.line(f"M{x0:.0f} {y0:.0f} L{x1:.0f} {y1:.0f}", "#ffa21f", 6)
    p.add('<text x="128" y="182" font-family="Noto Sans" font-weight="900" font-size="30" text-anchor="middle" fill="#ff7a2a">SPF 50</text>')


@item("beach_3")
def straw_hat(p):
    p.ground(128, 214, 112, 12)
    brim = ell_d(128, 158, 118, 44)
    weave = "".join(f'<ellipse cx="128" cy="158" rx="{r}" ry="{r * 0.37:.0f}" fill="none" stroke="#c4884a" stroke-width="3" opacity=".6"/>' for r in (60, 80, 100))
    p.vol(brim, "#ffe8a8", "#f2c46a", "#c48a3a", hl=(80, 146, 40, 10), extra=weave)
    crown = "M70 152 Q66 74 128 70 Q190 74 186 152 Q128 170 70 152 Z"
    p.vol(crown, "#ffe8a8", "#f2c46a", "#c48a3a", hl=(100, 90, 20, 16))
    p.add(f'<path d="M72 134 Q128 152 184 134 L186 150 Q128 168 70 150 Z" fill="#ff6b9a" stroke="{LINE}" stroke-width="5" stroke-linejoin="round"/>')
    for a in range(0, 360, 72):
        x, y = 172 + 11 * math.cos(math.radians(a)), 140 + 11 * math.sin(math.radians(a))
        p.add(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="8" fill="#fff" stroke="{LINE}" stroke-width="3"/>')
    p.add(f'<circle cx="172" cy="140" r="6" fill="#ffd23a"/>')


@item("beach_4")
def diving_mask(p):
    p.ground(128, 222, 100, 11)
    p.add(f'<path d="M196 40 L206 40 Q218 40 218 52 L218 176 Q218 198 196 198 L170 198" fill="none" stroke="{LINE}" stroke-width="26" stroke-linecap="round"/>')
    p.add('<path d="M196 40 L206 40 Q218 40 218 52 L218 176 Q218 198 196 198 L170 198" fill="none" stroke="#ffd23a" stroke-width="13" stroke-linecap="round"/>')
    p.add(f'<rect x="188" y="26" width="22" height="22" rx="6" fill="#ff8a2a" stroke="{LINE}" stroke-width="5"/>')
    frame = "M24 108 Q24 70 62 70 L164 70 Q200 70 200 108 Q200 160 156 160 Q140 160 128 140 Q116 160 100 160 Q24 160 24 108 Z"
    p.vol(frame, "#ffb070", "#ff8a2a", "#d0560a", hl=(60, 80, 30, 6))
    for x in (68, 160):
        lens = ell_d(x, 112, 34, 30)
        p.vol(lens, "#e6faff", "#9ae2f6", "#4ab0d0", w=6, rim=False, hl=(x - 12, 100, 12, 10), hl_op=0.9)


@item("beach_5")
def parasol(p):
    p.ground(128, 232, 70, 10)
    p.add(f'<path d="M128 70 L136 228" stroke="{LINE}" stroke-width="16" stroke-linecap="round"/>')
    p.add('<path d="M128 70 L136 228" stroke="#d9a060" stroke-width="7" stroke-linecap="round"/>')
    canopy = "M14 116 Q24 28 128 22 Q232 28 242 116 Q216 98 194 116 Q172 96 150 116 Q128 96 106 116 Q84 96 62 116 Q40 98 14 116 Z"
    cid = p.id("c")
    p.defs.append(f'<clipPath id="{cid}"><path d="{canopy}"/></clipPath>')
    stripes = "".join(f'<path d="M128 22 L{x} 140 L{x + 44} 140 Z" fill="#fff6ee"/>' for x in (38, 126, 214) if True)
    stripes = "".join(f'<path d="M128 18 L{x0} 130 L{x1} 130 Z" fill="#fff6ee"/>' for x0, x1 in ((14, 62), (106, 150), (194, 242)))
    p.add(f'<path d="{canopy}" fill="#ff5a5a"/><g clip-path="url(#{cid})">{stripes}'
          f'<path d="{canopy}" fill="none" stroke="#a4202c" stroke-width="22" opacity=".3" transform="translate(0 -9)" filter="{p.blur(5)}"/>'
          f'<ellipse cx="86" cy="54" rx="40" ry="14" fill="#fff" opacity=".5" filter="{p.blur(6)}"/></g>')
    p.add(f'<path d="{canopy}" fill="none" stroke="{LINE}" stroke-width="7" stroke-linejoin="round"/>')
    p.add(f'<circle cx="128" cy="18" r="9" fill="#ffd23a" stroke="{LINE}" stroke-width="5"/>')
    p.add('<ellipse cx="136" cy="230" rx="44" ry="8" fill="#ffe8a8" opacity=".9"/>')


@item("beach_6")
def lounger(p):
    p.ground(128, 226, 112, 12)
    for x0, x1 in ((50, 34), (186, 206), (196, 220)):
        p.add(f'<path d="M{x0} 170 L{x1} 220" stroke="{LINE}" stroke-width="14" stroke-linecap="round"/><path d="M{x0} 170 L{x1} 220" stroke="#d9a060" stroke-width="6" stroke-linecap="round"/>')
    seat = "M28 172 L170 172 L222 72 Q230 60 218 56 L206 54 Q196 54 192 64 L150 150 L36 150 Q22 150 22 162 Z"
    stripes = "".join(f'<rect x="{x}" y="40" width="22" height="160" fill="#fff6ee"/>' for x in (40, 84, 128))
    stripes += "".join(f'<path d="M{150 + k * 22} 150 L{196 + k * 12} 56" stroke="#fff6ee" stroke-width="12"/>' for k in (0, 1))
    p.vol(seat, "#8ff0f0", "#2ec4d4", "#178a9a", hl=(80, 154, 40, 6), extra=stripes)
    p.vol(rect_d(174, 40, 52, 28, 12), "#ffffff", "#f4f0ea", "#c9bcae", w=6, rim=False)
    p.spark(60, 70, 14)
    p.spark(100, 100, 9)


# ============================================================================ generators
@item("generator")
def cloche(p):
    p.ground(128, 234, 104, 12)
    p.vol(ell_d(128, 204, 108, 22), "#fff6ee", "#e6d6c4", "#b8a08a", hl=(96, 196, 40, 6))
    dome = "M38 196 Q38 76 128 70 Q218 76 218 196 Z"
    p.vol(dome, "#fff3a0", "#ffc21f", "#d8860c", hl=(90, 120, 26, 44))
    p.add(f'<rect x="28" y="190" width="200" height="20" rx="10" fill="#e8a01f" stroke="{LINE}" stroke-width="7"/>')
    p.vol(ell_d(128, 62, 18, 14), "#fff3a0", "#ffc21f", "#d8860c", hl=(122, 56, 6, 4))
    p.shine("M70 170 Q66 122 98 98", 11)
    p.spark(214, 70, 16)
    p.spark(40, 92, 11)
    p.spark(190, 30, 8)


@item("gen_services")
def reception_bell(p):
    p.ground(128, 234, 100, 12)
    p.vol(rect_d(34, 186, 188, 36, 14), "#a86a44", "#7a4a2a", "#4a2a18", hl=(80, 192, 40, 5))
    dome = "M50 188 Q50 98 128 92 Q206 98 206 188 Z"
    p.vol(dome, "#fff3a0", "#ffc21f", "#d8860c", hl=(96, 124, 22, 34))
    p.vol(rect_d(118, 62, 20, 32, 6), "#eef2f8", "#b4bfd0", "#7a869a", w=6)
    p.vol(ell_d(128, 58, 26, 10), "#eef2f8", "#b4bfd0", "#7a869a", w=6, hl=(120, 54, 10, 3))
    p.shine("M80 170 Q78 132 104 114", 10)
    for s in (-1, 1):
        p.line(f"M{128 + s * 92} 82 Q{128 + s * 108} 102 {128 + s * 96} 124", "#ffd23a", 7, op=0.9)
    p.spark(214, 52, 14)
    p.spark(44, 60, 10)


@item("gen_pool")
def life_ring(p):
    p.ground(128, 234, 92, 12)
    p.vol(ell_d(128, 128, 102, 102), "#fff3a0", "#ffc21f", "#d8860c", hl=(84, 70, 30, 18))
    for a in (45, 135, 225, 315):
        r0, r1 = 52, 102
        a0, a1 = math.radians(a - 18), math.radians(a + 18)
        d = (f"M{128 + r0 * math.cos(a0):.1f} {128 + r0 * math.sin(a0):.1f} L{128 + r1 * math.cos(a0):.1f} {128 + r1 * math.sin(a0):.1f} "
             f"A{r1} {r1} 0 0 1 {128 + r1 * math.cos(a1):.1f} {128 + r1 * math.sin(a1):.1f} L{128 + r0 * math.cos(a1):.1f} {128 + r0 * math.sin(a1):.1f} "
             f"A{r0} {r0} 0 0 0 {128 + r0 * math.cos(a0):.1f} {128 + r0 * math.sin(a0):.1f} Z")
        p.add(f'<path d="{d}" fill="#ff6b6b" stroke="{LINE}" stroke-width="5"/>')
    p.add(f'<circle cx="128" cy="128" r="50" fill="#bfeaff" stroke="{LINE}" stroke-width="7"/>')
    p.add(f'<circle cx="128" cy="128" r="102" fill="none" stroke="{LINE}" stroke-width="7"/>')
    p.add('<circle cx="128" cy="128" r="77" fill="none" stroke="#fff" stroke-width="4" stroke-dasharray="6 14" opacity=".8"/>')
    p.shine("M48 104 Q60 56 104 40", 11)
    p.spark(222, 50, 14)
    p.spark(34, 214, 10)


# ============================================================================ render
def render():
    os.makedirs(OUT, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        actions = []
        for name, content in ITEMS.items():
            src = os.path.join(tmp, name + ".svg")
            with open(src, "w") as f:
                f.write(content)
            dst = os.path.join(OUT, name + ".png")
            actions.append(f"file-open:{src};export-width:{SIZE};export-height:{SIZE};export-filename:{dst};export-do;file-close")
        subprocess.run(["inkscape", "--shell"], input="\n".join(actions) + "\nquit\n", text=True, check=True, capture_output=True)
    print(f"{os.path.relpath(OUT, ROOT)}: {len(ITEMS)} items")


if __name__ == "__main__":
    render()
