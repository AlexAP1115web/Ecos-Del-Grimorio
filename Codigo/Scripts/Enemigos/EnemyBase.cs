using UnityEngine;

// Clase base de todos los enemigos. Aquí va lo que comparten:
// referencia al jugador, daño por contacto, ralentización, aturdimiento, empuje y botín.
// Cada tipo de enemigo hereda y solo implementa su comportamiento en Think().
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [SerializeField] protected EnemyData data;
    [SerializeField] protected float contactDamage = 10f;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float detectionRange = 5f;

    protected Rigidbody2D rb;
    protected Health health;
    protected SpriteRenderer spriteRenderer;
    protected Transform player;

    private float slowMultiplier = 1f;
    private float slowUntil;
    private float stunUntil;
    private Color baseColor = Color.white;

    protected float SpeedMultiplier => Time.time < slowUntil ? slowMultiplier : 1f;
    protected bool IsStunned => Time.time < stunUntil;

    protected float DistanceToPlayer =>
        player != null ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
        if (data != null) ApplyData();
    }

    protected virtual void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        health.OnDeath.AddListener(OnDeath);
    }

    protected virtual void ApplyData()
    {
        health.SetMaxHealth(data.maxHealth, true);
        if (data.hitsToKill > 0) health.SetHitsToKill(data.hitsToKill);
        if (data.hasImmunity) health.SetResistance(data.immuneTo, 0f);
        contactDamage = data.contactDamage;
        moveSpeed = data.moveSpeed;
        detectionRange = data.detectionRange;
    }

    protected virtual void Update()
    {
        if (spriteRenderer != null)
        {
            // Tinte azul si está ralentizado, gris si está aturdido
            if (IsStunned) spriteRenderer.color = baseColor * new Color(0.6f, 0.6f, 0.6f, 1f);
            else if (Time.time < slowUntil) spriteRenderer.color = baseColor * new Color(0.6f, 0.8f, 1f, 1f);
            else spriteRenderer.color = baseColor;
        }

        if (IsStunned)
        {
            rb.linearVelocity = new Vector2(0f, rb.gravityScale > 0f ? rb.linearVelocity.y : 0f);
            return;
        }

        Think();
    }

    // Comportamiento propio de cada enemigo
    protected abstract void Think();

    protected void FaceTowards(float targetX)
    {
        if (spriteRenderer != null) spriteRenderer.flipX = targetX < transform.position.x;
    }

    public virtual void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowUntil = Time.time + duration;
    }

    public virtual void Stun(float duration)
    {
        stunUntil = Time.time + duration;
    }

    public virtual void Knockback(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (IsStunned || !collision.collider.CompareTag("Player")) return;
        var playerHealth = collision.collider.GetComponentInParent<Health>();
        if (playerHealth != null) playerHealth.TakeDamage(contactDamage);
    }

    protected virtual void OnDeath()
    {
        if (data != null && data.dropPrefab != null && Random.value <= data.dropChance)
            Instantiate(data.dropPrefab, transform.position, Quaternion.identity);
    }
}
