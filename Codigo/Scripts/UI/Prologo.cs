using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Prólogo de la historia al empezar una partida nueva: varias escenas con imagen
// y texto que se escribe poco a poco. Se avanza con Enter / X y se salta con Esc / Options.
[Serializable]
public class EscenaPrologo
{
    public Sprite imagen;
    [TextArea(3, 6)] public string texto;
}

public class Prologo : MonoBehaviour
{
    [SerializeField] private EscenaPrologo[] escenas = new EscenaPrologo[0];
    [SerializeField] private Image imagen;
    [SerializeField] private Text texto;
    [SerializeField] private Text ayuda;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private float letrasPorSegundo = 45f;

    int indice;
    float visibles;
    float fade;

    void Start()
    {
        Mostrar(0);
    }

    void Mostrar(int i)
    {
        indice = i;
        visibles = 0f;
        fade = 0f;
        imagen.sprite = escenas[i].imagen;
        imagen.enabled = escenas[i].imagen != null;
        ayuda.text = $"{Controles.TextoAceptar} continuar     {(Controles.HayControl ? "Options" : "Esc")} saltar";
    }

    void Update()
    {
        if (escenas.Length == 0) { Terminar(); return; }

        fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime * 1.5f);
        if (grupo != null) grupo.alpha = fade;

        string t = escenas[indice].texto;
        visibles = Mathf.Min(t.Length, visibles + letrasPorSegundo * Time.unscaledDeltaTime);
        texto.text = t.Substring(0, (int)visibles);

        if (Controles.PausaPresionado) { Terminar(); return; }

        if (Controles.AceptarPresionado || Controles.InteractuarPresionado || Controles.LanzarPresionado)
        {
            if (visibles < t.Length) visibles = t.Length;
            else if (indice + 1 < escenas.Length) Mostrar(indice + 1);
            else Terminar();
        }
    }

    void Terminar()
    {
        enabled = false;
        SceneManager.LoadScene(GameManager.FirstLevel);
    }
}
