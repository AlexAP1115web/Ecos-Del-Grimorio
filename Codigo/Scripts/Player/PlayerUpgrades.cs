using UnityEngine;

// mejoras: Nucleo de Ascua, Anillo de Escarcha y Pluma Ligera
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
        // recuperar las mejoras obtenidas en niveles anteriores
        if (GameManager.Instance == null) return;

        // cada Pagina Perdida encontrada da vida maxima extra
        int pages = GameManager.Instance.PagesFound;
        if (pages > 0 && health != null)
        {
            health.AddMaxHealth(pages * GameManager.HealthPerPage);
            health.Heal(health.MaxHealth);
        }

        foreach (var upgrade in new System.Collections.Generic.List<TipoItem>(GameManager.Instance.Upgrades)) Apply(upgrade);

        // mejora cosmetica del cofre secreto
        if (GameManager.Instance.HasCollectible("Capa dorada"))
        {
            if (controller != null) controller.Tint(new Color(1f, 0.92f, 0.7f));
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
