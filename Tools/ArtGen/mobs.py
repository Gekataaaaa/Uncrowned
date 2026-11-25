"""
Concept sketches for the Abyss of Despair bestiary. All face LEFT (toward the hero).
Writes individual PNGs and a contact sheet to Docs/Concepts/.

Run:  python Tools/ArtGen/mobs.py
"""
import os
from pixel import Canvas, hexc, sheet

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DOCS = os.path.join(ROOT, "Docs", "Concepts")
OUT = os.path.join(DOCS, "mobs")

# Abyss palette: bruised violets, ash greys, sickly glows.
VOID = hexc("#2a1f3d")
VOID_SH = hexc("#1c1529")
VOID_HI = hexc("#45345f")
ASH = hexc("#6f6a7a")
ASH_SH = hexc("#4b4656")
ASH_HI = hexc("#9a94a6")
BONE = hexc("#d9cfb8")
BONE_SH = hexc("#a89c86")
GLOW = hexc("#b46bff")
GLOW_HI = hexc("#e6c4ff")
BLOOD = hexc("#c4213a")
IRON = hexc("#5c5f6b")
IRON_HI = hexc("#8d909c")
RUST = hexc("#7a3f22")
CLOTH = hexc("#3b2a2a")
PALE = hexc("#cdbfd8")


# ---------------- Light ----------------

def hollow_crawler():
    """Light 1: skittering husk on six thin legs, one glowing eye. Fast, low HP, comes in packs."""
    c = Canvas(28, 16)
    for i, x in enumerate((7, 12, 17)):
        c.line([(x, 9), (x - 3, 15)], ASH_SH)
        c.line([(x + 2, 9), (x + 4, 15)], ASH)
    c.ell(4, 4, 24, 11, ASH)
    c.ell(6, 4, 24, 8, ASH_HI)
    for x in (10, 14, 18, 22):                    # back spines
        c.line([(x, 4), (x + 1, 1)], BONE)
    c.ell(1, 6, 7, 11, ASH_SH)                    # head
    c.px(3, 8, GLOW); c.px(2, 8, GLOW_HI)
    c.line([(1, 11), (0, 13)], BONE)              # mandible
    return c.outline()


def despair_wisp():
    """Light 2: floating violet flame with hollow eyes. Flies, dives at the hero, bursts on death."""
    c = Canvas(18, 26)
    c.poly([(9, 0), (16, 12), (15, 18), (9, 22), (3, 18), (2, 12)], GLOW)
    c.poly([(9, 5), (13, 13), (12, 17), (9, 19), (6, 17), (5, 13)], GLOW_HI)
    c.rect(6, 12, 7, 14, VOID_SH)                 # hollow eyes
    c.rect(10, 12, 11, 14, VOID_SH)
    c.line([(5, 21), (4, 25)], GLOW)              # trailing wisps
    c.line([(12, 21), (14, 25)], GLOW)
    c.px(9, 24, GLOW_HI)
    return c.outline(hexc("#3a1460"))


def gloomling():
    """Light 3: small horned imp with crimson eyes and long claws. Jumps in, slashes, hops back."""
    c = Canvas(22, 26)
    c.line([(17, 18), (21, 13)], VOID)            # tail
    c.px(21, 12, BLOOD)
    c.rect(8, 20, 9, 25, VOID_SH)                 # legs
    c.rect(13, 20, 14, 25, VOID)
    c.ell(6, 11, 17, 22, VOID)                    # hunched body
    c.ell(7, 12, 14, 17, VOID_HI)
    c.ell(3, 4, 13, 13, VOID)                     # head
    c.line([(5, 5), (2, 0)], BONE)                # horns
    c.line([(10, 5), (12, 0)], BONE)
    c.px(5, 8, BLOOD); c.px(8, 8, BLOOD)          # eyes
    c.line([(4, 11), (8, 11)], BONE_SH)           # teeth
    c.line([(6, 15), (1, 19)], VOID_SH)           # arm
    for dx in (0, 1, 2):                          # claws
        c.line([(1, 19), (dx, 22)], BONE)
    return c.outline()


# ---------------- Medium ----------------

def chainbound_ghoul():
    """Medium 1: gaunt prisoner of the Abyss, shackled. Swings its chain in a wide arc."""
    c = Canvas(34, 40)
    c.rect(14, 28, 15, 39, ASH_SH)                # legs
    c.rect(19, 28, 20, 39, ASH)
    c.rect(13, 25, 21, 30, CLOTH)                 # rag loincloth
    c.poly([(12, 13), (22, 12), (23, 26), (12, 26)], ASH)   # ribcage torso
    for y in (16, 19, 22):
        c.line([(14, y), (21, y)], ASH_SH)
    c.ell(9, 4, 19, 14, ASH)                      # drooping head
    c.rect(10, 4, 18, 6, CLOTH)                   # hair clumps
    c.px(11, 9, GLOW); c.px(14, 9, GLOW)
    c.line([(11, 12), (15, 12)], VOID_SH)
    c.line([(13, 15), (6, 24)], ASH_SH, 2)        # arm
    c.rect(4, 23, 8, 26, IRON)                    # shackle
    c.px(5, 24, IRON_HI)
    for i in range(6):                            # dangling chain
        c.rect(3 - i // 2, 27 + i * 2, 4 - i // 2, 28 + i * 2, IRON if i % 2 else IRON_HI)
    c.line([(22, 15), (26, 25)], ASH, 2)
    c.rect(24, 25, 28, 28, IRON)
    return c.outline()


def weeping_sentinel():
    """Medium 2: tall hooded guard behind a cracked white mask, tears of light. Keeps distance with a spear."""
    c = Canvas(32, 44)
    c.line([(3, 2), (3, 43)], RUST, 1)            # spear shaft
    c.poly([(3, 0), (5, 5), (3, 8), (1, 5)], IRON_HI)  # spear head
    c.poly([(9, 14), (24, 14), (28, 43), (6, 43)], VOID)     # robe
    c.poly([(15, 14), (24, 14), (28, 43), (18, 43)], VOID_SH)
    c.poly([(8, 3), (21, 2), (25, 16), (7, 17)], VOID_SH)    # hood
    c.ell(9, 5, 19, 16, BONE)                     # mask
    c.rect(11, 9, 12, 10, VOID_SH); c.rect(15, 9, 16, 10, VOID_SH)
    c.line([(11, 11), (10, 16)], GLOW)            # glowing tear tracks
    c.line([(16, 11), (16, 16)], GLOW)
    c.line([(17, 6), (14, 12)], BONE_SH)          # crack
    c.line([(10, 20), (4, 22)], VOID_HI, 2)       # arm gripping spear
    return c.outline()


# ---------------- Heavy ----------------

def abyssal_hulk():
    """Heavy: hulking mass of stone and flesh with glowing seams. Slow, huge slams, shrugs off knockback."""
    c = Canvas(48, 48)
    c.rect(14, 36, 21, 47, ASH_SH)                # legs
    c.rect(28, 36, 35, 47, ASH)
    c.ell(8, 12, 42, 42, ASH)                     # body
    c.ell(12, 14, 34, 30, ASH_HI)
    c.ell(14, 4, 26, 16, ASH_SH)                  # small sunken head
    c.px(17, 10, GLOW); c.px(21, 10, GLOW)
    c.line([(12, 22), (2, 34)], ASH_SH, 5)        # huge left arm
    c.ell(0, 32, 9, 42, ASH_SH)                   # fist
    c.line([(40, 22), (45, 34)], ASH, 4)
    c.ell(40, 32, 47, 40, ASH)
    for pts in ([(18, 20), (22, 26), (20, 32)], [(30, 18), (27, 26)], [(34, 28), (36, 34)]):
        c.line(pts, GLOW)                         # cracks with abyss light
    return c.outline()


# ---------------- Mini-bosses ----------------

def the_jailer():
    """Mini-boss 1: warden of the exiles. Iron helm, ring of keys, butcher's cleaver. Grabs and throws."""
    c = Canvas(56, 62)
    c.poly([(24, 16), (50, 18), (54, 61), (22, 61)], hexc("#3d1f24"))   # cape behind
    c.rect(20, 46, 27, 61, IRON)                   # legs
    c.rect(33, 46, 40, 61, IRON_HI)
    c.poly([(16, 20), (42, 20), (44, 48), (14, 48)], RUST)              # leather torso
    c.rect(14, 40, 44, 44, hexc("#2a1a14"))       # belt
    c.ell(26, 41, 34, 51, IRON_HI)                 # key ring
    c.ell(28, 43, 32, 49, (0, 0, 0, 0))
    c.line([(30, 50), (30, 56)], IRON_HI); c.line([(31, 50), (33, 55)], IRON)
    c.rect(18, 4, 36, 20, IRON)                    # bucket helm
    c.rect(18, 4, 36, 6, IRON_HI)
    c.rect(19, 11, 30, 12, VOID_SH)                # eye slit
    c.px(21, 11, BLOOD); c.px(25, 11, BLOOD)
    c.line([(18, 24), (8, 34)], RUST, 5)           # arm
    c.poly([(0, 22), (10, 20), (12, 38), (2, 40)], IRON_HI)             # cleaver
    c.line([(2, 25), (2, 37)], BLOOD)
    c.rect(8, 33, 12, 36, hexc("#2a1a14"))         # handle
    return c.outline()


def mother_of_murmurs():
    """Mini-boss 2: pale weeping woman fused to a spider's body. Spawns wisps, webs the floor."""
    c = Canvas(66, 50)
    for x0, x1 in ((30, 4), (34, 12), (40, 52), (44, 62), (36, 22), (42, 46)):   # legs
        c.line([(x0, 34), ((x0 + x1) // 2, 22), (x1, 49)], VOID_SH, 2)
    c.ell(26, 26, 58, 44, VOID)                    # abdomen
    c.ell(32, 28, 52, 36, VOID_HI)
    c.ell(40, 33, 44, 37, GLOW)                    # glowing marking
    c.poly([(22, 14), (32, 14), (34, 32), (20, 32)], PALE)              # torso
    c.line([(21, 16), (12, 26)], PALE, 2)          # arms reaching out
    c.line([(31, 17), (22, 28)], PALE, 2)
    c.ell(20, 2, 32, 15, PALE)                     # head
    c.poly([(19, 3), (33, 2), (36, 22), (16, 22)], hexc("#1a1a24"))     # long black hair
    c.ell(21, 5, 30, 14, PALE)
    c.px(23, 9, VOID_SH); c.px(27, 9, VOID_SH)
    c.line([(23, 10), (23, 14)], GLOW); c.line([(27, 10), (27, 14)], GLOW)  # tears
    c.line([(24, 13), (26, 13)], BLOOD)
    return c.outline()


# ---------------- Boss ----------------

def eyeless_sovereign():
    """Boss: the Eyeless Sovereign, tyrant of the Abyss who hoards the eyes of the damned.
    He covets the prince's mismatched eyes. Bone crown, empty bleeding sockets, endless arms."""
    c = Canvas(90, 100)
    c.ell(10, 20, 80, 99, hexc("#1f1430"))         # dark aura behind
    c.poly([(26, 40), (64, 40), (82, 99), (8, 99)], hexc("#4a1630"))     # royal robe
    c.poly([(45, 40), (64, 40), (82, 99), (50, 99)], hexc("#33101f"))
    c.rect(26, 40, 64, 46, hexc("#c9a04a"))        # gold collar
    for x in range(28, 64, 6):
        c.px(x, 43, BLOOD)
    c.line([(28, 46), (10, 64), (4, 80)], PALE, 4)    # long arms
    c.line([(62, 46), (78, 62), (84, 80)], PALE, 4)
    for dx in (-3, 0, 3):                          # claws
        c.line([(4, 80), (4 + dx, 88)], BONE)
        c.line([(84, 80), (84 + dx, 88)], BONE)
    c.ell(32, 14, 58, 42, PALE)                    # gaunt head
    c.ell(36, 24, 43, 31, VOID_SH)                 # empty eye sockets
    c.ell(47, 24, 54, 31, VOID_SH)
    c.line([(39, 31), (38, 40)], BLOOD)            # bleeding
    c.line([(51, 31), (52, 40)], BLOOD)
    c.line([(40, 37), (50, 37)], VOID_SH)
    for i, x in enumerate(range(32, 60, 5)):       # bone crown
        h = 12 if i % 2 == 0 else 7
        c.poly([(x, 16), (x + 2, 16 - h), (x + 4, 16)], BONE)
    c.rect(32, 14, 58, 17, BONE_SH)
    c.ell(42, 3, 48, 9, GLOW)                      # stolen eye set in the crown
    c.px(45, 6, GLOW_HI)
    for (x, y) in ((14, 30), (76, 34), (20, 92), (70, 90)):  # floating stolen eyes
        c.ell(x - 2, y - 2, x + 2, y + 2, BONE)
        c.px(x, y, GLOW if (x + y) % 2 else BLOOD)
    return c.outline()


MOBS = [
    ("Hollow Crawler", "LIGHT - fast pack husk", hollow_crawler),
    ("Despair Wisp", "LIGHT - flying diver", despair_wisp),
    ("Gloomling", "LIGHT - leaping imp", gloomling),
    ("Chainbound Ghoul", "MEDIUM - chain sweeper", chainbound_ghoul),
    ("Weeping Sentinel", "MEDIUM - spear keeper", weeping_sentinel),
    ("Abyssal Hulk", "HEAVY - slow slammer", abyssal_hulk),
    ("The Jailer", "MINI-BOSS - grabs, throws", the_jailer),
    ("Mother of Murmurs", "MINI-BOSS - webs, wisps", mother_of_murmurs),
    ("The Eyeless Sovereign", "BOSS - Abyss tyrant", eyeless_sovereign),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    items = []
    for name, sub, fn in MOBS:
        c = fn()
        c.save(os.path.join(OUT, name.lower().replace(" ", "_") + ".png"))
        items.append((name, sub, c.img))
    sheet(items, 4, 3, label_h=16).save(os.path.join(DOCS, "abyss_mobs.png"))
    print("mob sketches written to", DOCS)


if __name__ == "__main__":
    main()
