"""Assemble Unity-exported runtime poses into a review GIF; no generated art changes."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
gallery = root / "Docs" / "SkillGallery"
font = ImageFont.truetype(str(root / "Ragna/Assets/Ragna/Fonts/NanumGothic-Regular.ttf"), 23)
states = [("Idle", "대기"), ("Run", "이동"), ("Attack", "공격"), ("Cast", "시전"),
          ("Hurt", "피격"), ("Death", "사망"), ("Guard", "보호막")]
frames = []
for phase in range(4):
    canvas = Image.new("RGB", (720, 660), "#0e1522")
    draw = ImageDraw.Draw(canvas)
    for index, (state, label) in enumerate(states):
        strip = Image.open(gallery / f"animation-{state}.png").convert("RGBA")
        width = strip.width // 4
        pose = strip.crop((phase * width, 0, (phase + 1) * width, strip.height))
        pose.thumbnail((240, 180), Image.Resampling.NEAREST)
        x, y = (index % 3) * 240, (index // 3) * 220
        draw.text((x + 20, y + 8), label, font=font, fill="#e8c07c")
        canvas.paste(pose, (x + (240 - pose.width) // 2, y + 30), pose)
    frames.append(canvas)
frames[0].save(gallery / "character-animations.gif", save_all=True, append_images=frames[1:],
               duration=170, loop=0, disposal=2)
