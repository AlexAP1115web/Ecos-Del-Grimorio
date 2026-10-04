using System.IO;
using UnityEditor;
using UnityEngine;

// Funciones de apoyo para las herramientas del editor (CrearJuego).
public static class EditorHelpers
{
    public const string Sprites = "Assets/Sprites/";

    public static void CreateFolders(params string[] folders)
    {
        foreach (var f in folders)
        {
            if (AssetDatabase.IsValidFolder(f)) continue;
            string parent = Path.GetDirectoryName(f).Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(parent)) CreateFolders(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(f));
        }
    }

    public static void AddTag(string tag)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    // Asigna un campo [SerializeField] (aunque sea privado)
    public static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"No se encontró el campo '{field}' en {target.GetType().Name}");
            return;
        }

        switch (value)
        {
            case float f: prop.floatValue = f; break;
            case int i: prop.intValue = i; break;
            case bool b: prop.boolValue = b; break;
            case string s: prop.stringValue = s; break;
            case Vector2 v: prop.vector2Value = v; break;
            case Color c: prop.colorValue = c; break;
            case System.Enum e: prop.enumValueIndex = System.Convert.ToInt32(e); break;
            case Object o: prop.objectReferenceValue = o; break;
            case null: prop.objectReferenceValue = null; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetStrings(Object target, string field, string[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetFloats(Object target, string field, float[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).floatValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetResistances(Health health, params (Elemento element, float mult)[] values)
    {
        var so = new SerializedObject(health);
        var prop = so.FindProperty("resistances");
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("element").enumValueIndex = (int)values[i].element;
            el.FindPropertyRelative("multiplier").floatValue = values[i].mult;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static Sprite LoadSprite(string path)
    {
        if (!File.Exists(path)) return null;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static Sprite GeneratedSprite(string name, bool circle)
    {
        string path = $"{Sprites}Generados/{name}.png";
        if (!File.Exists(path))
        {
            int size = circle ? 64 : 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    if (circle)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                        a = Mathf.Clamp01((1f - d) * 4f);
                    }
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = circle ? 64 : 8; // 1 unidad de Unity
        importer.filterMode = circle ? FilterMode.Bilinear : FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    public static string FileName(string name) => name.Replace(" ", "_");

    public static SpriteRenderer AddSprite(GameObject go, Sprite sprite, float height, int order)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (sprite != null) go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
        return sr;
    }

    public static GameObject SavePrefab(GameObject go, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
