using UnityEngine;

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

    private Transform player;
    private bool talked;
    private bool promptShown;

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
        ui.StartDialogue(speakerName, portrait, lines, null);
    }
}
