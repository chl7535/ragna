using UnityEngine;

namespace Ragna
{
    public static class BackgroundCatalog
    {
        public static readonly string[] Paths = { "01-meadow", "02-forest", "03-coast", "04-ruins", "05-volcano", "06-citadel" };
        public static readonly string[] Names = { "시작의 초원", "고대의 숲", "푸른 해안", "잊힌 폐성", "불타는 화산", "월식의 마성" };
        // Road centre measured as a fraction from the bottom of each supplied panorama.
        public static readonly float[] Roads = { .255f, .30f, .255f, .255f, .255f, .255f };
        public static int Index(int floor) => Mathf.Clamp((floor - 1) / 5, 0, 5);
    }

    public sealed class PanoramaView
    {
        readonly Camera camera;
        readonly SpriteRenderer image;
        int region = -1;
        int floor = -1;
        float enteredAt;
        public PanoramaView(Transform parent, Camera camera, Material material)
        {
            this.camera = camera;
            var go = new GameObject("RegionPanorama"); go.transform.SetParent(parent, false);
            image = go.AddComponent<SpriteRenderer>(); image.sortingOrder = -40;
            image.sharedMaterial = material;
        }
        public void Update(RunSnapshot run)
        {
            int next = BackgroundCatalog.Index(run.floor);
            if (next != region)
            {
                image.sprite = Resources.Load<Sprite>("Backgrounds/" + BackgroundCatalog.Paths[next]);
                if (image.sprite == null) throw new System.InvalidOperationException("Missing region background: " + BackgroundCatalog.Paths[next]);
                region = next;
            }
            if (floor != run.floor) { floor = run.floor; enteredAt = run.time; }
            float top = camera.ViewportToWorldPoint(new Vector3(.5f, .795f, 10)).y;
            float bottom = camera.ViewportToWorldPoint(new Vector3(.5f, .285f, 10)).y;
            float viewportWidth = camera.orthographicSize * 2 * camera.aspect;
            float aspect = image.sprite.rect.width / image.sprite.rect.height;
            float road = BackgroundCatalog.Roads[region];
            float height = Mathf.Max((top - RunModel.GroundY) / (1 - road), (RunModel.GroundY - bottom) / road, viewportWidth / aspect) * 1.02f;
            float width = height * aspect;
            image.transform.localScale = new Vector3(width / image.sprite.bounds.size.x, height / image.sprite.bounds.size.y, 1);
            float margin = Mathf.Max(0, (width - viewportWidth) * .5f);
            // These are complete paintings, not seamless tiles. Pan within the safe image
            // area and stop at its edge instead of exposing a repeat seam or mirroring text.
            float pan = Mathf.Min(Mathf.Max(0, run.time - enteredAt) * .20f, margin);
            image.transform.position = new Vector3(camera.transform.position.x - pan, RunModel.GroundY + (.5f - road) * height, 0);
        }
    }
}
