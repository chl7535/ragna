using System;
using System.IO;
using System.Text;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace Ragna.Editor
{
    public static class BackgroundChecks
    {
        [CliCommand("ragna_background_checks", "Validate six region panoramas and render previews", MainThreadRequired = true)]
        public static string Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            var game = UnityEngine.Object.FindFirstObjectByType<RagnaGame>(); var camera = Camera.main;
            int checks = 0;
            void Check(bool condition) { if (!condition) throw new Exception("Background framing check failed."); checks++; }
            string directory = Path.GetFullPath("../Docs/BackgroundGallery"); Directory.CreateDirectory(directory);
            var html = new StringBuilder("<!doctype html><meta charset='utf-8'><title>Ragna backgrounds</title><style>body{background:#111b27;color:#eee;font:20px sans-serif;padding:20px}main{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}img{width:100%}figure{margin:0}</style><h1>지역 배경 적용</h1><main>");
            var previous = camera.targetTexture; var active = RenderTexture.active;
            var target = new RenderTexture(480, 852, 24);
            try
            {
                for (int region = 0; region < 6; region++)
                {
                    int floor = region * 5 + 1;
                    Check(BackgroundCatalog.Index(floor) == region && BackgroundCatalog.Index(floor + 4) == region);
                    foreach (float aspect in new[] { .45f, .5625f, .75f })
                    {
                        camera.aspect = aspect; game.PreviewBackground(floor);
                        var image = GameObject.Find("RegionPanorama").GetComponent<SpriteRenderer>();
                        float half = camera.orthographicSize * aspect;
                        Check(image.bounds.min.x <= -half && image.bounds.max.x >= half);
                        Check(image.bounds.min.y <= camera.ViewportToWorldPoint(new Vector3(.5f, .285f, 10)).y &&
                            image.bounds.max.y >= camera.ViewportToWorldPoint(new Vector3(.5f, .795f, 10)).y);
                    }
                    camera.ResetAspect(); game.PreviewBackground(floor); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                    var shot = new Texture2D(480, 420, TextureFormat.RGB24, false); shot.ReadPixels(new Rect(0, 245, 480, 420), 0, 0); shot.Apply();
                    string name = BackgroundCatalog.Paths[region] + ".png";
                    File.WriteAllBytes(Path.Combine(directory, name), shot.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(shot);
                    html.Append("<figure><img src='" + name + "'><figcaption>" + floor + "–" + (floor + 4) + "층 · " + BackgroundCatalog.Names[region] + "</figcaption></figure>");
                    camera.targetTexture = previous;
                }
            }
            finally { camera.targetTexture = previous; camera.ResetAspect(); RenderTexture.active = active; UnityEngine.Object.DestroyImmediate(target); }
            File.WriteAllText(Path.Combine(directory, "index.html"), html.Append("</main>").ToString());
            string result = "PASS: " + checks + " checks; six region ranges and viewport coverage at three portrait aspect ratios; six camera previews.";
            File.WriteAllText("../Docs/BACKGROUND_CHECKS.txt", result); return result;
        }
    }
}
