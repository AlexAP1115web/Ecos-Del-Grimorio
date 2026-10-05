using UnityEngine;

public enum Bendicion { Ninguna, Vida, Mana, VidaMaxima }

// Diálogo de un personaje (Maestra Sable) o de un jefe antes del combate.
// Con autoStart se inicia solo al acercarse (jefes); si no, aparece el aviso para hablar.
public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private string speakerName = "Maestra Sable";
    [SerializeField] private Sprite portrait;
    [TextArea(2, 4)]
    [SerializeField] private string[] lines =
    {
        "Ese grimorio perteneció a alguien que intentó ir más allá de lo permitido. Ten cuidado con lo que despiertas, Lira."
    };
    [SerializeField] private float talkRange = 2.5f;
    [SerializeField] private bool autoStart = false;
    [Tooltip("Regalo al terminar de hablar (espíritus de los guardianes)")]
    [SerializeField] private Bendicion bendicion = Bendicion.Ninguna;
    [SerializeField] private string mensajeBendicion = "";

    private Transform player;
    private bool talked;
    private bool promptShown;
    private bool bendecido;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    bool PlayerNear => player != null && Vector2.Distance(player.position, transform.position) < talkRange;

    void Update()
    {
        var ui = UIManager.Instance;
        if (ui == null || ui.InDialogue || Time.timeScale == 0f) return;

        bool near = PlayerNear;

        if (autoStart)
        {
            if (!talked && near) Talk(ui);
            return;
        }

        if (near && !promptShown)
        {
            ui.ShowPrompt($"{Controles.TextoInteractuar}  Hablar con {speakerName}");
            promptShown = true;
        }
        else if (!near && promptShown)
        {
            ui.HidePrompt();
            promptShown = false;
        }

        if (near && Controles.InteractuarPresionado) Talk(ui);
    }

    void Talk(UIManager ui)
    {
        talked = true;
        promptShown = false;
        ui.StartDialogue(speakerName, portrait, lines, bendicion == Bendicion.Ninguna ? null : (System.Action)Bendecir);
    }

    void Bendecir()
    {
        if (bendecido || player == null) return;
        bendecido = true;
        var h = player.GetComponent<Health>();
        var sc = player.GetComponent<SpellCaster>();
        switch (bendicion)
        {
            case Bendicion.Vida: if (h != null) h.Heal(h.MaxHealth); break;
            case Bendicion.Mana: if (sc != null) sc.RestoreMana(999f); break;
            case Bendicion.VidaMaxima: if (h != null) h.AddMaxHealth(25f); break;
        }
        AudioManager.Play(Sfx.ObjetoEspecial);
        Particula.Rafaga(player.position, new Color(0.7f, 0.9f, 1f, 1f), 24, 4f, 0.15f, 0.8f, -2f);
        if (GameManager.Instance != null && !string.IsNullOrEmpty(mensajeBendicion))
            GameManager.Instance.ShowMessage(mensajeBendicion, 3.5f);
    }
}
