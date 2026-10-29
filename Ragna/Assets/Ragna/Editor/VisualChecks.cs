using System;
using System.IO;
using System.Text;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Ragna.Editor
{
    public static class VisualChecks
    {
        [CliCommand("ragna_visual_checks", "Check animated pixel poses and render Warrior skill stages", MainThreadRequired = true)]
        public static string Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            int checks = 0;
            void Check(bool condition, string name) { if (!condition) throw new Exception("Visual check failed: " + name); checks++; }
            foreach (Job job in Enum.GetValues(typeof(Job)))
            {
                var original = PixelArt.Hero(job, 3);
                var idle = PixelArt.Animate(original, PixelMotion.Idle, 0);
                foreach (PixelMotion motion in Enum.GetValues(typeof(PixelMotion)))
                {
                    var first = PixelArt.Animate(original, motion, 0);
                    var last = PixelArt.Animate(original, motion, 3);
                    Check(first.texture.filterMode == FilterMode.Point && last.pixelsPerUnit == 16, "pixel sampling " + job + motion);
                    Check(first == PixelArt.Animate(original, motion, 0), "pose cache " + job + motion);
                    if (motion == PixelMotion.Guard) continue;
                    Check(Hash(first) != Hash(last), "visible motion frames " + job + motion);
                }
            }
            string directory = Path.GetFullPath("../Docs/SkillGallery"); Directory.CreateDirectory(directory);
            // Export the exact runtime frames for a browser animation preview.
            var animationHtml = new StringBuilder("<!doctype html><meta charset='utf-8'><title>Ragna animation</title><style>body{background:#0e1522;color:#eee;font:18px sans-serif;padding:24px}.pose{width:288px;height:216px;background-repeat:no-repeat;background-size:1152px 216px;image-rendering:pixelated;animation:frames .65s steps(4) infinite;display:inline-block}@keyframes frames{to{background-position:-1152px 0}}</style><h1>캐릭터 프레임 애니메이션</h1><p>실제 게임 스프라이트 · 대기 / 이동 / 공격 / 시전 / 피격 / 사망 / 보호막</p>");
            foreach (PixelMotion motion in Enum.GetValues(typeof(PixelMotion)))
            {
                var original = PixelArt.Hero(Job.Warrior, 3, careerStyle: 1);
                bool authored = ReferenceHero.TryFrame(Job.Warrior, motion, 0, out var first);
                if (!authored) first = PixelArt.Animate(original, motion, 0);
                int width = authored ? (int)first.rect.width : first.texture.width;
                int height = authored ? (int)first.rect.height : first.texture.height;
                if (authored) for (int i = 0; i < 4; i++)
                    if (ReferenceHero.TryFrame(Job.Warrior, motion, i, out var candidate)) width = Mathf.Max(width, (int)candidate.rect.width);
                var strip = new Texture2D(width * 4, height, TextureFormat.RGBA32, false);
                for (int frame = 0; frame < 4; frame++)
                {
                    Sprite pose;
                    if (!ReferenceHero.TryFrame(Job.Warrior, motion, frame, out pose)) pose = PixelArt.Animate(original, motion, frame);
                    int poseWidth = authored ? (int)pose.rect.width : width;
                    var pixels = authored ? pose.texture.GetPixels((int)pose.rect.x, (int)pose.rect.y, poseWidth, height) : pose.texture.GetPixels();
                    strip.SetPixels(frame * width + (width - poseWidth) / 2, 0, poseWidth, height, pixels);
                    if (authored) Check(pose.texture.filterMode == FilterMode.Point && pose.pixelsPerUnit == 90, "authored frame " + motion + frame);
                }
                strip.Apply(); File.WriteAllBytes(Path.Combine(directory, "animation-" + motion + ".png"), strip.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(strip);
                animationHtml.Append("<h2>" + motion + "</h2><div class='pose' style=\"height:" + (288 * height / width) + "px;background-size:1152px " + (288 * height / width) + "px;background-image:url('animation-" + motion + ".png')\"></div>");
            }
            File.WriteAllText(Path.Combine(directory, "animations.html"), animationHtml.ToString());
            var game = UnityEngine.Object.FindFirstObjectByType<RagnaGame>();
            var camera = Camera.main;
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var target = new RenderTexture(960, 540, 24);
            var html = new StringBuilder("<!doctype html><meta charset='utf-8'><title>Ragna skill growth</title><style>body{background:#0e1522;color:#eee;font:18px sans-serif;margin:24px}section{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;max-width:1200px}img{width:100%;image-rendering:pixelated}figure{margin:0}figcaption{padding:8px}</style><h1>직업별 스킬 성장</h1><p>실제 Unity 카메라 렌더 · 왼쪽부터 스킬 LV.1 / 4 / 8 / 12. 저장과 분리한 미리보기.</p>");
            try
            {
                foreach (Job job in Enum.GetValues(typeof(Job)))
                {
                    if (!JobData.IsSpecialized(job)) continue;
                    html.Append("<h2>" + JobData.Names[(int)job] + " · " + JobData.Skills[(int)job] + "</h2><section>");
                    for (int tier = 0; tier < 4; tier++)
                    {
                        game.PreviewGrowth(tier, (int)job);
                        camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                        var shot = new Texture2D(960, 540, TextureFormat.RGB24, false);
                        shot.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); shot.Apply();
                        string filename = job + "-" + tier + ".png";
                        File.WriteAllBytes(Path.Combine(directory, filename), shot.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(shot);
                        var effects = GameObject.Find("BattleWorld").GetComponentsInChildren<SpriteRenderer>(true);
                        int effectCount = 0; foreach (var effect in effects) if (effect.name == "Effect" && effect.gameObject.activeSelf) effectCount++;
                        Check(effectCount > 0 && effectCount <= 100, "bounded visible skill " + job + tier);
                        html.Append("<figure><img src='" + filename + "'><figcaption>진화 " + (tier + 1) + "/4</figcaption></figure>");
                    }
                    html.Append("</section>");
                }
            }
            finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; UnityEngine.Object.DestroyImmediate(target); }
            File.WriteAllText(Path.Combine(directory, "index.html"), html.ToString());
            string result = "PASS: " + checks + " visual checks; 4 rendered Warrior skill stages. Pixel poses, cache reuse, distinct frame content and effect bounds verified.";
            File.WriteAllText("../Docs/VISUAL_CHECKS.txt", result); return result;
        }
        static int Hash(Sprite sprite)
        {
            unchecked { int value = 17; foreach (var pixel in sprite.texture.GetPixels32()) value = value * 31 + pixel.GetHashCode(); return value; }
        }
    }
}
