using UnityEngine;

// Enemigo aéreo. Flota de forma errática alrededor de su punto de origen y,
// cuando ve a Lira, se lanza en picada contra ella.
// - Aves de Tormenta / Golems de Piedra Suspendida: embestida y regreso.
// - Motas Corruptas: activar explodeOnContact (explotan y dañan en área).
public class FlyingEnemyAI : EnemyBase
{
    [Header("Vuelo")]
    [SerializeField] private float hoverRadius = 1.5f;
    [SerializeField] private float hoverSpeed = 1.5f;
    [Tooltip("Si es verdadero, se acerca poco a poco a Lira en lugar de quedarse en su zona")]
    [SerializeField] private bool driftTowardsPlayer = false;

    [Header("Embestida")]
    [SerializeField] private float diveSpeed = 8f;
    [SerializeField] private float diveDuration = 0.6f;
    [SerializeField] private float diveCooldown = 2.5f;

    [Header("Explosión (Motas Corruptas)")]
    [SerializeField] private bool explodeOnContact = false;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private float explosionDamage = 20f;

    private Vector2 homePosition;
    private float seed;
    private float diveEndTime;
    private float nextDiveTime;
    private Vector2 diveDirection;
    private bool exploded;

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0f;
    }

    protected override void Start()
    {
        base.Start();
        homePosition = transform.position;
        seed = Random.Range(0f, 100f);
    }

    protected override void Think()
    {
        if (Time.time < diveEndTime)
        {
            rb.linearVelocity = diveDirection * diveSpeed * SpeedMultiplier;
            return;
        }

        bool seesPlayer = player != null && DistanceToPlayer < detectionRange;

        if (seesPlayer && !explodeOnContact && Time.time >= nextDiveTime)
        {
            diveDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
            diveEndTime = Time.time + diveDuration;
            nextDiveTime = Time.time + diveCooldown;
            FaceTowards(player.position.x);
            return;
        }

        if (seesPlayer && driftTowardsPlayer)
            homePosition = Vector2.MoveTowards(homePosition, player.position, moveSpeed * SpeedMultiplier * Time.deltaTime);

        // Movimiento errático con ruido de Perlin alrededor del punto de origen
        float t = Time.time * hoverSpeed;
        Vector2 offset = new Vector2(Mathf.PerlinNoise(seed, t) - 0.5f, Mathf.PerlinNoise(t, seed) - 0.5f) * 2f * hoverRadius;
        Vector2 target = homePosition + offset;
        rb.linearVelocity = (target - (Vector2)transform.position) * 3f * SpeedMultiplier;

        if (player != null) FaceTowards(player.position.x);
    }

    protected override void OnCollisionStay2D(Collision2D collision)
    {
        if (explodeOnContact && collision.collider.CompareTag("Player"))
        {
            Explode();
            return;
        }
        base.OnCollisionStay2D(collision);
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;
        AreaEffect.Spawn(transform.position, explosionRadius, new Color(0.7f, 0.2f, 0.9f, 0.8f), 0.4f);
        AreaEffect.Damage(transform.position, explosionRadius, explosionDamage, Elemento.Arcano, true);
        health.Kill();
    }
}
