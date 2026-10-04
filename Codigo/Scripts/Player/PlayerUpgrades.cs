using UnityEngine;

// Mejoras permanentes de los ítems especiales:
// Núcleo de Ascua (+daño de Fuego), Anillo de Escarcha (-daño de Fuego recibido), Pluma Ligera (+salto).
public class PlayerUpgrades : MonoBehaviour
{
    [SerializeField] private float fireDamageMultiplier = 1.3f;
    [SerializeField] private float fireResistance = 0.7f;
    [SerializeField] private float jumpMultiplier = 1.2f;

    private SpellCaster caster;
    private Health health;
    private PlayerController controller;

    public bool HasNucleoDeAscua { get; private set; }
    public bool HasAnilloDeEscarcha { get; private set; }
    public bool HasPlumaLigera { get; private set; }

    void Awake()
    {
        caster = GetComponent<SpellCaster>();
        health = GetComponent<Health>();
        controller = GetComponent<PlayerController>();
    }

    void Start()
    {
        // Recuperar las mejoras obtenidas en niveles anteriores
        if (GameManager.Instance == null) return;
        foreach (var upgrade in new System.Collections.Generic.List<TipoItem>(GameManager.Instance.Upgrades)) Apply(upgrade);

        // Mejora cosmética del cofre secreto
        if (GameManager.Instance.HasCollectible("Capa dorada"))
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.92f, 0.7f);
        }
    }

    public void Apply(TipoItem upgrade)
    {
        switch (upgrade)
        {
            case TipoItem.NucleoDeAscua:
                HasNucleoDeAscua = true;
                if (caster != null) caster.SetDamageMultiplier(Elemento.Fuego, fireDamageMultiplier);
                break;
            case TipoItem.AnilloDeEscarcha:
                HasAnilloDeEscarcha = true;
                if (health != null) health.SetResistance(Elemento.Fuego, fireResistance);
                break;
            case TipoItem.PlumaLigera:
                HasPlumaLigera = true;
                if (controller != null) controller.SetJumpMultiplier(jumpMultiplier);
                break;
            default:
                return;
        }

        if (GameManager.Instance != null) GameManager.Instance.AddUpgrade(upgrade);
    }
}
