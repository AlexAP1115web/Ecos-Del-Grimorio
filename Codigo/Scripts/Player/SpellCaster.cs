using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Maná, hechizos equipados y combos elementales.
// Según la mecánica del documento, Lira equipa hasta 3 hechizos:
//   1, 2, 3 -> lanza el hechizo del espacio 1, 2 o 3
//   J o clic izquierdo -> repite el espacio seleccionado
//   Q -> cambia el hechizo del espacio seleccionado por otro desbloqueado que no esté equipado
// Si se lanzan dos elementos distintos seguidos (dentro de comboWindow) se forma un combo.
public class SpellCaster : MonoBehaviour
{
    [Header("Maná")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float manaRegenRate = 6f;

    [Header("Hechizos")]
    [SerializeField] private SpellData[] spells = new SpellData[0];
    [SerializeField] private Elemento[] startUnlocked = { Elemento.Arcano };
    [SerializeField] private int maxEquipped = 3;
    [SerializeField] private Vector2 castOffset = new Vector2(0.8f, 0.3f);

    [Header("Combos")]
    [SerializeField] private float comboWindow = 0.6f;
    [SerializeField] private float comboManaCost = 15f;
    [Tooltip("Arte de los combos en el orden: Explosión Arcana, Vapor Cegador, Granizo Cortante, Tormenta de Ascuas")]
    [SerializeField] private Sprite[] comboSprites = new Sprite[4];

    public UnityEvent<float> OnManaChanged = new UnityEvent<float>();
    public event Action<Elemento> SpellUnlocked;
    public event Action<Elemento> SpellCast;
    public event Action<TipoCombo> ComboCast;
    public event Action EquippedChanged;

    private float currentMana;
    private readonly HashSet<Elemento> unlocked = new HashSet<Elemento>();
    private readonly List<Elemento> equipped = new List<Elemento>();
    private int selectedSlot;
    private readonly Dictionary<Elemento, float> nextCastTime = new Dictionary<Elemento, float>();
    private readonly Dictionary<Elemento, float> damageMultiplier = new Dictionary<Elemento, float>();
    private Elemento lastElement = Elemento.Arcano;
    private float lastCastTime = -10f;
    private bool archmageMode;
    private PlayerController controller;

    public float CurrentMana => currentMana;
    public float ManaPercent => currentMana / maxMana;
    public IReadOnlyList<Elemento> Equipped => equipped;
    public int SelectedSlot => selectedSlot;
    public Elemento EquippedElement => equipped.Count > 0 ? equipped[selectedSlot] : lastElement;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        currentMana = maxMana;
        foreach (var e in startUnlocked) AddUnlocked(e);
    }

    // Registra el hechizo y lo equipa si queda algún espacio libre
    void AddUnlocked(Elemento element)
    {
        if (!unlocked.Add(element)) return;
        if (equipped.Count < maxEquipped)
        {
            equipped.Add(element);
            EquippedChanged?.Invoke();
        }
    }

    void Start()
    {
        // Si ya se habían desbloqueado hechizos en niveles anteriores
        if (GameManager.Instance != null)
            foreach (var e in GameManager.Instance.UnlockedSpells) AddUnlocked(e);

        OnManaChanged.Invoke(ManaPercent);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (currentMana < maxMana)
        {
            currentMana = Mathf.Min(currentMana + manaRegenRate * Time.deltaTime, maxMana);
            OnManaChanged.Invoke(ManaPercent);
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) CastSlot(0);
        else if (kb.digit2Key.wasPressedThisFrame) CastSlot(1);
        else if (kb.digit3Key.wasPressedThisFrame) CastSlot(2);
        else if (kb.jKey.wasPressedThisFrame || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
            CastSlot(selectedSlot);

        if (kb.qKey.wasPressedThisFrame) SwapSelectedSlot();
    }

    public void CastSlot(int slot)
    {
        if (slot < 0 || slot >= equipped.Count) return;
        selectedSlot = slot;
        TryCast(equipped[slot]);
    }

    // Cambia el hechizo del espacio seleccionado por el siguiente desbloqueado que no esté equipado
    public void SwapSelectedSlot()
    {
        if (equipped.Count == 0) return;
        var order = (Elemento[])Enum.GetValues(typeof(Elemento));
        int start = Array.IndexOf(order, equipped[selectedSlot]);
        for (int i = 1; i <= order.Length; i++)
        {
            var candidate = order[(start + i) % order.Length];
            if (unlocked.Contains(candidate) && !equipped.Contains(candidate))
            {
                equipped[selectedSlot] = candidate;
                EquippedChanged?.Invoke();
                return;
            }
        }
    }

    SpellData GetSpell(Elemento element)
    {
        foreach (var s in spells)
            if (s != null && s.element == element) return s;
        return null;
    }

    public bool IsUnlocked(Elemento element) => unlocked.Contains(element);

    public void Unlock(Elemento element)
    {
        if (!unlocked.Contains(element))
        {
            AddUnlocked(element);
            SpellUnlocked?.Invoke(element);
            if (GameManager.Instance != null) GameManager.Instance.UnlockSpell(element);
        }
    }

    public void RestoreMana(float amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        OnManaChanged.Invoke(ManaPercent);
    }

    public void SetDamageMultiplier(Elemento element, float multiplier)
    {
        damageMultiplier[element] = multiplier;
    }

    public void SetArchmageMode(bool active)
    {
        archmageMode = active;
    }

    float GetDamage(SpellData spell)
    {
        float mult = damageMultiplier.TryGetValue(spell.element, out var m) ? m : 1f;
        return spell.damage * mult;
    }

    bool SpendMana(float cost)
    {
        if (archmageMode) return true;
        if (currentMana < cost) return false;
        currentMana -= cost;
        OnManaChanged.Invoke(ManaPercent);
        return true;
    }

    public void TryCast(Elemento element)
    {
        if (!equipped.Contains(element)) return;

        var spell = GetSpell(element);
        if (spell == null) return;

        if (!archmageMode && nextCastTime.TryGetValue(element, out float next) && Time.time < next) return;

        // ¿Se forma un combo con el hechizo anterior?
        TipoCombo combo = TipoCombo.Ninguno;
        if (element != lastElement && Time.time - lastCastTime <= comboWindow)
            combo = GetCombo(lastElement, element);

        if (combo != TipoCombo.Ninguno)
        {
            if (!SpendMana(comboManaCost)) return;
            CastCombo(combo);
            lastCastTime = -10f; // evita encadenar combo tras combo
        }
        else
        {
            if (!SpendMana(spell.manaCost)) return;
            FireProjectile(spell, GetDamage(spell), false, 1f);
            lastCastTime = Time.time;
        }

        lastElement = element;
        nextCastTime[element] = Time.time + spell.cooldown;
        SpellCast?.Invoke(element);
    }

    public static TipoCombo GetCombo(Elemento a, Elemento b)
    {
        bool Has(Elemento x) => a == x || b == x;

        if (Has(Elemento.Arcano) && Has(Elemento.Fuego)) return TipoCombo.ExplosionArcana;
        if (Has(Elemento.Fuego) && Has(Elemento.Hielo)) return TipoCombo.VaporCegador;
        if (Has(Elemento.Hielo) && Has(Elemento.Viento)) return TipoCombo.GranizoCortante;
        if (Has(Elemento.Fuego) && Has(Elemento.Viento)) return TipoCombo.TormentaDeAscuas;
        return TipoCombo.Ninguno;
    }

    Vector2 Facing => controller == null || controller.FacingRight ? Vector2.right : Vector2.left;

    Vector2 CastPoint => (Vector2)transform.position + new Vector2(castOffset.x * Facing.x, castOffset.y);

    void FireProjectile(SpellData spell, float damage, bool pierce, float speedMultiplier)
    {
        if (spell.projectilePrefab == null) return;
        var go = Instantiate(spell.projectilePrefab, CastPoint, Quaternion.identity);
        var projectile = go.GetComponent<SpellProjectile>();
        if (projectile != null) projectile.Init(spell, Facing, damage, pierce, speedMultiplier);
    }

    Sprite ComboSprite(TipoCombo combo)
    {
        int i = (int)combo - 1;
        return comboSprites != null && i >= 0 && i < comboSprites.Length ? comboSprites[i] : null;
    }

    void CastCombo(TipoCombo combo)
    {
        switch (combo)
        {
            case TipoCombo.ExplosionArcana:
            {
                // Explosión de área de alto daño frente a Lira
                Vector2 center = (Vector2)transform.position + Facing * 2f;
                AreaEffect.Spawn(center, 2.5f, Color.white, 0.6f, ComboSprite(combo));
                AreaEffect.Damage(center, 2.5f, 40f, Elemento.Fuego, false);
                break;
            }
            case TipoCombo.VaporCegador:
            {
                // Nube de vapor que "ciega" (aturde) a los enemigos cercanos
                AreaEffect.Spawn(transform.position, 3f, new Color(1f, 1f, 1f, 0.85f), 1.2f, ComboSprite(combo));
                foreach (var col in Physics2D.OverlapCircleAll(transform.position, 3f))
                {
                    var enemy = col.GetComponentInParent<EnemyBase>();
                    if (enemy != null) enemy.Stun(2.5f);
                }
                AreaEffect.Damage(transform.position, 3f, 5f, Elemento.Hielo, false);
                break;
            }
            case TipoCombo.GranizoCortante:
            {
                // Proyectil de hielo que atraviesa enemigos en línea recta
                var ice = GetSpell(Elemento.Hielo);
                if (ice != null) FireProjectile(ice, GetDamage(ice) * 2.5f, true, 1.5f);
                AreaEffect.Spawn(CastPoint + Facing, 1.2f, Color.white, 0.4f, ComboSprite(combo));
                break;
            }
            case TipoCombo.TormentaDeAscuas:
                StartCoroutine(EmberStorm());
                break;
        }

        ComboCast?.Invoke(combo);
    }

    IEnumerator EmberStorm()
    {
        // Ascuas alrededor de Lira: tres golpes de daño seguidos
        for (int i = 0; i < 3; i++)
        {
            AreaEffect.Spawn(transform.position, 3.5f, new Color(1f, 1f, 1f, 0.8f), 0.4f, ComboSprite(TipoCombo.TormentaDeAscuas));
            AreaEffect.Damage(transform.position, 3.5f, 12f, Elemento.Fuego, false);
            yield return new WaitForSeconds(0.4f);
        }
    }
}
