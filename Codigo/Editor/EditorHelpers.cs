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

    // Algunos archivos de arte traen espacios especiales o acentos con otra codificación
    // en el nombre; si no se encuentra la ruta exacta se busca el archivo equivalente.
    static string Normalize(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char ch in s.Normalize(System.Text.NormalizationForm.FormC))
            sb.Append(char.IsWhiteSpace(ch) || char.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.SpaceSeparator ? ' ' : ch);
        return string.Join(" ", sb.ToString().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    }

    public static string ResolvePath(string path)
    {
        if (File.Exists(path)) return path;
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) return null;
        string wanted = Normalize(Path.GetFileName(path));
        foreach (var f in Directory.GetFiles(dir))
            if (Normalize(Path.GetFileName(f)) == wanted) return f.Replace("\\", "/");
        return null;
    }

    public static Sprite LoadSprite(string path)
    {
        path = ResolvePath(path);
        if (path == null) return null;
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

    // Textura que se repite (ladrillos, tablas, borde). Se genera una vez en gris claro
    // y cada nivel la pinta de su color con el SpriteRenderer.
    public static Sprite TiledTexture(string name, System.Func<int, int, Color> pixel, int w, int h, int ppu)
    {
        string path = $"{Sprites}Generados/{name}.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, pixel(x, y));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = false;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; // necesario para que se repita bien
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static float Hash(int x, int y) => Mathf.Abs(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f) % 1f;

    public static Sprite Ladrillo() => TiledTexture("Ladrillo", (x, y) =>
    {
        int row = y / 16;
        int bx = (x + (row % 2) * 16) / 32;
        bool mortar = y % 16 < 2 || (x + (row % 2) * 16) % 32 < 2;
        if (mortar) return new Color(0.35f, 0.33f, 0.35f);
        float v = 0.75f + Hash(bx, row) * 0.2f + (Hash(x, y) - 0.5f) * 0.08f;
        if (y % 16 == 15) v += 0.1f; // brillo arriba de cada ladrillo
        return new Color(v, v, v);
    }, 64, 64, 32);

    public static Sprite Tablas() => TiledTexture("Tablas", (x, y) =>
    {
        int plank = y / 16;
        if (y % 16 < 1) return new Color(0.3f, 0.28f, 0.27f);
        float grain = Mathf.Sin((x + plank * 23) * 0.35f + Mathf.Sin(y * 0.8f) * 2f) * 0.05f;
        float v = 0.75f + Hash(plank, 3) * 0.15f + grain;
        if ((x + plank * 37) % 64 < 2) v -= 0.25f; // unión entre tablas
        if (((x + plank * 37) % 64 == 5 || (x + plank * 37) % 64 == 59) && y % 16 == 8) v = 0.35f; // clavos
        return new Color(v, v, v);
    }, 64, 32, 32);

    public static Sprite Borde() => TiledTexture("Borde", (x, y) =>
    {
        // Franja superior irregular (pasto, nieve, ceniza o runas según el color)
        float edge = 10f + Mathf.Sin(x * 0.7f) * 2f + Hash(x, 1) * 2f;
        if (y > edge) return new Color(1, 1, 1, 0);
        float v = 0.8f + (Hash(x, y) - 0.5f) * 0.25f + (y > edge - 2 ? 0.15f : 0f);
        return new Color(v, v, v, 1f);
    }, 32, 16, 32);

    public static Sprite Vineta() => TiledTexture("Vineta", (x, y) =>
    {
        float dx = (x - 127.5f) / 128f, dy = (y - 127.5f) / 128f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        return new Color(0, 0, 0, Mathf.Clamp01((d - 0.55f) * 1.6f) * 0.85f);
    }, 256, 256, 100);

    // Luz 2D de URP agregada desde el editor (tipo: 3 = puntual, 4 = global)
    public static Component Light(GameObject go, int type, Color color, float intensity, float radius = 0f)
    {
        var t = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime");
        if (t == null) return null;
        var light = go.AddComponent(t);
        var so = new SerializedObject(light);
        void F(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }
        var lt = so.FindProperty("m_LightType"); if (lt != null) lt.intValue = type;
        var c = so.FindProperty("m_Color"); if (c != null) c.colorValue = color;
        F("m_Intensity", intensity);
        if (radius > 0f)
        {
            F("m_PointLightOuterRadius", radius);
            F("m_PointLightInnerRadius", radius * 0.15f);
            F("m_FalloffIntensity", 0.6f);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return light;
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
