using UnityEngine;
using UnityEngine.UI;

// Llena una lista de la interfaz con los 10 logros. Los que faltan se ven apagados.
// La usan el menú principal y el menú de pausa.
public static class AchievementsList
{
    public static void Fill(Transform list, Font font)
    {
        var am = AchievementManager.Instance;
        if (am == null || list == null) return;

        foreach (Transform child in list) Object.Destroy(child.gameObject);

        // Las filas usan el ancho de la lista para que el texto no se salga del marco
        float width = ((RectTransform)list).rect.width;
        if (width < 100f) width = 900f;

        foreach (var logro in am.Logros)
        {
            bool got = am.IsUnlocked(logro.tipo);

            var row = new GameObject(logro.nombre, typeof(RectTransform));
            row.transform.SetParent(list, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 64);

            var iconGo = new GameObject("Icono", typeof(RectTransform));
            iconGo.transform.SetParent(row.transform, false);
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = logro.icono;
            icon.preserveAspect = true;
            icon.color = got ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.8f);

            var textGo = new GameObject("Texto", typeof(RectTransform));
            textGo.transform.SetParent(row.transform, false);
            textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(width - 76, 62);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 24;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = got ? new Color(1f, 0.9f, 0.6f) : new Color(0.7f, 0.7f, 0.7f);
            text.text = $"<b>{logro.nombre}</b>  {(got ? "(obtenido)" : "")}\n{logro.condicion}";
        }
    }
}
