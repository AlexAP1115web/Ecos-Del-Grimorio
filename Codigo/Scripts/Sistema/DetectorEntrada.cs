using UnityEngine;

// Revisa en cada cuadro si el jugador está usando el teclado o el control,
// para que los textos de ayuda (HUD, diálogos, cofres) muestren las teclas correctas.
// Se crea solo al iniciar el juego.
public class DetectorEntrada : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        var go = new GameObject("[Entrada]");
        DontDestroyOnLoad(go);
        go.AddComponent<DetectorEntrada>();
    }

    void Update()
    {
        Controles.ActualizarDispositivo();
    }
}
