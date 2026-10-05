using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Créditos / Epílogo: se muestra al derrotar al Eco de la Archimaga Elenora.
// El texto del final cambia según el porcentaje de coleccionables obtenidos.
public class CreditsScreen : MonoBehaviour
{
    [SerializeField] private RectTransform scrollingText;
    [SerializeField] private Text textComponent;
    [SerializeField] private float scrollSpeed = 70f;
    [SerializeField] private float endY = 2400f;

    private float inicio;
    private float terminoEn = -1f;

    void Start()
    {
        inicio = Time.unscaledTime;
        float percent = GameManager.Instance != null ? GameManager.Instance.CompletionPercent : 0f;
        bool fromGame = GameManager.Instance != null && GameManager.Instance.HasCollectible("Grimorio Completo");

        string epilogue;
        if (!fromGame)
            epilogue = "";
        else if (percent >= 1f)
            epilogue = "El grimorio está completo. Lira entiende por fin lo que buscaba la Archimaga Elenora\n" +
                       "y sella los ecos para siempre. La Torre de Cristal vuelve a brillar.\n\n";
        else if (percent >= 0.5f)
            epilogue = "Los ecos se apagan, pero algunas páginas del grimorio siguen perdidas en la Torre.\n" +
                       "Lira sabe que su historia aún no termina.\n\n";
        else
            epilogue = "Lira detiene al eco de Elenora, aunque la verdad sobre la Archimaga\n" +
                       "queda oculta entre las páginas que no encontró.\n\n";

        if (textComponent != null)
        {
            textComponent.text = epilogue +
                $"Coleccionables: {Mathf.RoundToInt(percent * 100f)}%\n\n\n" +
                "ECOS DEL GRIMORIO\n\n" +
                "Diseño, programación y documentación\nPérez Alcántara Alejandro\n\n" +
                "Creación de Videojuegos — 10° D\nUniversidad Tecnológica de Puebla\n\n" +
                "Docente\nJosé Francisco Espinosa Garita\n\n" +
                "Música, voces y efectos de sonido\nOriginales, creados por síntesis de audio\n\n" +
                "Motor\nUnity 6\n\n\n" +
                "FIN\n\nGracias por jugar\n\n(Enter o X para volver al menú)";
        }
    }

    void LateUpdate()
    {
        // El texto empieza justo en la parte de abajo de la pantalla para que se vea desde el principio
        if (!colocado && scrollingText != null)
        {
            colocado = true;
            scrollingText.anchoredPosition = new Vector2(scrollingText.anchoredPosition.x, 160f);
            if (textComponent != null) endY = Mathf.Max(endY, textComponent.preferredHeight + 1150f);
        }
    }

    private bool colocado;

    void Update()
    {
        if (scrollingText != null && colocado)
        {
            if (scrollingText.anchoredPosition.y < endY)
                scrollingText.anchoredPosition += Vector2.up * scrollSpeed * Time.unscaledDeltaTime;
            else if (terminoEn < 0f)
                terminoEn = Time.unscaledTime;
        }

        // Al terminar los créditos regresa solo al menú
        if (terminoEn > 0f && Time.unscaledTime - terminoEn > 3f)
        {
            SceneManager.LoadScene(GameManager.MenuScene);
            return;
        }

        // Los primeros segundos no se pueden saltar (para no perderse el final por presionar X)
        if (Time.unscaledTime - inicio > 3f && (Controles.AceptarPresionado || Controles.PausaPresionado))
            SceneManager.LoadScene(GameManager.MenuScene);
    }
}
