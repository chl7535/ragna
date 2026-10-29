using System.IO;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ragna.Editor
{
    public static class TitleSetup
    {
        [CliCommand("ragna_title_setup", "Import supplied title artwork and create an editable menu prefab", MainThreadRequired = true)]
        public static string Setup()
        {
            const string artPath = "Assets/Ragna/Resources/Title/TitleArtwork.png";
            AssetDatabase.ImportAsset(artPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(artPath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            var spriteSettings = new TextureImporterSettings(); importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(spriteSettings);
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            const string prefabPath = "Assets/Ragna/Resources/Title/TitleScreen.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return "Title prefab exists; authored edits preserved.";
            var root = new GameObject("TitleScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TitleScreen));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 1;
            var backdrop = Image("Backdrop", root.transform, Vector2.zero, Vector2.one);
            backdrop.color = new Color32(8,16,27,255); backdrop.raycastTarget = false;
            var safe = new GameObject("SafeArea", typeof(RectTransform)); safe.transform.SetParent(root.transform,false);
            Rect(safe.GetComponent<RectTransform>(),Vector2.zero,Vector2.one); root.GetComponent<TitleScreen>().safeArea = safe.GetComponent<RectTransform>();
            var viewport = new GameObject("ArtworkViewport", typeof(RectTransform), typeof(RectMask2D)); viewport.transform.SetParent(safe.transform,false);
            Rect(viewport.GetComponent<RectTransform>(), Vector2.zero,Vector2.one);
            var artwork = Image("Artwork",viewport.transform,Vector2.zero,Vector2.one);
            artwork.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath); artwork.raycastTarget = false;
            var fit = artwork.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = artwork.sprite.rect.width / artwork.sprite.rect.height;
            var menu = root.GetComponent<TitleScreen>();
            menu.gameStart = Button("GAME START",artwork.transform,new Vector2(.393f,.400f),new Vector2(.672f,.490f));
            menu.settings = Button("SETTINGS",artwork.transform,new Vector2(.409f,.327f),new Vector2(.645f,.386f));
            menu.credits = Button("CREDITS",artwork.transform,new Vector2(.409f,.259f),new Vector2(.645f,.319f));
            menu.quit = Button("QUIT",artwork.transform,new Vector2(.409f,.189f),new Vector2(.645f,.249f));
            PrefabUtility.SaveAsPrefabAsset(root,prefabPath); Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
            return "Supplied artwork imported unchanged. Editable title prefab: START active; other three buttons have no actions.";
        }
        static Image Image(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            Rect(go.GetComponent<RectTransform>(),min,max); return go.GetComponent<Image>();
        }
        static Button Button(string name,Transform parent,Vector2 min,Vector2 max)
        {
            var graphic = Image(name,parent,min,max); var button = graphic.gameObject.AddComponent<Button>();
            var colors = button.colors; colors.normalColor = new Color(1,1,1,0); colors.highlightedColor = new Color(.7f,.85f,1,.14f);
            colors.pressedColor = new Color(.25f,.55f,1,.3f); colors.selectedColor = colors.normalColor; button.colors = colors;
            button.targetGraphic = graphic; button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }
        static void Rect(RectTransform rect,Vector2 min,Vector2 max)
        { rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        [CliCommand("ragna_title_checks", "Exercise title buttons and saved-run resume without touching player saves", MainThreadRequired = true)]
        public static string Check()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play mode first.");
            var result = Object.FindFirstObjectByType<RagnaGame>().ReviewTitleUI();
            File.WriteAllText("../Docs/TITLE_CHECKS.txt",result); return result;
        }
    }
}
