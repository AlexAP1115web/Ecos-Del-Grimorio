using UnityEngine;
using UnityEngine.InputSystem;

// Diálogo sencillo de un personaje no jugable (Maestra Sable).
// Cuando Lira se acerca aparece el texto; con E avanza a la siguiente línea.
public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private string speakerName = "Maestra Sable";
    [TextArea(2, 4)]
    [SerializeField] private string[] lines =
    {
        "Ese grimorio perteneció a alguien que intentó ir más allá de lo permitido. Ten cuidado con lo que despiertas, Lira.",
        "Empecemos por lo básico. Repite conmigo el primer sello arcano.",
        "Usa A/D para moverte, Espacio para saltar y 1 para lanzar el hechizo Arcano. Con Q cambias el hechizo equipado."
    };
    [SerializeField] private float talkRange = 2.5f;

    private Transform player;
    private int index;
    private bool finished;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    bool PlayerNear => player != null && Vector2.Distance(player.position, transform.position) < talkRange;

    void Update()
    {
        if (finished || !PlayerNear) return;
        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            index++;
            if (index >= lines.Length) finished = true;
        }
    }

    void OnGUI()
    {
        if (finished || !PlayerNear || lines.Length == 0 || Time.timeScale == 0f) return;

        var style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.UpperLeft, wordWrap = true };
        style.padding = new RectOffset(16, 16, 12, 12);
        var rect = new Rect(40, Screen.height - 170, Screen.width - 80, 130);
        GUI.Box(rect, $"{speakerName}:\n{lines[index]}\n\n(E para continuar)", style);
    }
}
