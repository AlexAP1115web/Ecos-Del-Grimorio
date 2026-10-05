using UnityEngine;

// proyectil de enemigo. Solo daña a Lira
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [Tooltip("Ralentiza a Lira al impactar (1 = no ralentiza)")]
    [SerializeField] private float slowMultiplier = 0.6f;
    [SerializeField] private float slowDuration = 1.5f;
    [Tooltip("Pinta el proyectil del color de su elemento (para el círculo genérico)")]
    [SerializeField] private bool tintByElement = true;

    private float damage;
    private Elemento element;

    public void Init(Vector2 direction, float speed, float dmg, Elemento elem)
    {
        damage = dmg;
        element = elem;

        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && tintByElement) sr.color = ElementoColor.Get(elem);
        var trail = GetComponent<EstelaProyectil>();
        if (trail != null) trail.SetColor(ElementoColor.Get(elem) * new Color(1f, 1f, 1f, 0.6f));

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Player")) return;

        var health = other.GetComponentInParent<Health>();
        if (health != null) health.TakeDamage(damage, element);

        var controller = other.GetComponentInParent<PlayerController>();
        if (controller != null && element == Elemento.Hielo && slowMultiplier < 1f)
            controller.ApplySlow(slowMultiplier, slowDuration);

        Destroy(gameObject);
    }
}
