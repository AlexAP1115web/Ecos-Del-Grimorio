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

    private float knockbackUntil;
    private float breatheOffset;
    private Transform visual;
    private Vector3 visualBaseScale;
    private float punch;
    private Transform barRoot;
    private Transform barFill;
    private float barVisibleUntil;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
            if (spriteRenderer.transform != transform)
            {
                visual = spriteRenderer.transform;
                visualBaseScale = visual.localScale;
            }
        }
        breatheOffset = Random.Range(0f, 6f);
        if (data != null) ApplyData();
    }

    protected virtual void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        health.OnDeath.AddListener(OnDeath);
        health.Damaged += OnDamaged;
        if (!(this is BossController)) CreateHealthBar();
    }

    // Barra de vida pequeña encima del enemigo; solo aparece después de recibir daño
    void CreateHealthBar()
    {
        if (spriteRenderer == null) return;
        float top = spriteRenderer.bounds.max.y - transform.position.y + 0.25f;
        barRoot = new GameObject("BarraVida").transform;
        barRoot.SetParent(transform, false);
        barRoot.position = transform.position + Vector3.up * top;
        barRoot.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f);

        var bg = new GameObject("Fondo").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(barRoot, false);
        bg.sprite = AreaEffect.GetSquareSprite();
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        bg.sortingOrder = 30;
        bg.transform.localScale = new Vector3(1.1f, 0.14f, 1f);

        var fill = new GameObject("Vida").AddComponent<SpriteRenderer>();
        fill.transform.SetParent(barRoot, false);
        fill.sprite = AreaEffect.GetSquareSprite();
        fill.color = new Color(0.9f, 0.2f, 0.25f);
        fill.sortingOrder = 31;
        barFill = fill.transform;
        barFill.localScale = new Vector3(1f, 0.1f, 1f);

        barRoot.gameObject.SetActive(false);
    }

    void OnDamaged(float amount)
    {
        punch = 1f;
        barVisibleUntil = Time.time + 3f;
        if (spriteRenderer != null)
            Particula.Rafaga(spriteRenderer.bounds.center, new Color(1f, 0.95f, 0.8f, 0.9f), 6, 4f, 0.12f, 0.3f);
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
            // Destello rojo al recibir daño, azul si está ralentizado, gris si está aturdido
            if (Time.time - health.LastHitTime < 0.1f) spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f);
            else if (IsStunned) spriteRenderer.color = baseColor * new Color(0.6f, 0.6f, 0.6f, 1f);
            else if (Time.time < slowUntil) spriteRenderer.color = baseColor * new Color(0.6f, 0.8f, 1f, 1f);
            else spriteRenderer.color = baseColor;
        }

        AnimateVisual();

        if (barRoot != null)
        {
            bool show = Time.time < barVisibleUntil;
            if (barRoot.gameObject.activeSelf != show) barRoot.gameObject.SetActive(show);
            float pct = Mathf.Clamp01(health.Percent);
            barFill.localScale = new Vector3(pct, 0.1f, 1f);
            barFill.localPosition = new Vector3(-(1f - pct) * 0.5f, 0f, 0f);
        }

        if (Time.time < knockbackUntil) return;

        if (IsStunned)
        {
            rb.linearVelocity = new Vector2(0f, rb.gravityScale > 0f ? rb.linearVelocity.y : 0f);
            return;
        }

        Think();
    }

    // Comportamiento propio de cada enemigo
    protected abstract void Think();

    // Respira en reposo y se "aplasta" un momento al recibir un golpe
    void AnimateVisual()
    {
        if (visual == null) return;
        punch = Mathf.MoveTowards(punch, 0f, 6f * Time.deltaTime);
        float breathe = Mathf.Sin(Time.time * 3f + breatheOffset) * 0.02f;
        visual.localScale = new Vector3(
            visualBaseScale.x * (1f + punch * 0.2f),
            visualBaseScale.y * (1f - punch * 0.15f + breathe),
            visualBaseScale.z);
    }

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
        if (this is BossController) force *= 0.2f; // los jefes casi no retroceden
        rb.linearVelocity = force / Mathf.Max(rb.mass, 1f);
        knockbackUntil = Time.time + 0.15f;
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (IsStunned || contactDamage <= 0f || !collision.collider.CompareTag("Player")) return;
        var playerHealth = collision.collider.GetComponentInParent<Health>();
        if (playerHealth == null || playerHealth.IsInvulnerable) return;

        playerHealth.TakeDamage(contactDamage);

        // Empuja a Lira hacia atrás para que el golpe se sienta
        var controller = playerHealth.GetComponent<PlayerController>();
        float dir = Mathf.Sign(playerHealth.transform.position.x - transform.position.x);
        if (controller != null) controller.Push(new Vector2(dir * 9f, 6f));
    }

    protected virtual void OnDeath()
    {
        Vector2 center = spriteRenderer != null ? (Vector2)spriteRenderer.bounds.center : (Vector2)transform.position;
        Particula.Rafaga(center, baseColor * new Color(0.8f, 0.6f, 1f, 1f), 18, 6f, 0.2f, 0.6f, 4f);
        Particula.Rafaga(center, new Color(0.1f, 0.05f, 0.15f, 0.9f), 10, 3f, 0.3f, 0.8f);
        CameraFollow.Shake(this is BossController ? 0.5f : 0.12f, this is BossController ? 0.6f : 0.15f);
        Controles.Vibrar(0.4f, 0.4f, this is BossController ? 0.5f : 0.12f);
        if (GameManager.Instance != null) GameManager.Instance.HitStop(this is BossController ? 0.25f : 0.05f);

        if (data != null && data.dropPrefab != null && Random.value <= data.dropChance)
            Instantiate(data.dropPrefab, transform.position, Quaternion.identity);
    }
}
