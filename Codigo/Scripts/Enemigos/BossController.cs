using System;
using UnityEngine;

// Base para los jefes (Kaelor, Isolde, Threnody y el Eco de la Archimaga Elenora).
// Se activa cuando Lira entra a su arena, cambia de fase cuando la vida baja de ciertos
// porcentajes y ataca cada cierto tiempo. Al morir suelta sus recompensas.
public abstract class BossController : EnemyBase
{
    [Header("Jefe")]
    [SerializeField] protected string bossName = "Jefe";
    [Tooltip("Porcentajes de vida en los que cambia de fase, de mayor a menor. Ej: 0.5 = a la mitad de vida")]
    [SerializeField] protected float[] phaseThresholds = { 0.5f };
    [SerializeField] protected float attackInterval = 2.5f;
    [SerializeField] protected float activationRange = 12f;

    [Header("Arena")]
    [SerializeField] protected float arenaMinX = -100f;
    [SerializeField] protected float arenaMaxX = 100f;
    [SerializeField] protected float hoverHeight = 2f;

    [Header("Ataques y recompensas")]
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected GameObject minionPrefab;
    [SerializeField] protected GameObject[] rewards = new GameObject[0];
    [SerializeField] protected Vector2 rewardPosition;

    public static event Action<BossController> BossActivated;
    public static event Action<string> BossDefeated;
    public event Action<int> PhaseChanged;

    public int CurrentPhase { get; private set; }
    public string BossName => bossName;
    public Health BossHealth => health;
    protected bool IsActive { get; private set; }

    private float nextAttackTime;

    protected override void Start()
    {
        base.Start();
        health.OnHealthChanged.AddListener(CheckPhase);
    }

    void CheckPhase(float percent)
    {
        while (CurrentPhase < phaseThresholds.Length && percent <= phaseThresholds[CurrentPhase])
        {
            CurrentPhase++;
            OnPhaseEnter(CurrentPhase);
            PhaseChanged?.Invoke(CurrentPhase);
        }
    }

    protected override void Think()
    {
        if (!IsActive)
        {
            if (DistanceToPlayer > activationRange) return;
            IsActive = true;
            nextAttackTime = Time.time + 1.5f;
            BossActivated?.Invoke(this);
        }

        if (player != null) FaceTowards(player.position.x);
        Move();

        if (Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + attackInterval;
        }
    }

    // Movimiento entre ataques
    protected virtual void Move() { }

    protected abstract void PerformAttack();

    protected abstract void OnPhaseEnter(int phase);

    // Vuela hacia un punto encima de Lira sin salir de la arena (Threnody y Elenora)
    protected void HoverAbovePlayer(float sideOffset, float speed)
    {
        if (player == null) return;
        float x = Mathf.Clamp(player.position.x + sideOffset, arenaMinX, arenaMaxX);
        Vector2 target = new Vector2(x, hoverHeight);
        Vector2 dir = target - (Vector2)transform.position;
        rb.linearVelocity = Vector2.ClampMagnitude(dir * 2f, speed * SpeedMultiplier);
    }

    protected void Shoot(Vector2 origin, Vector2 direction, float speed, float damage, Elemento element)
    {
        if (projectilePrefab == null) return;
        var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var p = go.GetComponent<EnemyProjectile>();
        if (p != null) p.Init(direction, speed, damage, element);
    }

    protected void ShootAtPlayer(float speed, float damage, Elemento element, float angleOffset = 0f)
    {
        if (player == null) return;
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        dir = Quaternion.Euler(0f, 0f, angleOffset) * dir;
        Shoot(transform.position, dir, speed, damage, element);
    }

    protected void SummonMinion(Vector2 offset)
    {
        if (minionPrefab != null) Instantiate(minionPrefab, (Vector2)transform.position + offset, Quaternion.identity);
    }

    protected override void OnDeath()
    {
        base.OnDeath();

        Vector2 spot = rewardPosition != Vector2.zero ? rewardPosition : (Vector2)transform.position;
        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i] == null) continue;
            Vector2 pos = spot + new Vector2((i - (rewards.Length - 1) / 2f) * 1.5f, 0f);
            Instantiate(rewards[i], pos, Quaternion.identity);
        }

        BossDefeated?.Invoke(bossName);
    }
}
