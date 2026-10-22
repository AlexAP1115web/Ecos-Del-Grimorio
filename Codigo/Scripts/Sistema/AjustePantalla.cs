using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ajusta la interfaz a pantallas más anchas que 16:9 (la mayoría de los celulares son 19.5:9 o 20:9).
// Todas las pantallas del juego se diseñaron en 1920 x 1080. En una pantalla muy ancha el Canvas Scaler
// recortaba arriba y abajo (por ejemplo el texto de ayuda del menú principal), así que aquí se elige
// si la escala sigue el alto o el ancho para que siempre quepa el área completa de 1920 x 1080.
public class AjustePantalla : MonoBehaviour
{
    static AjustePantalla instancia;
    float ultimoAspecto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("[AjustePantalla]");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<AjustePantalla>();
        SceneManager.sceneLoaded += (e, m) => Aplicar();
        Aplicar();
    }

    // Revisa si se giró el celular o se cambió el tamaño de la ventana
    void Update()
    {
        float aspecto = (float)Screen.width / Mathf.Max(1, Screen.height);
        if (Mathf.Abs(aspecto - ultimoAspecto) > 0.01f)
        {
            ultimoAspecto = aspecto;
            Aplicar();
        }
    }

    static void Aplicar()
    {
        float aspecto = (float)Screen.width / Mathf.Max(1, Screen.height);
        // Más ancha que 16:9 → la escala sigue el alto; más angosta → sigue el ancho
        float match = aspecto > 16f / 9f ? 1f : 0f;
        foreach (var escala in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include))
            if (escala.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                escala.matchWidthOrHeight = match;
    }
}
