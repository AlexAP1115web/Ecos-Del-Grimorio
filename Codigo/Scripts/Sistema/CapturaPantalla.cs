using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Capturas de pantalla para las evidencias del proyecto.
// F12 (o el botón Share del control) guarda una imagen PNG en la carpeta "Capturas":
//  - En el editor: junto a la carpeta Assets del proyecto.
//  - En el .exe: en la carpeta de datos del juego (se muestra la ruta al guardar).
// Se crea sola al iniciar el juego, no hace falta ponerla en ninguna escena.
public class CapturaPantalla : MonoBehaviour
{
    static CapturaPantalla instancia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("[Capturas]");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<CapturaPantalla>();

        // El panel de depuración de Unity (Ctrl + Backspace o L3 + R3) se abre por accidente
        // al jugar con el control, así que lo desactivamos dentro del juego.
        UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
    }

    public static string Carpeta
    {
        get
        {
            string raiz = Application.isEditor
                ? Directory.GetParent(Application.dataPath).FullName
                : Application.persistentDataPath;
            return Path.Combine(raiz, "Capturas");
        }
    }

    void Update()
    {
        bool tecla = Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame;
        bool boton = Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;
        if (tecla || boton) StartCoroutine(Tomar());
    }

    IEnumerator Tomar()
    {
        Directory.CreateDirectory(Carpeta);
        string nombre = $"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string ruta = Path.Combine(Carpeta, nombre);

        // Se espera al final del cuadro para que la imagen salga completa (con HUD)
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(ruta);
        yield return null;
        yield return null;

        AudioManager.Play(Sfx.MenuAceptar, 0.6f);
        if (UIManager.Instance != null)
            UIManager.Instance.ShowMessage(Application.isEditor ? $"Captura guardada: {nombre}" : $"Captura guardada en {Carpeta}", 2.5f);
        Debug.Log($"Captura guardada en {ruta}");
    }
}
