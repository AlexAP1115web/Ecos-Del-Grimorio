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

    // Página suelta del grimorio (pergamino con renglones)
    public static Sprite Pagina() => TiledTexture("Pagina", (x, y) =>
    {
        float wob = Mathf.Sin(y * 0.5f) * 0.6f;
        if (x < 3 + wob || x > 28 + wob || y < 2 || y > 37) return new Color(0, 0, 0, 0);
        bool esquina = x - 3 + (37 - y) < 5;           // esquina doblada
        if (x > 21 + wob && y > 30 && (x - 21) > (y - 30)) return new Color(0.75f, 0.68f, 0.5f, 1f);
        if (esquina) return new Color(0, 0, 0, 0);
        bool orilla = x < 4.5f + wob || x > 26.5f + wob || y < 3.5f || y > 35.5f;
        float v = 0.92f + (Hash(x, y) - 0.5f) * 0.08f;
        var c = new Color(v, v * 0.93f, v * 0.75f, 1f);
        if (orilla) c *= 0.8f;
        bool renglon = y % 5 == 0 && y > 6 && y < 32 && x > 6 && x < 25 && Hash(x / 3, y) > 0.2f;
        if (renglon) c = new Color(0.35f, 0.3f, 0.45f, 1f);
        c.a = 1f;
        return c;
    }, 32, 40, 32);

    // Vasija de barro (se pinta del color del ala)
    public static Sprite Vasija() => TiledTexture("Vasija", (x, y) =>
    {
        float cx = 15.5f;
        float r;
        if (y >= 36) r = 7f;                                      // borde
        else if (y >= 31) r = 4.5f;                               // cuello
        else r = 5.5f + 8.5f * Mathf.Sin(Mathf.PI * (y + 2) / 35f); // cuerpo
        float dx = Mathf.Abs(x - cx);
        if (dx > r || y > 39) return new Color(0, 0, 0, 0);
        float luz = 0.65f + 0.35f * (1f - dx / r) - (x > cx ? 0.12f : 0f);
        if (dx > r - 1.2f) luz *= 0.55f;                            // contorno
        if (y == 20 || y == 21 || y == 33) luz *= 0.6f;                // franjas decorativas
        if (y >= 36 && y <= 37) luz *= 0.75f;
        luz += (Hash(x, y) - 0.5f) * 0.05f;
        return new Color(luz, luz, luz, 1f);
    }, 32, 40, 32);

    // Grietas que se ponen encima del ladrillo para marcar un muro que se puede romper
    static bool[] grietas;
    public static Sprite Grietas()
    {
        const int w = 64, h = 64;
        grietas = new bool[w * h];
        var r = new System.Random(5);
        for (int k = 0; k < 7; k++)
        {
            float x = r.Next(8, 56), y = r.Next(4, 60);
            float ang = (float)(r.NextDouble() * Mathf.PI * 2f);
            for (int paso = 0; paso < 40; paso++)
            {
                ang += (float)(r.NextDouble() - 0.5) * 1.1f;
                x += Mathf.Cos(ang); y += Mathf.Sin(ang);
                int ix = Mathf.RoundToInt(x), iy = Mathf.RoundToInt(y);
                if (ix < 1 || iy < 1 || ix >= w - 1 || iy >= h - 1) break;
                grietas[iy * w + ix] = true;
                if (paso < 12) grietas[iy * w + ix + 1] = true;
            }
        }
        return TiledTexture("Grietas", (x, y) =>
            grietas[y * w + x] ? new Color(0.05f, 0.03f, 0.03f, 0.95f)
            : (x > 0 && grietas[y * w + x - 1]) ? new Color(1f, 0.95f, 0.85f, 0.35f)
            : new Color(0, 0, 0, 0), w, h, 32);
    }

    // Viñeta blanca (se pinta de rojo para el destello de daño)
    public static Sprite VinetaBlanca() => TiledTexture("VinetaBlanca", (x, y) =>
    {
        float dx = (x - 127.5f) / 128f, dy = (y - 127.5f) / 128f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        return new Color(1, 1, 1, Mathf.Clamp01((d - 0.5f) * 1.8f));
    }, 256, 256, 100);

    // Panel oscuro con borde dorado y gemas en las esquinas. Es un sprite de 9 partes
    // (sliced): las esquinas no se deforman aunque el panel sea muy ancho.
    public static Sprite PanelUI()
    {
        const int S = 128, B = 44;
        var sprite = TiledTexture("PanelUI", (x, y) =>
        {
            int ex = Mathf.Min(x, S - 1 - x), ey = Mathf.Min(y, S - 1 - y);
            // esquinas redondeadas
            float rx = Mathf.Max(0, 10 - ex), ry = Mathf.Max(0, 10 - ey);
            if (rx * rx + ry * ry > 100) return new Color(0, 0, 0, 0);
            int e = Mathf.Min(ex, ey);
            var fondo = new Color(0.07f, 0.04f, 0.13f, 0.94f);
            var oro = new Color(0.93f, 0.76f, 0.38f, 1f);
            var oroOscuro = new Color(0.55f, 0.4f, 0.18f, 1f);
            // gema en cada esquina
            float gx = Mathf.Abs(ex - 20), gy = Mathf.Abs(ey - 20);
            if (gx + gy <= 9) return gx + gy >= 7.5f ? oro : new Color(0.62f, 0.32f, 0.95f, 1f) * (1.1f - (gx + gy) / 14f);
            if (e < 2) return oroOscuro;
            if (e < 6) return Color.Lerp(oro, oroOscuro, Mathf.Abs(e - 3.5f) / 2.5f);
            if (e == 11 || e == 12) return new Color(oro.r, oro.g, oro.b, 0.8f);
            return fondo;
        }, S, S, 100);

        string path = $"{Sprites}Generados/PanelUI.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.spriteBorder != new Vector4(B, B, B, B))
        {
            importer.spriteBorder = new Vector4(B, B, B, B);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return sprite;
    }

    // ---------- Decoración de los biomas ----------

    // Librero con libros de colores (Ala de Aprendizaje)
    public static Sprite Estante() => TiledTexture("Estante", (x, y) =>
    {
        var madera = new Color(0.42f, 0.27f, 0.16f, 1f);
        if (x < 4 || x > 59 || y < 3 || y > 92) return madera * (0.8f + Hash(x, y) * 0.1f);
        int fila = (y - 3) / 22;
        int yy = (y - 3) % 22;
        if (yy < 3) return madera * 1.1f;                     // repisa
        // libros: ancho variable según la posición
        int libro = (x - 4 + fila * 7) / 5;
        float alto = 12 + Hash(libro, fila) * 6;
        if (yy - 3 > alto || Hash(libro, fila + 9) < 0.12f) return new Color(0.12f, 0.08f, 0.07f, 1f);   // hueco
        Color[] colores = { new Color(0.6f, 0.15f, 0.15f), new Color(0.15f, 0.25f, 0.55f), new Color(0.2f, 0.45f, 0.25f),
                            new Color(0.5f, 0.35f, 0.15f), new Color(0.4f, 0.2f, 0.5f), new Color(0.7f, 0.6f, 0.3f) };
        var c = colores[(int)(Hash(libro, fila + 3) * 5.99f)];
        if ((x - 4 + fila * 7) % 5 == 0) c *= 0.6f;           // lomo
        if (yy - 3 > alto - 2 || yy == 6) c *= 1.25f;           // detalles dorados
        c.a = 1f;
        return c;
    }, 64, 96, 32);

    // Cadena que cuelga (Ala de Fuego); se repite hacia abajo
    public static Sprite Cadena() => TiledTexture("Cadena", (x, y) =>
    {
        float dx = (x - 7.5f) / 4.5f, dy = (y - 8f) / 6.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 0.62f && d < 1f) { float v = 0.55f + 0.3f * (1f - d); return new Color(v, v, v * 0.95f, 1f); }
        if (y >= 16 && y <= 31 && x >= 6 && x <= 9) { float v = x == 6 ? 0.45f : 0.7f; return new Color(v, v, v, 1f); }
        return new Color(0, 0, 0, 0);
    }, 16, 32, 32);

    // Fila de carámbanos pequeños para la parte de abajo de las plataformas (Ala de Hielo)
    public static Sprite Carambanos() => TiledTexture("Carambanos", (x, y) =>
    {
        int k = x / 8;
        float largo = 8 + Hash(k, 4) * 15;
        float cx = k * 8 + 4, mitad = 3.5f * Mathf.Pow(Mathf.Clamp01((y - (23 - largo)) / largo), 0.8f);
        if (y < 23 - largo || Mathf.Abs(x - cx) > mitad) return new Color(0, 0, 0, 0);
        float v = 0.85f + (x < cx ? 0.15f : 0f);
        return new Color(v, v, v, 0.9f);
    }, 32, 24, 32);

    // Carámbano grande que cae
    public static Sprite CarambanoGrande() => TiledTexture("CarambanoGrande", (x, y) =>
    {
        float mitad = 7f * Mathf.Pow(y / 39f, 0.8f);
        if (Mathf.Abs(x - 7.5f) > mitad) return new Color(0, 0, 0, 0);
        float v = 0.8f + (x < 7.5f ? 0.2f : 0f) - (Mathf.Abs(x - 7.5f) > mitad - 1f ? 0.25f : 0f);
        return new Color(v * 0.85f, v * 0.95f, v, 0.95f);
    }, 16, 40, 32);

    // Racimo de cristales (Ala de Hielo y Corazón)
    public static Sprite Cristales() => TiledTexture("Cristales", (x, y) =>
    {
        float[] cx = { 14, 25, 35 }, w = { 9, 12, 8 }, h = { 30, 44, 26 };
        foreach (int i in new[] { 1, 0, 2 })   // el cristal alto va al frente
        {
            float d = Mathf.Abs(x - cx[i]);
            float tope = h[i] - d / (w[i] / 2f) * w[i] * 0.9f;
            if (d < w[i] / 2f && y < tope)
            {
                float v = x < cx[i] ? 1f : 0.72f;
                if (d > w[i] / 2f - 1.2f) v *= 0.7f;
                return new Color(v, v, v, 0.92f);
            }
        }
        return new Color(0, 0, 0, 0);
    }, 48, 48, 32);

    // Enredadera que cuelga de las plataformas (Ala de Viento); se repite hacia abajo
    public static Sprite Enredadera() => TiledTexture("Enredadera", (x, y) =>
    {
        float tallo = 7.5f + Mathf.Sin(y * 0.26f) * 2.5f;
        if (Mathf.Abs(x - tallo) < 1.2f) return new Color(0.25f, 0.45f, 0.18f, 1f);
        int hoja = y / 8;
        float hx = tallo + (hoja % 2 == 0 ? 3.5f : -3.5f), hy = hoja * 8 + 4;
        float dx = (x - hx) / 3.2f, dy = (y - hy) / 2.2f;
        if (dx * dx + dy * dy < 1f) return new Color(0.35f, 0.65f, 0.25f, 1f) * (0.9f + Hash(hoja, 2) * 0.2f);
        return new Color(0, 0, 0, 0);
    }, 16, 48, 32);

    // Pasto sobre el suelo (Ala de Viento); se repite a lo ancho y se pinta del color del borde
    public static Sprite Hierba() => TiledTexture("Hierba", (x, y) =>
    {
        float alto = 5 + Hash(x, 1) * 10 * (0.6f + 0.4f * Mathf.Sin(x * 0.4f));
        if (y > alto) return new Color(0, 0, 0, 0);
        float v = 0.6f + 0.4f * (y / 16f);
        return new Color(v, v, v, 1f);
    }, 32, 16, 32);

    // Círculo de runas que flota en el Corazón del Grimorio
    public static Sprite RunaCirculo() => TiledTexture("RunaCirculo", (x, y) =>
    {
        float dx = x - 63.5f, dy = y - 63.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        float ang = Mathf.Atan2(dy, dx);
        float a = 0f;
        if (r > 56 && r < 60) a = 1f;
        else if (r > 40 && r < 42) a = 0.8f;
        else if (r > 44 && r < 53)
        {
            float seg = (ang + Mathf.PI) / (Mathf.PI * 2f) * 16f;
            int i = (int)seg;
            float f = seg - i;
            if (f > 0.25f && f < 0.75f && (Hash(i, (int)r / 3) > 0.45f || Mathf.Abs(f - 0.5f) < 0.06f)) a = 0.9f;
        }
        else if (r < 38 && (Mathf.Abs(dx) < 1.2f || Mathf.Abs(dy) < 1.2f || Mathf.Abs(Mathf.Abs(dx) - Mathf.Abs(dy)) < 1.5f) && r > 8) a = 0.5f;
        return new Color(1, 1, 1, a);
    }, 128, 128, 64);

    // Montículo para el primer plano (rocas, nieve, arbustos según el color)
    public static Sprite Monticulo() => TiledTexture("Monticulo", (x, y) =>
    {
        float borde = Mathf.Pow(Mathf.Sin(Mathf.PI * x / 127f), 0.45f);
        float alto = (26 + 9 * Mathf.Sin(x * 0.07f) + 6 * Mathf.Sin(x * 0.19f + 1f) + Hash(x, 7) * 2f) * borde;
        if (y > alto) return new Color(0, 0, 0, 0);
        float v = 0.8f + (Hash(x / 3, y / 3) - 0.5f) * 0.2f + (y > alto - 3 ? 0.2f : 0f);
        return new Color(v, v, v, 1f);
    }, 128, 48, 32);

    // Columna de fuego del géiser (de abajo hacia arriba)
    public static Sprite Llama() => TiledTexture("Llama", (x, y) =>
    {
        float t = y / 95f;
        float ancho = 15f * (1f - t * 0.55f) + Mathf.Sin(y * 0.35f) * 1.5f;
        float d = Mathf.Abs(x - 15.5f) / Mathf.Max(ancho, 0.1f);
        if (d > 1f) return new Color(0, 0, 0, 0);
        var centro = new Color(1f, 0.95f, 0.6f);
        var orilla = new Color(1f, 0.35f, 0.05f);
        var c = Color.Lerp(centro, orilla, d * 0.9f + t * 0.3f);
        c.a = (1f - d * d) * (1f - t * t);
        return c;
    }, 32, 96, 24);

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
