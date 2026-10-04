using UnityEngine;

// Piedra de Reaparición: al tocarla, Lira reaparece aquí si cae en combate.
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    private bool activated;
    private SpriteRenderer sr;

    void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activated || !other.CompareTag("Player")) return;
        activated = true;
        if (sr != null) sr.color = Color.white;
        AreaEffect.Spawn(transform.position, 1.2f, new Color(0.5f, 1f, 1f, 0.6f), 0.6f);
        if (GameManager.Instance != null) GameManager.Instance.SetCheckpoint(transform.position);
    }
}
