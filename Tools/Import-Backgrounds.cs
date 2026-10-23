// Run with Unity CLI eval. Preserve the supplied PNGs; only configure import settings.
foreach (string file in Ragna.BackgroundCatalog.Paths)
{
    string path = "Assets/Ragna/Resources/Backgrounds/" + file + ".png";
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
    if (importer == null) throw new System.Exception("Missing background: " + file);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    // Paintings keep their smooth shading; foreground pixel characters still use Point.
    importer.filterMode = UnityEngine.FilterMode.Bilinear;
    importer.mipmapEnabled = false; importer.isReadable = false;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.maxTextureSize = 2048;
    importer.spritePixelsPerUnit = 100;
    importer.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
    foreach (string platform in new[] { "Android", "iPhone" })
    {
        var settings = importer.GetPlatformTextureSettings(platform);
        settings.name = platform; settings.overridden = true; settings.maxTextureSize = 2048;
        settings.format = UnityEditor.TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(settings);
    }
    importer.SaveAndReimport();
}
return "Imported all six panoramas; originals unchanged, bilinear paintings, no mipmaps, mobile ASTC 6x6.";
