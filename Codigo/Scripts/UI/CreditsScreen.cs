using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Créditos / Epílogo: se muestra al derrotar al Eco de la Archimaga Elenora.
// El texto del final cambia según el porcentaje de coleccionables obtenidos.
public class CreditsScreen : MonoBehaviour
{
    [SerializeField] private RectTransform scrollingText;
    [SerializeField] private Text textComponent;
    [SerializeField] private float scrollSpeed = 60f;
    [SerializeField] private float endY = 2400f;

    void Start()
    {
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
                "Música y efectos de sonido\nOriginales, creados por síntesis de audio\n\n" +
                "Motor\nUnity 6\n\n\n" +
                "Gracias por jugar\n\n(Enter o X para volver al menú)";
        }
    }

    void Update()
    {
        if (scrollingText != null && scrollingText.anchoredPosition.y < endY)
            scrollingText.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        if (Controles.AceptarPresionado || Controles.PausaPresionado)
            SceneManager.LoadScene(GameManager.MenuScene);
    }
}
