using UnityEngine;

// Cofre secreto del Ala de Aprendizaje: se abre con la Llave Rúnica (E)
// y da una mejora cosmética: la capa de Lira toma un tono dorado.
public class SecretChest : MonoBehaviour
{
    [SerializeField] private float openRange = 1.8f;
    [SerializeField] private Color capeTint = new Color(1f, 0.92f, 0.7f);

    private Transform player;
    private bool opened;
    private bool promptShown;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Si ya se abrió en una partida anterior se aplica el tono directamente
        if (GameManager.Instance != null && GameManager.Instance.HasCollectible("Capa dorada"))
        {
            opened = true;
            ApplyTint();
        }
    }

    void Update()
    {
        if (opened || player == null) return;
        bool near = Vector2.Distance(player.position, transform.position) <= openRange;
        var ui = UIManager.Instance;
        if (ui != null && near != promptShown)
        {
            if (near) ui.ShowPrompt($"{Controles.TextoInteractuar}  Abrir cofre");
            else ui.HidePrompt();
            promptShown = near;
        }
        if (!near || !Controles.InteractuarPresionado) return;

        var gm = GameManager.Instance;
        if (gm == null || !gm.HasRunicKey)
        {
            if (gm != null) gm.ShowMessage("El cofre está sellado con una runa. Necesitas la Llave Rúnica.");
            return;
        }

        opened = true;
        if (ui != null) ui.HidePrompt();
        Particula.Rafaga(transform.position, new Color(1f, 0.85f, 0.3f, 1f), 25, 5f, 0.18f, 0.8f, 3f);
        gm.AddCollectible("Capa dorada");
        AudioManager.Play(Sfx.Cofre);
        gm.ShowMessage("Cofre abierto: la capa de Lira brilla con un tono dorado");
        AreaEffect.Spawn(transform.position, 1.5f, new Color(1f, 0.85f, 0.3f, 0.7f), 0.8f);
        ApplyTint();
    }

    void ApplyTint()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.5f);
        if (player == null) return;
        var pc = player.GetComponent<PlayerController>();
        if (pc != null) pc.Tint(capeTint);
    }
}
