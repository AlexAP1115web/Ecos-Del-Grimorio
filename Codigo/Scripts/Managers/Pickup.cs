using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// items que se recogen al tocarlos, los unicos no vuelven a salir
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    public static event Action<ItemData> Collected;

    private Vector3 startPosition;
    private bool taken;

    public ItemData Item => item;

    bool IsUnique => item != null && item.type != TipoItem.Mana && item.type != TipoItem.Vida;
    // las Paginas Perdidas comparten el mismo ItemData, asi que su id incluye la posicion
    string Id => SceneManager.GetActiveScene().name + ":" + item.itemName +
                 (item.type == TipoItem.PaginaPerdida ? $":{Mathf.RoundToInt(startPosition.x * 2f)}:{Mathf.RoundToInt(startPosition.y * 2f)}" : "");

    void Start()
    {
        startPosition = transform.position;
        GetComponent<Collider2D>().isTrigger = true;

        if (IsUnique && GameManager.Instance != null && GameManager.Instance.IsPickupTaken(Id))
            Destroy(gameObject);
    }

    void Update()
    {
        // pequeña animacion de flotar
        transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || item == null || !other.CompareTag("Player")) return;

        GameObject player = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!Apply(player)) return;

        taken = true;
        var gm = GameManager.Instance;
        if (gm != null && IsUnique) gm.MarkPickupTaken(Id);

        AudioManager.Play(item.type == TipoItem.PaginaPerdida ? Sfx.Pagina : IsUnique ? Sfx.ObjetoEspecial : Sfx.Objeto);

        // los fragmentos, la nota y el diario cuentan parte de la historia de Elenora
        if (item.type == TipoItem.PaginaPerdida)
        {
            if (gm != null)
            {
                string scene = SceneManager.GetActiveScene().name;
                gm.ShowMessage($"Página Perdida ({gm.PagesInScene(scene)}/{GameManager.PagesPerLevel} en esta ala): +{GameManager.HealthPerPage:0} de vida máxima", 4f);
            }
        }
        else if (item.lore != null && item.lore.Length > 0 && UIManager.Instance != null)
            UIManager.Instance.StartDialogue(item.itemName, item.icon, item.lore, null);
        else if (gm != null)
            gm.ShowMessage(string.IsNullOrEmpty(item.description) ? item.itemName : $"{item.itemName}: {item.description}");
        Collected?.Invoke(item);
        Particula.Rafaga(transform.position, new Color(1f, 0.9f, 0.5f, 1f), 14, 4f, 0.14f, 0.5f);
        Destroy(gameObject);
    }

    // devuelve false si el item no se pudo usar (ej. vida llena)
    bool Apply(GameObject player)
    {
        var gm = GameManager.Instance;

        switch (item.type)
        {
            case TipoItem.Mana:
                var caster = player.GetComponent<SpellCaster>();
                if (caster == null) return false;
                caster.RestoreMana(item.amount);
                return true;

            case TipoItem.Vida:
                var health = player.GetComponent<Health>();
                if (health == null || health.CurrentHealth >= health.MaxHealth) return false;
                health.Heal(item.amount);
                if (gm != null) gm.NotifyPotionUsed();
                return true;

            case TipoItem.FragmentoGrimorio:
                if (gm != null) gm.AddFragment();
                return true;

            case TipoItem.NucleoDeAscua:
            case TipoItem.AnilloDeEscarcha:
            case TipoItem.PlumaLigera:
                var upgrades = player.GetComponent<PlayerUpgrades>();
                if (upgrades != null) upgrades.Apply(item.type);
                return true;

            case TipoItem.PaginaPerdida:
                var h = player.GetComponent<Health>();
                if (gm != null && !gm.AddPage(Id)) return true;
                if (h != null) h.AddMaxHealth(GameManager.HealthPerPage);
                return true;

            case TipoItem.LlaveRunica:
                if (gm != null) gm.GiveRunicKey();
                return true;

            default:
                if (gm != null) gm.AddCollectible(item.itemName);
                return true;
        }
    }
}
