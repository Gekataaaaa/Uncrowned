"""Attack frames for the Uncrowned mobs (all face left, so 'forward' is -x).
Every mob gets 5 frames: idle, windup, strike (smear), impact, recover."""
import math
import numpy as np
from attack_pix import *


def darkest(a):
    op = a[a[..., 3] > 0][:, :3].astype(int)
    return tuple(op[op.sum(1).argmin()])


class Sheet:
    def __init__(self, name, L, R, T, B=0):
        self.src = load(name)
        self.h, self.w = self.src.shape[:2]
        self.L, self.T = L, T
        self.W, self.H = self.w + L + R, self.h + T + B
        self.out = darkest(self.src)
        self.frames = []

    def new(self):
        return blank(self.W, self.H)

    def at(self, img, dx=0, dy=0):
        f = self.new(); paste(f, img, self.L + dx, self.T + dy); return f

    @property
    def feet(self):
        return self.T + self.h  # y of the ground line (one past the last row)


VIO = (178, 102, 255); VIO_L = (232, 214, 255); VIO_D = (120, 50, 200)
DUST = (150, 146, 160); DUST_D = (104, 100, 118)
STEEL = (200, 202, 214)


# ---------------------------------------------------------------- Weeping Sentinel: staff thrust + tear bolt
def sentinel():
    s = Sheet("weeping_sentinel", L=18, R=2, T=3)
    staff, body = split(s.src, lambda x, y: x <= 5 or (x <= 7 and y <= 22))
    hand = (4, 21)
    base = s.feet - 1

    def pose(k, rot, sx, sy, glow=0):
        f = s.new()
        paste(f, shear(s.at(body), k, base), 0, 0)
        robe = tuple(int(v) for v in s.src[30, 12][:3])
        hx, hy = s.L + hand[0] + sx, s.T + hand[1] + sy
        if sx < 0:   # arm stretched out: draw the sleeve from the robe to the hand
            for o in (0, 1):
                line(f, s.L + 8, s.T + 18 + o, hx + 2, hy + o - 1, robe)
            line(f, s.L + 8, s.T + 20, hx + 2, hy + 1, s.out)
            line(f, s.L + 8, s.T + 17, hx + 2, hy - 2, s.out)
        place_rot(f, staff, rot, hand, (hx, hy))
        return f

    s.frames.append(s.at(s.src))
    f = pose(0.07, -12, 3, -3)                      # staff drawn back, gathering light
    hx, hy = s.L + 9, s.T + 1
    for dx, dy in ((0, -2), (-2, 0), (2, 1), (-1, 2), (1, -1)):
        put(f, hx + dx, hy + dy, VIO)
    put(f, hx, hy, VIO_L)
    s.frames.append(f)
    f = pose(-0.09, 22, -6, 1)                      # thrust
    tx, ty = s.L - 6, s.T + 6
    for i in range(4):
        line(f, tx + 5 + i * 3, ty + 3 + i % 2 * 2 - 2, tx + 7 + i * 3, ty + 3 + i % 2 * 2 - 2, (*VIO_L, 160))
    spark(f, tx, ty, 2, VIO)
    s.frames.append(f)
    f = pose(-0.09, 22, -6, 1)                      # tear bolt leaves the staff
    spark(f, tx + 1, ty, 1, VIO)
    tear = blank(5, 7)
    for (x, y) in ((2, 0), (1, 1), (2, 1), (3, 1), (0, 2), (1, 2), (2, 2), (3, 2), (4, 2),
                   (0, 3), (1, 3), (2, 3), (3, 3), (4, 3), (1, 4), (2, 4), (3, 4)):
        tear[y, x] = (*VIO, 255)
    tear[2, 1] = tear[3, 1] = (*VIO_L, 255)
    tear = outline(np.pad(tear, ((1, 1), (1, 1), (0, 0))), s.out)
    tear = np.array(Image.fromarray(tear).rotate(90, expand=True))   # teardrop flying left
    paste(f, tear, tx - 10, ty - 3)
    for i in range(3):
        put(f, tx - 2 + i * 2, ty + (i % 2), (*VIO, 140))
    s.frames.append(f)
    s.frames.append(pose(-0.03, 6, -2, 0))
    return s


# ---------------------------------------------------------------- Chainbound Ghoul: chain lash
def chain_px(f, pts, light=(196, 192, 210), dark=(110, 106, 124), out=None):
    """Draw a chain along a polyline: alternating light/dark links with an outline."""
    lay = np.zeros_like(f)
    n = 0
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        steps = int(max(abs(x1 - x0), abs(y1 - y0))) or 1
        for i in range(steps):
            t = i / steps
            put(lay, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, light if (n // 2) % 2 == 0 else dark)
            n += 1
    lay = outline(lay, out)
    paste(f, lay, 0, 0)


def ghoul():
    s = Sheet("chainbound_ghoul", L=18, R=2, T=8)
    chain, body = split(s.src, lambda x, y: x <= 6 and y >= 21)
    body = reoutline(body, s.out)
    base = s.feet - 1
    hx, hy = s.L + 5, s.T + 20                      # the hand holding the chain

    s.frames.append(s.at(s.src))
    f = shear(s.at(body), 0.08, base)               # winds the chain up over its shoulder
    chain_px(f, [(hx + 2, hy - 1), (hx + 1, hy - 8), (hx + 5, hy - 15), (hx + 11, hy - 17), (hx + 14, hy - 13)], out=s.out)
    s.frames.append(f)
    f = shear(s.at(body), -0.10, base)              # lash: chain snaps forward, smear above
    arc(f, hx - 1, hy - 2, 12, 15, 95, 175, (*VIO_L, 120), (255, 255, 255, 70))
    chain_px(f, [(hx - 2, hy - 3), (hx - 10, hy - 4), (hx - 18, hy - 3)], out=s.out)
    for dx, dy in ((0, 0), (-1, -1), (-1, 1), (-2, -2), (-2, 2)):   # hook
        put(f, hx - 19 + dx, hy - 3 + dy, (220, 220, 232))
    s.frames.append(f)
    f = shear(s.at(body), -0.08, base)              # impact: chain cracks at full reach
    chain_px(f, [(hx - 2, hy - 2), (hx - 9, hy - 1), (hx - 15, hy + 1), (hx - 17, hy + 3)], out=s.out)
    spark(f, hx - 18, hy + 3, 2, (255, 230, 150))
    s.frames.append(f)
    f = shear(s.at(body), -0.03, base)
    chain_px(f, [(hx, hy), (hx - 3, hy + 4), (hx - 4, hy + 8), (hx - 3, hy + 10)], out=s.out)
    s.frames.append(f)
    return s


# ---------------------------------------------------------------- Despair Wisp: dive
def wisp():
    s = Sheet("despair_wisp", L=12, R=4, T=8, B=6)
    a = s.src
    cx, cy = a.shape[1] // 2, a.shape[0] // 2
    s.frames.append(s.at(a))
    up = squash(a, 0.88, 1.12, cx, a.shape[0])
    s.frames.append(s.at(up, 1, -4))
    f = s.new()
    dive = rotate(a, 28, cx, cy)
    for i, al in ((2, 0.18), (1, 0.35)):
        paste(f, dive[0], s.L + cx + 3 * i - 8 + dive[1], s.T + cy - 2 * i + 4 + dive[2], al, tint=VIO)
    paste(f, dive[0], s.L + cx - 8 + dive[1], s.T + cy + 4 + dive[2])
    s.frames.append(f)
    f = s.at(squash(a, 1.2, 0.84, cx, a.shape[0]), -10, 5)
    for ang in range(0, 360, 45):
        r = 6
        spark(f, s.L - 9 + r * math.cos(math.radians(ang)) * 1.3, s.T + 18 + r * math.sin(math.radians(ang)), 0, VIO, VIO_L)
    spark(f, s.L - 9, s.T + 18, 1, VIO, VIO_L)
    s.frames.append(f)
    s.frames.append(s.at(a, -4, 2))
    return s


# ---------------------------------------------------------------- Gloomling: pounce + bite
def gloomling():
    s = Sheet("gloomling", L=14, R=2, T=10)
    a = s.src
    h, w = a.shape[:2]

    def open_mouth(img):
        top, bot = split(img, lambda x, y: y < 14)
        o = blank(w, h + 2)
        paste(o, bot, 0, 2); paste(o, top, 0, 0)
        o[14:16, :] = img[13:14, :].repeat(2, 0)      # stretch the cheek row into the gap
        for x in range(6, 13):
            o[14, x] = (70, 12, 30, 255); o[15, x] = (70, 12, 30, 255)
        for x in (6, 8, 10, 12):
            o[14, x] = (236, 226, 196, 255)
        for x in (7, 9, 11):
            o[15, x] = (236, 226, 196, 255)
        return o[2:]                                  # keep the same height: crop the top

    s.frames.append(s.at(a))
    s.frames.append(s.at(squash(a, 1.14, 0.8, w / 2, h)))
    f = s.new()
    r = rotate(squash(a, 0.95, 1.1, w / 2, h), 18, w // 2, h // 2)
    paste(f, r[0], s.L - 6 + w // 2 + r[1], s.T - 7 + h // 2 + r[2])
    for i, (dx, rr) in enumerate(((w // 2 + 2, 2), (w // 2 + 6, 1), (w // 2 - 2, 1))):
        puff(f, s.L + dx, s.feet - 2, rr, DUST, DUST_D)
    s.frames.append(f)
    f = s.new()
    r = rotate(open_mouth(a), 8, w // 2, h // 2)
    paste(f, r[0], s.L - 11 + w // 2 + r[1], s.T - 3 + h // 2 + r[2])
    arc(f, s.L - 4, s.T + 12, 8, 10, 120, 230, (255, 255, 255, 150), (*VIO_L, 70))
    s.frames.append(f)
    f = s.at(squash(a, 1.16, 0.84, w / 2, h), -12)
    puff(f, s.L - 13, s.feet - 2, 2, DUST, DUST_D); puff(f, s.L + 9, s.feet - 2, 1, DUST, DUST_D)
    s.frames.append(f)
    return s


# ---------------------------------------------------------------- Hollow Crawler: ram
def crawler():
    s = Sheet("hollow_crawler", L=14, R=6, T=4)
    a = s.src
    h, w = a.shape[:2]
    spikes, shell = split(a, lambda x, y: y <= 4)

    def raised(lift):
        o = blank(w, h); paste(o, shell, 0, 0); paste(o, spikes, 0, -lift); return o

    s.frames.append(s.at(a))
    s.frames.append(s.at(squash(raised(1), 0.84, 1.08, w, h), 3))
    f = s.at(squash(raised(1), 1.22, 0.88, w, h), -8)
    for i, y in enumerate((h - 9, h - 6, h - 12)):
        line(f, s.L + w - 2 + i * 2, s.T + y, s.L + w + 3 + i * 2, s.T + y, (*DUST, 180))
    puff(f, s.L + w - 4, s.feet - 2, 2, DUST, DUST_D); puff(f, s.L + w, s.feet - 2, 1, DUST, DUST_D)
    s.frames.append(f)
    f = s.at(squash(raised(0), 0.9, 1.04, 0, h), -11)
    spark(f, s.L - 12, s.T + h - 7, 3, (255, 230, 150))
    s.frames.append(f)
    s.frames.append(s.at(a, -5))
    return s


# ---------------------------------------------------------------- Abyssal Hulk: overhead ground slam
def hulk():
    s = Sheet("abyssal_hulk", L=10, R=2, T=18)
    a = s.src
    arm, body = split(a, lambda x, y: (x <= 10 and y >= 10) or (x <= 13 and y >= 24))
    body = reoutline(body, s.out)
    shoulder = (9, 12)
    base = s.feet - 1

    def pose(k, rot, dx, dy, bsy=1.0, glow=False):
        f = s.new()
        b = s.at(body)
        if bsy != 1.0: b = squash(b, 1 / bsy ** 0.5, bsy, s.L + 20, s.feet)
        paste(f, shear(b, k, base), 0, 0)
        arm_img = arm
        if glow:
            arm_img = arm.copy()
            m = (arm_img[..., 0] > 150) & (arm_img[..., 2] > 230)
            arm_img[m] = (240, 220, 255, 255)
        place_rot(f, arm_img, rot, shoulder, (s.L + shoulder[0] + dx, s.T + shoulder[1] + dy))
        if glow:
            m = (f[..., 0] > 150) & (f[..., 2] > 230) & (f[..., 1] < 150)
            f[m] = (240, 220, 255, 255)
        return f

    s.frames.append(s.at(a))
    s.frames.append(pose(0.10, 195, 7, -3))                     # fist raised over the head
    f = pose(-0.06, 25, -2, 4, 0.94)                             # slam
    arc(f, s.L + 10, s.T + 14, 16, 20, 100, 205, (*VIO_L, 130), (*VIO, 60))
    s.frames.append(f)
    f = pose(-0.06, 25, -2, 4, 0.94, glow=True)                  # impact: runes flare, ground cracks
    g = s.feet - 1
    for x, r in ((s.L - 11, 2), (s.L - 7, 3), (s.L + 6, 2), (s.L + 10, 1), (s.L - 14, 1)):
        puff(f, x, g - r + 1, r, DUST, DUST_D)
    for x0, x1 in ((s.L - 8, s.L - 16), (s.L + 2, s.L + 9)):
        line(f, x0, g, x1, g, (40, 24, 56))
    for x in (s.L - 17, s.L + 11):
        spark(f, x, g - 4, 1, VIO, VIO_L)
    s.frames.append(f)
    s.frames.append(pose(0.02, 8, 0, 1))
    return s


# ---------------------------------------------------------------- The Jailer: overhead axe chop
def jailer():
    s = Sheet("the_jailer", L=16, R=2, T=18)
    a = s.src
    axe, body = split(a, lambda x, y: x <= 10)
    body = reoutline(body, s.out)
    hand = (9, 20)
    base = s.feet - 1

    def pose(k, rot, dx, dy, front=False):
        f = s.new()
        if not front: place_rot(f, axe, rot, hand, (s.L + hand[0] + dx, s.T + hand[1] + dy))
        paste(f, shear(s.at(body), k, base), 0, 0)
        if front: place_rot(f, axe, rot, hand, (s.L + hand[0] + dx, s.T + hand[1] + dy))
        return f

    s.frames.append(s.at(a))
    s.frames.append(pose(0.06, -35, 5, -12, front=True))                      # axe hauled up behind the helmet
    f = pose(-0.07, 100, -3, 4)                                  # chop
    arc(f, s.L + 6, s.T + 24, 14, 19, 40, 200, (255, 255, 255, 170), (*STEEL, 90))
    s.frames.append(f)
    f = pose(-0.07, 100, -3, 4)                                  # impact: sparks and dust where the blade bites
    g = s.feet - 1
    spark(f, s.L - 12, g - 3, 3, (255, 220, 120))
    for x, r in ((s.L - 15, 2), (s.L - 8, 2), (s.L - 4, 1)):
        puff(f, x, g - r + 1, r, DUST, DUST_D)
    line(f, s.L - 14, g, s.L - 7, g, (40, 24, 30))
    s.frames.append(f)
    s.frames.append(pose(-0.02, 40, -1, 2))
    return s


ALL = [("Weeping Sentinel", sentinel), ("Chainbound Ghoul", ghoul), ("Despair Wisp", wisp),
       ("Gloomling", gloomling), ("Hollow Crawler", crawler), ("Abyssal Hulk", hulk), ("The Jailer", jailer)]
