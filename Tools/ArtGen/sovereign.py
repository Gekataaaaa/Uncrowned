"""
Eyeless Sovereign redesign: three bearded, broad-shouldered takes (no narrow waist), in the knight's style.
Writes to Art/Concepts/EyelessSovereign/: sovereign_variants.png (old design, A, B, C and the knight for
scale), each variant as sovereign_<letter>.png at native size, and sovereign_A_beards.png (A with each beard).
Variant A (long beard) is the chosen design; not in the game yet.

Run:  python Tools/ArtGen/sovereign.py
"""
import os
from PIL import ImageDraw
from world import (ramp, sprite, pad, trim, hero, hexc, OUT, GOLD, BONE, CRIM, IRON, GLOW_HI,
                   eyeless_sovereign, PREVIEW_DIR)
from pixel import sheet

CORPSE = ramp("#cfc6d0", "#a197a8", "#6c6276")      # grey dead skin
BEARD_GREY = ramp("#f2eee8", "#bdb5ac", "#837a72")
BEARD_BLACK = ramp("#5e4e60", "#3a2c3e", "#211622")
FUR = ramp("#e6ded6", "#b0a49a", "#746860")
ROBE_DARK = ramp("#6a3a5a", "#46263e", "#2a1426")
ROBE_VIOLET = ramp("#64507e", "#41325c", "#271d3c")
SOCKET = hexc("#140e1c")
GLOW_GEM = ramp("#e6c4ff", "#b46bff", "#6e2fbf")
BLOOD = hexc("#c8213c")


def face_details(c, lx, rx, y, tears=(7, 5, 6, 8)):
    """Two empty sockets (3x4 px) at x = lx, rx, top y, with blood tears running down."""
    d = ImageDraw.Draw(c)
    for x in (lx, rx):
        d.ellipse([x, y, x + 4, y + 4], fill=SOCKET)
    d.line([(lx - 1, y - 3), (lx + 4, y - 1)], fill=OUT)          # heavy frowning brows
    d.line([(rx, y - 1), (rx + 5, y - 3)], fill=OUT)
    nose = (lx + rx + 4) // 2
    d.line([(nose, y + 3), (nose, y + 6)], fill=hexc("#6c6276"))
    for x, ln in zip((lx + 1, lx + 3, rx + 1, rx + 3), tears):
        d.line([(x, y + 4), (x, y + 4 + ln)], fill=BLOOD)


def crown(t, x0, x1, y, tall=(0,), gem=True):
    t.rect(x0, y, x1, y + 4, GOLD)
    for i, x in enumerate(range(x0 + 1, x1 - 1, 5)):
        top = y - (7 if i in tall else 4)
        t.poly([(x, y + 1), (x + 3, y + 1), (x + 1, top)], GOLD)


def eyeball(t, x, y, r=3):
    t.ell(x - r, y - r, x + r, y + r, BONE)


def iris(d, x, y):
    d.point([(x, y), (x - 1, y)], fill=hexc("#ff2b4e"))


# ---------------------------------------------------------------- A: lich king

# The beard starts below the eyes (y = 24) and its top edge dips down in the middle, round the mouth,
# instead of running straight across the face. Each style lists its outline from the
# left cheek down and round to the right one; TOP closes it back along the arch.
TOP = [(50, 24), (44, 24), (41, 26), (38, 28), (35, 26), (32, 24), (26, 24)]
BEARDS = {
    # long: one wedge down to the chest
    "long": [[(26, 24), (28, 38), (34, 54), (38, 58), (42, 54), (48, 38)] + TOP],
    # forked: splits into two points, each bound with a gold ring
    "forked": [[(26, 24), (27, 36), (30, 50), (33, 62), (36, 48), (38, 44), (40, 48), (43, 62), (46, 50), (49, 36)] + TOP],
    # braided: full beard over the collar, then one thick braid down the robe
    "braided": [[(26, 24), (28, 34), (33, 40), (43, 40), (48, 34)] + TOP, [(35, 38), (41, 38), (41, 70), (38, 74), (35, 70)]],
    # full: short and broad, heavy drooping moustache
    "full": [[(25, 24), (25, 32), (30, 39), (38, 41), (46, 39), (51, 32), (51, 24)] + TOP],
}


def variant_a(beard_style="long"):
    """Lich king: fur-collared mantle as wide as he is tall, grey beard, sceptre with a violet gem."""
    W, H = 78, 88

    def cape(t):
        t.poly([(12, 32), (64, 32), (72, H - 1), (4, H - 1)], CRIM)

    def robe(t):
        t.poly([(22, 44), (54, 44), (58, H - 1), (18, H - 1)], ROBE_DARK)

    def sceptre(t):
        t.rect(66, 30, 67, H - 2, GOLD)

    def orb(t):   # gold finial holding an abyss-violet gem
        t.poly([(62, 27), (70, 27), (66, 31)], GOLD)
        t.ell(63, 20, 69, 27, GLOW_GEM)

    def collar(t):
        t.ell(10, 27, 66, 45, FUR)

    def head(t):
        t.ell(26, 9, 50, 33, CORPSE)

    def beard(t):
        for shape in BEARDS[beard_style]:
            t.poly(shape, BEARD_GREY)

    def hand(t):
        t.ell(63, 45, 69, 50, CORPSE)

    def crown_(t):   # sits on the head: its band covers the brow and the sideburns start under it
        crown(t, 26, 50, 9, tall=(1, 2, 3))

    c = sprite(W, H, [cape, robe, sceptre, orb, collar, head, beard, hand, crown_])
    face_details(c, 30, 41, 15)
    d = ImageDraw.Draw(c)
    d.point((65, 22), fill=GLOW_HI)
    shade = hexc("#837a72")
    d.line([(37, 25), (39, 25)], fill=SOCKET)                             # mouth in the dip
    d.point([(37, 11), (38, 11)], fill=hexc("#b46bff"))                  # gem in the band
    if beard_style != "braided":
        d.line([(38, 48), (38, H - 3)], fill=GOLD)                        # robe trim
    if beard_style == "long":
        d.line([(38, 40), (38, 50)], fill=shade)
    elif beard_style == "forked":
        d.line([(38, 36), (38, 44)], fill=shade)
        for x in (33, 42):
            d.line([(x, 53), (x + 3, 53)], fill=GOLD)
            d.line([(x, 54), (x + 3, 54)], fill=hexc("#b38f2e"))
    elif beard_style == "braided":
        for y in range(42, 68, 4):                                        # braid segments
            d.line([(36, y), (40, y + 2)], fill=shade)
        for y in (46, 58):
            d.line([(35, y), (41, y)], fill=GOLD)
    elif beard_style == "full":
        d.line([(33, 27), (31, 34)], fill=shade)                          # drooping moustache strands
        d.line([(43, 27), (45, 34)], fill=shade)
    return c


# ---------------------------------------------------------------- B: armoured tyrant

def variant_b():
    """Tyrant: pauldrons and breastplate under a crimson cloak, braided black beard, greatsword planted."""
    W, H = 78, 88

    def cloak(t):
        t.poly([(10, 30), (66, 30), (73, H - 1), (3, H - 1)], CRIM)

    def robe(t):
        t.poly([(20, 46), (56, 46), (60, H - 1), (16, H - 1)], ROBE_DARK)

    def blade(t):
        t.rect(37, 56, 39, H - 2, IRON)

    def chest(t):
        t.rect(21, 29, 55, 50, IRON)

    def pauldrons(t):
        t.ell(6, 24, 28, 42, IRON)
        t.ell(48, 24, 70, 42, IRON)

    def head(t):
        t.ell(27, 7, 49, 29, CORPSE)

    def beard(t):
        t.poly([(27, 20), (49, 20), (46, 30), (41, 38), (38, 44), (35, 38), (30, 30)], BEARD_BLACK)

    def guard(t):
        t.rect(29, 52, 47, 54, GOLD)

    def hands(t):
        t.ell(31, 47, 37, 53, CORPSE)
        t.ell(39, 47, 45, 53, CORPSE)

    def crown_(t):
        crown(t, 25, 51, 3, tall=(0, 2, 4))

    c = sprite(W, H, [cloak, robe, blade, chest, pauldrons, head, beard, guard, hands, crown_])
    face_details(c, 30, 42, 12, tears=(6, 4, 5, 7))
    d = ImageDraw.Draw(c)
    d.line([(32, 22), (37, 21), (39, 21), (44, 22)], fill=hexc("#5e4e60"))
    for y in (33, 39):                                       # gold rings on the braid
        d.line([(37, y), (39, y)], fill=GOLD)
    d.rectangle([36, 35, 40, 39], outline=GOLD)              # stolen eye set in the chest
    iris(d, 38, 37)
    d.point((37, 36), fill=BONE)
    return c


# ---------------------------------------------------------------- C: old archon

def variant_c():
    """Old archon: hooded and boxy, beard to the hem, eye-embroidered robe, a chain of eyes in hand."""
    W, H = 80, 88

    def robe(t):
        t.poly([(12, 28), (66, 28), (70, H - 1), (8, H - 1)], ROBE_VIOLET)

    def sleeves(t):
        t.poly([(8, 34), (22, 34), (24, 64), (4, 66)], ROBE_VIOLET)
        t.poly([(56, 34), (70, 34), (74, 66), (54, 64)], ROBE_VIOLET)

    def hood(t):
        t.ell(18, 2, 60, 38, ROBE_VIOLET)

    def face(t):
        t.ell(27, 11, 51, 33, CORPSE)

    def beard(t):
        t.poly([(27, 25), (51, 25), (49, 46), (43, 72), (39, 80), (35, 72), (29, 46)], BEARD_GREY)

    def hands(t):
        t.ell(4, 62, 12, 69, CORPSE)
        t.ell(66, 62, 74, 69, CORPSE)

    def eye_chain(t):
        for x, y in ((8, 72), (11, 78), (16, 82), (22, 84)):
            eyeball(t, x, y, 2)

    def circlet(t):
        crown(t, 28, 50, 9, tall=(2,))

    c = sprite(W, H, [robe, sleeves, hood, face, beard, hands, eye_chain, circlet])
    face_details(c, 31, 42, 16, tears=(5, 7, 6, 5))
    d = ImageDraw.Draw(c)
    for x, y in ((8, 72), (11, 78), (16, 82), (22, 84)):
        d.point((x, y), fill=hexc("#ff2b4e"))
    for cx in (20, 58):                                      # embroidered eyes on the robe
        d.ellipse([cx - 4, 74, cx + 4, 78], outline=GOLD)
        d.point((cx, 76), fill=hexc("#ff2b4e"))
    d.line([(34, 28), (38, 27), (40, 27), (44, 28)], fill=hexc("#837a72"))
    d.point((39, 6), fill=hexc("#b46bff"))
    return c


def beard_sheet(out):
    items = [(f"A: {k}", "beard", variant_a(k)) for k in BEARDS]
    sheet([(a, b, pad(c)) for a, b, c in items], 5, len(items)).save(os.path.join(out, "sovereign_A_beards.png"))


def main():
    out = os.path.join(PREVIEW_DIR, "Concepts", "EyelessSovereign")
    os.makedirs(out, exist_ok=True)
    variants = [("A", "Lich king", variant_a()), ("B", "Armoured tyrant", variant_b()), ("C", "Old archon", variant_c())]
    for letter, _, img in variants:
        pad(img).save(os.path.join(out, f"sovereign_{letter}.png"))
    items = [("Now", "current design", eyeless_sovereign())]
    items += [(f"{k}: {name}", "Eyeless Sovereign", img) for k, name, img in variants]
    items.append(("Knight", "scale", trim(hero("idle0"))))
    sheet([(a, b, pad(c)) for a, b, c in items], 5, 5).save(os.path.join(out, "sovereign_variants.png"))
    beard_sheet(out)
    print("wrote", os.path.join(out, "sovereign_variants.png"), "and sovereign_A_beards.png")


if __name__ == "__main__":
    main()
