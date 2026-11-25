"""
Mirrors the in-game art (Assets/Resources/Art) into Art/InGame for browsing, without .meta files.
The hero's frames are split by animation and eye mood, with a preview gif per animation:

  Art/InGame/Hero/<animation>/<mood>/knight_..png
  Art/InGame/Hero/<animation>/preview.gif     all four moods side by side, x3

Everything else keeps its folder layout. Art/InGame is wiped first: it is only a copy.

Run:  python Tools/ArtGen/sync_art.py
"""
import os
import re
import shutil
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "Assets", "Resources", "Art")
DST = os.path.join(ROOT, "Art", "InGame")
MOODS = ["neutral", "angry", "narrow", "feral"]
FRAME = re.compile(r"knight_(?:(angry|narrow|feral)_)?([A-Za-z]+?)(\d+)\.png$")
FPS = {"walk": 100, "idle": 180, "ult": 70, "spin": 60}


def main():
    if os.path.isdir(DST):
        shutil.rmtree(DST)
    anims = {}
    for dirpath, _, files in os.walk(SRC):
        rel = os.path.relpath(dirpath, SRC)
        for f in files:
            if f.endswith(".meta"):
                continue
            src = os.path.join(dirpath, f)
            m = FRAME.match(f) if rel == "Hero" else None
            if m:
                mood, anim, n = m.group(1) or "neutral", m.group(2), int(m.group(3))
                out = os.path.join(DST, "Hero", anim, mood)
                anims.setdefault(anim, {}).setdefault(mood, []).append((n, src))
            else:
                out = os.path.join(DST, rel)
            os.makedirs(out, exist_ok=True)
            shutil.copy2(src, os.path.join(out, f))

    for anim, moods in anims.items():
        seqs = [[Image.open(p).convert("RGBA") for _, p in sorted(moods[m])] for m in MOODS if m in moods]
        w, h = seqs[0][0].size
        frames = []
        for i in range(max(len(s) for s in seqs)):
            img = Image.new("RGBA", (w * len(seqs), h), (39, 40, 51, 255))
            for k, s in enumerate(seqs):
                img.alpha_composite(s[min(i, len(s) - 1)], (k * w, 0))
            frames.append(img.convert("RGB").resize((img.width * 3, h * 3), Image.NEAREST))
        frames[0].save(os.path.join(DST, "Hero", anim, "preview.gif"), save_all=True, append_images=frames[1:],
                       duration=FPS.get(anim, 110), loop=0)
    print("synced", DST, "-", len(anims), "hero animations:", ", ".join(sorted(anims)))


if __name__ == "__main__":
    main()
