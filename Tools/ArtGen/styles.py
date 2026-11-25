"""
Three style directions for the exiled prince (facing right, rusty greatsword in hand).
  A  Chibi Hero        - big head, big readable eyes (closest to Darkrise)
  B  Dark Fantasy      - tall heroic proportions, Abyss rim light, glowing eyes
  C  Anime ARPG        - mid proportions, spiky hair, eye-flare trail as a signature
Writes Docs/Concepts/hero_styles.png (each shown at the same zoom, so in-game size compares too).

Run:  python Tools/ArtGen/styles.py
"""
import os
from PIL import Image, ImageDraw, ImageFont
from shade import Sculpt, ramp, hexc

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DOCS = os.path.join(ROOT, "Docs", "Concepts")

SKIN = ramp("#6e3f3c", "#b46d5a", "#e8a787", "#ffdcc2")
HAIR = ramp("#7d5a22", "#c49a38", "#efd062", "#fff6c0")
CLOTH = ramp("#1a1734", "#2c2858", "#433f8c", "#6a66c2")
STEEL = ramp("#33363f", "#666a78", "#a7abb8", "#eef0f6")
LEATHER = ramp("#24140c", "#432818", "#6a4428", "#8e6a42")
GOLD = ramp("#5a3a10", "#9a6a1e", "#d6a640", "#ffe08a")
RUST = hexc("#8a4524")
LASH = hexc("#2a1622")
WHITE = hexc("#fbf6ff")
CRIMSON = hexc("#ff2b4e")
CRIMSON_DK = hexc("#8e0c25")
VIOLET = hexc("#b45cff")
VIOLET_DK = hexc("#4e1a8a")
ABYSS_RIM = hexc("#9b6cff")


def eye(s, x, y, w, h, iris, iris_dk, sclera=True):
    """Eye block: lash on top, sclera toward the nose (right), iris + pupil, white catch-light."""
    for i in range(w):
        s.dot(x + i, y, LASH)
    for j in range(1, h):
        for i in range(w):
            s.dot(x + i, y + j, iris)
        s.dot(x + w // 2, y + h - 1, iris_dk)        # pupil low
    if sclera:
        s.dot(x + w, y + 1, WHITE)
        if h > 2:
            s.dot(x + w, y + 2, WHITE)
    s.dot(x, y + 1, WHITE)                             # catch-light


def sword(s, hx, hy, tx, ty, width, rusty=True):
    """Greatsword from the hand (hx,hy) to the tip (tx,ty)."""
    import math
    dx, dy = tx - hx, ty - hy
    L = math.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    # grip behind the hand, guard just in front
    s.capsule(hx - ux * 3.5, hy - uy * 3.5, hx + ux * 1.5, hy + uy * 1.5, width * 0.55, width * 0.55, LEATHER)
    s.ellipse(hx - ux * 4.2, hy - uy * 4.2, width * 0.75, width * 0.75, GOLD)            # pommel
    gx, gy = hx + ux * 2.2, hy + uy * 2.2
    s.capsule(gx - uy * width * 2.2, gy + ux * width * 2.2, gx + uy * width * 2.2, gy - ux * width * 2.2, width * 0.6, width * 0.6, GOLD)
    s.capsule(gx + ux, gy + uy, tx, ty, width, width * 0.45, STEEL)
    s.line(gx + ux * 2, gy + uy * 2, tx - ux * 2, ty - uy * 2, STEEL[1])               # fuller
    if rusty:
        for t in (0.25, 0.45, 0.7):
            s.dot(gx + dx * t + uy, gy + dy * t - ux, RUST)


# ---------------------------------------------------------------- A: Chibi

def style_a():
    s = Sculpt(48, 42)
    s.capsule(14, 31, 13, 39, 2.3, 2.0, SKIN, -1)          # back leg
    s.ellipse(13.5, 39.6, 2.6, 1.4, SKIN, -1)
    s.capsule(12, 23, 10.5, 29, 1.9, 1.7, SKIN, -1)        # back arm
    s.ellipse(16, 26, 5.2, 5.6, SKIN)                      # torso
    s.poly([(11, 28), (21, 28), (20.5, 32), (17, 33.5), (15, 33.5), (11.5, 32)], CLOTH)
    s.capsule(18.5, 31, 19.5, 39, 2.3, 2.0, SKIN)          # front leg
    s.ellipse(20.5, 39.6, 2.6, 1.4, SKIN)
    s.ellipse(16, 13, 8.6, 8.2, SKIN)                      # head
    s.ellipse(10.8, 14, 1.4, 2.1, SKIN, -1)                # ear
    # hair: back mass, crown, bangs, a long strand
    s.ellipse(13, 10.5, 8.2, 7.2, HAIR, -1)
    s.ellipse(16.5, 6.5, 8.4, 4.8, HAIR)
    for a in ((13, 4, 19, 11.5, 2.2), (17, 3.5, 23, 11, 2.0), (21, 4.5, 25, 10, 1.6), (10, 5, 11, 15, 2.1), (8.5, 8, 5.5, 15, 1.7)):
        s.capsule(a[0], a[1], a[2], a[3], a[4], 0.9, HAIR)
    s.capsule(12, 2, 15, 0.5, 1.4, 0.6, HAIR)              # ahoge
    eye(s, 17, 13, 2, 4, VIOLET, VIOLET_DK, sclera=False)   # far eye: violet
    eye(s, 21, 13, 3, 4, CRIMSON, CRIMSON_DK)              # near eye: crimson
    s.dot(23, 19, hexc("#a24c46"))                          # mouth
    s.dot(22, 17, (255, 140, 140, 90)); s.dot(19, 17, (255, 140, 140, 90))   # blush
    s.ellipse(25, 16, 1.1, 1.3, SKIN)                       # nose bump
    sword(s, 23, 28, 45, 6, 1.5)
    s.capsule(19.5, 23, 23, 28, 1.9, 1.7, SKIN)            # front arm over the grip
    s.ellipse(23.2, 28.2, 1.9, 1.7, SKIN)
    return s.selout(black=True).image()


# ---------------------------------------------------------- B: Dark Fantasy

def style_b():
    s = Sculpt(64, 78, rim=ABYSS_RIM)
    s.capsule(17, 42, 15.5, 58, 3.3, 2.6, SKIN, -1)        # back leg
    s.capsule(15.5, 58, 15.5, 73, 2.5, 2.0, SKIN, -1)
    s.ellipse(16.5, 74.5, 3.2, 1.6, SKIN, -1)
    s.capsule(14.5, 22, 12, 33, 2.4, 2.1, SKIN, -1)        # back arm
    s.capsule(12, 33, 13, 43, 2.0, 1.8, SKIN, -1)
    s.capsule(20, 23, 20.5, 36, 6.4, 5.0, SKIN)            # torso
    s.ellipse(22.5, 27, 3.2, 2.4, SKIN, 1)                 # pec highlight
    for y in (31, 34):
        s.line(19, y, 23, y, SKIN[1])                      # abs
    s.line(17, 28, 21, 32, hexc("#9c4a44"))                # old scar
    s.poly([(14, 38), (27, 38), (27, 43), (22, 46), (19, 46), (14, 43)], CLOTH)
    s.capsule(23.5, 42, 25.5, 58, 3.3, 2.6, SKIN)          # front leg
    s.capsule(25.5, 58, 24.5, 73, 2.5, 2.0, SKIN)
    s.ellipse(26, 74.5, 3.2, 1.6, SKIN)
    s.capsule(20, 15, 20.5, 19, 2.2, 2.4, SKIN, -1)        # neck
    s.ellipse(20.5, 10.5, 5.4, 6.4, SKIN)                  # head
    s.ellipse(16.4, 11, 1.1, 1.8, SKIN, -1)
    # long swept-back hair falling over the shoulders
    s.ellipse(18.5, 7, 6.6, 5.2, HAIR)
    for a in ((15, 6, 11, 21, 2.7), (17, 5, 14.5, 22, 2.3), (13, 9, 9.5, 24, 1.9), (21, 3, 26, 8.5, 1.6), (19, 3, 22.5, 9.5, 1.4)):
        s.capsule(a[0], a[1], a[2], a[3], a[4], 0.8, HAIR)
    # glowing eyes
    for (x, c, dk) in ((21, VIOLET, VIOLET_DK), (24, CRIMSON, CRIMSON_DK)):
        s.dot(x, 10, LASH); s.dot(x + 1, 10, LASH)
        s.dot(x, 11, c); s.dot(x + 1, 11, dk if x == 21 else c)
        s.glow(x + 0.5, 11, c, 2)
    s.dot(25, 14, hexc("#8c4440"))                          # mouth
    sword(s, 33, 38, 62, 70, 2.0)                           # held low and forward
    s.capsule(26.5, 22, 29.5, 31, 2.5, 2.1, SKIN)          # front arm over the grip
    s.capsule(29.5, 31, 33, 37.5, 2.0, 1.9, SKIN)
    s.ellipse(33.2, 38.2, 2.2, 2.0, SKIN)
    return s.selout(strength=0.22).image()


# ------------------------------------------------------------ C: Anime ARPG

def style_c():
    s = Sculpt(64, 58)
    s.capsule(18.5, 38, 17, 54, 2.7, 2.2, SKIN, -1)        # back leg
    s.ellipse(17.5, 55.3, 2.8, 1.4, SKIN, -1)
    s.capsule(15, 24, 12.5, 34, 2.1, 1.8, SKIN, -1)        # back arm
    s.capsule(20.5, 25, 20.5, 35, 5.3, 4.3, SKIN)          # torso
    s.poly([(15.5, 35), (25.5, 35), (25, 40), (21.5, 42), (19.5, 42), (16, 40)], CLOTH)
    s.capsule(23, 38, 25, 54, 2.7, 2.2, SKIN)              # front leg
    s.ellipse(26, 55.3, 2.8, 1.4, SKIN)
    s.capsule(20.5, 18, 20.5, 21, 2.0, 2.2, SKIN, -1)      # neck
    s.ellipse(20.5, 12.5, 7.0, 7.4, SKIN)                  # head
    s.ellipse(15.2, 13.5, 1.2, 1.9, SKIN, -1)
    # spiky hair
    s.ellipse(18.5, 8.5, 7.6, 6.2, HAIR)
    for a in ((16, 4, 7, 2, 2.4), (15, 7, 7, 12, 2.2), (16, 10, 10, 18, 1.9), (19, 3, 17, -1, 2.0),
              (22, 3, 27, 1, 1.7), (21, 5, 26, 11, 1.8), (24, 5, 28.5, 9.5, 1.4)):
        s.capsule(a[0], a[1], a[2], a[3], a[4], 0.6, HAIR)
    eye(s, 21, 11, 2, 3, VIOLET, VIOLET_DK, sclera=False)
    eye(s, 24, 11, 3, 3, CRIMSON, CRIMSON_DK)
    # eye-flare trail streaming back from the crimson eye (combat signature)
    for i, a in enumerate((150, 110, 75, 45)):
        s.put(23 - i * 2, 12 + (i // 2), (CRIMSON[0], CRIMSON[1], CRIMSON[2], a))
        s.put(22 - i * 2, 12 + (i // 2), (CRIMSON[0], CRIMSON[1], CRIMSON[2], a // 2))
    s.dot(26, 16, hexc("#a24c46"))
    s.ellipse(27.6, 14, 0.9, 1.1, SKIN)
    sword(s, 31, 32, 60, 4, 1.7)                            # raised forward
    s.capsule(25.5, 25, 28.5, 30, 2.2, 1.9, SKIN)          # front arm over the grip
    s.capsule(28.5, 30, 31, 32, 1.9, 1.8, SKIN)
    s.ellipse(31.2, 32.2, 2.0, 1.8, SKIN)
    return s.selout(strength=0.3).image()


STYLES = [
    ("A - Chibi Hero", "Big head + big eyes, reads instantly. Closest to Darkrise.", style_a),
    ("B - Dark Fantasy", "Tall, heroic, Abyss rim light, glowing eyes. Grim tone.", style_b),
    ("C - Anime ARPG", "Mid proportions, spiky hair, crimson eye-flare in combat.", style_c),
]


def main():
    os.makedirs(DOCS, exist_ok=True)
    scale, pad, label = 6, 30, 70
    imgs = [(n, d, f()) for n, d, f in STYLES]
    cell_w = max(i[2].width for i in imgs) * scale + pad * 2
    cell_h = max(i[2].height for i in imgs) * scale + pad * 2 + label
    out = Image.new("RGBA", (cell_w * len(imgs), cell_h), hexc("#0d0a14"))
    d = ImageDraw.Draw(out)
    font = ImageFont.load_default(size=22)
    small = ImageFont.load_default(size=16)
    ground = cell_h - label - pad
    for k, (name, desc, img) in enumerate(imgs):
        big = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
        x0 = k * cell_w
        d.rectangle([x0 + 10, ground, x0 + cell_w - 10, ground + 4], fill=hexc("#3a2a55"))
        out.alpha_composite(big, (x0 + pad, ground - big.height))
        d.text((x0 + pad, ground + 14), name, fill=(245, 225, 160, 255), font=font)
        d.text((x0 + pad, ground + 42), desc, fill=(170, 160, 195, 255), font=small)
        img.save(os.path.join(DOCS, "style_" + name[0].lower() + ".png"))
    out.save(os.path.join(DOCS, "hero_styles.png"))
    print("styles written to", DOCS)


if __name__ == "__main__":
    main()
