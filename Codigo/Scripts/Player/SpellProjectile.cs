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
        // El arte apunta a la derecha: se gira hacia donde va el hechizo (y se voltea para no quedar de cabeza)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = data.color;
            sr.flipX = false;
            sr.flipY = direction.x < 0f;
        }

        var trail = GetComponent<EstelaProyectil>();
        if (trail != null) trail.SetColor(ElementoColor.Get(data.element) * new Color(1f, 1f, 1f, 0.7f));

        Destroy(gameObject, data.lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (data == null) return;
        if (other.CompareTag("Player") || other.GetComponent<SpellProjectile>() != null) return;

        if (other.CompareTag("Ground"))
        {
            Particula.Rafaga(transform.position, ElementoColor.Get(data.element), 6, 3f, 0.14f, 0.3f);
            Destroy(gameObject);
            return;
        }

        var health = other.GetComponentInParent<Health>();
        if (health == null || health.CompareTag("Player")) return;

        health.TakeDamage(damage, data.element);
        AudioManager.Play(Sfx.GolpeEnemigo, 0.7f);

        var enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            if (data.slowFactor < 1f) enemy.ApplySlow(data.slowFactor, data.slowDuration);
            float push = data.knockback > 0f ? data.knockback : 2.5f;
            enemy.Knockback(new Vector2(direction.x, 0.3f) * push);
        }

        Particula.Rafaga(transform.position, ElementoColor.Get(data.element), 10, 5f, 0.18f, 0.35f);
        CameraFollow.Shake(0.06f, 0.08f);
        Controles.Vibrar(0.15f, 0.3f, 0.06f);

        if (!piercing) Destroy(gameObject);
    }
}
