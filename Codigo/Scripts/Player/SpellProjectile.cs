using UnityEngine;

// Proyectil de los hechizos de Lira. Los datos (daño, velocidad, color, efectos)
// vienen del SpellData con el que se lanza.
[RequireComponent(typeof(Rigidbody2D))]
public class SpellProjectile : MonoBehaviour
{
    private SpellData data;
    private float damage;
    private Vector2 direction;
    private bool piercing;
    private Rigidbody2D rb;

    public void Init(SpellData spell, Vector2 dir, float finalDamage, bool pierce = false, float speedMultiplier = 1f)
    {
        data = spell;
        damage = finalDamage;
        direction = dir.normalized;
        piercing = pierce;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * data.projectileSpeed * speedMultiplier;

        // El arte de los hechizos ya viene orientado hacia la derecha, solo se voltea
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = data.color;
            sr.flipX = direction.x < 0f;
        }

        Destroy(gameObject, data.lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (data == null) return;
        if (other.CompareTag("Player") || other.GetComponent<SpellProjectile>() != null) return;

        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
            return;
        }

        var health = other.GetComponentInParent<Health>();
        if (health == null || health.CompareTag("Player")) return;

        health.TakeDamage(damage, data.element);

        var enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            if (data.slowFactor < 1f) enemy.ApplySlow(data.slowFactor, data.slowDuration);
            if (data.knockback > 0f) enemy.Knockback(direction * data.knockback);
        }

        if (!piercing) Destroy(gameObject);
    }
}
