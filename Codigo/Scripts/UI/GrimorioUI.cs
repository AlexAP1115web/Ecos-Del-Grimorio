using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Pantalla "Grimorio" del menú de pausa: muestra los hechizos, los combos, las mejoras
// y las armas de los enemigos. Lo que todavía no se consigue aparece apagado.
public class GrimorioUI : MonoBehaviour
{
    [SerializeField] private Font font;
    [SerializeField] private RectTransform contenido;
    [SerializeField] private SpellData[] hechizos = new SpellData[4];
    [Tooltip("Arte de los combos: Explosión Arcana, Vapor Cegador, Granizo Cortante, Tormenta de Ascuas")]
    [SerializeField] private Sprite[] combos = new Sprite[4];
    [SerializeField] private ItemData[] mejoras = new ItemData[3];
    [SerializeField] private Sprite esquive;
    [Tooltip("Garras de Tinta, Lanza Incandescente, Embestida Rocosa, Esquirlas de Hielo")]
    [SerializeField] private Sprite[] armas = new Sprite[4];

    static readonly Color Dorado = new Color(1f, 0.88f, 0.6f);
    static readonly Color Apagado = new Color(0.55f, 0.55f, 0.6f);

    void OnEnable()
    {
        if (contenido == null) return;
        foreach (Transform child in contenido) Destroy(child.gameObject);

        var player = GameObject.FindGameObjectWithTag("Player");
        var caster = player != null ? player.GetComponent<SpellCaster>() : null;
        var gm = GameManager.Instance;
        var upgrades = new HashSet<TipoItem>();
        if (gm != null) foreach (var u in gm.Upgrades) upgrades.Add(u);
        bool Tiene(Elemento e) => caster != null ? caster.IsUnlocked(e) : e == Elemento.Arcano;

        float izq = -340f, der = 340f;

        // ---- Hechizos
        Titulo("HECHIZOS", izq, 320f);
        for (int i = 0; i < hechizos.Length; i++)
        {
            var s = hechizos[i];
            if (s == null) continue;
            bool ok = Tiene(s.element);
            string detalle = ok ? $"Maná {s.manaCost:0}  ·  Daño {s.damage:0}  ·  {Efecto(s.element)}" : DondeSeConsigue(s.element);
            Fila(s.icon, s.spellName, detalle, izq, 255f - i * 72f, ok);
        }

        // ---- Mejoras y habilidades
        Titulo("MEJORAS Y HABILIDADES", izq, -45f);
        Fila(esquive, "Esquive", "Shift o Círculo: un instante sin recibir daño", izq, -105f, true);
        string[] efectos = { "+30% de daño con Fuego", "Recibes menos daño de fuego", "Saltas más alto" };
        for (int i = 0; i < mejoras.Length; i++)
        {
            var m = mejoras[i];
            if (m == null) continue;
            bool ok = upgrades.Contains(m.type);
            Fila(m.icon, m.itemName, ok ? efectos[i] : "Recompensa de un guardián", izq, -170f - i * 62f, ok);
        }

        // ---- Combos
        Titulo("COMBOS", der, 320f);
        var pares = new (Elemento a, Elemento b, string nombre, string efecto)[]
        {
            (Elemento.Arcano, Elemento.Fuego, "Explosión Arcana", "Arcano + Fuego: explosión de mucho daño"),
            (Elemento.Fuego, Elemento.Hielo, "Vapor Cegador", "Fuego + Hielo: aturde a los enemigos"),
            (Elemento.Hielo, Elemento.Viento, "Granizo Cortante", "Hielo + Viento: atraviesa enemigos"),
            (Elemento.Fuego, Elemento.Viento, "Tormenta de Ascuas", "Fuego + Viento: quema todo alrededor"),
        };
        for (int i = 0; i < pares.Length; i++)
        {
            bool ok = Tiene(pares[i].a) && Tiene(pares[i].b);
            Fila(i < combos.Length ? combos[i] : null, pares[i].nombre, pares[i].efecto, der, 255f - i * 72f, ok);
        }
        Texto($"Lanza los dos hechizos seguidos, o usa el combo rápido ({Controles.TextoCombo})", der, -22f, 620f, 22, Dorado);

        // ---- Armas de los enemigos
        Titulo("ARMAS DE LOS ENEMIGOS", der, -70f);
        string[] nombres = { "Garras de Tinta Corrosiva", "Lanza Incandescente", "Embestida Rocosa", "Esquirlas de Hielo" };
        string[] quien = { "Espectros de tinta: zarpazo de cerca", "Centinelas de Ceniza: estocada de fuego",
                           "Golems de Piedra: embestida", "Cristales Vivientes: disparo que congela" };
        for (int i = 0; i < nombres.Length; i++)
            Fila(i < armas.Length ? armas[i] : null, nombres[i], quien[i], der, -130f - i * 62f, true);

        // ---- Resumen
        if (gm != null && player != null)
        {
            var h = player.GetComponent<Health>();
            Texto($"Páginas Perdidas: {gm.PagesFound}/15     Fragmentos: {gm.Fragments}/5     Vida máxima: {(h != null ? h.MaxHealth : 0):0}",
                  0f, -350f, 1300f, 26, Color.white);
        }
    }

    static string Efecto(Elemento e)
    {
        switch (e)
        {
            case Elemento.Fuego: return "quema fuerte";
            case Elemento.Hielo: return "ralentiza";
            case Elemento.Viento: return "empuja";
            default: return "rápido";
        }
    }

    static string DondeSeConsigue(Elemento e)
    {
        switch (e)
        {
            case Elemento.Fuego: return "Se aprende al terminar el Ala de Aprendizaje";
            case Elemento.Hielo: return "Se aprende al vencer a Kaelor";
            case Elemento.Viento: return "Se aprende al vencer a Isolde";
            default: return "";
        }
    }

    void Titulo(string texto, float x, float y) => Texto($"<b>{texto}</b>", x, y, 620f, 30, Dorado);

    Text Texto(string texto, float x, float y, float ancho, int size, Color color)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(contenido, false);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(ancho, size + 14);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.text = texto;
        t.supportRichText = true;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        go.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return t;
    }

    void Fila(Sprite icono, string nombre, string detalle, float x, float y, bool activo)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(contenido, false);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(620f, 62f);

        var iconGo = new GameObject("Icono", typeof(RectTransform));
        var irt = iconGo.GetComponent<RectTransform>();
        irt.SetParent(rt, false);
        irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0f, 0.5f);
        irt.anchoredPosition = Vector2.zero;
        irt.sizeDelta = new Vector2(56f, 56f);
        var img = iconGo.AddComponent<Image>();
        img.sprite = icono;
        img.preserveAspect = true;
        img.enabled = icono != null;
        img.color = activo ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.9f);
        img.raycastTarget = false;

        var textGo = new GameObject("Texto", typeof(RectTransform));
        var trt = textGo.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0f, 0.5f);
        trt.anchoredPosition = new Vector2(68f, 0f);
        trt.sizeDelta = new Vector2(552f, 62f);
        var t = textGo.AddComponent<Text>();
        t.font = font;
        t.fontSize = 23;
        t.supportRichText = true;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = activo ? Color.white : Apagado;
        t.text = $"<b><color=#{ColorUtility.ToHtmlStringRGB(activo ? Dorado : Apagado)}>{nombre}</color></b>\n{(activo ? detalle : (string.IsNullOrEmpty(detalle) ? "Bloqueado" : detalle))}";
        t.raycastTarget = false;
    }
}
