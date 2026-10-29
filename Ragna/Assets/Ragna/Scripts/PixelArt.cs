using System.Collections.Generic;
using UnityEngine;

namespace Ragna
{
    public enum PixelMotion { Idle, Run, Attack, Cast, Hurt, Death, Guard }
    // Original placeholder sprites. Integer pixels, shared textures, no external art required.
    public static class PixelArt
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => cache.Clear();
        static readonly Color Outline = new Color32(16, 21, 38, 255);
        static readonly Color Skin = new Color32(238, 185, 151, 255);
        static Texture2D Canvas(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels(new Color[w * h]); return t;
        }
        static void Rect(Texture2D t, int x, int y, int w, int h, Color c)
        { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) if (xx >= 0 && xx < t.width && yy >= 0 && yy < t.height) t.SetPixel(xx, yy, c); }
        static Sprite Finish(string key, Texture2D t)
        { t.Apply(); var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 16); s.name = key; cache[key] = s; return s; }
        // Cache finite, integer-pixel poses. Texture work happens once per pose, never every frame.
        public static Sprite Animate(Sprite source, PixelMotion motion, int frame, bool humanoid = true)
        {
            frame = Mathf.Clamp(frame, 0, 3);
            if (!source.texture.isReadable) return source; // Imported replacement art can use transform animation.
            string key = "pose:" + source.GetInstanceID() + ":" + motion + ":" + frame + ":" + humanoid;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var rect = source.rect; int w = (int)rect.width, h = (int)rect.height;
            var pixels = source.texture.GetPixels((int)rect.x, (int)rect.y, w, h);
            var t = Canvas(w + 64, h + 32);
            int stride = new[] { -2, 0, 2, 0 }[frame];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                Color c = pixels[y * w + x]; if (c.a == 0) continue;
                int xx = x, yy = y;
                if (motion == PixelMotion.Run)
                {
                    if (y < h / 4) { xx += x < w / 2 ? stride : -stride; yy += x < w / 2 ? Mathf.Max(0, stride) : Mathf.Max(0, -stride); }
                    else if (y < h * .62f) xx += stride / 2;
                    if (!humanoid) yy = Mathf.RoundToInt(y * (1 + stride * .035f));
                }
                else if (motion == PixelMotion.Idle && y > h / 3) yy += frame % 2;
                else if (motion == PixelMotion.Attack || motion == PixelMotion.Cast)
                {
                    xx += frame == 0 ? -1 : frame < 3 ? 2 : 0;
                    if (humanoid && x >= w * .75f)
                    {
                        float angle = (motion == PixelMotion.Cast ? new[] { -10f, 20f, 35f, 0f } : new[] { 30f, -35f, -85f, -15f })[frame] * Mathf.Deg2Rad;
                        float dx = x - w * .78f, dy = y - h * .42f;
                        xx = Mathf.RoundToInt(w * .78f + dx * Mathf.Cos(angle) - dy * Mathf.Sin(angle));
                        yy = Mathf.RoundToInt(h * .42f + dx * Mathf.Sin(angle) + dy * Mathf.Cos(angle));
                    }
                }
                else if (motion == PixelMotion.Hurt) { xx -= 2 + frame; c = Color.Lerp(c, new Color(1, .45f, .5f, c.a), .4f); }
                else if (motion == PixelMotion.Guard) { yy = Mathf.RoundToInt(y * .92f); xx -= y > h / 2 ? 1 : 0; }
                else if (motion == PixelMotion.Death)
                {
                    float angle = frame * 28 * Mathf.Deg2Rad;
                    float dx = x - w / 2f;
                    xx = Mathf.RoundToInt(w / 2f + dx * Mathf.Cos(angle) - y * Mathf.Sin(angle));
                    yy = Mathf.RoundToInt(dx * Mathf.Sin(angle) + y * Mathf.Cos(angle));
                    // Final frame rests on the ground instead of disappearing.
                    yy += frame * 3;
                }
                Rect(t, xx + 32, yy + 16, 1, 1, c);
            }
            return Finish(key, t);
        }
        public static Sprite Hero(Job job, int tier, int ascension = 0, int careerStyle = 0, Color? tint = null)
        {
            Color c = tint ?? JobData.Colors[(int)job];
            string key = "hero" + job + tier + "a" + ascension + "s" + careerStyle + ColorUtility.ToHtmlStringRGBA(c); if (cache.TryGetValue(key, out var s)) return s;
            var t = Canvas(32, 40); Color dark = Color.Lerp(c, Outline, .55f);
            // Boots, outlined tunic, face, hair, pauldrons, belt, hands.
            Rect(t, 9, 2, 5, 6, Outline); Rect(t, 18, 2, 5, 6, Outline);
            Rect(t, 9, 3, 4, 2, dark); Rect(t, 18, 3, 4, 2, dark);
            if (tier > 0) { Rect(t, 6, 8, 20, 17, Outline); Rect(t, 7, 9, 18, 14, dark); Rect(t, 7, 9, 2, 13, c); }
            Rect(t, 10, 7, 12, 17, Outline); Rect(t, 11, 8, 10, 15, dark); Rect(t, 12, 13, 8, 9, c);
            Rect(t, 12, 24, 9, 10, Outline); Rect(t, 13, 25, 7, 8, Skin);
            Rect(t, 12, 30, 10, 5, Outline); Rect(t, 13, 32, 7, 3, dark); Rect(t, 18, 29, 2, 2, Outline);
            // Right-facing profile: nose and forward eye, with hair trailing to the left.
            Rect(t, 20, 26, 3, 3, Skin); Rect(t, 19, 29, 2, 2, Outline);
            Rect(t, 11, 26, 3, 7, dark);
            Rect(t, 7, 17, 5, 8, Outline); Rect(t, 8, 18, 4, 6, c); Rect(t, 8, 15, 3, 4, Skin);
            Rect(t, 21, 17, 5, 8, Outline); Rect(t, 22, 18, 3, 6, c); Rect(t, 23, 15, 3, 4, Skin);
            Rect(t, 11, 11, 10, 2, new Color32(244, 202, 116, 255));
            if (tier >= 1) { Rect(t, 11, 33, 11, 2, c); Rect(t, 9, 22, 5, 3, Color.white); Rect(t, 20, 22, 5, 3, Color.white); }
            if (tier >= 2) { Rect(t, 11, 35, 3, 3, c); Rect(t, 19, 35, 3, 3, c); Rect(t, 14, 19, 4, 3, Color.white); }
            if (tier >= 3) { Rect(t, 8, 35, 4, 3, new Color32(255, 212, 116, 255)); Rect(t, 22, 35, 4, 3, new Color32(255, 212, 116, 255)); Rect(t, 15, 35, 3, 5, Color.white); }
            if (ascension > 0)
            {
                Color gold = new Color32(255, 211, 114, 255);
                Rect(t, 10, 9, 1, 14, gold); Rect(t, 21, 9, 1, 14, gold);
                Rect(t, 15, 19, 3, 3, gold);
                if (ascension >= 3) { Rect(t, 6, 25, 5, 2, gold); Rect(t, 22, 25, 5, 2, gold); }
            }
            // Sword silhouettes for the current route.
            switch (job)
            {
                case Job.Warrior: Rect(t, 26, 10, 2, 20 + tier * 2, Outline); Rect(t, 26, 17, 2, 12 + tier * 2, Color.white); Rect(t, 23, 15, 8, 2, c); break;
            }
            if (careerStyle == 1)
            {
                Rect(t, 3, 10, 9, 14, Outline); Rect(t, 4, 11, 7, 12, c);
                Rect(t, 7, 11, 1, 12, Color.white); Rect(t, 4, 17, 7, 1, Color.white);
            }
            else if (careerStyle == 2)
            {
                Rect(t, 24, 14, 7, 24, Outline); Rect(t, 25, 18, 5, 19, c);
                Rect(t, 26, 19, 2, 17, Color.white); Rect(t, 22, 15, 10, 3, c);
                Rect(t, 12, 33, 10, 2, new Color32(246, 132, 104, 255));
            }
            return Finish(key, t);
        }
        public static Sprite Enemy(int kind)
        {
            string key = "enemy" + kind; if (cache.TryGetValue(key, out var s)) return s;
            var t = Canvas(kind == 3 ? 48 : 32, kind == 3 ? 48 : 32);
            if (kind == 0)
            {
                Rect(t, 5, 4, 23, 12, Outline); Rect(t, 8, 15, 17, 6, Outline);
                Rect(t, 7, 5, 19, 11, new Color32(79, 164, 153, 255)); Rect(t, 10, 15, 13, 4, new Color32(124, 210, 166, 255));
                Rect(t, 11, 11, 3, 4, Outline); Rect(t, 20, 11, 3, 4, Outline); Rect(t, 10, 16, 5, 2, Color.white);
            }
            else if (kind == 1)
            {
                Rect(t, 12, 5, 10, 21, Outline); Rect(t, 13, 6, 8, 19, new Color32(211, 204, 183, 255));
                Rect(t, 10, 22, 14, 9, Outline); Rect(t, 11, 23, 12, 7, new Color32(239, 229, 199, 255));
                Rect(t, 12, 24, 3, 3, Outline); Rect(t, 19, 24, 3, 3, Outline);
                Rect(t, 7, 10, 4, 12, Outline); Rect(t, 24, 8, 3, 19, new Color32(179, 131, 140, 255)); Rect(t, 12, 14, 9, 2, Outline);
            }
            else if (kind == 2)
            {
                Rect(t, 12, 8, 9, 16, Outline); Rect(t, 13, 9, 7, 14, new Color32(143, 99, 164, 255));
                Rect(t, 2, 16, 10, 9, Outline); Rect(t, 21, 16, 10, 9, Outline);
                Rect(t, 3, 19, 9, 5, new Color32(109, 78, 135, 255)); Rect(t, 21, 19, 9, 5, new Color32(109, 78, 135, 255));
                Rect(t, 14, 19, 2, 2, Color.red); Rect(t, 18, 19, 2, 2, Color.red);
            }
            else
            {
                Color armor = new Color32(108, 78, 132, 255), gold = new Color32(228, 173, 113, 255);
                Rect(t, 10, 3, 10, 9, Outline); Rect(t, 28, 3, 10, 9, Outline);
                Rect(t, 9, 10, 30, 27, Outline); Rect(t, 11, 11, 26, 23, armor);
                Rect(t, 17, 32, 16, 12, Outline); Rect(t, 19, 33, 12, 9, armor);
                Rect(t, 17, 42, 4, 6, gold); Rect(t, 29, 42, 4, 6, gold);
                Rect(t, 20, 36, 4, 2, Color.red); Rect(t, 27, 36, 4, 2, Color.red);
                Rect(t, 3, 23, 10, 13, Outline); Rect(t, 35, 23, 10, 13, Outline);
                Rect(t, 4, 26, 9, 8, gold); Rect(t, 35, 26, 9, 8, gold);
                Rect(t, 22, 18, 5, 10, gold); Rect(t, 18, 21, 13, 4, gold);
                Rect(t, 42, 4, 3, 31, Color.white); Rect(t, 38, 11, 10, 3, gold);
            }
            return Finish(key, t);
        }
        public static Sprite Shape(string shape)
        {
            if (cache.TryGetValue(shape, out var s)) return s;
            var t = Canvas(64, 64);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                bool fill = shape == "arrow" ? (Mathf.Abs(dy) < 2 && x > 4 && x < 52) || (x > 42 && Mathf.Abs(dy) < (60 - x) * .6f) || (x < 16 && Mathf.Abs(dy) < 7 && Mathf.Abs(dy) > 3) :
                    shape == "bullet" ? dx * dx / 800 + dy * dy / 30 < 1 :
                    shape == "meteor" ? (dx - 10) * (dx - 10) + (dy + 8) * (dy + 8) < 180 || (x < 42 && y > 24 && Mathf.Abs(dx + dy) < 9 && x + y < 75) :
                    shape == "fist" ? (x > 14 && x < 52 && y > 16 && y < 43) || (x > 8 && x < 35 && y > 24 && y < 49) :
                    shape == "cross" ? (Mathf.Abs(dx) < 4 && Mathf.Abs(dy) < 28) || (Mathf.Abs(dy - 6) < 4 && Mathf.Abs(dx) < 20) :
                    shape == "dagger" ? (x > 18 && Mathf.Abs(dy) < (61 - x) * .18f) || (x > 14 && x < 19 && Mathf.Abs(dy) < 12) || (x > 3 && x < 15 && Mathf.Abs(dy) < 2) :
                    shape == "rune" ? (d < 29 && d > 26) || (d < 20 && d > 18) || (Mathf.Abs(dx) < 2 && Mathf.Abs(dy) < 28) || (Mathf.Abs(dy) < 2 && Mathf.Abs(dx) < 28) :
                    shape == "ring" ? d < 30 && d > 27 : shape == "slash" ? d < 30 && d > 23 && x > 15 && y > 15 :
                    shape == "shadow" ? dx * dx / 900 + dy * dy / 100 < 1 : shape == "spark" ? Mathf.Abs(dx) + Mathf.Abs(dy) < 28 : true;
                if (fill) t.SetPixel(x, y, Color.white);
            }
            return Finish(shape, t);
        }
        public static Sprite Tile(int style)
        {
            string key = "tile" + style; if (cache.TryGetValue(key, out var s)) return s;
            var t = Canvas(16, 16);
            Color baseColor = style == 0 ? new Color32(34, 47, 58, 255) : style == 1 ? new Color32(46, 39, 54, 255) : new Color32(34, 31, 54, 255);
            Rect(t, 0, 0, 16, 16, baseColor); Rect(t, 0, 0, 16, 1, Color.Lerp(baseColor, Color.black, .25f));
            Rect(t, 0, 0, 1, 16, Color.Lerp(baseColor, Color.black, .2f));
            Rect(t, 2, 14, 12, 1, Color.Lerp(baseColor, Color.white, .08f));
            Rect(t, 11, 7, 2, 1, Color.Lerp(baseColor, Color.white, .04f));
            return Finish(key, t);
        }
    }
}
