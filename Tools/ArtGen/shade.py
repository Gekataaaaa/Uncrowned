"""
Shaded pixel primitives: ellipsoids and capsules lit like 3D volumes, quantized to 4-colour ramps,
with optional rim light and selective (colour-aware) outlines. Good enough to judge a style direction.
"""
import math
from PIL import Image


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def ramp(*hexes):
    return [hexc(h) for h in hexes]


LIGHT = (-0.5, -0.7, 0.55)
_l = math.sqrt(sum(c * c for c in LIGHT))
LIGHT = tuple(c / _l for c in LIGHT)


class Sculpt:
    def __init__(self, w, h, rim=None, rim_side=-1, light=None, flat=False):
        self.w, self.h = w, h
        self.flat = flat          # flat colours: no volume shading, one tone per part
        self.px = [[None] * w for _ in range(h)]
        self.rim = rim            # rim-light colour (e.g. Abyss glow) or None
        self.rim_side = rim_side  # -1: rim on the left edges
        if light is None:
            self.light = LIGHT
        else:
            n = math.sqrt(sum(c * c for c in light))
            self.light = tuple(c / n for c in light)

    # ---- lighting ----
    def _shade(self, nx, ny, nz, r, shift=0):
        if self.flat:
            # base tone; parts set further back (negative shift) one tone darker for separation
            return r[2 + min(shift, 0)]
        L = self.light
        l = nx * L[0] + ny * L[1] + nz * L[2]
        i = 0 if l < 0.05 else 1 if l < 0.42 else 2 if l < 0.78 else 3
        i = max(0, min(3, i + shift))
        if self.rim and nx * self.rim_side > 0.86 and nz < 0.45 and ny < 0.5:
            return self.rim
        return r[i]

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h and c is not None:
            if len(c) == 4 and c[3] < 255 and self.px[y][x] is not None:
                a = c[3] / 255
                b = self.px[y][x]
                c = (int(b[0] + (c[0] - b[0]) * a), int(b[1] + (c[1] - b[1]) * a), int(b[2] + (c[2] - b[2]) * a), 255)
            self.px[y][x] = c

    # ---- volumes ----
    def ellipse(self, cx, cy, rx, ry, r, shift=0):
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
                d = dx * dx + dy * dy
                if d <= 1:
                    self.put(x, y, self._shade(dx, dy, math.sqrt(1 - d), r, shift))

    def capsule(self, ax, ay, bx, by, r0, r1, r, shift=0):
        vx, vy = bx - ax, by - ay
        L2 = vx * vx + vy * vy or 1e-6
        rmax = max(r0, r1)
        for y in range(int(min(ay, by) - rmax) - 1, int(max(ay, by) + rmax) + 2):
            for x in range(int(min(ax, bx) - rmax) - 1, int(max(ax, bx) + rmax) + 2):
                px, py = x + 0.5, y + 0.5
                t = max(0, min(1, ((px - ax) * vx + (py - ay) * vy) / L2))
                cx, cy = ax + vx * t, ay + vy * t
                rad = r0 + (r1 - r0) * t
                nx, ny = (px - cx) / rad, (py - cy) / rad
                d = nx * nx + ny * ny
                if d <= 1:
                    self.put(x, y, self._shade(nx, ny, math.sqrt(1 - d), r, shift))

    def poly(self, pts, r, shift=0):
        """Flat cloth shape, lit top-to-bottom."""
        from PIL import ImageDraw
        m = Image.new("L", (self.w, self.h), 0)
        ImageDraw.Draw(m).polygon(pts, fill=255)
        ys = [p[1] for p in pts]
        top, bot = min(ys), max(ys)
        for y in range(self.h):
            for x in range(self.w):
                if m.getpixel((x, y)):
                    t = (y - top) / max(1, bot - top)
                    i = 2 if self.flat else 3 if t < 0.15 else 2 if t < 0.5 else 1 if t < 0.85 else 0
                    self.put(x, y, r[max(0, min(3, i + shift))])

    def dot(self, x, y, c):
        self.put(int(x), int(y), c)

    def line(self, x0, y0, x1, y1, c):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) or 1
        for i in range(n + 1):
            t = i / n
            self.put(round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t), c)

    def glow(self, x, y, c, radius=2):
        """Soft halo for glowing eyes / gems."""
        for yy in range(int(y - radius), int(y + radius) + 1):
            for xx in range(int(x - radius), int(x + radius) + 1):
                d = math.hypot(xx - x, yy - y)
                if 0 < d <= radius:
                    a = int(120 * (1 - d / (radius + 0.5)))
                    self.put(xx, yy, (c[0], c[1], c[2], a))

    # ---- finishing ----
    def selout(self, black=False, strength=0.32):
        """Outline every silhouette pixel; colour-aware (darkened neighbour) unless black=True."""
        out = [row[:] for row in self.px]
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] is not None:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < self.w and 0 <= ny < self.h and self.px[ny][nx] is not None:
                        c = self.px[ny][nx]
                        out[y][x] = hexc("#120c18") if black else (int(c[0] * strength), int(c[1] * strength), int(c[2] * strength * 1.2) if c[2] * strength * 1.2 < 255 else 255, 255)
                        break
        self.px = out
        return self

    def image(self):
        img = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] is not None:
                    img.putpixel((x, y), self.px[y][x])
        return img
