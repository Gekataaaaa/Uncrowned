"""Attack frames for the mobs (all but the Eyeless Sovereign) -> Art/MobAttacks/<mob>/.
Each mob: <mob>_attack0..4.png (idle, windup, strike, impact, recover), a strip of all 5,
and a x4 preview gif. Frames are wider than the source sprite: the extra room on the left is
for the lunge, so the feet of the original sprite sit at x = left_pad + sprite_width / 2.
Run: python Tools/ArtGen/mob_attacks.py"""
import os
from PIL import Image, ImageDraw, ImageSequence
from attack_frames import ALL

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Art", "MobAttacks")
S = 4
BG = (46, 42, 58, 255); GROUND = (30, 26, 38, 255)
LABELS = ["idle", "windup", "strike", "impact", "recover"]
DUR = [350, 280, 90, 180, 220]

rows, gifs, notes = [], [], []
for title, fn in ALL:
    s = fn()
    slug = title.lower().replace(" ", "_")
    d = os.path.join(ROOT, slug)
    os.makedirs(d, exist_ok=True)
    frames = [Image.fromarray(f) for f in s.frames]
    strip = Image.new("RGBA", (s.W * len(frames), s.H))
    for i, f in enumerate(frames):
        f.save(os.path.join(d, f"{slug}_attack{i}.png"))
        strip.paste(f, (i * s.W, 0))
    strip.save(os.path.join(d, f"{slug}_attack_strip.png"))
    big = []
    for f in frames:
        c = Image.new("RGBA", (s.W, s.H + 3), BG)
        ImageDraw.Draw(c).rectangle([0, s.feet, s.W, s.H + 3], fill=GROUND)
        c.alpha_composite(f)
        big.append(c.resize((c.width * S, c.height * S), Image.NEAREST))
    big[0].save(os.path.join(d, f"{slug}_attack.gif"), save_all=True, append_images=big[1:],
                duration=DUR, loop=0, disposal=2)
    rows.append((title, big)); gifs.append(big)
    notes.append(f"| {title} | {s.W}x{s.H} | {s.L + s.w // 2}, {s.feet} |")

# overview sheet + one gif with every mob
pad = 16
W = max(sum(b.width for b in bs) + pad * (len(bs) + 1) for _, bs in rows) + 170
H = sum(bs[0].height + pad + 20 for _, bs in rows) + pad
sheet = Image.new("RGBA", (W, H), (24, 22, 30, 255))
dr = ImageDraw.Draw(sheet)
y = pad
for title, bs in rows:
    dr.text((pad, y + bs[0].height // 2), title, fill=(230, 225, 240))
    x = 170
    for i, b in enumerate(bs):
        sheet.paste(b, (x, y + 16)); dr.text((x, y), LABELS[i], fill=(160, 155, 175)); x += b.width + pad
    y += bs[0].height + pad + 20
sheet.save(os.path.join(ROOT, "_all_attacks.png"))
W = sum(g[0].width for g in gifs) + 10 * (len(gifs) + 1); H = max(g[0].height for g in gifs) + 20
anim = []
for i in range(5):
    c = Image.new("RGBA", (W, H), (24, 22, 30, 255)); x = 10
    for g in gifs:
        c.paste(g[i], (x, H - 10 - g[i].height)); x += g[i].width + 10
    anim.append(c)
anim[0].save(os.path.join(ROOT, "_all_attacks.gif"), save_all=True, append_images=anim[1:], duration=DUR, loop=0)
print("\n".join(notes))
