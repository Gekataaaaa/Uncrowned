"""
In-game hero sprites: the exiled prince, broad-shouldered, barefoot in underwear, gripping a
rusty greatsword by the hilt. Flat colours, no volume shading (the sword is part of the sprite, like Darkrise - only clothes change).
Blond, far eye violet, near eye crimson. Facing right. 48x44 canvas, feet at the bottom row,
body centred on x=16 (Unity pivot is set to the feet: 16/48, 0).

Paper-doll layers per frame, back to front:
  body_<frame>   legs, torso, back arm, head
  (pants / boots / armor layers go here later)
  hair_<frame>   hair (helmets hide it later)
  front_<frame>  front arm + greatsword, always on top so armor never covers the blade

Run:  python Tools/ArtGen/hero.py
"""
import math
import os
from shade import Sculpt, ramp, hexc
from pixel import sheet

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Art", "Hero")
DOCS = os.path.join(ROOT, "Docs", "Concepts")

W, H = 48, 44


SKIN = ramp("#6e3f3c", "#b46d5a", "#e8a787", "#ffdcc2")
HAIR = ramp("#7d5a22", "#c49a38", "#efd062", "#fff6c0")
CLOTH = ramp("#1a1734", "#2c2858", "#433f8c", "#6a66c2")
STEEL = ramp("#33363f", "#666a78", "#a7abb8", "#eef0f6")
LEATHER = ramp("#24140c", "#432818", "#6a4428", "#8e6a42")
BRASS = ramp("#4a3010", "#87601c", "#c79a3c", "#f2d27c")
RUST = hexc("#8a4524")
LASH = hexc("#2a1622")
WHITE = hexc("#fbf6ff")
CRIMSON, CRIMSON_DK = hexc("#ff2b4e"), hexc("#8e0c25")
VIOLET, VIOLET_DK = hexc("#b45cff"), hexc("#4e1a8a")

# frame: (upper-body bob, back foot dx, back foot lift, front foot dx, front foot lift)
FRAMES = {
    "idle0": (0, 0, 0, 0, 0),
    "idle1": (1, 0, 0, 0, 0),
    "walk0": (0, -3, 0, 3, 0),
    "walk1": (1, -1, 2, 1, 0),
    "walk2": (0, 3, 0, -3, 0),
    "walk3": (1, 1, 0, -1, 2),
    "jump":  (0, -2, 3, 3, 4),
}


def leg(s, hip_x, hip_y, foot_dx, lift, shift):
    fx, fy = hip_x + foot_dx, 41.5 - lift
    kx, ky = (hip_x + fx) / 2 + 0.8, (hip_y + fy) / 2          # knee slightly forward
    s.capsule(hip_x, hip_y, kx, ky, 2.8, 2.3, SKIN, shift)
    s.capsule(kx, ky, fx, fy, 2.3, 1.9, SKIN, shift)
    s.ellipse(fx + 1, fy + 1.2, 2.6, 1.3, SKIN, shift)


def body(f):
    dy, bdx, blift, fdx, flift = f
    s = Sculpt(W, H, flat=True)
    leg(s, 14.5, 31.5, bdx, blift, -1)                          # back leg
    s.capsule(10, 19 + dy, 8.5, 25 + dy, 2.3, 2.0, SKIN, -1)  # back arm, hanging
    s.capsule(8.5, 25 + dy, 9.5, 30 + dy, 2.0, 1.8, SKIN, -1)
    s.ellipse(9.8, 30.6 + dy, 1.8, 1.7, SKIN, -1)
    # V-shaped torso: wide chest and shoulders, narrow waist
    s.capsule(16.5, 23 + dy, 16.5, 29, 5.6, 4.3, SKIN)
    s.ellipse(16.5, 20.5 + dy, 7.8, 4.6, SKIN)
    s.ellipse(10, 19 + dy, 2.8, 2.6, SKIN, -1)                # back shoulder
    s.poly([(11.5, 28.5), (21.5, 28.5), (21.5, 31.5), (18.5, 33.5), (14.5, 33.5), (11.5, 31.5)], CLOTH)
    leg(s, 18.5, 31.5, fdx, flift, 0)                           # front leg
    s.capsule(17, 14.5 + dy, 17, 17.5 + dy, 2.3, 2.6, SKIN, -1)   # neck
    s.ellipse(17.5, 9 + dy, 6.1, 6.4, SKIN, 1)                # head (one tone brighter)
    s.ellipse(12.6, 10 + dy, 1.2, 1.8, SKIN, -1)              # ear
    s.ellipse(23.6, 10.5 + dy, 0.9, 1.1, SKIN)                # nose
    # heterochromia: far eye violet (2 wide), near eye crimson (3 wide, with sclera)
    for x in (17, 18):
        s.dot(x, 8 + dy, LASH)
        s.dot(x, 9 + dy, VIOLET); s.dot(x, 10 + dy, VIOLET)
    s.dot(18, 10 + dy, VIOLET_DK); s.dot(17, 9 + dy, WHITE)
    for x in (20, 21, 22):
        s.dot(x, 8 + dy, LASH)
        s.dot(x, 9 + dy, CRIMSON); s.dot(x, 10 + dy, CRIMSON)
    s.dot(21, 10 + dy, CRIMSON_DK); s.dot(20, 9 + dy, WHITE); s.dot(23, 9 + dy, WHITE)
    s.dot(22, 13 + dy, hexc("#9c4a44"))                       # mouth
    return s.selout(strength=0.28)


def hair(f):
    dy = f[0]
    s = Sculpt(W, H, flat=True)
    s.ellipse(16.2, 5.6 + dy, 6.9, 4.8, HAIR)
    for a in ((14, 3, 9.5, 12, 2.4), (16, 4, 13, 14, 2.0), (12, 6, 10.5, 15.5, 1.8),
              (17, 2, 22.5, 7, 2.0), (19, 2.5, 24.5, 6.5, 1.6), (15.5, 1.5, 17.5, -0.5, 1.5)):
        s.capsule(a[0], a[1] + dy, a[2], a[3] + dy, a[4], 0.7, HAIR)
    s.selout(strength=0.28)
    # keep the face clear of the dark outline so the eyes stay readable
    for y in range(7 + dy, H):
        for x in range(17, W):
            p = s.px[y][x]
            if p is not None and p[0] < 80 and p[1] < 60:
                s.px[y][x] = None
    return s


def front(f):
    """Front arm gripping the greatsword by the hilt, blade forward and slightly down."""
    dy = f[0]
    s = Sculpt(W, H, flat=True)
    hx, hy = 29.5, 28.5 + dy                 # hand
    tx, ty = 47.0, 33.5 + dy                 # tip
    ux, uy = tx - hx, ty - hy
    L = math.hypot(ux, uy); ux, uy = ux / L, uy / L
    s.ellipse(hx - ux * 4.6, hy - uy * 4.6, 1.3, 1.3, BRASS)                         # pommel
    s.capsule(hx - ux * 3.8, hy - uy * 3.8, hx + ux * 1.2, hy + uy * 1.2, 1.0, 1.0, LEATHER)  # grip
    gx, gy = hx + ux * 2.2, hy + uy * 2.2
    s.capsule(gx - uy * 3, gy + ux * 3, gx + uy * 3, gy - ux * 3, 0.9, 0.9, BRASS)   # crossguard
    s.capsule(gx + ux, gy + uy, tx, ty, 1.7, 0.6, STEEL)                             # blade
    for t in (0.3, 0.55, 0.8):
        s.dot(gx + (tx - gx) * t, gy + (ty - gy) * t + 1, RUST)
    # arm over the grip: shoulder -> elbow -> fist
    s.ellipse(23, 19 + dy, 2.9, 2.7, SKIN)
    s.capsule(23, 19 + dy, 25.5, 24.5 + dy, 2.4, 2.1, SKIN)
    s.capsule(25.5, 24.5 + dy, hx, hy, 2.1, 1.9, SKIN)
    s.ellipse(hx + 0.2, hy + 0.2, 2.1, 1.9, SKIN)
    s.dot(hx + 1, hy - 1, SKIN[1])          # knuckle line
    return s.selout(strength=0.28)


def main():
    os.makedirs(OUT, exist_ok=True)
    # clear frames from older layouts
    for name in os.listdir(OUT):
        if name.endswith(".png") or name.endswith(".png.meta"):
            os.remove(os.path.join(OUT, name))

    preview = []
    for name, f in FRAMES.items():
        layers = {"body": body(f), "hair": hair(f), "front": front(f)}
        combo = None
        for prefix, sc in layers.items():
            img = sc.image()
            img.save(os.path.join(OUT, f"{prefix}_{name}.png"))
            combo = img if combo is None else (combo.alpha_composite(img) or combo)
        preview.append((name, "body + hair + front", combo))

    os.makedirs(DOCS, exist_ok=True)
    sheet(preview, 6, 4).save(os.path.join(DOCS, "hero_preview.png"))
    print("hero sprites written to", OUT)


if __name__ == "__main__":
    main()
