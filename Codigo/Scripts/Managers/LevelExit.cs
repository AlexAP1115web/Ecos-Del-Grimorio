using UnityEngine;

// salida del nivel, no abre hasta vencer al enemigo asignado
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] private string nextScene = "Nivel2_AlaDeFuego";
    [SerializeField] private Health requiredDefeat;
    [SerializeField] private bool unlocksSpell = true;
    [SerializeField] private Elemento spellToUnlock = Elemento.Fuego;
    [SerializeField] private SpriteRenderer portalRenderer;

    private bool used;

    bool IsOpen => requiredDefeat == null || requiredDefeat.IsDead;

    void Update()
    {
        if (portalRenderer != null)
        {
            var c = portalRenderer.color;
            c.a = IsOpen ? 0.6f + Mathf.Sin(Time.time * 4f) * 0.2f : 0.15f;
            portalRenderer.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag("Player")) return;

        if (!IsOpen)
        {
            Debug.Log("La salida está sellada. Derrota al guardián de la sala.");
            return;
        }

        used = true;
        AudioManager.Play(Sfx.Portal);
        if (GameManager.Instance != null)
            GameManager.Instance.CompleteLevel(nextScene, spellToUnlock, unlocksSpell);
    }
}
