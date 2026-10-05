using System.IO;
using UnityEditor;
using UnityEngine;

// Ecos del Grimorio > Configurar sprites
// pone todas las imagenes de Sprites como Sprite con filtro bilinear
public static class ConfigureSprites
{
    [MenuItem("Ecos del Grimorio/Configurar sprites")]
    public static void ConfigureAll()
    {
        string root = "Assets/Sprites";
        if (!Directory.Exists(root)) return;

        int count = 0;
        foreach (string file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

            string path = file.Replace("\\", "/");
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            count++;
        }

        Debug.Log($"Sprites configurados: {count}");
    }
}
