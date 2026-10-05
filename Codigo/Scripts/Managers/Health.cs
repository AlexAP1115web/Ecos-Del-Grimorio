using System;
using UnityEngine;
using UnityEngine.Events;

public interface IDamageable
{
    void TakeDamage(float amount, Elemento? element = null);
}

[Serializable]
public class ElementResistance
{
    public Elemento element;
    [Tooltip("1 = daño normal, 0.5 = la mitad, 0 = inmune")]
    [Range(0f, 2f)] public float multiplier = 1f;
}

// Vida compartida por Lira y los enemigos. Maneja resistencias por elemento,
// invulnerabilidad breve después de un golpe y el conteo por impactos (Espectro Mayor).
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int hitsToKill = 0;
    [SerializeField] private float invulnerableTime = 0f;
    [SerializeField] private bool destroyOnDeath = true;
    [Tooltip("Multiplica todo el daño recibido (Lira usa menos de 1 para que el juego sea más accesible)")]
    [SerializeField] private float damageTakenMultiplier = 1f;
    [SerializeField] private ElementResistance[] resistances = new ElementResistance[0];

    public UnityEvent<float> OnHealthChanged = new UnityEvent<float>();
    public UnityEvent OnDeath = new UnityEvent();
    public event Action<float> Damaged;
    // Se lanza cuando un golpe no hace daño por la resistencia al elemento (barreras que solo quema el fuego)
    public event Action<Elemento?> Resisted;

    private float currentHealth;
    private int hitsTaken;
    private float invulnerableUntil;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer[] blinkRenderers;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float Percent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public float LastHitTime { get; private set; } = -10f;

    // Invulnerabilidad extra (por ejemplo durante el esquive)
    public void SetInvulnerable(float seconds)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        // Lira está hecha de varias partes (cuerpo y piernas): parpadean todas
        if (invulnerableTime > 0f) blinkRenderers = GetComponentsInChildren<SpriteRenderer>();
        OnHealthChanged.Invoke(Percent);
    }

    void Update()
    {
        // Parpadeo mientras es invulnerable
        if (blinkRenderers != null && invulnerableTime > 0f)
        {
            bool blinking = Time.time < invulnerableUntil;
            bool visible = !blinking || Mathf.FloorToInt(Time.time * 15f) % 2 == 0;
            foreach (var r in blinkRenderers)
                if (r != null && r.gameObject.name != "Sombra") r.enabled = visible;
        }
    }

    public void SetMaxHealth(float value, bool refill)
    {
        maxHealth = value;
        if (refill) currentHealth = maxHealth;
        OnHealthChanged.Invoke(Percent);
    }

    // Aumenta la vida máxima (Páginas Perdidas) y cura esa misma cantidad
    public void AddMaxHealth(float extra)
    {
        maxHealth += extra;
        currentHealth = Mathf.Min(currentHealth + extra, maxHealth);
        OnHealthChanged.Invoke(Percent);
    }

    public void SetHitsToKill(int hits)
    {
        hitsToKill = hits;
    }

    public void SetResistance(Elemento element, float multiplier)
    {
        foreach (var r in resistances)
        {
            if (r.element == element)
            {
                r.multiplier = multiplier;
                return;
            }
        }
        var list = new System.Collections.Generic.List<ElementResistance>(resistances);
        list.Add(new ElementResistance { element = element, multiplier = multiplier });
        resistances = list.ToArray();
    }

    float GetMultiplier(Elemento? element)
    {
        if (element == null) return 1f;
        foreach (var r in resistances)
            if (r.element == element.Value) return r.multiplier;
        return 1f;
    }

    public void TakeDamage(float amount, Elemento? element = null)
    {
        if (IsDead || Time.time < invulnerableUntil) return;

        float finalDamage = amount * GetMultiplier(element) * damageTakenMultiplier;
        if (finalDamage <= 0f)
        {
            Resisted?.Invoke(element); // inmune a ese elemento
            return;
        }

        if (hitsToKill > 0)
        {
            hitsTaken++;
            currentHealth = maxHealth * (1f - (float)hitsTaken / hitsToKill);
        }
        else
        {
            currentHealth -= finalDamage;
        }

        currentHealth = Mathf.Max(currentHealth, 0f);
        invulnerableUntil = Time.time + invulnerableTime;
        LastHitTime = Time.time;

        if (CompareTag("Player")) AudioManager.Play(Sfx.DanoLira);
        Damaged?.Invoke(finalDamage);
        OnHealthChanged.Invoke(Percent);

        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged.Invoke(Percent);
    }

    public void Kill()
    {
        if (IsDead) return;
        currentHealth = 0f;
        OnHealthChanged.Invoke(0f);
        Die();
    }

    void Die()
    {
        IsDead = true;
        OnDeath.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }
}
