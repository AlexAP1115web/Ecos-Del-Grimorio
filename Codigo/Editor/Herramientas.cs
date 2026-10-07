using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramientas del menú "Ecos del Grimorio" para las entregas:
//  - Capturar niveles: abre cada nivel y guarda imágenes del inicio, un personaje, enemigos y el jefe.
//  - Compilar juego: genera el .exe para Windows en la carpeta Build del proyecto.
public static class Herramientas
{
    const int Ancho = 1920;
    const int Alto = 1080;

    [MenuItem("Ecos del Grimorio/Capturar niveles para evidencias")]
    static void CapturarNiveles()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escenaOriginal = EditorSceneManager.GetActiveScene().path;
        string carpeta = Path.Combine(CapturaPantalla.Carpeta, "Niveles");
        Directory.CreateDirectory(carpeta);

        var niveles = EditorBuildSettings.scenes
            .Where(e => e.enabled && Path.GetFileName(e.path).StartsWith("Nivel"))
            .Select(e => e.path).ToArray();

        int total = 0;
        try
        {
            for (int i = 0; i < niveles.Length; i++)
            {
                string nombre = Path.GetFileNameWithoutExtension(niveles[i]);
                EditorUtility.DisplayProgressBar("Capturas", nombre, (float)i / niveles.Length);
                EditorSceneManager.OpenScene(niveles[i], OpenSceneMode.Single);
                total += CapturarEscena(nombre, carpeta);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (!string.IsNullOrEmpty(escenaOriginal)) EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog("Capturas", $"Se guardaron {total} imágenes en:\n{carpeta}", "Aceptar");
        EditorUtility.RevealInFinder(carpeta);
    }

    // Toma una imagen en cada punto interesante del nivel
    static int CapturarEscena(string nombre, string carpeta)
    {
        var cam = Camera.main;
        if (cam == null) return 0;

        var puntos = new List<(string etiqueta, Vector3 pos)>();

        var lira = GameObject.FindGameObjectWithTag("Player");
        if (lira != null) puntos.Add(("1_Inicio", lira.transform.position));

        var npc = Object.FindFirstObjectByType<NPCDialogue>();
        if (npc != null) puntos.Add(("2_Personaje", npc.transform.position));

        // Dos grupos de enemigos: uno a un tercio del nivel y otro a dos tercios
        var enemigos = Object.FindObjectsByType<EnemyBase>()
            .Where(e => !(e is BossController))
            .OrderBy(e => e.transform.position.x).ToArray();
        if (enemigos.Length > 0) puntos.Add(("3_Enemigos", enemigos[enemigos.Length / 3].transform.position));
        if (enemigos.Length > 2) puntos.Add(("4_Enemigos", enemigos[enemigos.Length * 2 / 3].transform.position));

        var jefe = Object.FindFirstObjectByType<BossController>();
        if (jefe != null) puntos.Add(("5_Jefe", jefe.transform.position));

        // Mismos ajustes que usa la cámara al seguir a Lira
        var seguir = cam.GetComponent<CameraFollow>();
        Vector2 offset = new Vector2(0f, 1.2f);
        bool limites = false;
        float minX = 0, maxX = 0, minY = 0, maxY = 0;
        if (seguir != null)
        {
            var so = new SerializedObject(seguir);
            offset = so.FindProperty("offset").vector2Value;
            limites = so.FindProperty("useBounds").boolValue;
            minX = so.FindProperty("minX").floatValue;
            maxX = so.FindProperty("maxX").floatValue;
            minY = so.FindProperty("minY").floatValue;
            maxY = so.FindProperty("maxY").floatValue;
        }

        var rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
        float alto = cam.orthographicSize;
        float ancho = alto * Ancho / Alto;
        int hechas = 0;

        foreach (var (etiqueta, pos) in puntos)
        {
            Vector3 p = new Vector3(pos.x, pos.y + offset.y, cam.transform.position.z);
            if (limites)
            {
                p.x = Mathf.Clamp(p.x, minX + ancho, Mathf.Max(minX + ancho, maxX - ancho));
                p.y = Mathf.Clamp(p.y, minY + alto, Mathf.Max(minY + alto, maxY - alto));
            }
            cam.transform.position = p;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;

            File.WriteAllBytes(Path.Combine(carpeta, $"{nombre}_{etiqueta}.png"), tex.EncodeToPNG());
            hechas++;
        }

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        return hechas;
    }

    [MenuItem("Ecos del Grimorio/Abrir carpeta de capturas")]
    static void AbrirCapturas()
    {
        Directory.CreateDirectory(CapturaPantalla.Carpeta);
        EditorUtility.RevealInFinder(CapturaPantalla.Carpeta);
    }

    [MenuItem("Ecos del Grimorio/Compilar juego (.exe)")]
    static void Compilar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string[] escenas = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        if (escenas.Length == 0)
        {
            EditorUtility.DisplayDialog("Compilar", "No hay escenas en Build Settings. Primero usa \"Crear juego completo\".", "Aceptar");
            return;
        }

        // Datos que aparecen en la ventana y en las propiedades del .exe
        PlayerSettings.companyName = "Alejandro Pérez Alcántara";
        PlayerSettings.productName = "Ecos del Grimorio";
        PlayerSettings.bundleVersion = "0.2";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = Ancho;
        PlayerSettings.defaultScreenHeight = Alto;

        string carpeta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build");
        string exe = Path.Combine(carpeta, "EcosDelGrimorio.exe");

        var opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        var resumen = reporte.summary;

        if (resumen.result == BuildResult.Succeeded)
        {
            float mb = resumen.totalSize / (1024f * 1024f);
            EditorUtility.DisplayDialog("Compilar",
                $"Listo. El juego quedó en:\n{carpeta}\n\nTamaño: {mb:0.0} MB\nTiempo: {resumen.totalTime.TotalMinutes:0.0} min\n\n" +
                "Para entregarlo comprime toda la carpeta Build (no solo el .exe).", "Aceptar");
            EditorUtility.RevealInFinder(exe);
        }
        else
        {
            EditorUtility.DisplayDialog("Compilar", $"La compilación terminó con {resumen.totalErrors} errores. Revisa la consola.", "Aceptar");
        }
    }
}
