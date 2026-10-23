// Standalone Unity CLI eval snippet. No manual .meta edits.
const string path = "Assets/Ragna/Resources/Heroes/AdventurerSheet.png";
UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
if (importer == null) throw new System.Exception("Character sheet is missing.");
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Multiple;
importer.filterMode = UnityEngine.FilterMode.Point;
importer.mipmapEnabled = false;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
importer.maxTextureSize = 4096;
importer.isReadable = true;
importer.alphaIsTransparency = true;
importer.GetSourceTextureWidthAndHeight(out int width, out int height);
if (width % 4 != 0 || height % 7 != 0) throw new System.Exception("Sheet must have an exact 4 by 7 grid.");
float cellW = width / 4f, cellH = height / 7f;
importer.spritePixelsPerUnit = 90;
importer.SaveAndReimport();
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories(); factory.Init();
var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
if (provider == null) throw new System.Exception("Sprite provider unavailable.");
provider.InitSpriteEditorDataProvider();
var edit = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (edit == null) throw new System.Exception("Sprite edit capabilities unavailable; slicing aborted.");
var capability = edit.GetEditCapability();
foreach (var required in new[] { UnityEditor.U2D.Sprites.EEditCapability.CreateAndDeleteSprite, UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect,
    UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName, UnityEditor.U2D.Sprites.EEditCapability.EditPivot })
    if (!capability.HasCapability(required)) throw new System.Exception("Unsupported sprite edit: " + required);
var existing = provider.GetSpriteRects();
var rects = new UnityEditor.SpriteRect[28];
var names = new System.Collections.Generic.List<UnityEditor.SpriteNameFileIdPair>();
// Generated rows are visually separated but not perfectly uniform. Use inspected
// transparent gutters so hair, boots and sword poses are never cut by a nominal grid.
int[] rowEdges = { 25, 273, 507, 736, 995, 1220, 1420, 1641 };
int[] baselines = { 261, 497, 722, 982, 1210, 1409, 1628 };
int[][] colEdges = {
    new[] { 0, 237, 474, 711, 948 }, new[] { 0, 237, 474, 711, 948 },
    new[] { 0, 242, 480, 725, 948 }, new[] { 0, 237, 445, 719, 948 },
    new[] { 0, 237, 474, 711, 948 }, new[] { 0, 230, 470, 683, 948 },
    new[] { 0, 237, 474, 711, 948 }
};
if (width != 948 || height != 1659) throw new System.Exception("Reinspect gutters for a different source sheet.");
for (int row = 0; row < 7; row++) for (int col = 0; col < 4; col++)
{
    string name = ((Ragna.PixelMotion)row) + "_" + col;
    var id = UnityEditor.GUID.Generate();
    foreach (var previous in existing) if (previous.name == name) id = previous.spriteID;
    float frameW = colEdges[row][col + 1] - colEdges[row][col];
    float frameH = rowEdges[row + 1] - rowEdges[row];
    rects[row * 4 + col] = new UnityEditor.SpriteRect { name = name, spriteID = id,
        rect = new UnityEngine.Rect(colEdges[row][col], height - rowEdges[row + 1], frameW, frameH),
        alignment = UnityEngine.SpriteAlignment.Custom,
        pivot = new UnityEngine.Vector2(.5f, (rowEdges[row + 1] - baselines[row] + 112.5f) / frameH) };
    names.Add(new UnityEditor.SpriteNameFileIdPair(name, id));
}
var nameProvider = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteNameFileIdDataProvider>();
if (nameProvider == null) throw new System.Exception("Name/ID provider unavailable; slicing aborted.");
provider.SetSpriteRects(rects); nameProvider.SetNameFileIdPairs(names); provider.Apply(); importer.SaveAndReimport();
return "Imported reference hero: 28 authored frames, point sampling, transparent, stable names and pivots.";
