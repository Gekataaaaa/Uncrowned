"""Small pixel-art toolkit for drawing attack frames from the existing mob sprites."""
import math
import os
import numpy as np
from PIL import Image

MOBS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Art", "Mobs") + os.sep


def load(name):
    return np.array(Image.open(MOBS + name + ".png").convert("RGBA"))


def blank(w, h):
    return np.zeros((h, w, 4), np.uint8)


def split(a, pred):
    """Return (part, rest): pixels where pred(x, y) is true go to part."""
    h, w = a.shape[:2]
    ys, xs = np.mgrid[0:h, 0:w]
    m = np.vectorize(pred)(xs, ys).astype(bool)
    part = a.copy(); part[~m] = 0
    rest = a.copy(); rest[m] = 0
    return part, rest


def paste(dst, src, ox, oy, alpha=1.0, tint=None):
    """Alpha-over src onto dst with its top-left at (ox, oy). alpha < 1 makes ghosts."""
    h, w = src.shape[:2]
    H, W = dst.shape[:2]
    for y in range(h):
        Y = y + oy
        if Y < 0 or Y >= H: continue
        for x in range(w):
            X = x + ox
            if X < 0 or X >= W: continue
            p = src[y, x]
            if p[3] == 0: continue
            c = np.array(p[:3], float)
            if tint is not None:
                c = c * 0.4 + np.array(tint, float) * 0.6
            a = p[3] / 255.0 * alpha
            d = dst[Y, X]
            da = d[3] / 255.0
            oa = a + da * (1 - a)
            if oa <= 0: continue
            col = (c * a + d[:3].astype(float) * da * (1 - a)) / oa
            dst[Y, X] = [*np.clip(col, 0, 255).astype(np.uint8), int(oa * 255)]


def put(dst, x, y, col):
    H, W = dst.shape[:2]
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        if len(col) == 3: col = (*col, 255)
        if col[3] >= 255 or dst[y, x, 3] == 0:
            dst[y, x] = col
        else:
            tmp = np.zeros((1, 1, 4), np.uint8); tmp[0, 0] = col
            paste(dst, tmp, x, y)


def scale2x(a):
    h, w = a.shape[:2]
    p = np.pad(a, ((1, 1), (1, 1), (0, 0)), mode="edge")
    E = p[1:-1, 1:-1]
    B = p[:-2, 1:-1]; D = p[1:-1, :-2]; F = p[1:-1, 2:]; Hh = p[2:, 1:-1]
    eq = lambda u, v: np.all(u == v, axis=2)
    out = np.zeros((h * 2, w * 2, 4), np.uint8)
    c = ~eq(B, Hh) & ~eq(D, F)
    e0 = np.where((c & eq(D, B))[..., None], D, E)
    e1 = np.where((c & eq(B, F))[..., None], F, E)
    e2 = np.where((c & eq(D, Hh))[..., None], D, E)
    e3 = np.where((c & eq(Hh, F))[..., None], F, E)
    out[0::2, 0::2] = e0; out[0::2, 1::2] = e1
    out[1::2, 0::2] = e2; out[1::2, 1::2] = e3
    return out


def rotate(part, deg, px, py):
    """RotSprite-style rotation (counter-clockwise on screen) of part about pixel (px, py).
    Returns (img, ox, oy): paste img at (qx + ox, qy + oy) to put the pivot at (qx, qy)."""
    h, w = part.shape[:2]
    S = 2 * max(h, w) + 6
    c = S // 2
    big = blank(S, S)
    big[c - py:c - py + h, c - px:c - px + w] = part
    up = scale2x(scale2x(scale2x(big)))
    im = Image.fromarray(up).rotate(deg, resample=Image.NEAREST, center=(c * 8 + 4, c * 8 + 4))
    r = np.array(im)[4::8, 4::8].copy()
    return r, -c, -c


def place_rot(dst, part, deg, pivot, at):
    r, ox, oy = rotate(part, deg, *pivot)
    paste(dst, r, at[0] + ox, at[1] + oy)


def resample(a, fn):
    """Inverse-map every pixel: fn(x, y) -> source (x, y)."""
    h, w = a.shape[:2]
    out = blank(w, h)
    for y in range(h):
        for x in range(w):
            sx, sy = fn(x + 0.5, y + 0.5)
            ix, iy = int(math.floor(sx)), int(math.floor(sy))
            if 0 <= ix < w and 0 <= iy < h:
                out[y, x] = a[iy, ix]
    return out


def shear(a, k, base_y):
    """Lean: a row d pixels above base_y moves k*d to the right (k > 0 leans back for a left-facing mob)."""
    return resample(a, lambda x, y: (x - k * (base_y - y), y))


def squash(a, sx, sy, ax, ay):
    return resample(a, lambda x, y: ((x - ax) / sx + ax, (y - ay) / sy + ay))


def outline(a, col):
    """Grow a 1-px outline around opaque pixels (for freshly drawn chains, bolts...)."""
    h, w = a.shape[:2]
    out = a.copy()
    op = a[..., 3] > 0
    for y in range(h):
        for x in range(w):
            if op[y, x]: continue
            if any(0 <= y + dy < h and 0 <= x + dx < w and op[y + dy, x + dx]
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out[y, x] = (*col, 255)
    return out


def reoutline(a, col):
    """Edge pixels of the silhouette become outline-coloured (after cutting parts away)."""
    h, w = a.shape[:2]
    out = a.copy()
    op = a[..., 3] > 0
    for y in range(h):
        for x in range(w):
            if not op[y, x]: continue
            if any(not (0 <= y + dy < h and 0 <= x + dx < w) or not op[y + dy, x + dx]
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out[y, x] = (*col, 255)
    return out


def arc(dst, cx, cy, r0, r1, a0, a1, col_outer, col_inner):
    """Smear arc between radii r0..r1, angles a0..a1 in degrees (0 = right, 90 = up)."""
    steps = int(abs(a1 - a0) * max(r1, 1) / 20) + 8
    for i in range(steps + 1):
        t = i / steps
        ang = math.radians(a0 + (a1 - a0) * t)
        # the trailing end of the smear is thinner
        thick = r0 + (r1 - r0) * min(1, t * 1.6)
        r = r1
        while r >= thick - 0.01:
            col = col_outer if r >= r1 - 0.6 else col_inner
            put(dst, cx + r * math.cos(ang), cy - r * math.sin(ang), col)
            r -= 0.5


def line(dst, x0, y0, x1, y1, col):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / max(n, 1)
        put(dst, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, col)


def puff(dst, x, y, r, col, shade):
    """Round dust puff."""
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r + r * 0.6:
                put(dst, x + dx, y + dy, shade if dy > 0 or dx > r // 2 else col)


def spark(dst, x, y, size, col, core=(255, 255, 255)):
    put(dst, x, y, core)
    for i in range(1, size + 1):
        c = col if i > 1 else core
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            put(dst, x + dx * i, y + dy * i, c)
    if size >= 2:
        for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
            put(dst, x + dx, y + dy, col)
