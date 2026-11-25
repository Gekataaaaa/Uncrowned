"""
World concept previews in the knight's style: chunky chibi pixels, dark #272833 outline around
every part, 3-tone ramps lit from the top-left, lavender / crimson / gold accents.

Writes to Art/Concepts/ (Mobs/, Backgrounds/, Transitions/):
  mobs.png            bestiary (light, medium, heavy, mini-boss, boss), all facing LEFT
  stage_1-1.png       Pit of Exiles mock with HUD
  stage_1-2.png       Weeping Hollows (palette variant of the same kit)
  boss_arena.png      Throne of the Eyeless
  town.png            Lastlight, the refuge
  world_map.png       road out of the Abyss
  transition.gif      stage gate -> crown iris -> title card -> next location
  transition_keys.png key frames of the gif

Run:  python Tools/ArtGen/world.py
"""
import math
import os
import random
from PIL import Image, ImageDraw
from pixel import Canvas, hexc

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
HERO = os.path.join(ROOT, "Assets", "Resources", "Art", "Hero")
OUT_DIR = os.path.join(ROOT, "Art", "Concepts")

W, H = 320, 180
SCALE = 4
OUT = hexc("#272833")
BLACK = hexc("#0b0910")

# (highlight, mid, shadow) - mid is what parts are drawn with, autoshade adds the rest.
RAMPS = {}


def ramp(hi, mid, sh):
    r = (hexc(hi), hexc(mid), hexc(sh))
    RAMPS[r[1]] = r
    return r[1]


LAV = ramp("#acbbd0", "#7b7bab", "#68538a")       # the knight's armor
VIO = ramp("#4f3f6e", "#362b50", "#231b36")       # abyss flesh
ASH = ramp("#a29cb0", "#76708a", "#4e4860")
HULK = ramp("#8f86a3", "#655c7c", "#3f3852")
BONE = ramp("#fff6e0", "#e3d6b8", "#a89878")
CRIM = ramp("#a8324a", "#6e1d33", "#3e0e1d")
RED = ramp("#ff6a6a", "#de5050", "#a64545")
GOLD = ramp("#fff3b0", "#eed878", "#b38f2e")
RUST = ramp("#c47a44", "#8a4524", "#5a2a14")
IRON = ramp("#b6bac6", "#7a7e8c", "#4c4f5e")
WOOD = ramp("#9a7448", "#6a4428", "#432818")
CLOTH = ramp("#7a6a5a", "#544638", "#352a20")
GLOW = ramp("#f0dcff", "#b46bff", "#7a35c8")
TEAL = ramp("#9fe0e8", "#4fa6b8", "#2a6478")

GLOW_HI = hexc("#f0dcff")
EYE_RED = hexc("#ff2b4e")
ICE = hexc("#82d7f0")
FIRE = [hexc("#fff3b0"), hexc("#ffc44a"), hexc("#ff7a2a"), hexc("#c8361c")]


# ---------------------------------------------------------------- pixel font (3x5)

FONT_ROWS = """
A .#. #.# ### #.# #.#
B ##. #.# ##. #.# ##.
C .## #.. #.. #.. .##
D ##. #.# #.# #.# ##.
E ### #.. ##. #.. ###
F ### #.. ##. #.. #..
G .## #.. #.# #.# .##
H #.# #.# ### #.# #.#
I ### .#. .#. .#. ###
J ..# ..# ..# #.# .#.
K #.# #.# ##. #.# #.#
L #.. #.. #.. #.. ###
M #.# ### ### #.# #.#
N ##. #.# #.# #.# #.#
O .#. #.# #.# #.# .#.
P ##. #.# ##. #.. #..
Q .#. #.# #.# ##. .##
R ##. #.# ##. #.# #.#
S .## #.. .#. ..# ##.
T ### .#. .#. .#. .#.
U #.# #.# #.# #.# ###
V #.# #.# #.# #.# .#.
W #.# #.# ### ### #.#
X #.# #.# .#. #.# #.#
Y #.# #.# .#. .#. .#.
Z ### ..# .#. #.. ###
0 ### #.# #.# #.# ###
1 .#. ##. .#. .#. ###
2 ##. ..# .#. #.. ###
3 ##. ..# .#. ..# ##.
4 #.# #.# ### ..# ..#
5 ### #.. ##. ..# ##.
6 .## #.. ### #.# ###
7 ### ..# .#. .#. .#.
8 ### #.# ### #.# ###
9 ### #.# ### ..# ##.
- ... ... ### ... ...
. ... ... ... ... .#.
' .#. .#. ... ... ...
: ... .#. ... .#. ...
! .#. .#. .#. ... .#.
, ... ... ... .#. #..
"""
FONT = {l[0]: "".join(l.split()[1:]) for l in FONT_ROWS.strip().splitlines()}
FONT[" "] = "." * 15


def text_w(s, scale=1):
    return (len(s) * 4 - 1) * scale


def text(img, x, y, s, color, scale=1, shadow=None):
    d = ImageDraw.Draw(img)
    for pass_, col in ((1, shadow), (0, color)):
        if col is None:
            continue
        cx = x + pass_ * scale
        for ch in s.upper():
            g = FONT.get(ch, FONT[" "])
            for i, b in enumerate(g):
                if b == "#":
                    px, py = cx + (i % 3) * scale, y + pass_ * scale + (i // 3) * scale
                    d.rectangle([px, py, px + scale - 1, py + scale - 1], fill=col)
            cx += 4 * scale


# ---------------------------------------------------------------- sprite building

def autoshade(c, thick=1):
    src = c.img.copy()
    sp, dp = src.load(), c.img.load()

    def clear(x, y):
        return not (0 <= x < c.w and 0 <= y < c.h) or sp[x, y][3] == 0

    for y in range(c.h):
        for x in range(c.w):
            p = sp[x, y]
            if p[3] == 0 or p not in RAMPS:
                continue
            hi, _, sh = RAMPS[p]
            if clear(x + 1, y) or any(clear(x, y + k) for k in range(1, thick + 1)):
                dp[x, y] = sh
            elif clear(x, y - 1) or clear(x - 1, y):
                dp[x, y] = hi


def part(c, fn, shade=True, outline=True, thick=1):
    """Draws one body part on its own layer, shades and outlines it, then lays it on top."""
    t = Canvas(c.w, c.h)
    fn(t)
    if shade:
        autoshade(t, thick)
    if outline:
        t.outline(OUT)
    c.img.alpha_composite(t.img)


def sprite(w, h, parts):
    c = Canvas(w, h)
    for p in parts:
        part(c, *p) if isinstance(p, tuple) else part(c, p)
    return c.img


# ---------------------------------------------------------------- bestiary (facing left)

def hollow_crawler():
    def legs(t):
        for x in (8, 13, 18):
            t.line([(x, 8), (x - 3, 13)], ASH)
            t.line([(x + 1, 8), (x + 3, 13)], ASH)

    def body(t):
        t.ell(5, 3, 23, 10, ASH)

    def head(t):
        t.ell(1, 5, 9, 11, ASH)

    def spikes(t):
        for x in (9, 13, 17, 21):
            t.rect(x, 1, x, 3, BONE)
            t.px(x, 0, BONE)

    c = sprite(26, 15, [(legs, False), (body, True, True, 2), spikes, head])
    c.putpixel((3, 7), GLOW_HI)
    c.putpixel((4, 7), hexc("#b46bff"))
    return c


def despair_wisp():
    def flame(t):
        t.poly([(8, 1), (11, 4), (14, 9), (14, 14), (12, 18), (10, 22), (8, 19), (6, 22), (3, 18), (2, 13), (3, 8), (6, 4)], GLOW)

    def mask(t):
        t.ell(4, 9, 11, 16, BONE)

    c = sprite(17, 24, [flame, mask])
    d = ImageDraw.Draw(c)
    d.rectangle([5, 11, 6, 13], fill=OUT)
    d.rectangle([9, 11, 10, 13], fill=OUT)
    d.point([(7, 15), (8, 15)], fill=OUT)
    return c


def gloomling():
    def tail(t):
        t.line([(14, 13), (17, 11), (18, 7)], VIO)

    def body(t):
        t.ell(4, 6, 15, 16, VIO)
        t.rect(5, 15, 7, 17, VIO)
        t.rect(11, 15, 13, 17, VIO)

    def horns(t):
        t.poly([(6, 7), (8, 6), (5, 1)], BONE)
        t.poly([(11, 6), (13, 7), (14, 2)], BONE)

    def claw(t):
        t.ell(1, 10, 5, 13, VIO)

    c = sprite(20, 19, [tail, body, horns, claw])
    d = ImageDraw.Draw(c)
    d.point([(18, 6), (19, 7)], fill=RED)
    d.rectangle([6, 9, 7, 10], fill=EYE_RED)
    d.point([(6, 9)], fill=hexc("#ffb0b0"))
    d.point([(10, 10)], fill=EYE_RED)
    d.line([(6, 13), (10, 13)], fill=BONE)
    return c


def chainbound_ghoul():
    def legs(t):
        t.rect(10, 20, 11, 28, ASH)
        t.rect(14, 20, 15, 28, ASH)

    def cloth(t):
        t.rect(8, 18, 16, 22, CLOTH)

    def torso(t):
        t.ell(7, 8, 17, 20, ASH)

    def head(t):
        t.ell(3, 3, 11, 12, ASH)

    def arm(t):
        t.line([(9, 12), (5, 15), (4, 19)], ASH, 2)

    def shackle(t):
        t.rect(3, 18, 6, 19, IRON)

    def chain(t):
        for i, (x, y) in enumerate([(4, 21), (3, 23), (2, 25), (2, 27), (4, 28), (6, 28)]):
            t.rect(x, y, x + (1 if i % 2 else 0), y + (0 if i % 2 else 1), IRON)

    c = sprite(20, 30, [legs, cloth, torso, head, (chain, False), arm, shackle])
    d = ImageDraw.Draw(c)
    for y in (12, 15, 18):
        d.line([(10, y), (15, y)], fill=OUT)
    d.point([(5, 7), (8, 7)], fill=GLOW_HI)
    d.line([(5, 10), (8, 10)], fill=OUT)
    return c


def weeping_sentinel():
    def robe(t):
        t.poly([(8, 10), (16, 10), (20, 37), (4, 37)], VIO)

    def hood(t):
        t.ell(6, 5, 18, 18, VIO)

    def mask(t):
        t.ell(7, 9, 14, 17, BONE)

    def spear(t):
        t.rect(3, 6, 3, 37, WOOD)

    def head(t):
        t.poly([(3, 0), (5, 4), (3, 7), (1, 4)], IRON)

    def hand(t):
        t.ell(2, 19, 5, 22, BONE)

    c = sprite(22, 39, [robe, hood, mask, spear, head, hand])
    d = ImageDraw.Draw(c)
    d.rectangle([8, 12, 9, 13], fill=OUT)
    d.rectangle([12, 12, 13, 13], fill=OUT)
    d.line([(8, 14), (8, 17)], fill=hexc("#b46bff"))
    d.line([(12, 14), (12, 16)], fill=hexc("#b46bff"))
    d.point([(8, 18), (12, 17)], fill=GLOW_HI)
    return c


def abyssal_hulk():
    def back_arm(t):
        t.ell(27, 13, 35, 25, HULK)

    def legs(t):
        t.rect(11, 25, 16, 33, HULK)
        t.rect(22, 25, 27, 33, HULK)

    def body(t):
        t.ell(6, 4, 32, 28, HULK)

    def head(t):
        t.ell(9, 2, 18, 11, HULK)

    def fist(t):
        t.line([(11, 13), (6, 20)], HULK, 5)
        t.ell(1, 21, 11, 32, HULK)

    c = sprite(37, 35, [back_arm, legs, (body, True, True, 2), head, fist])
    d = ImageDraw.Draw(c)
    d.point([(11, 6), (14, 6)], fill=GLOW_HI)
    d.line([(20, 10), (18, 14), (22, 17), (19, 22)], fill=hexc("#b46bff"))
    d.line([(26, 12), (28, 17)], fill=hexc("#b46bff"))
    d.point([(5, 25), (7, 27)], fill=hexc("#b46bff"))
    return c


def the_jailer():
    def cape(t):
        t.poly([(14, 12), (30, 12), (36, 43), (20, 43)], CRIM)

    def legs(t):
        t.rect(13, 34, 17, 43, IRON)
        t.rect(21, 34, 25, 43, IRON)

    def body(t):
        t.ell(9, 12, 29, 36, RUST)

    def belt(t):
        t.rect(10, 28, 28, 30, WOOD)

    def helm(t):
        t.rect(12, 1, 24, 14, IRON)

    def axe_handle(t):
        t.rect(7, 4, 8, 40, WOOD)

    def axe_head(t):
        t.poly([(7, 4), (1, 1), (0, 8), (1, 15), (7, 12)], IRON)

    def fist(t):
        t.ell(6, 18, 11, 23, RUST)

    c = sprite(38, 45, [cape, legs, body, belt, helm, axe_handle, axe_head, fist])
    d = ImageDraw.Draw(c)
    d.line([(13, 6), (23, 6)], fill=OUT)
    d.line([(13, 7), (23, 7)], fill=OUT)
    d.point([(15, 6), (16, 6), (19, 6), (20, 6)], fill=EYE_RED)
    d.ellipse([16, 29, 21, 35], outline=GOLD)
    d.point([(17, 36), (20, 37)], fill=GOLD)
    d.line([(1, 4), (1, 12)], fill=RED)  # blood on the blade
    return c


def eyeless_sovereign():
    def arms(t):
        for sx in (1, -1):
            x0 = 36 - 18 * sx
            t.line([(x0, 42), (x0 - 10 * sx, 56), (x0 - 14 * sx, 70)], BONE, 2)

    def robe(t):
        t.poly([(22, 36), (50, 36), (68, 81), (4, 81)], CRIM)

    def collar(t):
        t.poly([(17, 34), (55, 34), (52, 42), (20, 42)], GOLD)

    def skull(t):
        t.ell(22, 10, 50, 38, BONE)

    def crown(t):
        t.rect(23, 7, 49, 12, GOLD)
        for i, x in enumerate(range(24, 49, 5)):
            top = 0 if i == 2 else 3
            t.poly([(x, 8), (x + 3, 8), (x + 1, top)], BONE)

    def claws(t):
        for x0 in (4, 64):
            for k in (-2, 0, 2):
                t.line([(x0, 70), (x0 + k, 74)], BONE)

    c = sprite(73, 83, [robe, arms, claws, collar, skull, crown])
    d = ImageDraw.Draw(c)
    d.ellipse([26, 18, 33, 26], fill=hexc("#140e1c"))
    d.ellipse([39, 18, 46, 26], fill=hexc("#140e1c"))
    d.polygon([(35, 27), (37, 27), (36, 30)], fill=hexc("#140e1c"))
    d.line([(29, 31), (43, 31)], fill=OUT)
    for x in (31, 34, 37, 40):
        d.point((x, 32), fill=OUT)
    for x, ln in ((28, 9), (31, 6), (41, 7), (44, 9)):
        d.line([(x, 25), (x, 25 + ln)], fill=hexc("#c8213c"))
    d.ellipse([34, 0, 38, 4], fill=hexc("#b46bff"))
    d.point((35, 1), fill=GLOW_HI)
    for x in range(22, 52, 5):
        d.point((x, 38), fill=RED)
    return c


def floating_eye():
    def ball(t):
        t.ell(0, 0, 6, 6, BONE)

    c = sprite(9, 9, [ball])
    d = ImageDraw.Draw(c)
    d.rectangle([3, 3, 4, 4], fill=EYE_RED)
    d.point((4, 4), fill=hexc("#140e1c"))
    return c


def pad(img, m=1):
    """Room for the outline that `sprite` draws at the canvas edge."""
    out = Image.new("RGBA", (img.width + 2 * m, img.height + 2 * m))
    out.alpha_composite(img, (m, m))
    return out


MOBS = {}


def build_mobs():
    for name, fn in [("crawler", hollow_crawler), ("wisp", despair_wisp), ("gloomling", gloomling),
                     ("ghoul", chainbound_ghoul), ("sentinel", weeping_sentinel), ("hulk", abyssal_hulk),
                     ("jailer", the_jailer), ("sovereign", eyeless_sovereign), ("eye", floating_eye)]:
        MOBS[name] = fn()


# ---------------------------------------------------------------- scene helpers

def hero(name="idle0"):
    return Image.open(os.path.join(HERO, f"knight_{name}.png")).convert("RGBA")


def put(img, spr, fx, fy, feet=None):
    """Pastes a sprite with its feet (default: bottom centre of its bbox) at (fx, fy)."""
    if feet is None:
        bb = spr.getbbox()
        feet = ((bb[0] + bb[2]) // 2, bb[3])
    img.alpha_composite(spr, (int(fx - feet[0]), int(fy - feet[1])))


def put_hero(img, fx, fy, frame="idle0"):
    put(img, hero(frame), fx, fy, feet=(54, 56))


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)


def bands(img, top, bottom, y0, y1, n=6):
    """Vertical gradient in hard bands with a dithered seam, the way pixel skies are done."""
    px = img.load()
    for y in range(y0, y1):
        f = (y - y0) / max(1, y1 - y0 - 1) * (n - 1)
        k = int(f)
        frac = f - k
        if frac > 0.75 and k + 1 < n:
            k2 = k + 1
        else:
            k2 = k
        for x in range(img.width):
            kk = k2 if (frac > 0.6 and (x + y) % 2 == 0) else k
            px[x, y] = lerp(top, bottom, kk / (n - 1))


def heights(seed, base, amp, step=3, rough=2):
    rnd = random.Random(seed)
    ph = [rnd.random() * 6 for _ in range(3)]
    hs = []
    for x in range(0, W + step, step):
        v = base + amp * (0.55 * math.sin(x * 0.021 + ph[0]) + 0.3 * math.sin(x * 0.053 + ph[1]) + 0.15 * math.sin(x * 0.13 + ph[2]))
        v += rnd.randint(-rough, rough)
        hs.extend([int(v)] * step)
    return hs[:W]


def fill_below(img, hs, color, rim=None):
    d = ImageDraw.Draw(img)
    for x, h in enumerate(hs):
        d.line([(x, h), (x, H)], fill=color)
        if rim and (x == 0 or hs[x - 1] >= h):
            d.point((x, h), fill=rim)


def fill_above(img, hs, color):
    d = ImageDraw.Draw(img)
    for x, h in enumerate(hs):
        d.line([(x, 0), (x, h)], fill=color)


def stalactites(img, seed, color, y0, n, length, width=4, rim=None):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    for _ in range(n):
        x = rnd.randint(0, W)
        ln = rnd.randint(length // 2, length)
        w = rnd.randint(width // 2 + 1, width)
        d.polygon([(x - w, y0), (x + w, y0), (x, y0 + ln)], fill=color)
        if rim:
            d.line([(x - w, y0), (x, y0 + ln)], fill=rim)


def glow(img, cx, cy, r, color, strength=0.35, rings=4):
    """Banded additive-ish light pool (no smooth falloff, so it stays pixel-crisp)."""
    px = img.load()
    for y in range(max(0, cy - r), min(img.height, cy + r + 1)):
        for x in range(max(0, cx - r), min(img.width, cx + r + 1)):
            dd = math.hypot(x - cx, (y - cy) * 1.15) / r
            if dd >= 1:
                continue
            band = math.ceil((1 - dd) * rings) / rings
            t = strength * band
            p = px[x, y]
            px[x, y] = tuple(min(255, int(p[i] + (color[i] - p[i]) * t)) for i in range(3)) + (p[3],)


def chain_line(img, x, y0, y1, color, hi=None):
    d = ImageDraw.Draw(img)
    for y in range(y0, y1, 4):
        if (y // 4) % 2:
            d.rectangle([x - 1, y, x + 1, y + 2], outline=color)
        else:
            d.line([(x, y), (x, y + 3)], fill=hi or color)


def ground(img, y, top, mid, dark, seam, seed=1):
    d = ImageDraw.Draw(img)
    d.rectangle([0, y, W, H], fill=mid)
    d.line([(0, y), (W, y)], fill=top)
    d.line([(0, y + 1), (W, y + 1)], fill=top)
    rnd = random.Random(seed)
    for row, ry in enumerate(range(y + 2, H, 7)):
        off = 0 if row % 2 else 9
        for x in range(-off, W, 18):
            d.line([(x, ry), (x, ry + 6)], fill=seam)
        d.line([(0, ry + 6), (W, ry + 6)], fill=seam)
    # darken lower rows
    px = img.load()
    for yy in range(y + 10, H):
        for x in range(W):
            if (x + yy) % 2 == 0 or yy > y + 18:
                p = px[x, yy]
                px[x, yy] = lerp(p, dark, 0.45 if yy <= y + 18 else 0.7)
    for _ in range(18):
        x = rnd.randint(0, W)
        d.point((x, y + 2), fill=seam)


def particles(img, seed, n, colors, y0=0, y1=H):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    for _ in range(n):
        x, y = rnd.randint(0, W - 1), rnd.randint(y0, y1)
        c = rnd.choice(colors)
        d.point((x, y), fill=c)
        if rnd.random() < 0.3:
            d.point((x + 1, y), fill=c)


def crystal(img, x, y, h, col=GLOW):
    def f(t):
        t.poly([(x, y), (x - 2, y - h // 2), (x, y - h), (x + 2, y - h // 2)], col)
    c = Canvas(W, H)
    part(c, f)
    glow(img, x, y - h // 2, h + 6, hexc("#b46bff"), 0.25, 3)
    img.alpha_composite(c.img)


def skull_pile(img, x, y):
    def f(t):
        t.ell(x, y - 5, x + 6, y, BONE)
        t.ell(x + 5, y - 3, x + 9, y, BONE)
    c = Canvas(W, H)
    part(c, f)
    d = ImageDraw.Draw(c.img)
    d.point([(x + 2, y - 3), (x + 4, y - 3)], fill=OUT)
    img.alpha_composite(c.img)


def bar(img, x, y, w, h, frac, fill, back=hexc("#1a1424")):
    d = ImageDraw.Draw(img)
    d.rectangle([x - 1, y - 1, x + w, y + h], fill=OUT)
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=back)
    d.rectangle([x, y, x + int(w * frac) - 1, y + h - 1], fill=fill)
    d.line([(x, y), (x + int(w * frac) - 1, y)], fill=lerp(fill, (255, 255, 255), 0.4))


def hud(img, hp=0.8, mana=0.55):
    d = ImageDraw.Draw(img)
    # portrait: the knight's two eyes in a little frame
    d.rectangle([4, 4, 21, 21], fill=OUT)
    d.rectangle([5, 5, 20, 20], fill=hexc("#7b7bab"))
    d.rectangle([5, 14, 20, 20], fill=hexc("#68538a"))
    d.rectangle([8, 10, 10, 12], fill=hexc("#4696c3"))
    d.point((8, 10), fill=ICE)
    d.rectangle([14, 10, 16, 12], fill=hexc("#aa190f"))
    d.point((14, 10), fill=hexc("#ff6a6a"))
    d.line([(5, 5), (20, 5)], fill=hexc("#de5050"))
    bar(img, 25, 6, 64, 5, hp, hexc("#de5050"))
    bar(img, 25, 14, 48, 3, mana, hexc("#4696c3"))
    # skill slots
    for i, col in enumerate([hexc("#acbbd0"), hexc("#de5050"), hexc("#eed878"), hexc("#b46bff")]):
        x = W - 74 + i * 18
        d.rectangle([x, H - 20, x + 14, H - 6], fill=OUT)
        d.rectangle([x + 1, H - 19, x + 13, H - 7], fill=hexc("#2a2238"))
        d.polygon([(x + 4, H - 9), (x + 10, H - 16), (x + 11, H - 15), (x + 5, H - 8)], fill=col)
    d.rectangle([W - 74 + 54 + 1, H - 19, W - 74 + 54 + 13, H - 13], fill=hexc("#140e1c"))  # ult on cooldown


def save(img, name):
    os.makedirs(os.path.dirname(os.path.join(OUT_DIR, name)), exist_ok=True)
    img.resize((img.width * SCALE, img.height * SCALE), Image.NEAREST).save(os.path.join(OUT_DIR, name))


# ---------------------------------------------------------------- scenes

def cave_backdrop(pal, seed):
    """Shared abyss parallax: sky, light shaft, far walls, mid pillars, chains, ceiling."""
    img = Image.new("RGBA", (W, H), BLACK)
    bands(img, pal["sky_top"], pal["sky_bot"], 0, H, 7)
    # shaft of pale light from the pit mouth far above
    shaft = Image.new("RGBA", (W, H))
    ImageDraw.Draw(shaft).polygon([(150, 0), (196, 0), (240, H), (130, H)], fill=pal["shaft"])
    img.alpha_composite(shaft)
    particles(img, seed, 40, [pal["mote"]], 0, 140)
    fill_below(img, heights(seed + 1, 92, 22, 4, 3), pal["far"], pal["far_rim"])
    stalactites(img, seed + 2, pal["far"], 0, 26, 40, 6)
    for x in (40, 118, 260):
        chain_line(img, x, 0, 70 + (x % 30), pal["far_rim"])
    fill_below(img, heights(seed + 3, 122, 16, 3, 2), pal["mid"], pal["mid_rim"])
    stalactites(img, seed + 4, pal["mid"], 0, 14, 32, 7, pal["mid_rim"])
    fill_above(img, heights(seed + 5, 8, 6, 3, 2), pal["mid"])
    return img


PIT = dict(sky_top=hexc("#0d0a14"), sky_bot=hexc("#2a1f3d"), shaft=hexc("#cfc4ff", 22), mote=hexc("#8f6fd0"),
           far=hexc("#231b33"), far_rim=hexc("#33294a"), mid=hexc("#17121f"), mid_rim=hexc("#3a2f55"),
           g_top=hexc("#5e5480"), g_mid=hexc("#3a3152"), g_dark=hexc("#120e1a"), g_seam=hexc("#272035"))

HOLLOWS = dict(sky_top=hexc("#070d14"), sky_bot=hexc("#163240"), shaft=hexc("#bfefff", 18), mote=hexc("#6fc2d6"),
               far=hexc("#10222e"), far_rim=hexc("#1e3c4c"), mid=hexc("#0a141c"), mid_rim=hexc("#2b5568"),
               g_top=hexc("#4f8a9c"), g_mid=hexc("#244452"), g_dark=hexc("#08121a"), g_seam=hexc("#16303c"))


def stage_pit(with_actors=True):
    img = cave_backdrop(PIT, 11)
    ground(img, 150, PIT["g_top"], PIT["g_mid"], PIT["g_dark"], PIT["g_seam"])
    for x, hh in ((22, 9), (27, 6), (232, 11), (238, 7), (300, 8)):
        crystal(img, x, 150, hh)
    skull_pile(img, 70, 150)
    skull_pile(img, 196, 150)
    # broken cage hanging from the ceiling
    d = ImageDraw.Draw(img)
    chain_line(img, 268, 0, 34, hexc("#4c4f5e"), hexc("#7a7e8c"))
    d.rectangle([258, 34, 278, 58], outline=hexc("#4c4f5e"))
    for x in (262, 266, 270, 274):
        d.line([(x, 34), (x, 58 if x != 270 else 46)], fill=hexc("#4c4f5e"))
    if with_actors:
        put_hero(img, 92, 150)
        put(img, MOBS["crawler"], 176, 150)
        put(img, MOBS["gloomling"], 214, 150)
        put(img, MOBS["wisp"], 150, 104)
        put(img, MOBS["ghoul"], 262, 150)
    # foreground framing
    chain_line(img, 8, 0, 120, hexc("#0b0910"))
    fill_below(img, [H if 30 < x < 300 else 160 + abs(x - (15 if x <= 30 else 310)) for x in range(W)], hexc("#08060c"))
    return img


def stage_hollows():
    img = cave_backdrop(HOLLOWS, 23)
    d = ImageDraw.Draw(img)
    # weeping faces carved in the far wall, crying water
    for fx, fy in ((70, 96), (210, 88)):
        d.ellipse([fx, fy, fx + 22, fy + 26], fill=hexc("#1e3c4c"))
        d.rectangle([fx + 5, fy + 9, fx + 8, fy + 12], fill=hexc("#07101a"))
        d.rectangle([fx + 14, fy + 9, fx + 17, fy + 12], fill=hexc("#07101a"))
        d.line([(fx + 6, fy + 13), (fx + 6, 150)], fill=hexc("#4fa6b8"))
        d.line([(fx + 16, fy + 13), (fx + 16, 150)], fill=hexc("#4fa6b8"))
        d.line([(fx + 7, fy + 20), (fx + 15, fy + 20)], fill=hexc("#07101a"))
    ground(img, 150, HOLLOWS["g_top"], HOLLOWS["g_mid"], HOLLOWS["g_dark"], HOLLOWS["g_seam"], 4)
    # puddles reflecting the shaft
    for x0, x1 in ((60, 104), (190, 236)):
        d.rectangle([x0, 151, x1, 152], fill=hexc("#9fe0e8"))
        d.line([(x0 + 4, 153), (x1 - 4, 153)], fill=hexc("#4fa6b8"))
    for x, hh in ((30, 8), (290, 10)):
        crystal(img, x, 150, hh, TEAL)
    put_hero(img, 120, 150)
    put(img, MOBS["sentinel"], 232, 150)
    put(img, MOBS["wisp"], 190, 110)
    fill_below(img, [H if 26 < x < 296 else 156 + abs(x - (13 if x <= 26 else 308)) for x in range(W)], hexc("#04080c"))
    return img


def boss_arena():
    img = Image.new("RGBA", (W, H), BLACK)
    bands(img, hexc("#0a0710"), hexc("#2b0f1e"), 0, H, 7)
    d = ImageDraw.Draw(img)
    # colonnade
    for x in range(10, W, 52):
        d.rectangle([x, 20, x + 14, 150], fill=hexc("#1e1626"))
        d.line([(x, 20), (x, 150)], fill=hexc("#3a2c48"))
        d.rectangle([x - 3, 18, x + 17, 24], fill=hexc("#2a2036"))
        # tattered banners
        if (x // 52) % 2:
            d.polygon([(x + 18, 30), (x + 34, 30), (x + 34, 78), (x + 26, 70), (x + 18, 80)], fill=hexc("#5e1a2c"))
            d.line([(x + 18, 30), (x + 18, 80)], fill=hexc("#8a2a3e"))
            d.rectangle([x + 24, 44, x + 28, 48], fill=hexc("#b38f2e"))
    stalactites(img, 7, hexc("#0e0a14"), 0, 20, 26, 6)
    # throne of bones
    d.polygon([(200, 150), (206, 40), (230, 22), (254, 40), (260, 150)], fill=hexc("#3a3040"))
    for k, x in enumerate(range(204, 260, 7)):
        d.polygon([(x, 42), (x + 5, 42), (x + 2, 26 - (6 if k in (3, 4) else 0) + abs(k - 4) * 2)], fill=hexc("#e3d6b8"))
    for x in range(204, 258, 8):
        d.ellipse([x, 60, x + 6, 66], fill=hexc("#a89878"))
        d.point([(x + 2, 62), (x + 4, 62)], fill=OUT)
    glow(img, 230, 100, 70, hexc("#b46bff"), 0.28, 4)
    ground(img, 150, hexc("#6e1d33"), hexc("#2e1420"), hexc("#0a0508"), hexc("#1e0c14"), 9)
    # aura + boss + orbiting eyes
    glow(img, 232, 112, 46, hexc("#ff2b4e"), 0.22, 3)
    put(img, MOBS["sovereign"], 232, 148)
    for a in range(6):
        ang = a * math.pi / 3 + 0.4
        put(img, MOBS["eye"], 232 + int(math.cos(ang) * 52), 104 + int(math.sin(ang) * 34))
    put_hero(img, 96, 150, "atkA2")
    # boss bar
    name = "THE EYELESS SOVEREIGN"
    text(img, (W - text_w(name)) // 2, H - 22, name, hexc("#e3d6b8"), shadow=OUT)
    bar(img, 60, H - 14, 200, 4, 0.72, hexc("#c8213c"))
    hud(img, 0.46, 0.9)
    return img


def town():
    img = Image.new("RGBA", (W, H), BLACK)
    bands(img, hexc("#0c0a10"), hexc("#2a1c22"), 0, H, 7)
    fill_below(img, heights(41, 86, 18, 4, 3), hexc("#221820"), hexc("#3a2a30"))
    # crack in the cave roof letting a little sky in
    d = ImageDraw.Draw(img)
    d.polygon([(132, 0), (176, 0), (160, 18), (150, 26), (144, 14)], fill=hexc("#3a4870"))
    particles(img, 5, 10, [hexc("#cfd8ff")], 0, 18)
    stalactites(img, 42, hexc("#140e14"), 0, 22, 30, 6, hexc("#2a1c24"))
    fill_above(img, heights(43, 10, 6, 3, 2), hexc("#140e14"))

    def house(x, w, h, roof, lit=True):
        base = 150
        d.rectangle([x, base - h, x + w, base], fill=hexc("#3a2418"))
        for yy in range(base - h + 4, base, 5):
            d.line([(x, yy), (x + w, yy)], fill=hexc("#2a180e"))
        d.line([(x, base - h), (x, base)], fill=hexc("#5a3a22"))
        d.rectangle([x - 1, base - 6, x + w + 1, base], fill=hexc("#4e4868"))
        d.polygon([(x - 4, base - h), (x + w // 2, base - h - roof), (x + w + 4, base - h)], fill=hexc("#5e2630"))
        d.line([(x - 4, base - h), (x + w // 2, base - h - roof)], fill=hexc("#8a3a3e"))
        dx = x + w // 2 - 3
        d.rectangle([dx, base - 12, dx + 6, base], fill=hexc("#1a0e08"))
        if lit:
            for wx in (x + 4, x + w - 9):
                glow(img, wx + 2, base - h + 10, 14, hexc("#ffb84a"), 0.3, 3)
                d.rectangle([wx, base - h + 7, wx + 5, base - h + 12], fill=hexc("#ffc46b"))
                d.line([(wx + 2, base - h + 7), (wx + 2, base - h + 12)], fill=hexc("#7a3a1a"))

    house(20, 40, 34, 16)
    house(200, 44, 40, 18)
    # forge: open shed with a glowing hearth and anvil
    d.rectangle([250, 104, 300, 150], fill=hexc("#2e2228"))
    d.polygon([(246, 104), (304, 104), (296, 92), (254, 92)], fill=hexc("#4e4868"))
    d.rectangle([282, 60, 290, 92], fill=hexc("#3a3550"))
    glow(img, 270, 136, 30, hexc("#ff7a2a"), 0.45, 4)
    d.rectangle([258, 128, 280, 140], fill=hexc("#5a2a14"))
    d.rectangle([260, 130, 278, 134], fill=hexc("#ffc44a"))
    d.polygon([(284, 140), (296, 140), (294, 144), (286, 144)], fill=hexc("#7a7e8c"))
    d.rectangle([288, 144, 292, 150], fill=hexc("#4c4f5e"))
    # lantern string
    pts = [(60, 110), (130, 118), (200, 108)]
    for a, b in zip(pts, pts[1:]):
        for i in range(0, 21):
            t = i / 20
            x = a[0] + (b[0] - a[0]) * t
            y = a[1] + (b[1] - a[1]) * t + math.sin(t * math.pi) * 8
            d.point((x, y), fill=hexc("#2a1c18"))
            if i % 5 == 2:
                glow(img, int(x), int(y) + 3, 7, hexc("#ffb84a"), 0.35, 2)
                d.rectangle([x - 1, y + 1, x + 1, y + 4], fill=hexc("#ffc46b"))
    ground(img, 150, hexc("#6a4c3e"), hexc("#3a2a24"), hexc("#100a0a"), hexc("#2a1c18"), 6)
    # campfire in the square
    glow(img, 150, 140, 54, hexc("#ff9a3a"), 0.32, 5)
    d.line([(140, 150), (160, 146)], fill=hexc("#432818"), width=3)
    d.line([(140, 146), (160, 150)], fill=hexc("#6a4428"), width=3)
    for i, col in enumerate(FIRE[::-1]):
        s = 8 - i * 2
        d.polygon([(150 - s, 146), (150, 132 + i * 3 - (2 if i == 3 else 0)), (150 + s, 146)], fill=col)
    particles(img, 8, 8, [hexc("#ffc44a"), hexc("#ff7a2a")], 110, 135)
    # townsfolk
    put(img, npc_smith(), 244, 150)
    put(img, npc_merchant(), 72, 150)
    put(img, npc_elder(), 184, 150)
    put_hero(img, 124, 150)
    # sign
    text(img, 6, 6, "LASTLIGHT", hexc("#ffc46b"), 2, shadow=OUT)
    text(img, 6, 20, "A REFUGE IN THE ABYSS", hexc("#a89878"), shadow=OUT)
    return img


def npc_smith():
    def legs(t):
        t.rect(5, 16, 7, 21, CLOTH)
        t.rect(10, 16, 12, 21, CLOTH)

    def body(t):
        t.ell(2, 6, 15, 18, hexc("#b46d5a") if False else RUST)

    def apron(t):
        t.rect(5, 10, 12, 19, WOOD)

    def head(t):
        t.ell(4, 0, 12, 8, ramp("#ffdcc2", "#e8a787", "#b46d5a"))

    def hammer(t):
        t.rect(0, 6, 1, 15, WOOD)
        t.rect(-1, 4, 3, 7, IRON)

    c = sprite(17, 23, [legs, body, apron, head, hammer])
    d = ImageDraw.Draw(c)
    d.rectangle([4, 5, 12, 8], fill=hexc("#7a4a2a"))  # beard
    d.point((6, 3), fill=OUT)
    return pad(c)


def npc_merchant():
    def pack(t):
        t.rect(9, 3, 17, 15, WOOD)

    def cloak(t):
        t.poly([(5, 4), (11, 4), (14, 21), (2, 21)], ramp("#6a9a6a", "#3e6a4e", "#24402e"))

    def hood(t):
        t.ell(3, 0, 12, 10, ramp("#6a9a6a", "#3e6a4e", "#24402e"))

    c = sprite(19, 23, [pack, cloak, hood])
    d = ImageDraw.Draw(c)
    d.ellipse([4, 3, 9, 8], fill=hexc("#140e1c"))
    d.point([(5, 5), (7, 5)], fill=hexc("#ffc44a"))
    return pad(c)


def npc_elder():
    def robe(t):
        t.poly([(4, 6), (10, 6), (13, 20), (1, 20)], ramp("#8a86a8", "#5e5a7c", "#3c3852"))

    def head(t):
        t.ell(3, 0, 10, 7, ramp("#ffdcc2", "#e8a787", "#b46d5a"))

    def staff(t):
        t.rect(0, 2, 1, 20, WOOD)

    c = sprite(15, 22, [robe, head, staff])
    d = ImageDraw.Draw(c)
    d.rectangle([3, 0, 10, 2], fill=hexc("#e3d6b8"))
    d.rectangle([3, 5, 8, 8], fill=hexc("#e3d6b8"))
    d.point((5, 3), fill=OUT)
    return pad(c)


def world_map():
    img = Image.new("RGBA", (W, H), BLACK)
    bands(img, hexc("#2a2440"), hexc("#140e1c"), 0, H, 6)
    d = ImageDraw.Draw(img)
    mx0, my0, mx1, my1 = 16, 12, W - 16, H - 12
    # regions: kingdom glow on top, ash wilds, abyss swirl at the bottom
    glow(img, 160, 18, 80, hexc("#eed878"), 0.22, 4)
    rnd = random.Random(3)
    for _ in range(26):
        x, y = rnd.randint(40, 280), rnd.randint(50, 90)
        d.line([(x, y), (x, y - 6)], fill=hexc("#3e3442"))
        d.line([(x, y - 4), (x + 2, y - 6)], fill=hexc("#3e3442"))
    for r in range(70, 10, -12):
        d.arc([160 - r * 2, 150 - r, 160 + r * 2, 150 + r], 200, 340, fill=hexc("#4a2a6e"), width=2)
    glow(img, 120, 160, 70, hexc("#7a35c8"), 0.25, 4)
    # frame
    d.rectangle([mx0 - 3, my0 - 3, mx1 + 3, my1 + 3], outline=OUT, width=1)
    d.rectangle([mx0 - 2, my0 - 2, mx1 + 2, my1 + 2], outline=hexc("#7b7bab"), width=1)
    d.rectangle([mx0 - 1, my0 - 1, mx1 + 1, my1 + 1], outline=hexc("#68538a"), width=1)
    for cx, cy in ((mx0 - 2, my0 - 2), (mx1 + 2, my0 - 2), (mx0 - 2, my1 + 2), (mx1 + 2, my1 + 2)):
        d.rectangle([cx - 2, cy - 2, cx + 2, cy + 2], fill=hexc("#eed878"), outline=OUT)

    nodes = [("1-1", .12, .06, "stage", 1), ("town", .30, .08, "town", 1), ("1-2", .44, .17, "stage", 0),
             ("1-3", .24, .27, "stage", -1), ("1-4", .42, .33, "skull", -1), ("1-5", .60, .40, "boss", -1),
             ("2-1", .58, .64, "stage", -1), ("2-2", .40, .74, "stage", -1), ("etheraion", .50, .90, "castle", -1)]
    req = {"town": "1-1", "1-2": "1-1", "1-3": "1-2", "1-4": "1-3", "1-5": "1-4", "2-1": "1-5", "2-2": "2-1", "etheraion": "2-2"}

    def pos(n):
        return int(mx0 + 10 + n[1] * (mx1 - mx0 - 20)), int(my1 - 8 - n[2] * (my1 - my0 - 16))

    by = {n[0]: n for n in nodes}
    for n in nodes:
        if n[0] in req:
            a, b = pos(by[req[n[0]]]), pos(n)
            steps = int(math.hypot(b[0] - a[0], b[1] - a[1]) / 4)
            for i in range(1, steps):
                t = i / steps
                col = hexc("#eed878") if n[4] >= 0 else hexc("#4e4868")
                d.point((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t), fill=col)
    # fog of war over the locked upper map
    px = img.load()
    for y in range(my0, 104):
        for x in range(mx0, mx1):
            if (x + y) % 2 == 0 or y < 70:
                px[x, y] = lerp(px[x, y], hexc("#0e0b14"), 0.55 if y < 70 else 0.35)

    for n in nodes:
        x, y = pos(n)
        kind, state = n[3], n[4]
        rim = hexc("#eed878") if state == 1 else hexc("#acbbd0") if state == 0 else hexc("#4e4868")
        if kind == "stage":
            d.ellipse([x - 4, y - 4, x + 4, y + 4], fill=OUT)
            d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=rim)
            d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=hexc("#2a2238"))
        elif kind == "town":
            d.rectangle([x - 5, y - 3, x + 5, y + 4], fill=OUT)
            d.rectangle([x - 4, y - 2, x + 4, y + 3], fill=hexc("#6a4428"))
            d.polygon([(x - 6, y - 3), (x, y - 8), (x + 6, y - 3)], fill=hexc("#6e1d33"), outline=OUT)
            d.point((x, y), fill=hexc("#ffc46b"))
            glow(img, x, y, 9, hexc("#ffb84a"), 0.3, 2)
        elif kind in ("skull", "boss"):
            s = 4 if kind == "skull" else 6
            d.ellipse([x - s, y - s, x + s, y + s - 1], fill=OUT)
            d.ellipse([x - s + 1, y - s + 1, x + s - 1, y + s - 2], fill=rim if kind == "skull" else hexc("#6e1d33"))
            d.point([(x - 2, y - 1), (x + 2, y - 1)], fill=OUT)
            if kind == "boss":
                for k in (-4, 0, 4):
                    d.polygon([(x + k - 1, y - 5), (x + k + 1, y - 5), (x + k, y - 9)], fill=hexc("#4e4868"))
        elif kind == "castle":
            d.rectangle([x - 12, y - 6, x + 12, y + 4], fill=OUT)
            d.rectangle([x - 11, y - 5, x + 11, y + 3], fill=hexc("#3c3852"))
            for k in (-11, -3, 5):
                d.rectangle([x + k, y - 12, x + k + 6, y - 5], fill=hexc("#3c3852"), outline=OUT)
            d.polygon([(x - 2, y - 18), (x + 2, y - 18), (x, y - 24)], fill=hexc("#eed878"))
        label = n[0].upper() if n[0] not in ("town", "etheraion") else ("LASTLIGHT" if n[0] == "town" else "ETHERAION")
        col = hexc("#e3d6b8") if state >= 0 else hexc("#5e5a7c")
        text(img, x - text_w(label) // 2, y + 7, label, col, shadow=OUT)
    # hero marker on 1-2 (current target): little banner with the two eyes
    x, y = pos(by["1-2"])
    x, y = x + 13, y + 8
    d.polygon([(x - 5, y - 18), (x + 5, y - 18), (x + 5, y - 9), (x, y - 6), (x - 5, y - 9)], fill=OUT)
    d.polygon([(x - 4, y - 17), (x + 4, y - 17), (x + 4, y - 10), (x, y - 7), (x - 4, y - 10)], fill=hexc("#7b7bab"))
    d.point((x - 2, y - 13), fill=ICE)
    d.point((x + 2, y - 13), fill=hexc("#ff2b4e"))
    # tooltip
    d.rectangle([200, 130, 300, 162], fill=hexc("#140e1c"), outline=hexc("#68538a"))
    text(img, 205, 134, "1-2 WEEPING HOLLOWS", hexc("#eed878"))
    text(img, 205, 143, "CAVERNS THAT CRY IN", hexc("#a89878"))
    text(img, 205, 150, "BORROWED VOICES.", hexc("#a89878"))
    return img


# ---------------------------------------------------------------- transition

def crown_mask(cx, cy, s):
    """Crown-shaped iris: the screen closes down to a crown around the hero."""
    m = Image.new("L", (W, H), 0)
    pts = [(-1, .6), (-1, -.45), (-.6, -.05), (-.32, -.75), (0, -.12), (.32, -.75), (.6, -.05), (1, -.45), (1, .6)]
    if s > 0:
        ImageDraw.Draw(m).polygon([(cx + px * s, cy + py * s) for px, py in pts], fill=255)
    return m


def iris(frame, cx, cy, s):
    out = Image.new("RGBA", (W, H), BLACK)
    out.paste(frame, (0, 0), crown_mask(cx, cy, s))
    return out


def gate_stage():
    """End of 1-1: the road ends in a glowing arch that leads down to the next stage."""
    img = stage_pit(with_actors=False)
    d = ImageDraw.Draw(img)
    gx = 262
    glow(img, gx, 128, 40, hexc("#b46bff"), 0.35, 4)
    d.rectangle([gx - 16, 100, gx + 16, 150], fill=OUT)
    d.rectangle([gx - 15, 101, gx + 15, 150], fill=hexc("#3a3152"))
    d.rectangle([gx - 10, 108, gx + 10, 150], fill=hexc("#140e1c"))
    d.pieslice([gx - 10, 98, gx + 10, 118], 180, 360, fill=hexc("#140e1c"))
    d.rectangle([gx - 9, 112, gx + 9, 150], fill=hexc("#5a2aa0"))
    d.rectangle([gx - 7, 116, gx + 7, 150], fill=hexc("#b46bff"))
    d.rectangle([gx - 4, 120, gx + 4, 150], fill=hexc("#f0dcff"))
    d.rectangle([gx - 3, 94, gx + 3, 100], fill=hexc("#eed878"), outline=OUT)
    text(img, gx - text_w("1-2") // 2, 88, "1-2", hexc("#e3d6b8"), shadow=OUT)
    return img


def title_card(title, sub, k, n):
    """Black card; text brightens in three steps and a red rule grows under it."""
    img = Image.new("RGBA", (W, H), BLACK)
    t = min(1, k / (n * 0.6))
    step = min(3, int(t * 4))
    shade = [hexc("#2a2238"), hexc("#68538a"), hexc("#acbbd0"), hexc("#fff6e0")][step]
    num, name = title.split(" ", 1)
    text(img, (W - text_w(num)) // 2, 62, num, [hexc("#2a0a10"), hexc("#5f0a05"), hexc("#a64545"), hexc("#de5050")][step])
    text(img, (W - text_w(name, 2)) // 2, 74, name, shade, 2)
    half = int(min(1, k / (n * 0.5)) * 70)
    ImageDraw.Draw(img).line([(W // 2 - half, 90), (W // 2 + half, 90)], fill=hexc("#de5050"))
    if step == 3:
        text(img, (W - text_w(sub)) // 2, 96, sub, hexc("#68538a"))
    return img


def transition():
    frames, durs = [], []
    base = gate_stage()
    walk = [f"walk{i}" for i in range(6)]
    # 1. hero walks into the gate
    for i in range(14):
        f = base.copy()
        put_hero(f, 150 + i * 7, 150, walk[i % 6])
        frames.append(f)
        durs.append(80)
    hero_x = 150 + 13 * 7
    last = frames[-1]
    # 2. crown iris closes on the hero
    for s in (300, 200, 130, 80, 50, 32, 22, 14, 0):
        frames.append(iris(last, hero_x, 132, s))
        durs.append(60)
    durs[-1] = 200
    # 3. title card
    for k in range(10):
        frames.append(title_card("1-2 WEEPING HOLLOWS", "CAVERNS THAT CRY IN BORROWED VOICES", k, 10))
        durs.append(90)
    durs[-1] = 900
    # 4. iris opens on the new stage, hero already standing at the entrance
    nxt = stage_hollows()
    for s in (0, 14, 22, 32, 50, 80, 130, 200, 300):
        frames.append(iris(nxt, 120, 132, s))
        durs.append(60)
    frames.append(nxt)
    durs.append(1500)
    big = [f.convert("RGB").resize((W * 3, H * 3), Image.NEAREST) for f in frames]
    os.makedirs(OUT_DIR, exist_ok=True)
    big[0].save(os.path.join(OUT_DIR, "Transitions/transition.gif"), save_all=True, append_images=big[1:], duration=durs, loop=0, optimize=True)
    keys = [frames[6], frames[14 + 5], frames[14 + 9 + 9], frames[14 + 9 + 10 + 4]]
    strip = Image.new("RGBA", (W * 2 + 6, H * 2 + 6), hexc("#0e0b14"))
    for i, k in enumerate(keys):
        strip.alpha_composite(k, ((i % 2) * (W + 6), (i // 2) * (H + 6)))
    save(strip, "Transitions/transition_keys.png")


def mobs_sheet():
    from pixel import sheet
    items = [("Hollow Crawler", "LIGHT - pack husk", MOBS["crawler"]),
             ("Despair Wisp", "LIGHT - flying diver", MOBS["wisp"]),
             ("Gloomling", "LIGHT - leaping imp", MOBS["gloomling"]),
             ("Chainbound Ghoul", "MEDIUM - chain sweep", MOBS["ghoul"]),
             ("Weeping Sentinel", "MEDIUM - spear keeper", MOBS["sentinel"]),
             ("Abyssal Hulk", "HEAVY - slow slammer", MOBS["hulk"]),
             ("The Jailer", "MINI-BOSS 1-4", MOBS["jailer"]),
             ("Eyeless Sovereign", "BOSS 1-5", MOBS["sovereign"]),
             ("Knight (scale ref)", "our hero", hero("idle0").crop(hero("idle0").getbbox()))]
    img = sheet([(a, b, pad(c)) for a, b, c in items], 5, 3)
    img.save(os.path.join(OUT_DIR, "Mobs/mobs.png"))


def main():
    for sub in ("Mobs", "Backgrounds", "Transitions"):
        os.makedirs(os.path.join(OUT_DIR, sub), exist_ok=True)
    build_mobs()
    mobs_sheet()
    s = stage_pit()
    hud(s)
    save(s, "Backgrounds/stage_1-1.png")
    s2 = stage_hollows()
    hud(s2, 0.62, 0.3)
    save(s2, "Backgrounds/stage_1-2.png")
    save(boss_arena(), "Backgrounds/boss_arena.png")
    save(town(), "Backgrounds/town.png")
    save(world_map(), "Backgrounds/world_map.png")
    transition()
    print("wrote", OUT_DIR)


if __name__ == "__main__":
    main()
