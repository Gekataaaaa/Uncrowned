"""Bakes the knight's eye expressions: back eye ice blue, front eye crimson.

Reads the untouched frames from Tools/ArtGen/hero_src (white eyes) and writes one set per mood into
Assets/Resources/Art/Hero: neutral -> knight_<frame>.png, the others -> knight_<mood>_<frame>.png.
PlayerAppearance.SetEyes switches between them at runtime.

usage: python Tools/ArtGen/eyes.py [out_dir]
"""
import glob
import os
import sys

from PIL import Image

SRC = "Tools/ArtGen/hero_src"
EYE = (242, 242, 242, 255)
RIM = (166, 166, 166, 255)
SOCKET = (39, 40, 51, 255)
#       glint            light            mid              dark
ICE = ((225, 250, 255), (130, 215, 240), (70, 150, 195), (30, 80, 115))
CRIMSON = ((255, 170, 150), (235, 60, 40), (170, 25, 15), (95, 10, 5))

# Eye cuts drawn over the eye's box (rim row + eye rows). Back eye is 4 wide, front eye 3 (the face
# looks right, so the inner corner is on the right of the back eye and the left of the front eye).
# g glint, l light, m mid, d dark rim, . empty socket. None = keep the original shape, just recolor.
MOODS = {
    "neutral": None,
    "angry": (["dd..", "gmdd", "mmmm", ".ll."], ["..d", "dgm", "mmm", "ll."]),
    "narrow": (["....", "dddd", "gmmm", ".ll."], ["...", "ddd", "gmm", "ll."]),
    "feral": ([".dd.", "gmdm", "mmdm", ".ll."], [".dd", "gdm", "mdm", ".l."]),
}


def clusters(im):
    p, (w, h) = im.load(), im.size
    seen, out = set(), []
    for y in range(h):
        for x in range(w):
            if (x, y) in seen or p[x, y] != EYE:
                continue
            stack, c = [(x, y)], []
            seen.add((x, y))
            while stack:
                a, b = stack.pop()
                c.append((a, b))
                for q in ((a + 1, b), (a - 1, b), (a, b + 1), (a, b - 1)):
                    if 0 <= q[0] < w and 0 <= q[1] < h and q not in seen and p[q] == EYE:
                        seen.add(q)
                        stack.append(q)
            out.append(c)
    return out


def dist(a, b):
    return abs(min(a)[0] - min(b)[0]) + abs(min(a)[1] - min(b)[1])


def find_eyes(cs):
    """(back, front) eye clusters; either may be None. The blade's swoosh uses the same white, so match by shape."""
    a = next((c for c in cs if len(c) == 8), None)
    if a is None:  # half-closed eyes in hit1: 4 + 3
        a = next((c for c in cs if len(c) == 4 and any(len(d) == 3 and dist(c, d) <= 8 for d in cs)), None)
    want = 7 if a is None or len(a) == 8 else 3
    rs = [c for c in cs if len(c) == want and (a is None or dist(a, c) <= 10)]
    r = min(rs, key=lambda c: dist(a, c)) if a and rs else (rs[0] if len(rs) == 1 else None)
    return a, r


def side_by_side(a, r):
    """Only a face seen straight from the side can take a new cut; turned heads (spin, salto) just get recolored."""
    if not (a and r and len(a) == 8):
        return False
    (ax, ay), (rx, ry) = min(a), min(r)
    return 5 <= rx - ax <= 8 and abs(ry - ay) <= 1


def recolor(p, cluster, pal):
    bottom = max(y for _, y in cluster)
    glint = min(cluster, key=lambda q: (q[1], q[0]))  # top-left pixel catches the light
    for x, y in cluster:
        for q in ((x + dx, y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
            if p[q] == RIM:  # the grey lid pixels touching the eye become its dark rim
                p[q] = pal[3] + (255,)
    for x, y in cluster:
        c = pal[0] if (x, y) == glint else pal[1] if y == bottom else pal[2]
        p[x, y] = c + (255,)


def reshape(p, cluster, pal, rows):
    """Clears the eye and its rim to the socket, then draws `rows` from one row above the eye's top-left."""
    x0 = min(x for x, _ in cluster)
    y0 = min(y for _, y in cluster) - 1
    for x, y in cluster:
        for q in ((x + dx, y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
            if p[q] in (RIM, EYE):
                p[q] = SOCKET
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch != ".":
                p[x0 + i, y0 + j] = pal["glmd".index(ch)] + (255,)


def bake(im, shape):
    p = im.load()
    a, r = find_eyes(clusters(im))
    if shape and side_by_side(a, r):
        reshape(p, a, ICE, shape[0])
        reshape(p, r, CRIMSON, shape[1])
    else:
        if a:
            recolor(p, a, ICE)
        if r:
            recolor(p, r, CRIMSON)
    return im


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else "Assets/Resources/Art/Hero"
    os.makedirs(out, exist_ok=True)
    files = sorted(glob.glob(os.path.join(SRC, "knight_*.png")))
    assert files, "no source frames in " + SRC
    for mood, shape in MOODS.items():
        prefix = "knight_" if mood == "neutral" else f"knight_{mood}_"
        for f in files:
            frame = os.path.basename(f)[len("knight_"):]
            bake(Image.open(f).convert("RGBA"), shape).save(os.path.join(out, prefix + frame))
    print(f"{len(files)} frames x {len(MOODS)} moods -> {out}")


if __name__ == "__main__":
    main()
