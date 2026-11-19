"""Rough placeholder fish sprites for GHOTI. Usage: python fishart.py <out_dir> [sheet.png]"""
import math, os, sys
from PIL import Image, ImageDraw, ImageFilter

S = 1024          # draw size (supersampled)
OUT_SIZE = 256
STROKE = 22       # outline px at draw size
OUTLINE = (40, 32, 56, 255)


PAD = 0.07        # keep every fin inside the image


def T(v):
    return (PAD + v * (1 - 2 * PAD)) * S


def P(x, y):
    return (T(x), T(y))


def box(cx, cy, rx, ry):
    k = 1 - 2 * PAD
    return [T(cx) - rx * k * S, T(cy) - ry * k * S, T(cx) + rx * k * S, T(cy) + ry * k * S]


class Fish:
    """Collects silhouette shapes (get one shared outer outline) + detail layers."""

    def __init__(self):
        self.shapes = []   # (kind, geom, color)
        self.details = []  # callables(draw, img)

    def ell(self, cx, cy, rx, ry, color):
        self.shapes.append(("e", box(cx, cy, rx, ry), color))
        return self

    def poly(self, pts, color):
        self.shapes.append(("p", [P(x, y) for x, y in pts], color))
        return self

    def detail(self, fn):
        self.details.append(fn)
        return self

    def render(self):
        img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        # pass 1: outline silhouette (shapes grown by STROKE)
        for kind, g, _ in self.shapes:
            if kind == "e":
                x0, y0, x1, y1 = g
                d.ellipse([x0 - STROKE, y0 - STROKE, x1 + STROKE, y1 + STROKE], fill=OUTLINE)
            else:
                d.polygon(g, fill=OUTLINE)
                d.line(g + [g[0]], fill=OUTLINE, width=STROKE * 2, joint="curve")
                for p in g:
                    d.ellipse([p[0] - STROKE, p[1] - STROKE, p[0] + STROKE, p[1] + STROKE], fill=OUTLINE)
        # pass 2: fills
        for kind, g, c in self.shapes:
            if kind == "e":
                d.ellipse(g, fill=c)
            else:
                d.polygon(g, fill=c)
        for fn in self.details:
            fn(img)
        return img.resize((OUT_SIZE, OUT_SIZE), Image.LANCZOS)


# ---------------------------------------------------------------- detail helpers

def overlay(img, draw_fn, mask_box=None, blur=0):
    """Draw on a transparent layer, optionally clip to an ellipse, then composite."""
    layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(layer))
    if blur:
        layer = layer.filter(ImageFilter.GaussianBlur(blur))
    if mask_box is not None:
        m = Image.new("L", (S, S), 0)
        ImageDraw.Draw(m).ellipse(mask_box, fill=255)
        a = Image.composite(layer.getchannel("A"), Image.new("L", (S, S), 0), m)
        layer.putalpha(a)
    img.alpha_composite(layer)


def shade(cx, cy, rx, ry):
    """Soft highlight on top + darker belly, clipped to body ellipse."""
    b = box(cx, cy, rx, ry)

    def fn(img):
        overlay(img, lambda d: d.ellipse(box(cx, cy + ry * 0.55, rx * 1.1, ry * 0.6), fill=(0, 0, 0, 45)), b, blur=30)
        overlay(img, lambda d: d.ellipse(box(cx - rx * 0.25, cy - ry * 0.5, rx * 0.45, ry * 0.25), fill=(255, 255, 255, 110)), b, blur=18)
    return fn


def eye(x, y, r=0.065, look=(0.012, 0.0)):
    def fn(img):
        d = ImageDraw.Draw(img)
        d.ellipse(box(x, y, r + 0.012, r + 0.012), fill=OUTLINE)
        d.ellipse(box(x, y, r, r), fill=(255, 255, 255, 255))
        pr = r * 0.62
        d.ellipse(box(x + look[0], y + look[1], pr, pr), fill=(30, 24, 40, 255))
        d.ellipse(box(x + look[0] - pr * 0.35, y + look[1] - pr * 0.4, pr * 0.32, pr * 0.32), fill=(255, 255, 255, 255))
    return fn


def smile(x, y, w=0.05):
    def fn(img):
        d = ImageDraw.Draw(img)
        d.arc(box(x, y, w, w * 0.8), 20, 160, fill=OUTLINE, width=14)
    return fn


def blush(x, y):
    return lambda img: overlay(img, lambda d: d.ellipse(box(x, y, 0.04, 0.025), fill=(255, 110, 130, 140)), blur=6)


def stripes(body, xs, width, color, curve=0.0):
    cx, cy, rx, ry = body

    def fn(img):
        def dr(d):
            for x in xs:
                pts = []
                for i in range(21):
                    t = i / 20
                    yy = cy - ry * 1.2 + t * ry * 2.4
                    bend = math.sin(t * math.pi) * curve
                    pts.append(P(x + bend, yy))
                d.line(pts, fill=OUTLINE, width=int(width * S) + STROKE)
                d.line(pts, fill=color, width=int(width * S))
        overlay(img, dr, box(cx, cy, rx, ry))
    return fn


def spots(body, pts, color, outline=False):
    cx, cy, rx, ry = body

    def fn(img):
        def dr(d):
            for x, y, r in pts:
                if outline:
                    d.ellipse(box(x, y, r + 0.01, r + 0.01), fill=OUTLINE)
                d.ellipse(box(x, y, r, r), fill=color)
        overlay(img, dr, box(cx, cy, rx, ry))
    return fn


def scales(body, color, step=0.07):
    cx, cy, rx, ry = body

    def fn(img):
        def dr(d):
            y = cy - ry
            row = 0
            while y < cy + ry:
                x = cx - rx + (step / 2 if row % 2 else 0)
                while x < cx + rx * 0.55:
                    d.arc(box(x, y, step * 0.55, step * 0.5), 200, 340, fill=color, width=9)
                    x += step
                y += step * 0.6
                row += 1
        overlay(img, dr, box(cx, cy, rx, ry))
    return fn


def whiskers(x, y, length=0.16, droop=0.08):
    def fn(img):
        d = ImageDraw.Draw(img)
        for dy, k in ((0.0, 1), (0.03, 1.3)):
            pts = [P(x, y + dy), P(x + length * 0.5, y + dy + droop * 0.4 * k), P(x + length * 0.8, y + dy + droop * k)]
            d.line(pts, fill=OUTLINE, width=14, joint="curve")
    return fn


def bolt(x, y, s, color):
    def fn(img):
        pts = [(0, -1), (0.45, -1), (0.1, -0.1), (0.5, -0.1), (-0.2, 1.1), (0.05, 0.15), (-0.35, 0.15)]
        pp = [P(x + px * s, y + py * s) for px, py in pts]
        d = ImageDraw.Draw(img)
        d.polygon(pp, fill=color, outline=OUTLINE, width=10)
    return fn


# ---------------------------------------------------------------- generic fish

def basic(body_c, fin_c, cx=0.55, cy=0.5, rx=0.34, ry=0.3, tail="fork", tail_len=0.22, dorsal=True):
    f = Fish()
    tx = cx - rx + 0.03
    if tail == "fork":
        f.poly([(tx + 0.04, cy), (tx - tail_len, cy - 0.17), (tx - tail_len * 0.6, cy), (tx - tail_len, cy + 0.17)], fin_c)
    elif tail == "fan":
        f.poly([(tx + 0.05, cy), (tx - tail_len, cy - 0.24), (tx - tail_len * 1.1, cy), (tx - tail_len, cy + 0.24)], fin_c)
        f.ell(tx - tail_len * 0.75, cy, tail_len * 0.45, 0.22, fin_c)
    elif tail == "round":
        f.ell(tx - tail_len * 0.4, cy, tail_len * 0.6, 0.15, fin_c)
    if dorsal:
        f.poly([(cx - rx * 0.4, cy - ry * 0.8), (cx - rx * 0.1, cy - ry - 0.1), (cx + rx * 0.3, cy - ry * 0.85)], fin_c)
    f.poly([(cx - rx * 0.1, cy + ry * 0.85), (cx - rx * 0.3, cy + ry + 0.07), (cx + rx * 0.25, cy + ry * 0.8)], fin_c)
    f.ell(cx, cy, rx, ry, body_c)
    body = (cx, cy, rx, ry)
    return f, body


def face(f, body, eye_r=0.065):
    cx, cy, rx, ry = body
    ex, ey = cx + rx * 0.5, cy - ry * 0.2
    f.detail(shade(*body))
    f.detail(lambda img: None)
    return ex, ey, eye_r


def finish(f, body, eye_r=0.065, blushy=True, mouth=True):
    cx, cy, rx, ry = body
    ex, ey = cx + rx * 0.52, cy - ry * 0.22
    f.detail(eye(ex, ey, eye_r))
    if blushy:
        f.detail(blush(ex - 0.01, ey + eye_r + 0.05))
    if mouth:
        f.detail(smile(cx + rx * 0.82, cy + ry * 0.12, 0.035))
    return f


def C(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


# ---------------------------------------------------------------- the 20 FIH

def guppy():
    f, b = basic(C("ffb347"), C("ff5fa2"), cx=0.6, rx=0.28, ry=0.24, tail="fan", tail_len=0.26)
    f.detail(shade(*b))
    f.detail(spots(b, [(0.5, 0.45, 0.03), (0.58, 0.56, 0.025), (0.45, 0.58, 0.02)], C("ffd27f")))
    return finish(f, b, 0.07)


def clownfish():
    f, b = basic(C("ff7a1a"), C("ff7a1a"), tail="round", tail_len=0.2)
    f.detail(shade(*b))
    f.detail(stripes(b, [0.4, 0.62], 0.07, C("ffffff"), curve=0.03))
    return finish(f, b)


def catfish():
    f, b = basic(C("8b8f9c"), C("6d7180"), rx=0.37, ry=0.27, tail="round", tail_len=0.18)
    f.detail(shade(*b))
    f.detail(spots(b, [(0.4, 0.42, 0.025), (0.5, 0.38, 0.02), (0.36, 0.55, 0.02)], C("6d7180")))
    finish(f, b, 0.055, blushy=False)
    return f.detail(whiskers(0.86, 0.55))


def goldfish():
    f, b = basic(C("ffc21a"), C("ff8c1a"), rx=0.32, ry=0.31, tail="fan", tail_len=0.24)
    f.detail(shade(*b))
    f.detail(scales(b, (255, 150, 30, 160), 0.08))
    return finish(f, b)


def pufferfish():
    f = Fish()
    cx, cy, r = 0.52, 0.52, 0.36
    for i in range(14):
        a = i / 14 * math.tau
        x, y = cx + math.cos(a) * r, cy + math.sin(a) * r
        x2, y2 = cx + math.cos(a) * (r + 0.09), cy + math.sin(a) * (r + 0.09)
        n = (-math.sin(a) * 0.035, math.cos(a) * 0.035)
        f.poly([(x + n[0], y + n[1]), (x2, y2), (x - n[0], y - n[1])], C("e8b23a"))
    f.poly([(0.18, 0.52), (0.07, 0.4), (0.07, 0.64)], C("f0c24a"))
    f.ell(cx, cy, r, r, C("ffd95a"))
    b = (cx, cy, r, r)
    f.detail(shade(*b))
    f.detail(spots(b, [(0.4, 0.32, 0.03), (0.52, 0.28, 0.025), (0.33, 0.45, 0.025)], C("c9962a")))
    f.detail(lambda img: overlay(img, lambda d: d.ellipse(box(0.55, 0.7, 0.25, 0.15), fill=(255, 255, 235, 200)), box(cx, cy, r, r)))
    return finish(f, b, 0.07)


def jellyfish():
    f = Fish()
    for i, x in enumerate([0.3, 0.42, 0.54, 0.66]):
        pts = []
        for k in range(9):
            t = k / 8
            pts.append((x + math.sin(t * 5 + i) * 0.03 - 0.018, 0.55 + t * 0.35))
        for k in range(8, -1, -1):
            t = k / 8
            pts.append((x + math.sin(t * 5 + i) * 0.03 + 0.018, 0.55 + t * 0.35))
        f.poly(pts, C("ffb3e6"))
    f.poly([(0.14, 0.58)] + [(0.5 - 0.36 * math.cos(a / 20 * math.pi), 0.5 - 0.36 * math.sin(a / 20 * math.pi)) for a in range(21)] + [(0.86, 0.58)], C("ff8fd8"))
    b = (0.5, 0.5, 0.36, 0.36)
    f.detail(lambda img: overlay(img, lambda d: d.ellipse(box(0.38, 0.3, 0.12, 0.06), fill=(255, 255, 255, 120)), blur=16))
    f.detail(eye(0.4, 0.44, 0.055))
    f.detail(eye(0.6, 0.44, 0.055))
    f.detail(smile(0.5, 0.48, 0.03))
    f.detail(blush(0.33, 0.53))
    return f.detail(blush(0.67, 0.53))


def tilapia():
    f, b = basic(C("9fb59a"), C("7d9478"), rx=0.35, ry=0.29)
    f.detail(shade(*b))
    f.detail(stripes(b, [0.36, 0.46, 0.56], 0.035, C("7d9478")))
    return finish(f, b)


def angelfish():
    f = Fish()
    cx, cy = 0.55, 0.5
    f.poly([(0.5, 0.3), (0.42, 0.04), (0.62, 0.28)], C("e8e8f0"))
    f.poly([(0.5, 0.7), (0.42, 0.96), (0.62, 0.72)], C("e8e8f0"))
    f.poly([(0.32, 0.5), (0.12, 0.34), (0.16, 0.5), (0.12, 0.66)], C("e8e8f0"))
    f.ell(cx, cy, 0.26, 0.3, C("f4f4fa"))
    b = (cx, cy, 0.26, 0.3)
    f.detail(shade(*b))
    f.detail(stripes(b, [0.45, 0.6], 0.045, C("3a3550")))
    return finish(f, b, 0.06)


def octopus():
    f = Fish()
    for i in range(5):
        x = 0.24 + i * 0.13
        f.poly([(x - 0.05, 0.6), (x - 0.03 + (i - 2) * 0.03, 0.9), (x + 0.03 + (i - 2) * 0.03, 0.92), (x + 0.05, 0.6)], C("b57ce0"))
    f.ell(0.5, 0.45, 0.33, 0.32, C("c38bf0"))
    b = (0.5, 0.45, 0.33, 0.32)
    f.detail(shade(*b))
    f.detail(spots(b, [(0.36, 0.28, 0.035), (0.6, 0.24, 0.03), (0.68, 0.36, 0.025)], C("a46ed0")))
    f.detail(eye(0.4, 0.5, 0.06))
    f.detail(eye(0.6, 0.5, 0.06))
    f.detail(smile(0.5, 0.58, 0.03))
    return f


def snakehead():
    f, b = basic(C("4f6b3a"), C("3d542c"), cx=0.53, rx=0.4, ry=0.24, tail="round", tail_len=0.14, dorsal=False)
    f.poly([(0.25, 0.3), (0.5, 0.2), (0.75, 0.3)], C("3d542c"))
    f.ell(0.53, 0.5, 0.4, 0.24, C("4f6b3a"))
    f.detail(shade(*b))
    f.detail(spots(b, [(0.3, 0.52, 0.045), (0.45, 0.58, 0.05), (0.6, 0.55, 0.04), (0.38, 0.42, 0.03)], C("2c3d20")))
    finish(f, b, 0.05, blushy=False)
    return f


def swordfish():
    f, b = basic(C("3f7fd1"), C("2c5fa8"), cx=0.48, rx=0.3, ry=0.25)
    f.poly([(0.74, 0.46), (0.98, 0.5), (0.74, 0.52)], C("9cc0ee"))
    f.ell(0.48, 0.5, 0.3, 0.25, C("3f7fd1"))
    f.detail(shade(*b))
    f.detail(lambda img: overlay(img, lambda d: d.ellipse(box(0.5, 0.62, 0.3, 0.12), fill=(220, 235, 255, 220)), box(*b)))
    return finish(f, b, 0.055)


def arowana():
    f, b = basic(C("f2c94c"), C("d9a520"), cx=0.53, rx=0.4, ry=0.24, tail="round", tail_len=0.16)
    f.detail(shade(*b))
    f.detail(scales(b, (190, 120, 10, 200), 0.075))
    f.detail(lambda img: overlay(img, lambda d: d.ellipse(box(0.45, 0.4, 0.2, 0.05), fill=(255, 255, 220, 140)), blur=10))
    finish(f, b, 0.05, blushy=False)
    return f.detail(whiskers(0.9, 0.42, 0.08, -0.06))


def mekong():
    f, b = basic(C("7a8090"), C("5c6170"), cx=0.53, rx=0.4, ry=0.31, tail="fork", tail_len=0.14)
    f.detail(shade(*b))
    # legendary crown
    f.detail(lambda img: ImageDraw.Draw(img).polygon(
        [P(*p) for p in [(0.5, 0.2), (0.54, 0.08), (0.58, 0.16), (0.62, 0.06), (0.66, 0.16), (0.7, 0.08), (0.74, 0.2)]],
        fill=C("ffd34d"), outline=OUTLINE, width=12))
    finish(f, b, 0.05, blushy=False)
    return f.detail(whiskers(0.86, 0.55, 0.12, 0.1))


def sardine():
    f, b = basic(C("a9c4dc"), C("7f9fbc"), cx=0.55, rx=0.36, ry=0.2, tail="fork", tail_len=0.18)
    f.detail(shade(*b))
    f.detail(lambda img: overlay(img, lambda d: d.rectangle([0, T(0.42), S, T(0.47)], fill=C("4f6f95")), box(*b)))
    f.detail(spots(b, [(0.45, 0.4, 0.02), (0.53, 0.4, 0.02), (0.61, 0.4, 0.02)], C("4f6f95")))
    return finish(f, b, 0.05)


def betta():
    f = Fish()
    cx, cy = 0.58, 0.5
    f.poly([(0.42, 0.45), (0.06, 0.15), (0.0, 0.5), (0.06, 0.85), (0.42, 0.55)], C("3f5fe0"))
    f.poly([(0.42, 0.35), (0.45, 0.08), (0.7, 0.32)], C("d6304a"))
    f.poly([(0.42, 0.65), (0.4, 0.95), (0.7, 0.68)], C("d6304a"))
    f.ell(cx, cy, 0.25, 0.2, C("e8394f"))
    b = (cx, cy, 0.25, 0.2)
    f.detail(shade(*b))
    f.detail(lambda img: overlay(img, lambda d: [d.line([P(0.4, 0.5), P(0.08, 0.5 + k)], fill=(255, 255, 255, 60), width=10) for k in (-0.25, -0.12, 0, 0.12, 0.25)]))
    return finish(f, b, 0.055)


def seahorse():
    f = Fish()
    f.ell(0.5, 0.62, 0.16, 0.22, C("ff9a3c"))                      # belly
    f.ell(0.52, 0.28, 0.13, 0.12, C("ff9a3c"))                     # head
    f.poly([(0.6, 0.26), (0.8, 0.3), (0.8, 0.36), (0.6, 0.34)], C("ff9a3c"))  # snout
    f.poly([(0.42, 0.2), (0.44, 0.08), (0.5, 0.17), (0.54, 0.07), (0.58, 0.17)], C("e0782a"))  # crest
    pts = [(0.4, 0.78), (0.36, 0.9), (0.46, 0.96), (0.52, 0.9), (0.46, 0.88), (0.48, 0.8)]
    f.poly(pts, C("ff9a3c"))                                        # curled tail
    f.poly([(0.34, 0.5), (0.22, 0.56), (0.34, 0.66)], C("ffc48a"))  # back fin
    b = (0.5, 0.62, 0.16, 0.22)
    f.detail(shade(*b))
    f.detail(lambda img: overlay(img, lambda d: [d.arc(box(0.54, 0.45 + k * 0.07, 0.1, 0.03), 0, 180, fill=C("e0782a"), width=10) for k in range(5)], box(*b)))
    f.detail(eye(0.55, 0.26, 0.05))
    return f.detail(blush(0.52, 0.35))


def koi():
    f, b = basic(C("fff8f0"), C("ffe7d6"), rx=0.37, ry=0.27, tail="fan", tail_len=0.18)
    f.detail(shade(*b))
    f.detail(spots(b, [(0.48, 0.38, 0.11), (0.7, 0.5, 0.07), (0.38, 0.6, 0.06)], C("e8402a")))
    f.detail(spots(b, [(0.58, 0.62, 0.04)], C("2a2230")))
    finish(f, b, 0.055, blushy=False)
    return f.detail(whiskers(0.88, 0.52, 0.07, 0.05))


def manta():
    f = Fish()
    f.poly([(0.5, 0.3), (0.06, 0.62), (0.3, 0.6), (0.5, 0.74), (0.7, 0.6), (0.94, 0.62)], C("3a4f8a"))
    f.poly([(0.47, 0.7), (0.5, 0.96), (0.53, 0.7)], C("2c3c6e"))
    f.poly([(0.38, 0.36), (0.36, 0.24), (0.44, 0.32)], C("3a4f8a"))
    f.poly([(0.62, 0.36), (0.64, 0.24), (0.56, 0.32)], C("3a4f8a"))
    f.ell(0.5, 0.48, 0.17, 0.17, C("3a4f8a"))
    f.detail(lambda img: overlay(img, lambda d: d.ellipse(box(0.5, 0.56, 0.12, 0.07), fill=(230, 240, 255, 180)), blur=6))
    f.detail(eye(0.42, 0.44, 0.045))
    f.detail(eye(0.58, 0.44, 0.045))
    return f.detail(smile(0.5, 0.5, 0.03))


def eel():
    f = Fish()
    pts_top, pts_bot = [], []
    for i in range(13):
        t = i / 12
        x = 0.1 + t * 0.78
        y = 0.5 + math.sin(t * math.tau * 0.9) * 0.12
        w = 0.07 + 0.06 * math.sin(t * math.pi) + 0.03 * t
        pts_top.append((x, y - w))
        pts_bot.append((x, y + w))
    f.poly(pts_top + pts_bot[::-1], C("e6d84a"))
    f.ell(0.84, 0.47, 0.11, 0.1, C("e6d84a"))
    f.detail(lambda img: overlay(img, lambda d: d.line([P(0.1 + i / 12 * 0.78, 0.5 + math.sin(i / 12 * math.tau * 0.9) * 0.12 + 0.05) for i in range(13)], fill=(150, 150, 40, 200), width=18), blur=4))
    f.detail(bolt(0.5, 0.2, 0.1, C("fff36b")))
    f.detail(bolt(0.25, 0.78, 0.07, C("fff36b")))
    f.detail(eye(0.86, 0.43, 0.045))
    return f.detail(smile(0.92, 0.5, 0.025))


def whaleshark():
    f, b = basic(C("3c5f8f"), C("2f4b73"), cx=0.54, rx=0.4, ry=0.3, tail="fork", tail_len=0.13)
    f.detail(shade(*b))
    f.detail(lambda img: overlay(img, lambda d: d.ellipse(box(0.6, 0.72, 0.38, 0.14), fill=(230, 240, 250, 230)), box(*b)))
    f.detail(spots(b, [(x, y, 0.022) for x, y in [(0.3, 0.42), (0.38, 0.35), (0.46, 0.42), (0.54, 0.33), (0.4, 0.5), (0.5, 0.52),
                                                     (0.58, 0.45), (0.33, 0.56), (0.62, 0.36), (0.25, 0.5), (0.47, 0.28)]], C("f2f6ff")))
    # legendary crown
    f.detail(lambda img: ImageDraw.Draw(img).polygon(
        [P(*p) for p in [(0.48, 0.2), (0.52, 0.06), (0.57, 0.15), (0.62, 0.04), (0.67, 0.15), (0.72, 0.06), (0.76, 0.2)]],
        fill=C("ffd34d"), outline=OUTLINE, width=12))
    return finish(f, b, 0.05)


FISH = [guppy, clownfish, catfish, goldfish, pufferfish, jellyfish, tilapia,
        angelfish, octopus, snakehead, swordfish, arowana, mekong,
        sardine, betta, seahorse, koi, manta, eel, whaleshark]


def main():
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    imgs = []
    for i, fn in enumerate(FISH, start=1):
        img = fn().render()
        img.save(os.path.join(out, f"Fish_{i}.png"))
        imgs.append(img)
    if len(sys.argv) > 2:
        sheet = Image.new("RGBA", (OUT_SIZE * 5, OUT_SIZE * 4), (190, 225, 245, 255))
        d = ImageDraw.Draw(sheet)
        for i, img in enumerate(imgs):
            x, y = (i % 5) * OUT_SIZE, (i // 5) * OUT_SIZE
            d.ellipse([x + 2, y + 2, x + OUT_SIZE - 2, y + OUT_SIZE - 2], outline=(255, 80, 80, 255), width=2)  # collider
            sheet.alpha_composite(img, (x, y))
            d.text((x + 6, y + 4), str(i + 1), fill=(0, 0, 0, 255))
        sheet.save(sys.argv[2])
    print("done", len(imgs))


if __name__ == "__main__":
    main()
