using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ítem que Lira recoge al tocarlo. El efecto depende del ItemData asignado.
// Los coleccionables y mejoras solo se recogen una vez: si Lira muere y reinicia
// el nivel, ya no vuelven a aparecer.
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
    string Id => SceneManager.GetActiveScene().name + ":" + item.itemName;

    void Start()
    {
        startPosition = transform.position;
        GetComponent<Collider2D>().isTrigger = true;

        if (IsUnique && GameManager.Instance != null && GameManager.Instance.IsPickupTaken(Id))
            Destroy(gameObject);
    }

    void Update()
    {
        // Pequeña animación de flotar
        transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken || item == null || !other.CompareTag("Player")) return;

        GameObject player = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!Apply(player)) return;

        taken = true;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (IsUnique) gm.MarkPickupTaken(Id);
            gm.ShowMessage(string.IsNullOrEmpty(item.description) ? item.itemName : $"{item.itemName}: {item.description}");
        }
        Collected?.Invoke(item);
        Particula.Rafaga(transform.position, new Color(1f, 0.9f, 0.5f, 1f), 14, 4f, 0.14f, 0.5f);
        Destroy(gameObject);
    }

    // Devuelve false si el ítem no se pudo usar (ej. vida llena)
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

            case TipoItem.LlaveRunica:
                if (gm != null) gm.GiveRunicKey();
                return true;

            default:
                if (gm != null) gm.AddCollectible(item.itemName);
                return true;
        }
    }
}
