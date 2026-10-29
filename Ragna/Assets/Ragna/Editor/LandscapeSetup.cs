using System.IO;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Ragna.Editor
{
    public static class LandscapeSetup
    {
        [CliCommand("ragna_landscape_checks", "Verify landscape-only configuration, camera, Safe Area and editable HUD", MainThreadRequired = true)]
        public static string Check()
        {
            int count=0;
            void Assert(bool value,string label) { if(!value) throw new System.Exception("Landscape check: "+label); count++; }
            Assert(PlayerSettings.defaultInterfaceOrientation==UIOrientation.LandscapeLeft,"Landscape orientation");
            Assert(!PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown,"Portrait disabled");
            Assert(PlayerSettings.defaultScreenWidth==1920 && PlayerSettings.defaultScreenHeight==1080,"Reference resolution");
            var pixel=Camera.main.GetComponent<PixelPerfectCamera>();
            Assert(pixel.refResolutionX==320 && pixel.refResolutionY==180 && pixel.cropFrame==PixelPerfectCamera.CropFrame.None,"Wide pixel camera without crop");
            var hud=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ragna/Resources/UI/LandscapeHUD.prefab").GetComponent<BattleHUD>();
            Assert(hud.hp!=null && hud.hpFill!=null && hud.floor!=null && hud.xpFill!=null,"HUD values assigned");
            Assert(hud.potion!=null && hud.guard!=null && hud.prepare!=null && hud.growth!=null && hud.pause!=null,"HUD buttons assigned");
            var title=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ragna/Resources/Title/TitleScreen.prefab");
            Assert(title.GetComponent<CanvasScaler>().referenceResolution==new Vector2(1920,1080),"Title reference resolution");
            Assert(title.GetComponent<TitleScreen>().safeArea!=null,"Title Safe Area");
            Assert(title.transform.Find("SafeArea/ArtworkViewport").GetComponent<RectTransform>().anchorMin==Vector2.zero,"Full landscape title viewport");
            Assert(title.transform.Find("SafeArea/ArtworkViewport/Artwork").GetComponent<AspectRatioFitter>().aspectMode==AspectRatioFitter.AspectMode.FitInParent,"Title artwork keeps proportions");
            if(EditorApplication.isPlaying)
            {
                foreach(var canvas in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    Assert(canvas.referenceResolution==new Vector2(1920,1080) && canvas.matchWidthOrHeight==1,"Runtime landscape scaler "+canvas.name);
                var screen=Object.FindFirstObjectByType<TitleScreen>();
                if(screen!=null)
                {
                    var safe=Screen.safeArea;
                    Assert(Vector2.Distance(screen.safeArea.anchorMin,new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height))<.001f,"Safe Area minimum");
                    Assert(Vector2.Distance(screen.safeArea.anchorMax,new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height))<.001f,"Safe Area maximum");
                }
            }
            string result="PASS: "+count+" landscape configuration and UI checks. Portrait disabled; editable HUD; Safe Area; 1920x1080 reference.";
            File.WriteAllText("../Docs/LANDSCAPE_CHECKS.txt",result);return result;
        }
        static TMP_FontAsset font;
        static readonly Color ink = new Color32(14,21,34,246), panel = new Color32(25,35,51,255), gold = new Color32(232,192,124,255);
        [CliCommand("ragna_landscape_setup", "Apply landscape-only settings, camera, editable HUD and title layout", MainThreadRequired = true)]
        public static string Setup()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) throw new System.InvalidOperationException("Expected existing URP pipeline.");
            Directory.CreateDirectory("Assets/Ragna/Resources/UI");
            const string layoutPath = "Assets/Ragna/Resources/UI/LandscapeDisplay.asset";
            if (AssetDatabase.LoadAssetAtPath<LandscapeDisplaySettings>(layoutPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<LandscapeDisplaySettings>(),layoutPath);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth=1920; PlayerSettings.defaultScreenHeight=1080;
            var camera=Camera.main; Undo.RecordObject(camera,"Landscape camera"); camera.orthographicSize=LandscapeDisplaySettings.Current.cameraHalfHeight;
            var pixel=camera.GetComponent<PixelPerfectCamera>(); Undo.RecordObject(pixel,"Landscape pixel camera");
            pixel.refResolutionY=Mathf.RoundToInt(LandscapeDisplaySettings.Current.cameraHalfHeight * pixel.assetsPPU * 2);
            pixel.refResolutionX=Mathf.RoundToInt(pixel.refResolutionY * 16f / 9); pixel.cropFrame=PixelPerfectCamera.CropFrame.None;
            camera.allowMSAA=false; QualitySettings.antiAliasing=0;
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene); EditorSceneManager.SaveScene(camera.gameObject.scene);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Ragna/Fonts/NanumGothic SDF.asset");
            const string hudPath="Assets/Ragna/Resources/UI/LandscapeHUD.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(hudPath)==null) CreateHUD(hudPath);
            string titlePath="Assets/Ragna/Resources/Title/TitleScreen.prefab";
            var title=PrefabUtility.LoadPrefabContents(titlePath);
            var scaler=title.GetComponent<CanvasScaler>(); scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=1;
            var safe=title.transform.Find("SafeArea");
            if(safe==null) { safe=new GameObject("SafeArea",typeof(RectTransform)).transform; safe.SetParent(title.transform,false); Rect((RectTransform)safe,0,0,1,1); }
            var viewport=title.transform.Find("ArtworkViewport") ?? safe.Find("ArtworkViewport");
            viewport.SetParent(safe,false); Rect((RectTransform)viewport,0,0,1,1);
            viewport.Find("Artwork").GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            title.GetComponent<TitleScreen>().safeArea=(RectTransform)safe;
            PrefabUtility.SaveAsPrefabAsset(title,titlePath); PrefabUtility.UnloadPrefabContents(title);
            AssetDatabase.SaveAssets();
            return "Landscape only: 1920x1080, 320x180 pixel camera, editable HUD, title Safe Area. Portrait disabled.";
        }
        static void CreateHUD(string path)
        {
            var root=new GameObject("LandscapeHUD",typeof(RectTransform),typeof(BattleHUD)); Rect((RectTransform)root.transform,0,0,1,1);
            var hud=root.GetComponent<BattleHUD>();
            var header=Panel("Header",root.transform,0,.90f,1,1,ink);
            hud.title=Text("Region",header,.02f,.52f,.28f,.95f,30,"시작의 초원");
            hud.floor=Text("Floor",header,.30f,.52f,.40f,.95f,30,"01 / 30");
            hud.hpFill=Bar("PlayerHealth",header,.02f,.10f,.40f,.43f,new Color32(205,88,105,255));
            hud.hp=Text("HPValue",header,.02f,.10f,.40f,.43f,28,"HP 180 / 180");
            hud.floorFill=Bar("FloorProgress",header,.43f,.16f,.64f,.23f,gold);
            Text("Logo",header,.43f,.40f,.64f,.94f,35,"R A G N A R").color=gold;
            hud.prepare=Button("Prepare",header,.70f,.18f,.79f,.84f,"준비");
            hud.growth=Button("Growth",header,.80f,.18f,.89f,.84f,"성장");
            hud.pause=Button("Pause",header,.90f,.18f,.985f,.84f,"정지");
            var bottom=Panel("Controls",root.transform,0,0,1,.17f,ink);
            hud.level=Text("Level",bottom,.02f,.62f,.40f,.94f,32,"모험가 · LV.1");
            hud.skill=Text("Skill",bottom,.02f,.30f,.40f,.61f,28,"용기의 일격"); hud.skill.color=gold;
            hud.xpFill=Bar("Experience",bottom,.02f,.12f,.40f,.19f,gold);
            Text("ControlHint",bottom,.43f,.52f,.68f,.89f,22,"물약과 보호막으로 생존하세요");
            hud.info=Text("CombatStatus",bottom,.43f,.12f,.68f,.49f,24,"자동사냥 · 몬스터 추적");
            hud.potion=Button("Potion",bottom,.70f,.15f,.83f,.88f,"물약"); hud.potionText=hud.potion.GetComponentInChildren<TMP_Text>();
            hud.potion.GetComponent<Image>().color=new Color32(109,47,62,255);
            hud.guard=Button("Guard",bottom,.85f,.15f,.985f,.88f,"보호막"); hud.guardText=hud.guard.GetComponentInChildren<TMP_Text>();
            hud.guard.GetComponent<Image>().color=new Color32(42,72,99,255);
            hud.banner=Text("Banner",root.transform,.22f,.82f,.78f,.89f,40,""); hud.banner.color=gold;
            hud.hint=Text("DangerHint",root.transform,.27f,.18f,.73f,.25f,30,""); hud.hint.color=gold;
            PrefabUtility.SaveAsPrefabAsset(root,path); Object.DestroyImmediate(root);
        }
        static RectTransform Panel(string name,Transform parent,float x0,float y0,float x1,float y1,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            Rect((RectTransform)go.transform,x0,y0,x1,y1); go.GetComponent<Image>().color=color; go.GetComponent<Image>().raycastTarget=false;
            return (RectTransform)go.transform;
        }
        static TMP_Text Text(string name,Transform parent,float x0,float y0,float x1,float y1,int size,string text)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            Rect((RectTransform)go.transform,x0,y0,x1,y1); var t=go.GetComponent<TMP_Text>();
            t.font=font;t.fontSharedMaterial=font.material;t.fontSize=size;t.text=text;t.color=new Color32(237,237,223,255);
            t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        static Image Bar(string name,Transform parent,float x0,float y0,float x1,float y1,Color color)
        { var bg=Panel(name,parent,x0,y0,x1,y1,new Color32(44,48,59,255));return Panel("Fill",bg,0,0,1,1,color).GetComponent<Image>(); }
        static Button Button(string name,Transform parent,float x0,float y0,float x1,float y1,string label)
        {
            var r=Panel(name,parent,x0,y0,x1,y1,panel); var image=r.GetComponent<Image>();image.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;Text("Label",r,.03f,.03f,.97f,.97f,30,label);
            var colors=b.colors;colors.highlightedColor=new Color(.85f,.9f,1);colors.pressedColor=new Color(.65f,.75f,.85f);b.colors=colors;return b;
        }
        static void Rect(RectTransform r,float x0,float y0,float x1,float y1)
        {r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
