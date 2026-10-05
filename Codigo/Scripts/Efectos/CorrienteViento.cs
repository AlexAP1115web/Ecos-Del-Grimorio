using UnityEngine;

// corriente de aire que sube a Lira (ala de viento)
[RequireComponent(typeof(BoxCollider2D))]
public class CorrienteViento : MonoBehaviour
{
    [SerializeField] private float fuerza = 9f;
    [SerializeField] private float aceleracion = 40f;
    [SerializeField] private Color colorParticulas = new Color(0.8f, 1f, 0.85f, 0.6f);

    private BoxCollider2D box;
    private float nextFx;
    private float nextSound;

    void Start()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    void Update()
    {
        // rayitas de aire que suben
        if (Time.time >= nextFx)
        {
            nextFx = Time.time + 0.08f;
            Vector2 b = box.bounds.min;
            var pos = new Vector2(Random.Range(box.bounds.min.x, box.bounds.max.x), b.y + Random.Range(0f, 1f));
            Particula.Rafaga(pos, colorParticulas, 1, 0.3f, 0.06f, 1.2f, -5f);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || other.attachedRigidbody == null) return;
        var rb = other.attachedRigidbody;
        var v = rb.linearVelocity;
        rb.linearVelocity = new Vector2(v.x, Mathf.MoveTowards(v.y, fuerza, aceleracion * Time.fixedDeltaTime * 2f));
        if (Time.time >= nextSound)
        {
            nextSound = Time.time + 1.2f;
            AudioManager.Play(Sfx.Viento2, 0.5f);
        }
    }
}
