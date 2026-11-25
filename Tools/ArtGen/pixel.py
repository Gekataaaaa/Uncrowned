"""Tiny pixel-art helpers shared by the art generators (Pillow, no antialiasing)."""
from PIL import Image, ImageDraw


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


OUTLINE = hexc("#140e1c")


class Canvas:
    def __init__(self, w, h):
        self.img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)
        self.w, self.h = w, h

    def px(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.img.putpixel((int(x), int(y)), c)

    def rect(self, x0, y0, x1, y1, c):
        self.d.rectangle([x0, y0, x1, y1], fill=c)

    def ell(self, x0, y0, x1, y1, c):
        self.d.ellipse([x0, y0, x1, y1], fill=c)

    def poly(self, pts, c):
        self.d.polygon(pts, fill=c)

    def line(self, pts, c, w=1):
        self.d.line(pts, fill=c, width=w)

    def outline(self, color=OUTLINE):
        """1px outer outline around every opaque pixel (4-neighbour)."""
        src = self.img.copy()
        sp = src.load()
        dp = self.img.load()
        for y in range(self.h):
            for x in range(self.w):
                if sp[x, y][3] != 0:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < self.w and 0 <= ny < self.h and sp[nx, ny][3] != 0:
                        dp[x, y] = color
                        break
        return self

    def save(self, path):
        self.img.save(path)
        return self


def sheet(items, scale, cols, bg="#0e0b14", label_h=18, pad=12):
    """Contact sheet: items = [(name, subtitle, Image)], each scaled by `scale`."""
    from PIL import ImageFont
    font = ImageFont.load_default()
    cell_w = max(i[2].width for i in items) * scale + pad * 2
    rows = (len(items) + cols - 1) // cols
    row_h = [max(i[2].height for i in items[r * cols:(r + 1) * cols]) * scale + pad * 2 + label_h * 2 for r in range(rows)]
    row_y = [sum(row_h[:r]) for r in range(rows)]
    out = Image.new("RGBA", (cell_w * cols, sum(row_h)), hexc(bg))
    d = ImageDraw.Draw(out)
    for n, (name, sub, img) in enumerate(items):
        cx, cy = (n % cols) * cell_w, row_y[n // cols]
        cell_h = row_h[n // cols]
        big = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
        ox = cx + (cell_w - big.width) // 2
        oy = cy + pad + (cell_h - pad * 2 - label_h * 2 - big.height)  # feet on a shared baseline
        out.alpha_composite(big, (ox, oy))
        d.text((cx + pad, cy + cell_h - label_h * 2 - 2), name, fill=(240, 230, 210, 255), font=font)
        d.text((cx + pad, cy + cell_h - label_h - 2), sub, fill=(150, 140, 170, 255), font=font)
    return out
