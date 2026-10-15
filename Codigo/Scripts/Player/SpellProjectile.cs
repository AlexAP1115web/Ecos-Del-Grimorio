using UnityEngine;

// Proyectil de los hechizos de Lira. Los datos (daño, velocidad, color, efectos)
// vienen del SpellData con el que se lanza.
// Con la ayuda para apuntar el proyectil se curva poco a poco hacia un enemigo
// (sirve sobre todo contra las aves, que se mueven mucho).
[RequireComponent(typeof(Rigidbody2D))]
public class SpellProjectile : MonoBehaviour
{
    private SpellData data;
    private float damage;
    private Vector2 direction;
    private bool piercing;
    private Rigidbody2D rb;
    private float velocidad;
    private SpriteRenderer sr;

    // Enemigo al que sigue (null = va en línea recta)
    private Collider2D objetivo;
    private float giro;

    public void Init(SpellData spell, Vector2 dir, float finalDamage, bool pierce = false, float speedMultiplier = 1f)
    {
        data = spell;
        damage = finalDamage;
        direction = dir.normalized;
        piercing = pierce;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        velocidad = data.projectileSpeed * speedMultiplier;
        rb.linearVelocity = direction * velocidad;

        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = data.color;
            sr.flipX = false;
        }
        Orientar();

        var trail = GetComponent<EstelaProyectil>();
        if (trail != null) trail.SetColor(ElementoColor.Get(data.element) * new Color(1f, 1f, 1f, 0.7f));

        Destroy(gameObject, data.lifetime);
    }

    // El arte apunta a la derecha: se gira hacia donde va el hechizo (y se voltea para no quedar de cabeza)
    void Orientar()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (sr != null) sr.flipY = direction.x < 0f;
    }

    // Ayuda para apuntar: el hechizo gira hasta "gradosPorSegundo" para alcanzar al enemigo
    public void Seguir(Collider2D enemigo, float gradosPorSegundo)
    {
        objetivo = enemigo;
        giro = gradosPorSegundo;
    }

    void FixedUpdate()
    {
        if (objetivo == null || rb == null) return;
        if (!objetivo.enabled || !objetivo.gameObject.activeInHierarchy) { objetivo = null; return; }

        Vector2 hacia = (Vector2)objetivo.bounds.center - rb.position;
        if (hacia.sqrMagnitude < 0.04f) return;
        direction = ((Vector2)Vector3.RotateTowards(direction, hacia.normalized, giro * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f)).normalized;
        rb.linearVelocity = direction * velocidad;
        Orientar();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (data == null) return;
        if (other.CompareTag("Player") || other.GetComponent<SpellProjectile>() != null) return;

        // El viento empuja cajas y objetos sueltos
        var empujable = other.GetComponentInParent<Empujable>();
        if (empujable != null)
        {
            empujable.Empujar(direction, data.element);
            Particula.Rafaga(transform.position, ElementoColor.Get(data.element), 8, 4f, 0.14f, 0.3f);
            if (!piercing) Destroy(gameObject);
            return;
        }

        // Las puertas rúnicas no se rompen: solo avisan cómo se abren
        var puerta = other.GetComponentInParent<PuertaRunica>();
        if (puerta != null)
        {
            puerta.Pista();
            Particula.Rafaga(transform.position, ElementoColor.Get(data.element), 6, 3f, 0.14f, 0.3f);
            Destroy(gameObject);
            return;
        }

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
            if (data.element == Elemento.Hielo) enemy.GolpeDeHielo();
            float push = data.knockback > 0f ? data.knockback : 2.5f;
            enemy.Knockback(new Vector2(direction.x, 0.3f) * push);
        }

        Particula.Rafaga(transform.position, ElementoColor.Get(data.element), 10, 5f, 0.18f, 0.35f);
        CameraFollow.Shake(0.06f, 0.08f);
        Controles.Vibrar(0.15f, 0.3f, 0.06f);

        if (!piercing) Destroy(gameObject);
    }
}
