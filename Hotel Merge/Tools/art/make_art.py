#!/usr/bin/env python3
"""Generates every sprite the game uses as SVG, then renders them to PNG with Inkscape.

Run from anywhere:  python3 Tools/art/make_art.py
Output goes to Assets/Resources/Art/{Icons,UI}. (Merge items: make_items.py; the UI kit: make_kit.py.)
"""
import math
import os
import subprocess
import tempfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Art")
INK = "#3a2438"  # shared dark outline colour


def O(w=7):
    return f'stroke="{INK}" stroke-width="{w}" stroke-linejoin="round" stroke-linecap="round"'


def lg(id_, *cols, x2=0, y2=1):
    n = len(cols) - 1
    stops = "".join(f'<stop offset="{i / n:.2f}" stop-color="{c}"/>' for i, c in enumerate(cols))
    return f'<linearGradient id="{id_}" x1="0" y1="0" x2="{x2}" y2="{y2}">{stops}</linearGradient>'


def rg(id_, *cols, cx=0.35, cy=0.3, r=0.85):
    n = len(cols) - 1
    stops = "".join(f'<stop offset="{i / n:.2f}" stop-color="{c}"/>' for i, c in enumerate(cols))
    return f'<radialGradient id="{id_}" cx="{cx}" cy="{cy}" r="{r}">{stops}</radialGradient>'


def svg(body, defs="", w=256, h=256):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">'
            f"<defs>{defs}</defs>{body}</svg>")


SHADOW = '<ellipse cx="128" cy="234" rx="72" ry="12" fill="#2a1030" opacity=".18"/>'


def hl(d, w=10, op=0.75):
    return f'<path d="{d}" fill="none" stroke="#fff" stroke-width="{w}" stroke-linecap="round" opacity="{op}"/>'


def straw(x1, y1, x2, y2, col="#ff4f6d"):
    return (f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{INK}" stroke-width="20" stroke-linecap="round"/>'
            f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="#fff" stroke-width="10" stroke-linecap="round"/>'
            f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{col}" stroke-width="10" stroke-dasharray="12 12"/>')


def blob_outline(circles, fill, w=14):
    """Union of circles with a single outer outline."""
    a = "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{INK}" stroke="{INK}" stroke-width="{w}"/>' for x, y, r in circles)
    b = "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"/>' for x, y, r in circles)
    return a + b


def star_path(cx, cy, ro, ri, n=5, rot=-90):
    import math
    pts = []
    for i in range(n * 2):
        r = ro if i % 2 == 0 else ri
        a = math.radians(rot + i * 180 / n)
        pts.append(f"{cx + r * math.cos(a):.1f} {cy + r * math.sin(a):.1f}")
    return "M" + " L".join(pts) + " Z"


def sparkle(cx, cy, s, col="#fffbe0"):
    d = (f"M{cx} {cy - s} Q{cx + s * .18} {cy - s * .18} {cx + s} {cy} Q{cx + s * .18} {cy + s * .18} {cx} {cy + s} "
         f"Q{cx - s * .18} {cy + s * .18} {cx - s} {cy} Q{cx - s * .18} {cy - s * .18} {cx} {cy - s} Z")
    return f'<path d="{d}" fill="{col}"/>'


# ----------------------------------------------------------------------------- items
ITEMS = {}

# Drinks chain ------------------------------------------------------------------
glass = "M74 52 L182 52 L168 216 Q166 228 154 228 L102 228 Q90 228 88 216 Z"
ITEMS["drink_1"] = svg(
    SHADOW +
    f'<path d="{glass}" fill="url(#g)"/>'
    f'<clipPath id="c"><path d="{glass}"/></clipPath>'
    f'<g clip-path="url(#c)"><rect x="60" y="110" width="140" height="130" fill="url(#w)"/>'
    f'<ellipse cx="128" cy="110" rx="60" ry="9" fill="#c4eeff"/></g>'
    f'<path d="{glass}" fill="none" {O()}/>'
    f'<ellipse cx="128" cy="52" rx="54" ry="10" fill="#effbff" {O(6)}/>'
    + hl("M94 70 L104 204", 11) +
    '<circle cx="140" cy="168" r="7" fill="#fff" opacity=".7"/><circle cx="152" cy="190" r="5" fill="#fff" opacity=".6"/>'
    '<circle cx="132" cy="140" r="4" fill="#fff" opacity=".6"/>',
    lg("g", "#ffffff", "#d3f1ff") + lg("w", "#86d8ff", "#2f8fdc"))

milk_body = ("M104 58 L152 58 L152 84 Q182 100 182 130 L182 206 Q182 228 160 228 L96 228 "
             "Q74 228 74 206 L74 130 Q74 100 104 84 Z")
ITEMS["drink_2"] = svg(
    SHADOW +
    f'<path d="{milk_body}" fill="url(#m)" {O()}/>'
    f'<clipPath id="c"><path d="{milk_body}"/></clipPath>'
    f'<g clip-path="url(#c)"><rect x="60" y="140" width="140" height="52" fill="url(#l)"/></g>'
    '<path d="M128 148 Q142 166 142 174 A14 14 0 0 1 114 174 Q114 166 128 148 Z" fill="#fff"/>'
    f'<path d="{milk_body}" fill="none" {O()}/>'
    f'<rect x="96" y="28" width="64" height="34" rx="11" fill="url(#t)" {O()}/>'
    '<rect x="104" y="34" width="20" height="8" rx="4" fill="#fff" opacity=".6"/>'
    + hl("M90 128 L90 206", 11),
    lg("m", "#ffffff", "#efe4cf") + lg("l", "#69b8ff", "#3a86e0") + lg("t", "#6ee8ee", "#1f9fb0"))

tulip = "M86 56 L170 56 Q168 92 152 116 Q172 146 166 184 Q162 206 128 206 Q94 206 90 184 Q84 146 104 116 Q88 92 86 56 Z"
ITEMS["drink_3"] = svg(
    SHADOW +
    f'<ellipse cx="128" cy="208" rx="86" ry="23" fill="url(#r)" {O()}/>'
    '<ellipse cx="128" cy="203" rx="54" ry="11" fill="#ffd9d9" opacity=".55"/>'
    f'<path d="{tulip}" fill="#fff4ee" opacity=".75"/>'
    f'<clipPath id="c"><path d="{tulip}"/></clipPath>'
    '<g clip-path="url(#c)"><rect x="70" y="78" width="120" height="140" fill="url(#t)"/>'
    '<ellipse cx="128" cy="78" rx="44" ry="7" fill="#ffab73"/></g>'
    f'<path d="{tulip}" fill="none" {O()}/>'
    f'<ellipse cx="128" cy="56" rx="42" ry="8" fill="none" {O(6)}/>'
    + hl("M100 70 Q104 100 116 116", 9) + hl("M104 140 Q100 160 104 180", 8, .5) +
    '<path d="M150 40 Q140 28 150 16" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".9"/>'
    '<path d="M112 42 Q102 30 112 18" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".9"/>',
    lg("r", "#ff7a7a", "#c93434") + lg("t", "#ff8a44", "#a8220c"))

cup = "M62 100 L194 100 L184 170 Q178 196 150 196 L106 196 Q78 196 72 170 Z"
handle = "M184 116 Q224 114 218 148 Q212 176 176 170"
ITEMS["drink_4"] = svg(
    SHADOW +
    f'<ellipse cx="128" cy="202" rx="96" ry="25" fill="url(#p)" {O()}/>'
    f'<path d="{handle}" fill="none" stroke="{INK}" stroke-width="28" stroke-linecap="round"/>'
    f'<path d="{handle}" fill="none" stroke="#fff" stroke-width="14" stroke-linecap="round"/>'
    f'<path d="{cup}" fill="url(#p)" {O()}/>'
    '<path d="M70 150 L186 150 L183 166 L73 166 Z" fill="#4fa3ea"/>'
    f'<ellipse cx="128" cy="100" rx="66" ry="16" fill="#fff" {O()}/>'
    '<ellipse cx="128" cy="102" rx="54" ry="11" fill="url(#k)"/>'
    '<path d="M128 110 Q114 100 120 96 Q126 93 128 99 Q130 93 136 96 Q142 100 128 110 Z" fill="#f2d2a9"/>'
    + hl("M84 118 Q86 150 98 176", 10, .8) +
    '<path d="M100 76 Q88 60 100 46 Q112 32 100 18" fill="none" stroke="#d9cfe0" stroke-width="14" stroke-linecap="round"/>'
    '<path d="M100 76 Q88 60 100 46 Q112 32 100 18" fill="none" stroke="#fff" stroke-width="8" stroke-linecap="round"/>'
    '<path d="M148 72 Q136 58 148 44 Q160 30 148 18" fill="none" stroke="#d9cfe0" stroke-width="14" stroke-linecap="round"/>'
    '<path d="M148 72 Q136 58 148 44 Q160 30 148 18" fill="none" stroke="#fff" stroke-width="8" stroke-linecap="round"/>',
    lg("p", "#ffffff", "#ddd2c6") + lg("k", "#a8653a", "#5a2f17"))

tall = "M80 52 L176 52 L166 218 Q165 230 152 230 L104 230 Q91 230 90 218 Z"
lemon = ('<circle cx="178" cy="62" r="30" fill="#fff27a" {o}/>'
         '<circle cx="178" cy="62" r="21" fill="#ffd93b"/>'
         + "".join(f'<line x1="178" y1="62" x2="{178 + 21 * c:.1f}" y2="{62 + 21 * s:.1f}" stroke="#fff6c0" stroke-width="3"/>'
                   for c, s in [(1, 0), (.5, .87), (-.5, .87), (-1, 0), (-.5, -.87), (.5, -.87)])
         + '<circle cx="178" cy="62" r="4" fill="#fff"/>').format(o=O(6))
ITEMS["drink_5"] = svg(
    SHADOW + straw(138, 170, 166, 12) +
    f'<path d="{tall}" fill="#fffbe6" opacity=".75"/>'
    f'<clipPath id="c"><path d="{tall}"/></clipPath>'
    '<g clip-path="url(#c)"><rect x="60" y="86" width="140" height="160" fill="url(#y)"/>'
    '<ellipse cx="128" cy="86" rx="56" ry="8" fill="#fff59a"/>'
    '<rect x="96" y="104" width="34" height="34" rx="8" fill="#fff" opacity=".55" transform="rotate(-14 113 121)"/>'
    '<rect x="128" y="132" width="32" height="32" rx="8" fill="#fff" opacity=".5" transform="rotate(18 144 148)"/>'
    '<circle cx="112" cy="190" r="5" fill="#fff" opacity=".7"/><circle cx="140" cy="204" r="4" fill="#fff" opacity=".7"/></g>'
    f'<path d="{tall}" fill="none" {O()}/>'
    + hl("M96 70 L104 210", 10) + lemon,
    lg("y", "#fff27a", "#ffb81f"))

shake = "M76 94 L180 94 L164 200 Q160 214 146 214 L110 214 Q96 214 92 200 Z"
ITEMS["drink_6"] = svg(
    SHADOW + straw(150, 90, 190, 20, "#4fa3ea") +
    f'<ellipse cx="128" cy="230" rx="46" ry="10" fill="#ffe3f1" {O(6)}/>'
    f'<rect x="116" y="208" width="24" height="20" fill="#ffd0e6" {O(6)}/>'
    f'<path d="{shake}" fill="url(#k)" {O()}/>'
    '<path d="M84 120 L174 120 L172 132 L86 132 Z" fill="#fff" opacity=".45"/>'
    + hl("M94 108 Q96 160 110 200", 10, .7)
    + blob_outline([(98, 88, 24), (128, 72, 31), (158, 88, 24), (128, 96, 26)], "url(#c)")
    + '<rect x="104" y="72" width="10" height="5" rx="2" fill="#4fa3ea" transform="rotate(30 109 74)"/>'
      '<rect x="140" y="80" width="10" height="5" rx="2" fill="#ffd23a" transform="rotate(-25 145 82)"/>'
      '<rect x="120" y="58" width="10" height="5" rx="2" fill="#7ad94f" transform="rotate(60 125 60)"/>'
      '<rect x="160" y="94" width="10" height="5" rx="2" fill="#ff5c9e" transform="rotate(10 165 96)"/>'
      f'<path d="M134 32 Q140 14 156 8" fill="none" {O(5)}/>'
      f'<circle cx="132" cy="44" r="16" fill="url(#h)" {O(6)}/>'
      '<circle cx="126" cy="38" r="5" fill="#fff" opacity=".8"/>',
    lg("k", "#ffc2de", "#ff5c9e") + lg("c", "#ffffff", "#f6e4ee") + lg("h", "#ff7b7b", "#d11f3a"))

# Food chain ---------------------------------------------------------------------
egg = "M128 38 C182 38 206 128 202 160 C198 202 164 224 128 224 C92 224 58 202 54 160 C50 128 74 38 128 38 Z"
ITEMS["food_1"] = svg(
    SHADOW +
    f'<path d="{egg}" fill="url(#e)" {O()}/>'
    '<ellipse cx="100" cy="100" rx="13" ry="26" fill="#fff" opacity=".85" transform="rotate(20 100 100)"/>'
    '<ellipse cx="160" cy="150" rx="6" ry="4" fill="#d9b98a"/><ellipse cx="142" cy="186" rx="5" ry="3" fill="#d9b98a"/>'
    '<ellipse cx="96" cy="176" rx="4" ry="3" fill="#d9b98a"/><ellipse cx="150" cy="96" rx="4" ry="3" fill="#d9b98a"/>',
    lg("e", "#fffdf6", "#f0d6a8"))

bread = ("M70 120 Q44 104 52 74 Q62 42 104 46 Q128 30 152 46 Q194 42 204 74 Q212 104 186 120 "
         "L182 206 Q181 222 164 222 L92 222 Q75 222 74 206 Z")
ITEMS["food_2"] = svg(
    SHADOW +
    f'<path d="{bread}" fill="url(#c)" {O()}/>'
    f'<path d="{bread}" fill="url(#m)" transform="translate(128 140) scale(.8) translate(-128 -134)"/>'
    '<g stroke="#c27a2c" stroke-width="9" stroke-linecap="round" opacity=".55">'
    '<line x1="92" y1="100" x2="148" y2="176"/><line x1="122" y1="84" x2="172" y2="152"/><line x1="86" y1="140" x2="120" y2="186"/></g>'
    f'<rect x="106" y="112" width="44" height="32" rx="7" fill="url(#b)" {O(5)} transform="rotate(-10 128 128)"/>'
    '<rect x="112" y="116" width="16" height="6" rx="3" fill="#fff" opacity=".7" transform="rotate(-10 128 128)"/>',
    lg("c", "#eda957", "#b8661f") + lg("m", "#ffe7aa", "#f2b45a") + lg("b", "#fff7a8", "#ffd84a"))

tri = "M46 206 L210 206 L128 56 Z"
ITEMS["food_3"] = svg(
    SHADOW +
    f'<line x1="128" y1="96" x2="128" y2="26" stroke="{INK}" stroke-width="12" stroke-linecap="round"/>'
    '<line x1="128" y1="96" x2="128" y2="26" stroke="#e3b47a" stroke-width="5" stroke-linecap="round"/>'
    f'<path d="{tri}" fill="none" stroke="{INK}" stroke-width="32" stroke-linejoin="round"/>'
    f'<clipPath id="c"><path d="{tri}"/></clipPath>'
    '<g clip-path="url(#c)">'
    '<rect x="30" y="30" width="200" height="200" fill="#ffeab4"/>'
    '<path d="M30 124 Q44 112 58 124 Q72 112 86 124 Q100 112 114 124 Q128 112 142 124 Q156 112 170 124 Q184 112 198 124 Q212 112 230 124 L230 142 L30 142 Z" fill="#6fd34a"/>'
    '<rect x="30" y="140" width="200" height="18" fill="#ff5a4a"/>'
    '<rect x="30" y="158" width="200" height="14" fill="#ffd23a"/>'
    '<rect x="30" y="172" width="200" height="14" fill="#ff9db4"/>'
    '<circle cx="104" cy="96" r="3" fill="#e9c27a"/><circle cx="140" cy="104" r="3" fill="#e9c27a"/><circle cx="124" cy="80" r="3" fill="#e9c27a"/>'
    '</g>'
    f'<path d="{tri}" fill="none" stroke="#dd9a45" stroke-width="16" stroke-linejoin="round"/>'
    + hl("M112 86 L82 146", 7, .6) +
    f'<circle cx="128" cy="26" r="13" fill="url(#o)" {O(6)}/><circle cx="124" cy="22" r="4" fill="#fff" opacity=".7"/>',
    lg("o", "#9be05a", "#3f9a2a"))

ITEMS["food_4"] = svg(
    SHADOW +
    f'<path d="M60 178 L196 178 Q200 216 166 218 L90 218 Q56 216 60 178 Z" fill="url(#b)" {O()}/>'
    f'<rect x="50" y="148" width="156" height="36" rx="17" fill="url(#p)" {O()}/>'
    f'<path d="M54 142 L202 142 L202 154 L178 154 L166 176 L154 154 L86 154 L76 172 L66 154 L54 154 Z" fill="#ffd23a" {O(5)}/>'
    f'<rect x="58" y="126" width="140" height="18" rx="9" fill="#ff5a4a" {O(5)}/>'
    f'<path d="M46 130 Q58 114 70 128 Q82 114 94 128 Q106 114 118 128 Q130 114 142 128 Q154 114 166 128 Q178 114 190 128 Q202 114 212 130 L206 138 L52 138 Z" fill="#7ad94f" {O(5)}/>'
    f'<path d="M50 122 Q50 50 128 48 Q206 50 206 122 Q206 132 196 132 L60 132 Q50 132 50 122 Z" fill="url(#t)" {O()}/>'
    + "".join(f'<ellipse cx="{x}" cy="{y}" rx="6" ry="3.5" fill="#fff4d6" transform="rotate({r} {x} {y})"/>'
              for x, y, r in [(96, 76, -20), (128, 66, 10), (160, 78, 30), (112, 98, 40), (146, 100, -10), (84, 104, 15), (174, 104, -30)])
    + hl("M74 96 Q84 68 110 60", 9, .6),
    lg("b", "#f7b45c", "#d9832e") + lg("p", "#8f4f2c", "#57280f") + lg("t", "#ffcd7d", "#e08a32"))

ITEMS["food_5"] = svg(
    SHADOW +
    f'<ellipse cx="128" cy="142" rx="106" ry="84" fill="url(#w)" {O()}/>'
    f'<ellipse cx="128" cy="134" rx="94" ry="72" fill="url(#c)" {O(5)}/>'
    '<ellipse cx="128" cy="134" rx="80" ry="60" fill="#e8432f"/>'
    '<ellipse cx="128" cy="134" rx="75" ry="55" fill="url(#h)"/>'
    '<g stroke="#e5a24a" stroke-width="3" opacity=".7">'
    '<line x1="128" y1="134" x2="128" y2="62"/><line x1="128" y1="134" x2="200" y2="140"/>'
    '<line x1="128" y1="134" x2="72" y2="186"/><line x1="128" y1="134" x2="60" y2="104"/><line x1="128" y1="134" x2="170" y2="190"/></g>'
    + "".join(f'<ellipse cx="{x}" cy="{y}" rx="13" ry="10" fill="#d8332a" stroke="#9e1f1a" stroke-width="3"/>'
              f'<ellipse cx="{x - 4}" cy="{y - 3}" rx="4" ry="2.5" fill="#ff8c80"/>'
              for x, y in [(100, 104), (156, 106), (128, 140), (90, 156), (166, 160), (128, 178), (126, 92)])
    + "".join(f'<ellipse cx="{x}" cy="{y}" rx="7" ry="4" fill="#3fae3a" transform="rotate({r} {x} {y})"/>'
              for x, y, r in [(112, 124, 30), (146, 130, -40), (106, 176, 10), (152, 84, 60)]),
    lg("w", "#d9a066", "#9c6034") + lg("c", "#ffd08a", "#e39a45") + lg("h", "#fff09a", "#ffc93a"))

drip_white = ("M48 158 L208 158 L208 172 Q201 192 194 172 Q186 168 178 172 Q170 196 162 172 Q150 166 140 172 "
              "Q132 188 124 172 Q112 166 102 172 Q94 194 86 172 Q74 166 64 172 Q56 186 48 172 Z")
ITEMS["food_6"] = svg(
    SHADOW +
    f'<ellipse cx="128" cy="214" rx="100" ry="19" fill="url(#pl)" {O()}/>'
    f'<rect x="46" y="146" width="164" height="68" rx="14" fill="url(#k)" {O()}/>'
    '<rect x="50" y="190" width="156" height="8" fill="#a3563a" opacity=".55"/>'
    f'<path d="{drip_white}" fill="#fff6fa"/>'
    f'<rect x="76" y="98" width="104" height="58" rx="12" fill="url(#cr)" {O()}/>'
    '<path d="M76 108 L180 108 L180 118 Q174 134 168 118 Q158 114 150 118 Q142 136 134 118 Q124 114 116 118 Q108 132 100 118 Q90 114 84 118 Q80 128 76 118 Z" fill="#ff7fab"/>'
    f'<rect x="122" y="58" width="12" height="42" rx="4" fill="#6fc3ff" {O(5)}/>'
    '<path d="M128 26 Q144 44 136 54 Q128 62 120 54 Q112 44 128 26 Z" fill="#ffb21f" stroke="#e05b10" stroke-width="4"/>'
    '<path d="M128 38 Q134 48 130 52 Q126 54 124 50 Q122 46 128 38 Z" fill="#fff3a0"/>'
    + "".join(f'<circle cx="{x}" cy="{y}" r="10" fill="url(#s)" {O(5)}/><circle cx="{x - 3}" cy="{y - 3}" r="3" fill="#fff" opacity=".8"/>'
              for x, y in [(92, 96), (164, 96)])
    + hl("M60 168 L60 200", 8, .6) + hl("M88 128 L88 146", 7, .6),
    lg("pl", "#ffffff", "#ddd5e6") + lg("k", "#ffc1d6", "#f2709a") + lg("cr", "#fffaf2", "#f3d8bc") + lg("s", "#ff7b7b", "#d11f3a"))

# Generator: golden serving cloche ---------------------------------------------------
dome = "M38 194 Q38 72 128 70 Q218 72 218 194 Z"
ITEMS["generator"] = svg(
    SHADOW +
    f'<ellipse cx="128" cy="200" rx="112" ry="28" fill="url(#s)" {O()}/>'
    '<ellipse cx="128" cy="196" rx="86" ry="15" fill="#fff" opacity=".5"/>'
    f'<path d="{dome}" fill="url(#g)" {O()}/>'
    f'<rect x="32" y="182" width="192" height="20" rx="10" fill="url(#b)" {O(6)}/>'
    f'<rect x="118" y="62" width="20" height="14" fill="url(#b)" {O(6)}/>'
    f'<circle cx="128" cy="54" r="18" fill="url(#g)" {O()}/>'
    '<circle cx="122" cy="48" r="6" fill="#fff" opacity=".85"/>'
    + hl("M66 164 Q68 110 116 92", 13, .8) + hl("M64 182 L62 176", 9, .6) +
    sparkle(200, 74, 18) + sparkle(54, 96, 12) + sparkle(186, 128, 9),
    rg("g", "#fff6c0", "#ffd84a", "#eaa018", "#b4690a", cx=.38, cy=.3, r=.8) + lg("b", "#ffe788", "#cf880f")
    + lg("s", "#ffffff", "#c8c0d8"))

# Hotel services: bath chain -------------------------------------------------------------
def bubble(x, y, r):
    return (f'<circle cx="{x}" cy="{y}" r="{r}" fill="url(#bb)" {O(5)}/>'
            f'<circle cx="{x - r * .35:.1f}" cy="{y - r * .35:.1f}" r="{r * .28:.1f}" fill="#fff"/>')


BUBBLE_DEF = rg("bb", "#ffffff", "#e3f6ff", "#a6dcff", cx=.4, cy=.35)

ITEMS["bath_1"] = svg(
    SHADOW +
    '<g transform="rotate(-8 128 160)">'
    f'<rect x="46" y="122" width="164" height="92" rx="38" fill="#d8649f" {O()}/>'
    f'<rect x="46" y="106" width="164" height="90" rx="38" fill="url(#s)" {O()}/>'
    '<rect x="78" y="126" width="100" height="46" rx="23" fill="#fff" opacity=".35"/>'
    '<path d="M128 164 Q106 150 112 140 Q120 132 128 142 Q136 132 144 140 Q150 150 128 164 Z" fill="#fff" opacity=".8"/>'
    + hl("M70 132 Q74 118 92 116", 9, .85) + '</g>'
    + bubble(66, 92, 18) + bubble(98, 60, 12) + bubble(184, 78, 22) + bubble(152, 44, 10) + bubble(210, 122, 9),
    lg("s", "#ffe3f2", "#ff9ccc") + BUBBLE_DEF)

ITEMS["bath_2"] = svg(
    SHADOW +
    f'<rect x="146" y="38" width="48" height="14" rx="7" fill="#e9e6f2" {O(6)}/>'
    f'<rect x="92" y="30" width="72" height="26" rx="11" fill="url(#c)" {O(7)}/>'
    f'<rect x="120" y="54" width="16" height="14" fill="#c9c3d6" {O(5)}/>'
    f'<rect x="104" y="64" width="48" height="30" rx="8" fill="url(#c)" {O(7)}/>'
    f'<rect x="72" y="88" width="112" height="140" rx="34" fill="url(#b)" {O()}/>'
    f'<rect x="90" y="128" width="76" height="62" rx="16" fill="#fff" {O(5)}/>'
    '<path d="M128 140 Q144 160 144 168 A16 16 0 0 1 112 168 Q112 160 128 140 Z" fill="#35c0a6"/>'
    '<circle cx="122" cy="166" r="4" fill="#fff"/>'
    + hl("M88 108 L88 206", 10, .7),
    lg("b", "#a6f5e2", "#22ae96") + lg("c", "#ffffff", "#c9c3d6"))

towel_clip = '<clipPath id="tc"><rect x="44" y="96" width="168" height="96" rx="26"/></clipPath>'
ITEMS["bath_3"] = svg(
    SHADOW + towel_clip +
    f'<rect x="44" y="112" width="168" height="104" rx="28" fill="#2f86d6" {O()}/>'
    '<path d="M52 206 Q128 222 204 206" fill="none" stroke="#1d5fa8" stroke-width="5" opacity=".6"/>'
    f'<rect x="44" y="96" width="168" height="96" rx="26" fill="url(#t)"/>'
    '<g clip-path="url(#tc)"><rect x="44" y="160" width="168" height="12" fill="#fff" opacity=".9"/>'
    '<rect x="44" y="178" width="168" height="6" fill="#fff" opacity=".7"/>'
    + "".join(f'<circle cx="{60 + (i * 23) % 150}" cy="{104 + (i * 17) % 50}" r="2.5" fill="#fff" opacity=".35"/>' for i in range(22)) +
    '</g>'
    f'<rect x="44" y="96" width="168" height="96" rx="26" fill="none" {O()}/>'
    f'<rect x="168" y="186" width="26" height="22" rx="4" fill="#fff" {O(4)}/>'
    + hl("M66 120 Q72 106 96 104", 10, .8),
    lg("t", "#a8dcff", "#4a9fe8"))


def towel(y, x, w, col1, h=50):
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="20" fill="url(#{col1})" {O()}/>'
            f'<rect x="{x}" y="{y + h - 18}" width="{w}" height="7" fill="#fff" opacity=".8"/>'
            f'<path d="M{x + 18} {y + 12} Q{x + 30} {y + 6} {x + 50} {y + 6}" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".7"/>')


ITEMS["bath_4"] = svg(
    SHADOW +
    towel(170, 36, 184, "a") + towel(122, 46, 164, "b") + towel(76, 58, 140, "c") +
    '<rect x="118" y="76" width="20" height="144" fill="#ff5c7a" opacity=".95"/>'
    f'<rect x="118" y="76" width="20" height="144" fill="none" {O(5)}/>'
    f'<path d="M128 72 Q96 44 90 66 Q88 86 128 76 Z" fill="#ff7b93" {O(6)}/>'
    f'<path d="M128 72 Q160 44 166 66 Q168 86 128 76 Z" fill="#ff7b93" {O(6)}/>'
    f'<circle cx="128" cy="74" r="11" fill="#ff5c7a" {O(6)}/>',
    lg("a", "#ffd0e3", "#ff8fb8") + lg("b", "#c8f5df", "#5fd3a0") + lg("c", "#fff3b0", "#ffcc3a"))

robe = ("M90 46 L166 46 L208 90 L190 128 L176 118 L180 226 L76 226 L80 118 L66 128 L48 90 Z")
ITEMS["bath_5"] = svg(
    SHADOW +
    f'<path d="{robe}" fill="url(#r)" {O()}/>'
    '<path d="M100 46 L128 112 L156 46" fill="#e2dcf0"/>'
    f'<path d="M92 48 Q108 92 140 132 L140 224" fill="none" stroke="#8fd0ff" stroke-width="14" stroke-linecap="round"/>'
    f'<path d="M164 48 Q148 92 128 118" fill="none" stroke="#8fd0ff" stroke-width="14" stroke-linecap="round"/>'
    f'<path d="M92 48 Q108 92 140 132 L140 224" fill="none" stroke="{INK}" stroke-width="4" opacity=".4"/>'
    f'<rect x="78" y="138" width="102" height="18" rx="8" fill="#8fd0ff" {O(5)}/>'
    f'<path d="M124 150 L110 196 L124 194 Z" fill="#8fd0ff" {O(5)}/>'
    f'<path d="M134 150 L150 192 L136 192 Z" fill="#8fd0ff" {O(5)}/>'
    f'<circle cx="128" cy="147" r="10" fill="#6fbcf0" {O(5)}/>'
    f'<rect x="92" y="172" width="30" height="26" rx="6" fill="none" stroke="#c9c3d6" stroke-width="5"/>'
    + hl("M72 104 L86 68", 9, .8),
    lg("r", "#ffffff", "#e8e1f5"))

ITEMS["bath_6"] = svg(
    SHADOW +
    f'<path d="M66 134 Q128 26 190 134" fill="none" stroke="{INK}" stroke-width="22" stroke-linecap="round"/>'
    '<path d="M66 134 Q128 26 190 134" fill="none" stroke="#d99a55" stroke-width="10" stroke-linecap="round"/>'
    f'<rect x="138" y="62" width="34" height="74" rx="12" fill="url(#bt)" {O(6)}/>'
    f'<rect x="146" y="48" width="18" height="18" rx="5" fill="#c9c3d6" {O(5)}/>'
    f'<circle cx="98" cy="114" r="34" fill="url(#tw)" {O(6)}/>'
    '<path d="M98 114 m-22 0 a22 22 0 1 1 22 22 a14 14 0 1 1 -14 -14 a7 7 0 1 1 7 7" fill="none" stroke="#e86f9e" stroke-width="5" stroke-linecap="round"/>'
    + "".join(f'<ellipse cx="{186 + 14 * c:.1f}" cy="{106 + 14 * s:.1f}" rx="10" ry="7" fill="#fff" {O(4)} transform="rotate({a} {186 + 14 * c:.1f} {106 + 14 * s:.1f})"/>'
              for c, s, a in [(1, 0, 0), (.31, .95, 72), (-.81, .59, 144), (-.81, -.59, 216), (.31, -.95, 288)])
    + f'<circle cx="186" cy="106" r="8" fill="#ffd23a" {O(4)}/>'
    f'<path d="M40 136 L216 136 L198 218 Q196 228 184 228 L72 228 Q60 228 58 218 Z" fill="url(#w)" {O()}/>'
    + "".join(f'<path d="M{48 + i * 2} {156 + i * 20} L{208 - i * 2} {156 + i * 20}" stroke="#a8652a" stroke-width="5" opacity=".55"/>' for i in range(4))
    + "".join(f'<path d="M{70 + i * 24} 142 L{74 + i * 22} 224" stroke="#a8652a" stroke-width="4" opacity=".35"/>' for i in range(6))
    + f'<rect x="34" y="128" width="188" height="22" rx="11" fill="url(#rim)" {O(6)}/>',
    lg("w", "#f2c27e", "#c27c38") + lg("rim", "#ffd99a", "#d99a55") + lg("tw", "#ffe0ee", "#ff9cc4")
    + lg("bt", "#c9b8ff", "#8f6fe8"))

# Hotel services: comfort chain ------------------------------------------------------------
ITEMS["care_1"] = svg(
    SHADOW +
    '<g transform="rotate(-18 128 136)">'
    + "".join(f'<rect x="{50 + i * 13}" y="124" width="8" height="62" rx="4" fill="url(#c)" {O(4)}/>' for i in range(12)) +
    f'<rect x="38" y="92" width="180" height="42" rx="18" fill="url(#c)" {O()}/>'
    + hl("M58 104 L170 104", 8, .75) + '</g>',
    lg("c", "#e6d2ff", "#9a62f0"))

ITEMS["care_2"] = svg(
    SHADOW +
    '<g transform="rotate(-28 128 140)">'
    f'<rect x="28" y="128" width="170" height="28" rx="14" fill="url(#h)" {O()}/>'
    f'<rect x="168" y="122" width="56" height="36" rx="12" fill="url(#h)" {O()}/>'
    f'<rect x="174" y="84" width="46" height="40" rx="6" fill="#fff" {O(6)}/>'
    + "".join(f'<line x1="{182 + i * 10}" y1="88" x2="{182 + i * 10}" y2="120" stroke="#8fd0ff" stroke-width="4"/>' for i in range(4)) +
    f'<path d="M168 82 Q176 66 190 74 Q200 60 212 72 Q226 66 228 82 Z" fill="#fff" {O(5)}/>'
    '<path d="M176 78 Q190 70 202 76 Q214 70 222 78" fill="none" stroke="#5fd3a0" stroke-width="5" stroke-linecap="round"/>'
    + hl("M44 136 L150 136", 7, .7) + '</g>',
    lg("h", "#9fd8ff", "#2f8fe8"))


def slipper(x, y, rot):
    return (f'<g transform="translate({x} {y}) rotate({rot})">'
            f'<ellipse cx="0" cy="0" rx="38" ry="88" fill="url(#so)" {O()}/>'
            '<ellipse cx="0" cy="6" rx="27" ry="72" fill="#fffaf2"/>'
            f'<path d="M-40 -44 Q0 -70 40 -44 L40 4 Q0 18 -40 4 Z" fill="url(#st)" {O()}/>'
            '<path d="M-28 -40 Q0 -56 26 -42" fill="none" stroke="#fff" stroke-width="6" stroke-linecap="round" opacity=".7"/>'
            f'<circle cx="0" cy="-18" r="9" fill="#ffd23a" {O(4)}/></g>')


ITEMS["care_3"] = svg(
    SHADOW + slipper(92, 136, -12) + slipper(166, 128, 10),
    lg("so", "#fff6ec", "#e6cdb0") + lg("st", "#ffc4dc", "#ff7fae"))

ITEMS["care_4"] = svg(
    SHADOW +
    f'<path d="M226 92 Q238 104 226 116 M238 80 Q256 104 238 128" fill="none" stroke="#8fd0ff" stroke-width="7" stroke-linecap="round"/>'
    f'<rect x="104" y="118" width="44" height="104" rx="18" fill="url(#d)" {O()} transform="rotate(-14 126 170)"/>'
    f'<rect x="110" y="148" width="16" height="28" rx="6" fill="#fff" {O(4)} transform="rotate(-14 126 170)"/>'
    f'<rect x="96" y="70" width="112" height="66" rx="30" fill="url(#d)" {O()}/>'
    f'<rect x="196" y="76" width="26" height="54" rx="8" fill="#5a4a6a" {O(6)}/>'
    f'<circle cx="92" cy="103" r="54" fill="url(#d)" {O()}/>'
    f'<circle cx="92" cy="103" r="34" fill="#ffe6ee" {O(5)}/>'
    + "".join(f'<circle cx="92" cy="103" r="{r}" fill="none" stroke="#e8456b" stroke-width="4" opacity=".55"/>' for r in (24, 14))
    + f'<circle cx="92" cy="103" r="5" fill="#e8456b"/>'
    + hl("M70 70 Q86 58 106 60", 9, .8)
    + '<path d="M108 222 Q96 236 80 228 Q64 220 52 234" fill="none" stroke="#5a4a6a" stroke-width="6" stroke-linecap="round"/>',
    lg("d", "#ffb3c4", "#ec5478"))

iron_body = "M48 176 Q60 108 140 102 L182 102 Q210 106 210 176 Z"
ITEMS["care_5"] = svg(
    SHADOW +
    f'<path d="M86 106 Q86 50 136 48 L182 48 Q204 50 198 76 L190 106 L170 106 L176 78 Q178 70 170 70 L138 70 Q108 72 106 106 Z" fill="url(#h)" {O(6)}/>'
    f'<path d="{iron_body}" fill="url(#b)" {O()}/>'
    f'<path d="M34 202 L218 202 Q222 186 210 172 L54 172 Q38 178 34 202 Z" fill="url(#s)" {O()}/>'
    f'<circle cx="168" cy="140" r="16" fill="#fff" {O(5)}/>'
    f'<line x1="168" y1="140" x2="176" y2="132" stroke="{INK}" stroke-width="5" stroke-linecap="round"/>'
    + hl("M76 154 Q90 124 126 116", 9, .75)
    + "".join(f'<path d="M{x} 96 Q{x - 10} 80 {x} 66 Q{x + 10} 52 {x} 38" fill="none" stroke="#fff" stroke-width="9" stroke-linecap="round" opacity=".95"/>'
              f'<path d="M{x} 96 Q{x - 10} 80 {x} 66 Q{x + 10} 52 {x} 38" fill="none" stroke="#c9e8ff" stroke-width="15" stroke-linecap="round" opacity=".45"/>'
              for x in (40, 64)),
    lg("b", "#a6f0ff", "#2fa8d6") + lg("s", "#ffffff", "#b9b2cc") + lg("h", "#5a4a6a", "#2b1f38"))

ITEMS["care_6"] = svg(
    SHADOW +
    f'<path d="M106 78 L106 50 Q106 38 118 38 L138 38 Q150 38 150 50 L150 78" fill="none" stroke="{INK}" stroke-width="20" stroke-linecap="round"/>'
    '<path d="M106 78 L106 50 Q106 38 118 38 L138 38 Q150 38 150 50 L150 78" fill="none" stroke="#6d6680" stroke-width="9" stroke-linecap="round"/>'
    f'<circle cx="84" cy="226" r="11" fill="#5a4a6a" {O(5)}/><circle cx="172" cy="226" r="11" fill="#5a4a6a" {O(5)}/>'
    f'<rect x="54" y="70" width="148" height="152" rx="24" fill="url(#v)" {O()}/>'
    + "".join(f'<line x1="{x}" y1="80" x2="{x}" y2="212" stroke="#c4510f" stroke-width="5" opacity=".5"/>' for x in (92, 128, 164))
    + f'<path d="{star_path(94, 120, 24, 11)}" fill="#ffe36a" {O(5)}/>'
    f'<circle cx="160" cy="172" r="24" fill="#4fa3ea" {O(5)}/>'
    '<text x="160" y="182" font-family="Noto Sans" font-weight="900" font-size="28" fill="#fff" text-anchor="middle">H</text>'
    f'<rect x="76" y="164" width="40" height="28" rx="6" fill="#fff" {O(4)} transform="rotate(-10 96 178)"/>'
    '<path d="M84 176 L108 172" stroke="#ff7fae" stroke-width="5" stroke-linecap="round"/>'
    + hl("M70 92 L70 196", 9, .7),
    lg("v", "#ffc27a", "#ff7a2a"))

# Pool area: pool fun chain ------------------------------------------------------------------
WATER = '<ellipse cx="128" cy="222" rx="100" ry="20" fill="#7fd6ff" opacity=".55"/><ellipse cx="128" cy="222" rx="70" ry="11" fill="#fff" opacity=".35"/>'

ITEMS["pool_1"] = svg(
    SHADOW +
    '<clipPath id="bc"><circle cx="128" cy="124" r="92"/></clipPath>'
    '<g clip-path="url(#bc)"><rect x="0" y="0" width="256" height="256" fill="#fff"/>'
    '<ellipse cx="54" cy="124" rx="56" ry="104" fill="#ff4f5e"/>'
    '<ellipse cx="202" cy="124" rx="56" ry="104" fill="#2f8fe8"/>'
    '<ellipse cx="128" cy="124" rx="30" ry="100" fill="#ffd23a"/>'
    '<circle cx="128" cy="124" r="92" fill="url(#sh)"/></g>'
    f'<circle cx="128" cy="124" r="92" fill="none" {O()}/>'
    f'<ellipse cx="128" cy="38" rx="20" ry="9" fill="#fff" {O(5)}/>'
    + hl("M66 92 Q76 60 108 48", 12, .85),
    '<radialGradient id="sh" cx=".35" cy=".3" r=".9"><stop offset="0" stop-color="#fff" stop-opacity="0"/>'
    '<stop offset=".6" stop-color="#fff" stop-opacity="0"/><stop offset="1" stop-color="#2a1030" stop-opacity=".3"/></radialGradient>')


def floatie(x):
    return (f'<rect x="{x}" y="92" width="78" height="124" rx="32" fill="url(#f)" {O()}/>'
            f'<rect x="{x}" y="138" width="78" height="26" fill="#fff" opacity=".9"/>'
            f'<rect x="{x}" y="92" width="78" height="124" rx="32" fill="none" {O()}/>'
            f'<ellipse cx="{x + 39}" cy="96" rx="39" ry="14" fill="#ffd0a0" {O(6)}/>'
            f'<ellipse cx="{x + 39}" cy="97" rx="22" ry="7" fill="#b85c0a"/>'
            f'<rect x="{x + 54}" y="176" width="12" height="16" rx="4" fill="#fff" {O(4)}/>'
            + hl(f"M{x + 16} 112 L{x + 16} 196", 8, .7))


ITEMS["pool_2"] = svg(SHADOW + floatie(48) + floatie(132), lg("f", "#ffc27a", "#ff7a2a"))

ITEMS["pool_3"] = svg(
    SHADOW + WATER +
    f'<path d="M50 168 Q50 122 112 122 L172 128 Q204 112 222 98 Q228 140 218 168 Q206 214 138 218 Q66 220 50 168 Z" fill="url(#y)" {O()}/>'
    f'<path d="M114 158 Q150 134 186 154 Q164 192 120 178 Z" fill="#ffc21f" {O(5)}/>'
    f'<circle cx="92" cy="94" r="48" fill="url(#y)" {O()}/>'
    f'<path d="M50 100 Q18 100 20 116 Q40 132 62 120 Z" fill="url(#o)" {O(6)}/>'
    '<path d="M24 112 Q40 116 58 112" fill="none" stroke="#b85c0a" stroke-width="4" stroke-linecap="round"/>'
    f'<ellipse cx="94" cy="84" rx="9" ry="12" fill="{INK}"/><circle cx="97" cy="79" r="4" fill="#fff"/>'
    '<ellipse cx="112" cy="108" rx="11" ry="7" fill="#ff8fa6" opacity=".7"/>'
    + hl("M64 70 Q76 52 98 50", 9, .85) + hl("M76 150 Q90 136 112 134", 8, .6),
    lg("y", "#fff59a", "#ffc928") + lg("o", "#ffb35a", "#ff7a1f"))

ITEMS["pool_4"] = svg(
    SHADOW + WATER +
    '<path d="M24 140 A104 70 0 1 0 232 140 A104 70 0 1 0 24 140 Z M84 132 A44 24 0 1 1 172 132 A44 24 0 1 1 84 132 Z" fill="url(#p)" fill-rule="evenodd"/>'
    f'<ellipse cx="128" cy="140" rx="104" ry="70" fill="none" {O()}/>'
    f'<ellipse cx="128" cy="132" rx="44" ry="24" fill="url(#w)" {O(6)}/>'
    + "".join(f'<rect x="{x}" y="{y}" width="20" height="7" rx="3.5" fill="{c}" transform="rotate({r} {x + 10} {y + 3})"/>'
              for x, y, r, c in [(56, 110, 30, "#fff"), (176, 104, -30, "#ffd23a"), (62, 158, -20, "#4fa3ea"), (178, 160, 25, "#fff"),
                                 (110, 178, 5, "#7ad94f"), (148, 186, -12, "#ffd23a"), (98, 92, -10, "#4fa3ea"), (150, 88, 15, "#7ad94f"),
                                 (36, 136, 80, "#ffd23a"), (204, 134, -80, "#4fa3ea")])
    + hl("M50 118 Q66 86 110 76", 11, .8),
    lg("p", "#ffc4dc", "#ff6fa8") + lg("w", "#a6e6ff", "#3aa8e8"))

mat = "M40 150 L168 90 Q184 84 198 94 L226 116 Q236 126 224 134 L96 196 Q80 202 68 192 L40 170 Q30 160 40 150 Z"
ITEMS["pool_5"] = svg(
    SHADOW + WATER +
    f'<path d="{mat}" fill="#1f8fb0" {O()} transform="translate(0 16)"/>'
    f'<path d="{mat}" fill="url(#m)" {O()}/>'
    '<clipPath id="mc"><path d="' + mat + '"/></clipPath>'
    '<g clip-path="url(#mc)">'
    + "".join(f'<path d="M{30 + d} {160 + d * .4:.0f} L{170 + d} {96 + d * .4:.0f}" stroke="#1f9fb8" stroke-width="5" opacity=".5"/>' for d in (34, 64, 94))
    + '<path d="M180 90 L230 128 L210 150 L160 112 Z" fill="#fff" opacity=".35"/></g>'
    + hl("M60 150 L150 108", 9, .8),
    lg("m", "#9ff5e8", "#2ab8c8"))

ITEMS["pool_6"] = svg(
    SHADOW + WATER +
    f'<path d="M180 150 Q222 92 176 62 Q148 48 154 30" fill="none" stroke="{INK}" stroke-width="34" stroke-linecap="round"/>'
    '<path d="M180 150 Q222 92 176 62 Q148 48 154 30" fill="none" stroke="#ff8fb8" stroke-width="20" stroke-linecap="round"/>'
    '<path d="M24 160 A104 58 0 1 0 232 160 A104 58 0 1 0 24 160 Z M82 154 A46 22 0 1 1 174 154 A46 22 0 1 1 82 154 Z" fill="url(#p)" fill-rule="evenodd"/>'
    f'<ellipse cx="128" cy="160" rx="104" ry="58" fill="none" {O()}/>'
    f'<ellipse cx="128" cy="154" rx="46" ry="22" fill="url(#w)" {O(6)}/>'
    f'<path d="M36 150 Q14 120 30 104 Q44 128 60 136 Z" fill="#ff6fa8" {O(6)}/>'
    f'<circle cx="152" cy="36" r="24" fill="url(#p)" {O()}/>'
    f'<path d="M132 36 Q110 42 112 64 Q124 58 136 48 Z" fill="#fff" {O(5)}/>'
    f'<path d="M114 56 Q112 64 118 62" fill="{INK}" {O(5)}/>'
    f'<circle cx="156" cy="30" r="5" fill="{INK}"/><circle cx="157" cy="28" r="2" fill="#fff"/>'
    + hl("M48 140 Q64 116 104 108", 10, .8),
    lg("p", "#ffc4dc", "#ff6fa8") + lg("w", "#a6e6ff", "#3aa8e8"))

# Pool area: beach chain ---------------------------------------------------------------------
ITEMS["beach_1"] = svg(
    SHADOW +
    f'<path d="M42 112 L16 96 M214 112 L240 96" fill="none" stroke="{INK}" stroke-width="16" stroke-linecap="round"/>'
    '<path d="M42 112 L16 96 M214 112 L240 96" fill="none" stroke="#ff6fa8" stroke-width="7" stroke-linecap="round"/>'
    f'<path d="M116 118 Q128 102 140 118" fill="none" stroke="{INK}" stroke-width="16" stroke-linecap="round"/>'
    '<path d="M116 118 Q128 102 140 118" fill="none" stroke="#ff6fa8" stroke-width="7" stroke-linecap="round"/>'
    + "".join(f'<path d="M{x} 104 Q{x} 92 {x + 14} 92 L{x + 70} 92 Q{x + 84} 92 {x + 84} 106 Q{x + 82} 170 {x + 42} 170 Q{x + 2} 170 {x} 104 Z" fill="url(#fr)" {O()}/>'
              f'<path d="M{x + 10} 106 Q{x + 10} 102 {x + 16} 102 L{x + 68} 102 Q{x + 74} 102 {x + 74} 108 Q{x + 72} 160 {x + 42} 160 Q{x + 12} 160 {x + 10} 106 Z" fill="url(#l)"/>'
              f'<path d="M{x + 22} 116 L{x + 34} 104 M{x + 24} 140 L{x + 52} 112" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".75"/>'
              for x in (36, 136)),
    lg("fr", "#ffa6cc", "#ef4f8f") + lg("l", "#6a4fd0", "#2a1f6a"))

ITEMS["beach_2"] = svg(
    SHADOW +
    f'<rect x="68" y="34" width="120" height="22" rx="5" fill="#e9e6f2" {O(6)}/>'
    + "".join(f'<line x1="{x}" y1="38" x2="{x}" y2="52" stroke="#a59eb8" stroke-width="3"/>' for x in range(80, 180, 12))
    + f'<path d="M72 54 L184 54 Q180 140 162 196 L94 196 Q76 140 72 54 Z" fill="url(#t)" {O()}/>'
    f'<rect x="90" y="192" width="76" height="38" rx="12" fill="url(#c)" {O()}/>'
    f'<circle cx="128" cy="112" r="22" fill="#ffb21f" {O(5)}/>'
    + "".join(f'<line x1="{128 + 30 * c:.1f}" y1="{112 + 30 * s:.1f}" x2="{128 + 40 * c:.1f}" y2="{112 + 40 * s:.1f}" stroke="#ff9420" stroke-width="6" stroke-linecap="round"/>'
              for c, s in [(1, 0), (.71, .71), (0, 1), (-.71, .71), (-1, 0), (-.71, -.71), (0, -1), (.71, -.71)])
    + '<text x="128" y="182" font-family="Noto Sans" font-weight="900" font-size="26" fill="#ef4f8f" text-anchor="middle">SPF 50</text>'
    + hl("M88 72 Q92 130 104 180", 9, .7),
    lg("t", "#ffffff", "#ffe9a8") + lg("c", "#ffc27a", "#ff7a2a"))

ITEMS["beach_3"] = svg(
    SHADOW +
    f'<ellipse cx="128" cy="164" rx="114" ry="44" fill="url(#s)" {O()}/>'
    + "".join(f'<ellipse cx="128" cy="164" rx="{r}" ry="{r * .38:.0f}" fill="none" stroke="#c48a3a" stroke-width="3" opacity=".5"/>' for r in (100, 86))
    + '<ellipse cx="128" cy="154" rx="62" ry="20" fill="#a86a28" opacity=".35"/>'
    f'<path d="M68 154 Q70 72 128 70 Q186 72 188 154 Z" fill="url(#s)" {O()}/>'
    + "".join(f'<path d="M{72 + i} {120 + i * .1:.0f} Q128 {112 + i * .2:.0f} {184 - i} {120 + i * .1:.0f}" fill="none" stroke="#c48a3a" stroke-width="3" opacity=".45"/>' for i in (0,))
    + f'<path d="M70 128 Q128 146 186 128 L188 152 Q128 170 68 152 Z" fill="url(#r)" {O(6)}/>'
    + "".join(f'<ellipse cx="{162 + 11 * c:.1f}" cy="{140 + 11 * s:.1f}" rx="9" ry="6" fill="#fff" {O(3)} transform="rotate({a} {162 + 11 * c:.1f} {140 + 11 * s:.1f})"/>'
              for c, s, a in [(1, 0, 0), (.31, .95, 72), (-.81, .59, 144), (-.81, -.59, 216), (.31, -.95, 288)])
    + f'<circle cx="162" cy="140" r="7" fill="#ffd23a" {O(3)}/>'
    + hl("M88 116 Q96 86 122 80", 9, .7),
    lg("s", "#ffe9a6", "#e6b25e") + lg("r", "#ff8fb8", "#e8456b"))

mask = ("M44 104 Q44 72 78 72 L178 72 Q212 72 212 104 L212 138 Q212 168 182 168 L158 168 Q142 168 136 152 "
        "Q128 140 120 152 Q114 168 98 168 L74 168 Q44 168 44 138 Z")
ITEMS["beach_4"] = svg(
    SHADOW +
    f'<path d="M40 118 Q12 118 14 150 Q18 186 60 182" fill="none" stroke="{INK}" stroke-width="22" stroke-linecap="round"/>'
    '<path d="M40 118 Q12 118 14 150 Q18 186 60 182" fill="none" stroke="#5a4a6a" stroke-width="10" stroke-linecap="round"/>'
    f'<path d="{mask}" fill="url(#f)" {O()}/>'
    f'<rect x="58" y="86" width="64" height="66" rx="20" fill="url(#l)" {O(5)}/>'
    f'<rect x="134" y="86" width="64" height="66" rx="20" fill="url(#l)" {O(5)}/>'
    '<path d="M70 112 L86 96 M146 112 L162 96" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".8"/>'
    f'<path d="M222 30 L222 176 Q222 210 190 210 L168 210" fill="none" stroke="{INK}" stroke-width="26" stroke-linecap="round"/>'
    '<path d="M222 30 L222 176 Q222 210 190 210 L168 210" fill="none" stroke="#ffd23a" stroke-width="14" stroke-linecap="round"/>'
    f'<rect x="210" y="18" width="24" height="18" rx="6" fill="#ff7a2a" {O(5)}/>'
    f'<rect x="150" y="200" width="22" height="22" rx="6" fill="#5a4a6a" {O(5)}/>',
    lg("f", "#ffc27a", "#ff7a2a") + lg("l", "#d6f4ff", "#7fcfff"))

canopy = ("M22 124 Q24 40 128 28 Q232 40 234 124 Q217 140 200 124 Q183 140 166 124 Q149 140 132 124 "
          "Q115 140 98 124 Q81 140 64 124 Q47 140 30 124 Z")
ITEMS["beach_5"] = svg(
    SHADOW +
    '<ellipse cx="128" cy="226" rx="72" ry="14" fill="#ffe0a0"/>'
    f'<rect x="122" y="70" width="12" height="160" rx="5" fill="url(#w)" {O(5)}/>'
    f'<path d="{canopy}" fill="#ff4f6d"/>'
    '<clipPath id="cc"><path d="' + canopy + '"/></clipPath>'
    '<g clip-path="url(#cc)">'
    '<path d="M128 20 L64 150 L98 150 Z" fill="#fff"/><path d="M128 20 L166 150 L200 150 Z" fill="#fff"/>'
    '<path d="M128 20 L10 150 L20 150 Z" fill="#fff"/>'
    '<ellipse cx="128" cy="124" rx="120" ry="28" fill="#2a1030" opacity=".12"/></g>'
    f'<path d="{canopy}" fill="none" {O()}/>'
    f'<circle cx="128" cy="24" r="10" fill="#ffd23a" {O(5)}/>'
    + hl("M48 92 Q60 56 100 42", 9, .8),
    lg("w", "#f2c27e", "#c27c38"))

seat = "M28 146 L146 146 L194 62 L228 78 L170 186 L28 186 Z"
ITEMS["beach_6"] = svg(
    SHADOW +
    f'<path d="M50 184 L40 226 M150 184 L160 226 M206 110 L224 226" fill="none" stroke="{INK}" stroke-width="18" stroke-linecap="round"/>'
    '<path d="M50 184 L40 226 M150 184 L160 226 M206 110 L224 226" fill="none" stroke="#d99a55" stroke-width="9" stroke-linecap="round"/>'
    f'<path d="{seat}" fill="#fff"/>'
    '<clipPath id="sc"><path d="' + seat + '"/></clipPath>'
    '<g clip-path="url(#sc)">'
    + "".join(f'<path d="M{x} 30 L{x - 70} 220" stroke="#2fb0c8" stroke-width="22"/>' for x in range(30, 320, 44))
    + '</g>'
    f'<path d="{seat}" fill="none" {O()}/>'
    f'<rect x="180" y="58" width="56" height="30" rx="14" fill="url(#pl)" {O(5)} transform="rotate(-58 208 73)"/>'
    f'<path d="M64 140 L120 140 Q128 168 118 194 L68 194 Q58 166 64 140 Z" fill="url(#tw)" {O(5)}/>',
    lg("pl", "#ffffff", "#ffd0e3") + lg("tw", "#ffe36a", "#ffb21f"))

# Area generators -----------------------------------------------------------------------------
bell = "M42 190 Q42 98 128 96 Q214 98 214 190 Z"
ITEMS["gen_services"] = svg(
    SHADOW +
    f'<ellipse cx="128" cy="206" rx="110" ry="26" fill="url(#base)" {O()}/>'
    '<ellipse cx="128" cy="200" rx="88" ry="13" fill="#fff" opacity=".25"/>'
    f'<rect x="119" y="62" width="18" height="40" rx="6" fill="url(#b)" {O(6)}/>'
    f'<ellipse cx="128" cy="60" rx="30" ry="13" fill="url(#g)" {O()}/>'
    f'<path d="{bell}" fill="url(#g)" {O()}/>'
    f'<rect x="34" y="180" width="188" height="20" rx="10" fill="url(#b)" {O(6)}/>'
    + hl("M70 164 Q72 118 114 104", 13, .8) +
    f'<path d="M24 92 Q14 70 28 52 M232 92 Q242 70 228 52" fill="none" stroke="#fff" stroke-width="9" stroke-linecap="round" opacity=".9"/>'
    + sparkle(204, 132, 14) + sparkle(58, 136, 9),
    rg("g", "#fff6c0", "#ffd84a", "#eaa018", "#b4690a", cx=.38, cy=.3, r=.8) + lg("b", "#ffe788", "#cf880f")
    + lg("base", "#c0506e", "#7a2040"))

ITEMS["gen_pool"] = svg(
    SHADOW +
    '<path d="M128 26 A98 98 0 1 1 127.9 26 Z M128 82 A44 44 0 1 0 128.1 82 Z" fill="url(#g)" fill-rule="evenodd"/>'
    '<circle cx="128" cy="124" r="71" fill="none" stroke="#fff" stroke-width="54" stroke-dasharray="55.7 55.8" transform="rotate(-22 128 124)" opacity=".95"/>'
    f'<circle cx="128" cy="124" r="98" fill="none" {O(8)}/>'
    f'<circle cx="128" cy="124" r="44" fill="none" {O(8)}/>'
    '<circle cx="128" cy="124" r="98" fill="none" stroke="#fff3c4" stroke-width="5" stroke-dasharray="10 14" opacity=".9"/>'
    + hl("M56 92 Q70 50 116 38", 12, .85)
    + sparkle(212, 44, 18) + sparkle(36, 196, 12) + sparkle(200, 196, 9),
    rg("g", "#fff6c0", "#ffd84a", "#eaa018", "#b4690a", cx=.38, cy=.3, r=.8))

# ----------------------------------------------------------------------------- icons
ICONS = {}
ICONS["coin"] = svg(
    f'<circle cx="128" cy="134" r="104" fill="#b8690a" {O(10)}/>'
    f'<circle cx="128" cy="124" r="104" fill="url(#g)" {O(10)}/>'
    '<circle cx="128" cy="124" r="78" fill="none" stroke="#e1960f" stroke-width="9"/>'
    f'<path d="{star_path(128, 128, 52, 24)}" fill="#fff0a0" stroke="#d48510" stroke-width="7" stroke-linejoin="round"/>'
    + hl("M58 96 Q74 52 120 42", 12, .8),
    rg("g", "#fff7c4", "#ffd84a", "#f0a91a", cx=.35, cy=.3, r=.9))
ICONS["energy"] = svg(
    f'<path d="M150 18 L60 142 L118 142 L98 238 L198 102 L140 102 Z" fill="url(#y)" {O(11)}/>'
    + hl("M138 40 L90 112", 9, .8),
    lg("y", "#fff7a0", "#ffb21f"))
ICONS["star"] = svg(
    f'<path d="{star_path(128, 136, 112, 52)}" fill="url(#y)" {O(10)}/>'
    + hl("M92 98 L110 72", 10, .8),
    rg("y", "#fff7b8", "#ffd23a", "#f09a12", cx=.4, cy=.35))
ICONS["check"] = svg(
    f'<circle cx="128" cy="134" r="100" fill="#1d7f36" {O(10)}/>'
    f'<circle cx="128" cy="124" r="100" fill="url(#g)" {O(10)}/>'
    f'<path d="M76 128 L112 164 L182 90" fill="none" stroke="{INK}" stroke-width="40" stroke-linecap="round" stroke-linejoin="round"/>'
    '<path d="M76 128 L112 164 L182 90" fill="none" stroke="#fff" stroke-width="24" stroke-linecap="round" stroke-linejoin="round"/>',
    lg("g", "#8ef07a", "#25a845"))
ICONS["cross"] = svg(
    f'<circle cx="128" cy="134" r="100" fill="#a3182c" {O(10)}/>'
    f'<circle cx="128" cy="124" r="100" fill="url(#g)" {O(10)}/>'
    f'<path d="M88 84 L168 164 M168 84 L88 164" fill="none" stroke="{INK}" stroke-width="40" stroke-linecap="round"/>'
    '<path d="M88 84 L168 164 M168 84 L88 164" fill="none" stroke="#fff" stroke-width="24" stroke-linecap="round"/>',
    lg("g", "#ff8a8a", "#e0304a"))
ICONS["lock"] = svg(
    f'<path d="M84 120 L84 88 Q84 40 128 40 Q172 40 172 88 L172 120" fill="none" stroke="{INK}" stroke-width="36"/>'
    '<path d="M84 120 L84 88 Q84 40 128 40 Q172 40 172 88 L172 120" fill="none" stroke="#c9c3d6" stroke-width="20"/>'
    f'<rect x="54" y="110" width="148" height="118" rx="26" fill="url(#g)" {O(10)}/>'
    f'<path d="M128 146 A16 16 0 0 1 138 174 L142 200 L114 200 L118 174 A16 16 0 0 1 128 146 Z" fill="{INK}"/>'
    + hl("M74 130 L74 200", 10, .7),
    lg("g", "#ffe27a", "#e39a14"))
ICONS["clock"] = svg(
    f'<circle cx="128" cy="132" r="100" fill="url(#r)" {O(10)}/>'
    '<circle cx="128" cy="132" r="76" fill="#fff"/>'
    f'<path d="M128 80 L128 132 L164 150" fill="none" {O(14)}/>'
    f'<circle cx="128" cy="132" r="10" fill="{INK}"/>',
    lg("r", "#7fd0ff", "#3a86e0"))
ICONS["board"] = svg(
    f'<rect x="38" y="40" width="180" height="200" rx="22" fill="url(#w)" {O(10)}/>'
    f'<rect x="58" y="62" width="140" height="160" rx="10" fill="#fffaf0" {O(6)}/>'
    f'<rect x="92" y="22" width="72" height="40" rx="12" fill="url(#m)" {O(8)}/>'
    + "".join(f'<path d="M72 {y} L84 {y + 12} L100 {y - 8}" fill="none" stroke="#2fb34f" stroke-width="9" stroke-linecap="round" stroke-linejoin="round"/>'
              f'<rect x="112" y="{y - 2}" width="70" height="10" rx="5" fill="#d3c4b0"/>'
              for y in (98, 140, 182)),
    lg("w", "#e2a15c", "#a8612a") + lg("m", "#e6e1f0", "#a59eb8"))
ICONS["hammer"] = svg(
    f'<rect x="114" y="70" width="28" height="170" rx="12" fill="url(#w)" {O(9)} transform="rotate(35 128 150)"/>'
    f'<path d="M58 70 L148 22 Q166 14 178 30 L196 54 Q206 70 190 80 L110 126 Z" fill="url(#g)" {O(9)}/>'
    + hl("M78 74 L146 36", 9, .7),
    lg("w", "#e7a96a", "#a8612a") + lg("g", "#e9e6f2", "#8f88a6"))
ICONS["arrow_up"] = svg(
    f'<path d="M58 168 L128 96 L198 168" fill="none" stroke="{INK}" stroke-width="54" stroke-linecap="round" stroke-linejoin="round"/>'
    '<path d="M58 168 L128 96 L198 168" fill="none" stroke="#fff" stroke-width="32" stroke-linecap="round" stroke-linejoin="round"/>')
ICONS["back"] = svg(
    f'<path d="M150 56 L78 128 L150 200" fill="none" stroke="{INK}" stroke-width="54" stroke-linecap="round" stroke-linejoin="round"/>'
    '<path d="M150 56 L78 128 L150 200" fill="none" stroke="#fff" stroke-width="32" stroke-linecap="round" stroke-linejoin="round"/>')
ICONS["plus"] = svg(
    f'<path d="M128 56 L128 200 M56 128 L200 128" fill="none" stroke="{INK}" stroke-width="58" stroke-linecap="round"/>'
    '<path d="M128 56 L128 200 M56 128 L200 128" fill="none" stroke="#fff" stroke-width="34" stroke-linecap="round"/>')
ICONS["heart"] = svg(
    f'<path d="M128 222 Q36 160 32 104 Q30 50 82 46 Q114 44 128 76 Q142 44 174 46 Q226 50 224 104 Q220 160 128 222 Z" fill="url(#r)" {O(10)}/>'
    + hl("M62 92 Q66 70 86 68", 11, .85),
    lg("r", "#ff8fa6", "#e8314f"))
ICONS["pin"] = svg(
    f'<line x1="128" y1="150" x2="128" y2="236" stroke="{INK}" stroke-width="14" stroke-linecap="round"/>'
    '<line x1="128" y1="150" x2="128" y2="230" stroke="#c9c3d6" stroke-width="6" stroke-linecap="round"/>'
    f'<path d="M84 160 L172 160 Q176 132 152 124 L148 72 Q168 64 164 40 L92 40 Q88 64 108 72 L104 124 Q80 132 84 160 Z" fill="url(#r)" {O(9)}/>'
    + hl("M110 52 L116 120", 9, .7),
    lg("r", "#ff7b7b", "#d11f3a"))
ICONS["guest"] = svg(
    f'<circle cx="128" cy="134" r="98" fill="url(#s)" {O(10)}/>'
    f'<ellipse cx="96" cy="124" rx="13" ry="18" fill="{INK}"/><ellipse cx="160" cy="124" rx="13" ry="18" fill="{INK}"/>'
    '<circle cx="100" cy="116" r="5" fill="#fff"/><circle cx="164" cy="116" r="5" fill="#fff"/>'
    '<ellipse cx="76" cy="160" rx="16" ry="10" fill="#ff8fa6" opacity=".7"/><ellipse cx="180" cy="160" rx="16" ry="10" fill="#ff8fa6" opacity=".7"/>'
    f'<path d="M104 166 Q128 190 152 166" fill="none" {O(10)}/>'
    + hl("M60 110 Q66 74 100 56", 11, .6),
    lg("s", "#ffe0c2", "#f5b88a"))
ICONS["kitchen"] = ITEMS["generator"]
ICONS["sparkle"] = svg(sparkle(128, 128, 120, "#ffffff"))
ICONS["glow"] = svg('<circle cx="128" cy="128" r="128" fill="url(#g)"/>',
                    '<radialGradient id="g"><stop offset="0" stop-color="#fff" stop-opacity="1"/>'
                    '<stop offset=".45" stop-color="#fff" stop-opacity=".45"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>')
ICONS["bed"] = svg(
    f'<rect x="30" y="80" width="40" height="150" rx="14" fill="url(#w)" {O(9)}/>'
    f'<rect x="40" y="132" width="186" height="64" rx="18" fill="url(#b)" {O(9)}/>'
    f'<rect x="56" y="104" width="62" height="38" rx="16" fill="#fff" {O(8)}/>'
    f'<rect x="196" y="160" width="30" height="70" rx="10" fill="url(#w)" {O(9)}/>',
    lg("w", "#e7a96a", "#a8612a") + lg("b", "#8fd0ff", "#3a86e0"))

ICONS["sound_on"] = svg(
    f'<path d="M36 98 L82 98 L136 52 L136 204 L82 158 L36 158 Z" fill="url(#w)" {O(10)}/>'
    f'<path d="M166 94 Q188 128 166 162 M190 68 Q230 128 190 188" fill="none" stroke="{INK}" stroke-width="26" stroke-linecap="round"/>'
    '<path d="M166 94 Q188 128 166 162 M190 68 Q230 128 190 188" fill="none" stroke="#fff" stroke-width="12" stroke-linecap="round"/>',
    lg("w", "#ffffff", "#d9d2e6"))
ICONS["sound_off"] = svg(
    f'<path d="M36 98 L82 98 L136 52 L136 204 L82 158 L36 158 Z" fill="url(#w)" {O(10)}/>'
    f'<path d="M166 96 L226 160 M226 96 L166 160" fill="none" stroke="{INK}" stroke-width="30" stroke-linecap="round"/>'
    '<path d="M166 96 L226 160 M226 96 L166 160" fill="none" stroke="#ff6f7f" stroke-width="15" stroke-linecap="round"/>',
    lg("w", "#ffffff", "#d9d2e6"))
ICONS["shop"] = svg(
    f'<path d="M92 92 L92 72 Q92 34 128 34 Q164 34 164 72 L164 92" fill="none" stroke="{INK}" stroke-width="26" stroke-linecap="round"/>'
    '<path d="M92 92 L92 72 Q92 34 128 34 Q164 34 164 72 L164 92" fill="none" stroke="#ffd0e3" stroke-width="11" stroke-linecap="round"/>'
    f'<path d="M48 84 L208 84 L222 214 Q224 232 206 232 L50 232 Q32 232 34 214 Z" fill="url(#b)" {O(10)}/>'
    '<rect x="48" y="84" width="160" height="22" fill="#fff" opacity=".35"/>'
    f'<path d="{star_path(128, 160, 44, 20)}" fill="#ffe36a" {O(7)}/>'
    + hl("M60 112 L54 200", 9, .7),
    lg("b", "#ff9ec4", "#ef4f8f"))
def gear_path(cx, cy, ro, ri, teeth=8):
    pts = []
    for i in range(teeth * 4):
        a = math.radians(i * 360 / (teeth * 4) - 90 + 360 / (teeth * 8))
        r = ro if (i % 4) in (1, 2) else ri
        pts.append(f"{cx + r * math.cos(a):.1f} {cy + r * math.sin(a):.1f}")
    return "M" + " L".join(pts) + " Z"


ICONS["settings"] = svg(
    f'<path d="{gear_path(128, 128, 112, 84)}" fill="url(#w)" {O(10)}/>'
    f'<circle cx="128" cy="128" r="38" fill="#8fd0ff" {O(9)}/>'
    + hl("M70 86 Q86 66 110 58", 10, .8),
    lg("w", "#ffffff", "#c9c0da"))
ICONS["globe"] = svg(
    f'<circle cx="128" cy="128" r="104" fill="url(#b)" {O(10)}/>'
    '<path d="M128 26 Q66 128 128 230 M128 26 Q190 128 128 230 M26 128 L230 128 M46 76 L210 76 M46 180 L210 180" '
    'fill="none" stroke="#fff" stroke-width="9" opacity=".85"/>'
    + hl("M66 72 Q84 50 108 42", 10, .7),
    lg("b", "#8fd8ff", "#3a86e0"))
ICONS["services"] = ITEMS["gen_services"]
ICONS["pool"] = ITEMS["gen_pool"]

# ----------------------------------------------------------------------------- UI (9-slice friendly)
UI = {}


def button(c1, c2, dark):
    return svg(
        f'<rect x="5" y="14" width="118" height="108" rx="34" fill="{dark}" {O(6)}/>'
        f'<rect x="5" y="5" width="118" height="104" rx="34" fill="url(#g)" {O(6)}/>'
        '<rect x="22" y="16" width="84" height="22" rx="11" fill="#fff" opacity=".38"/>',
        lg("g", c1, c2), 128, 128)


UI["btn_green"] = button("#9cf27e", "#33b84b", "#1b7a32")
UI["btn_red"] = button("#ff9a9a", "#e8384f", "#9e1b30")
UI["btn_orange"] = button("#ffd27a", "#ff9420", "#b85c0a")
UI["btn_blue"] = button("#8fd8ff", "#2f8fe8", "#1a5aa8")
UI["btn_purple"] = button("#d4a8ff", "#8f4fe8", "#5a2aa0")
UI["btn_gray"] = button("#e6e1ee", "#a8a0ba", "#6d6680")
UI["btn_white"] = button("#ffffff", "#f1e6dc", "#bfa58e")
UI["panel"] = svg(
    f'<rect x="5" y="14" width="118" height="108" rx="36" fill="#c58f5d" {O(6)}/>'
    f'<rect x="5" y="5" width="118" height="106" rx="36" fill="url(#g)" {O(6)}/>'
    '<rect x="14" y="14" width="100" height="88" rx="28" fill="none" stroke="#fff" stroke-width="3" opacity=".7"/>',
    lg("g", "#fffaf0", "#ffecd2"), 128, 128)
UI["panel_header"] = svg(
    f'<rect x="5" y="12" width="118" height="110" rx="36" fill="#b0306a" {O(6)}/>'
    f'<rect x="5" y="5" width="118" height="106" rx="36" fill="url(#g)" {O(6)}/>'
    '<rect x="22" y="16" width="84" height="20" rx="10" fill="#fff" opacity=".35"/>',
    lg("g", "#ff9ec4", "#ef4f8f"), 128, 128)
UI["pill"] = svg('<rect x="2" y="2" width="124" height="124" rx="62" fill="#2b1838" opacity=".55"/>', "", 128, 128)
UI["pill_light"] = svg(
    f'<rect x="4" y="4" width="120" height="120" rx="60" fill="#fff8ee" {O(6)}/>', "", 128, 128)
UI["card"] = svg(
    '<rect x="8" y="12" width="114" height="112" rx="16" fill="#000" opacity=".2"/>'
    f'<rect x="4" y="4" width="114" height="112" rx="16" fill="url(#g)" {O(5)}/>',
    lg("g", "#fffbe0", "#fff0b0"), 128, 128)
UI["bubble"] = svg(f'<rect x="4" y="4" width="120" height="120" rx="40" fill="#fff" {O(7)}/>', "", 128, 128)
UI["bubble_tail"] = svg(
    f'<path d="M8 0 L64 56 L120 0" fill="#fff" {O(7)}/><rect x="0" y="0" width="128" height="6" fill="#fff"/>', "", 128, 64)
UI["circle"] = svg('<circle cx="64" cy="64" r="62" fill="#fff"/>', "", 128, 128)
UI["ring"] = svg('<circle cx="64" cy="64" r="54" fill="none" stroke="#fff" stroke-width="16"/>', "", 128, 128)
UI["rounded"] = svg('<rect x="0" y="0" width="128" height="128" rx="40" fill="#fff"/>', "", 128, 128)
UI["cell_a"] = svg(
    '<rect x="3" y="3" width="122" height="122" rx="22" fill="#fff4ea"/>'
    '<rect x="3" y="3" width="122" height="122" rx="22" fill="none" stroke="#e9b99c" stroke-width="4" opacity=".7"/>', "", 128, 128)
UI["cell_b"] = svg(
    '<rect x="3" y="3" width="122" height="122" rx="22" fill="#ffe3d0"/>'
    '<rect x="3" y="3" width="122" height="122" rx="22" fill="none" stroke="#e9b99c" stroke-width="4" opacity=".7"/>', "", 128, 128)
UI["select"] = svg(
    '<rect x="5" y="5" width="118" height="118" rx="24" fill="none" stroke="#fff" stroke-width="10"/>'
    '<rect x="5" y="5" width="118" height="118" rx="24" fill="none" stroke="#ffcf3a" stroke-width="5"/>', "", 128, 128)
UI["bg_merge"] = svg('<rect width="64" height="512" fill="url(#g)"/>',
                     lg("g", "#ffb48f", "#f7a3a3", "#e98fb4"), 64, 512)
UI["bg_sky"] = svg('<rect width="64" height="512" fill="url(#g)"/>',
                   lg("g", "#4fb2f5", "#8fd3ff", "#d6f0ff", "#ffe9cc"), 64, 512)
UI["bg_title"] = svg('<rect width="64" height="512" fill="url(#g)"/>',
                     '<linearGradient id="g" x1="0" y1="0" x2="0" y2="1">'
                     '<stop offset="0" stop-color="#2b1050" stop-opacity=".0"/>'
                     '<stop offset=".55" stop-color="#2b1050" stop-opacity=".15"/>'
                     '<stop offset="1" stop-color="#2b1050" stop-opacity=".8"/></linearGradient>', 64, 512)
cork_dots = "".join(
    f'<circle cx="{(i * 37) % 256}" cy="{(i * 71) % 256}" r="{2 + i % 3}" fill="#8a5528" opacity=".35"/>' for i in range(90))
UI["cork"] = svg(f'<rect width="256" height="256" fill="#d29a5f"/>{cork_dots}', "", 256, 256)
UI["logo"] = svg(
    '<g font-family="Noto Sans" font-weight="900" text-anchor="middle">'
    f'<text x="512" y="200" font-size="190" fill="#fff" stroke="{INK}" stroke-width="34" stroke-linejoin="round" paint-order="stroke">HOTEL</text>'
    f'<text x="512" y="404" font-size="210" fill="url(#g)" stroke="{INK}" stroke-width="36" stroke-linejoin="round" paint-order="stroke">MERGE</text>'
    '</g>'
    f'<path d="{star_path(170, 70, 44, 20)}" fill="#ffd23a" {O(10)}/>'
    f'<path d="{star_path(858, 70, 44, 20)}" fill="#ffd23a" {O(10)}/>'
    + sparkle(930, 300, 34) + sparkle(96, 330, 26),
    lg("g", "#fff3a0", "#ffc21f", "#ff8a1f"), 1024, 480)


UI["btn_teal"] = button("#9ff5e8", "#22b8b0", "#137a78")
UI["bg_services"] = svg('<rect width="64" height="512" fill="url(#g)"/>',
                        lg("g", "#c7b4ff", "#a8b8ff", "#f0b8e0"), 64, 512)
UI["bg_pool"] = svg('<rect width="64" height="512" fill="url(#g)"/>',
                    lg("g", "#7fe6ff", "#4fc6ec", "#a6f0dc"), 64, 512)
UI["shine"] = svg('<rect width="64" height="128" fill="url(#g)"/>',
                  '<linearGradient id="g" x1="0" y1="0" x2="1" y2="0">'
                  '<stop offset="0" stop-color="#fff" stop-opacity="0"/><stop offset=".5" stop-color="#fff" stop-opacity=".7"/>'
                  '<stop offset="1" stop-color="#fff" stop-opacity="0"/></linearGradient>', 64, 128)
UI["dock"] = svg(
    '<rect x="4" y="10" width="120" height="114" rx="48" fill="#2b1838" opacity=".25"/>'
    '<rect x="4" y="4" width="120" height="114" rx="48" fill="#fff" opacity=".55"/>'
    '<rect x="4" y="4" width="120" height="114" rx="48" fill="none" stroke="#fff" stroke-width="5"/>', "", 128, 128)


def render(group, items, size_for):
    folder = os.path.join(OUT, group)
    os.makedirs(folder, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        actions = []
        for name, content in items.items():
            src = os.path.join(tmp, name + ".svg")
            with open(src, "w") as f:
                f.write(content)
            w, h = size_for(name)
            dst = os.path.join(folder, name + ".png")
            actions.append(f"file-open:{src};export-width:{w};export-height:{h};"
                           f"export-filename:{dst};export-do;file-close")
        subprocess.run(["inkscape", "--shell"], input="\n".join(actions) + "\nquit\n",
                       text=True, check=True, capture_output=True)
    print(f"{group}: {len(items)} files")


UI_SIZES = {"bg_merge": (64, 512), "bg_sky": (64, 512), "bg_title": (64, 512), "cork": (256, 256),
            "logo": (1024, 480), "bubble_tail": (128, 64),
            "bg_services": (64, 512), "bg_pool": (64, 512), "shine": (64, 128)}
# Merge items are drawn by make_items.py (512 px, UI kit style); ITEMS here only feeds the old icons above.
render("Icons", ICONS, lambda n: (256, 256))
render("UI", UI, lambda n: UI_SIZES.get(n, (128, 128)))
