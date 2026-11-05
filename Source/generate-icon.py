from pathlib import Path
from PIL import Image, ImageDraw

# Original geometric artwork, generated locally; no third-party icon assets.
out = Path(__file__).resolve().parent.parent
n = 1024
im = Image.new('RGBA', (n, n))
pixels = im.load()
for y in range(n):
    for x in range(n):
        t = (x + y) / (2 * n)
        glow = max(0, 1 - ((x-220)**2 + (y-100)**2)**.5 / 900)
        pixels[x, y] = (int(24 + 30*t + 18*glow), int(102 - 32*t + 60*glow), int(228 + 15*t), 255)
mask = Image.new('L', (n,n)); d = ImageDraw.Draw(mask)
d.rounded_rectangle((32,32,992,992), radius=225, fill=255)
im.putalpha(mask); d = ImageDraw.Draw(im)
d.rounded_rectangle((190,285,805,685), radius=62, outline='white', width=45)
d.rounded_rectangle((463,680,535,775), radius=18, fill='white')
d.rounded_rectangle((350,765,650,808), radius=20, fill='white')
d.rounded_rectangle((260,585,735,615), radius=15, fill=(162,239,255,255))
cx,cy,r,inner = 720,285,165,45
d.polygon([(cx,cy-r),(cx+inner,cy-inner),(cx+r,cy),(cx+inner,cy+inner),(cx,cy+r),(cx-inner,cy+inner),(cx-r,cy),(cx-inner,cy-inner)], fill=(205,255,242,255))
d.rounded_rectangle((295,380,465,417), radius=15, fill='white')
d.rounded_rectangle((295,450,560,481), radius=15, fill=(162,239,255,255))
im.save(out/'PurePC.png')
im.save(out/'PurePC.ico', sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
