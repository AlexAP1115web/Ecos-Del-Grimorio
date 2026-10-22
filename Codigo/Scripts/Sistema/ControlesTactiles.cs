using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum BotonTactil { Izquierda, Derecha, Arriba, Abajo, Saltar, Hechizo1, Hechizo2, Hechizo3, Combo, Esquivar, Cambiar, Usar, Pausa }

// Controles en pantalla para jugar en el celular (versión web).
// Solo aparecen en celulares y tabletas, o en una computadora con pantalla táctil después del primer toque.
// No usan el EventSystem: cada cuadro se revisan todos los dedos que tocan la pantalla y se marca
// el botón que está debajo de cada uno, así se pueden presionar varios al mismo tiempo
// (por ejemplo caminar, saltar y lanzar un hechizo). Controles.cs lee estos botones igual que el teclado.
[DefaultExecutionOrder(-1000)]
public class ControlesTactiles : MonoBehaviour
{
    static ControlesTactiles instancia;
    static readonly int Total = System.Enum.GetValues(typeof(BotonTactil)).Length;
    static readonly bool[] ahora = new bool[Total];
    static readonly bool[] antes = new bool[Total];
    static bool toqueLibre;
    static bool activo;

    // ¿Se está jugando con la pantalla táctil?
    public static bool Activo => activo;
    public static bool Sostenido(BotonTactil b) => activo && ahora[(int)b];
    public static bool Presionado(BotonTactil b) => activo && ahora[(int)b] && !antes[(int)b];
    // Un toque nuevo fuera de los botones (sirve para avanzar los diálogos)
    public static bool ToqueLibre => activo && toqueLibre;

    Canvas canvas;
    RectTransform[] botones = new RectTransform[Total];
    Image[] imagenes = new Image[Total];
    GameObject grupoJuego, avisoGirar;
    Sprite circulo, triangulo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("[ControlesTactiles]");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<ControlesTactiles>();
        activo = Application.isMobilePlatform;
    }

    void Start()
    {
        CrearInterfaz();
        canvas.gameObject.SetActive(activo);
    }

    void Update()
    {
        for (int i = 0; i < Total; i++) { antes[i] = ahora[i]; ahora[i] = false; }
        toqueLibre = false;

        var pantalla = Touchscreen.current;
        if (pantalla == null) return;

        foreach (var toque in pantalla.touches)
        {
            if (!toque.isInProgress) continue;
            if (!activo)
            {
                // Primer toque en una computadora con pantalla táctil: se encienden los controles
                activo = true;
                canvas.gameObject.SetActive(true);
            }
            Vector2 pos = toque.position.ReadValue();
            bool sobreBoton = false;
            if (grupoJuego.activeInHierarchy)
            {
                for (int i = 0; i < Total; i++)
                {
                    if (botones[i] == null || !botones[i].gameObject.activeInHierarchy) continue;
                    if (DentroDelCirculo(botones[i], pos))
                    {
                        ahora[i] = true;
                        sobreBoton = true;
                    }
                }
            }
            if (!sobreBoton && toque.press.wasPressedThisFrame) toqueLibre = true;
        }

        if (!activo) return;

        // Solo se ven mientras se juega; en los menús se toca directo sobre los botones del juego
        bool jugando = GameManager.Instance != null && GameManager.Instance.State == GameState.Jugando
                       && SceneManager.GetActiveScene().name.StartsWith("Nivel");
        grupoJuego.SetActive(jugando);
        avisoGirar.SetActive(Screen.height > Screen.width);

        // El botón se aclara mientras se presiona
        for (int i = 0; i < Total; i++)
            if (imagenes[i] != null) imagenes[i].color = new Color(1f, 1f, 1f, ahora[i] ? 0.55f : 0.28f);
    }

    // ---------- Construcción de la interfaz (por código, igual que el resto del juego) ----------

    void CrearInterfaz()
    {
        circulo = CrearCirculo();
        triangulo = CrearTriangulo();

        var go = new GameObject("CanvasTactil");
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var escala = go.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 1f;

        grupoJuego = new GameObject("Botones");
        grupoJuego.transform.SetParent(go.transform, false);
        var rt = grupoJuego.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;

        Vector2 abajoIzq = new Vector2(0, 0), abajoDer = new Vector2(1, 0), arribaDer = new Vector2(1, 1);
        // Cruceta a la izquierda
        // (las flechas se dibujan con un triángulo porque la fuente de Unity no trae esos símbolos)
        Boton(BotonTactil.Izquierda, "", abajoIzq, new Vector2(160, 250), 205, 90f);
        Boton(BotonTactil.Derecha, "", abajoIzq, new Vector2(450, 250), 205, -90f);
        Boton(BotonTactil.Arriba, "", abajoIzq, new Vector2(305, 445), 170, 0f);
        Boton(BotonTactil.Abajo, "", abajoIzq, new Vector2(305, 75), 155, 180f);
        // Acciones a la derecha
        Boton(BotonTactil.Saltar, "Saltar", abajoDer, new Vector2(-200, 200), 275);
        Boton(BotonTactil.Hechizo1, "1", abajoDer, new Vector2(-470, 150), 190);
        Boton(BotonTactil.Hechizo2, "2", abajoDer, new Vector2(-430, 375), 190);
        Boton(BotonTactil.Hechizo3, "3", abajoDer, new Vector2(-215, 470), 190);
        Boton(BotonTactil.Esquivar, "Esquivar", abajoDer, new Vector2(-700, 110), 180);
        Boton(BotonTactil.Combo, "Combo", abajoDer, new Vector2(-680, 320), 170);
        Boton(BotonTactil.Cambiar, "Cambiar", abajoDer, new Vector2(-640, 500), 145);
        Boton(BotonTactil.Usar, "Usar", abajoDer, new Vector2(-430, 600), 145);
        Boton(BotonTactil.Pausa, "II", arribaDer, new Vector2(-100, -230), 130);

        // Aviso para girar el celular
        avisoGirar = new GameObject("GiraTuCelular");
        avisoGirar.transform.SetParent(go.transform, false);
        var fondo = avisoGirar.AddComponent<Image>();
        fondo.color = new Color(0.05f, 0.02f, 0.1f, 0.92f);
        fondo.raycastTarget = false;
        var frt = avisoGirar.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
        Texto(avisoGirar.transform, "Gira tu celular para jugar", 64);
        avisoGirar.SetActive(false);
    }

    void Boton(BotonTactil cual, string etiqueta, Vector2 ancla, Vector2 pos, float tamano, float flecha = float.NaN)
    {
        var go = new GameObject("Boton_" + cual);
        go.transform.SetParent(grupoJuego.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite = circulo;
        img.raycastTarget = false;   // no tapa los botones de los menús
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(tamano, tamano);
        if (!float.IsNaN(flecha))
        {
            // Flecha: triángulo que apunta hacia arriba, girado según la dirección
            var f = new GameObject("Flecha");
            f.transform.SetParent(go.transform, false);
            var fi = f.AddComponent<Image>();
            fi.sprite = triangulo;
            fi.color = new Color(1f, 1f, 1f, 0.9f);
            fi.raycastTarget = false;
            var frt = f.GetComponent<RectTransform>();
            frt.sizeDelta = new Vector2(tamano * 0.42f, tamano * 0.42f);
            frt.localEulerAngles = new Vector3(0f, 0f, flecha);
        }
        else Texto(go.transform, etiqueta, etiqueta.Length > 2 ? 34 : 64);
        botones[(int)cual] = rt;
        imagenes[(int)cual] = img;
    }

    // El dedo cuenta si cae dentro del círculo del botón (un poco más grande que el dibujo,
    // para que sea fácil atinarle sin ver la pantalla)
    static bool DentroDelCirculo(RectTransform rt, Vector2 pos)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, pos, null, out var local)) return false;
        float radio = rt.rect.width * 0.5f * 1.12f;
        return (local - rt.rect.center).sqrMagnitude <= radio * radio;
    }

    static void Texto(Transform padre, string texto, int tamano)
    {
        var go = new GameObject("Texto");
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<Text>();
        t.text = texto;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = tamano;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 1f, 1f, 0.9f);
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Triángulo que apunta hacia arriba, con bordes suaves
    static Sprite CrearTriangulo()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;   // 0..1
                float ancho = 1f - v;                          // se angosta hacia arriba
                float d = Mathf.Min(ancho / 2f - Mathf.Abs(u - 0.5f), v - 0.1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(d * 40f)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    // Círculo con orilla clara, dibujado píxel por píxel
    static Sprite CrearCirculo()
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01((1f - d) * 30f);                // borde suave
                float orilla = d > 0.9f ? 1f : 0.55f;                   // anillo más claro
                tex.SetPixel(x, y, new Color(orilla, orilla, orilla, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
