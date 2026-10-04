using UnityEngine;
using UnityEngine.UI;

// HUD del documento (sección 2.6): barra de vida, barra de maná, hechizos equipados
// y contador de Fragmentos de grimorio. Cuando un jefe se activa aparece su barra de vida.
public class HUD : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private SpellCaster playerCaster;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image manaFill;
    [SerializeField] private Text fragmentsText;

    [Header("Hechizos equipados")]
    [Tooltip("Íconos por elemento en orden: Arcano, Fuego, Hielo, Viento")]
    [SerializeField] private Sprite[] elementIcons = new Sprite[4];
    [SerializeField] private Image[] slotIcons = new Image[3];
    [SerializeField] private Image[] slotFrames = new Image[3];

    [Header("Jefe")]
    [SerializeField] private GameObject bossPanel;
    [SerializeField] private Image bossFill;
    [SerializeField] private Text bossName;

    private Health bossHealth;

    void Start()
    {
        if (playerHealth == null || playerCaster == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                if (playerHealth == null) playerHealth = p.GetComponent<Health>();
                if (playerCaster == null) playerCaster = p.GetComponent<SpellCaster>();
            }
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(SetHealth);
            SetHealth(playerHealth.Percent);
        }
        if (playerCaster != null)
        {
            playerCaster.OnManaChanged.AddListener(SetMana);
            SetMana(playerCaster.ManaPercent);
        }

        if (bossPanel != null) bossPanel.SetActive(false);
        BossController.BossActivated += ShowBoss;
    }

    void OnDestroy()
    {
        BossController.BossActivated -= ShowBoss;
    }

    void Update()
    {
        UpdateSlots();

        if (fragmentsText != null)
        {
            int fragments = GameManager.Instance != null ? GameManager.Instance.Fragments : 0;
            fragmentsText.text = $"Fragmentos: {fragments}/5";
        }

        if (bossPanel != null && bossPanel.activeSelf && bossHealth == null)
            bossPanel.SetActive(false); // el jefe ya fue derrotado
    }

    void UpdateSlots()
    {
        if (playerCaster == null) return;
        for (int i = 0; i < slotIcons.Length; i++)
        {
            bool hasSpell = i < playerCaster.Equipped.Count;
            if (slotIcons[i] != null)
            {
                slotIcons[i].enabled = hasSpell;
                if (hasSpell) slotIcons[i].sprite = elementIcons[(int)playerCaster.Equipped[i]];
            }
            if (slotFrames[i] != null)
                slotFrames[i].color = i == playerCaster.SelectedSlot && hasSpell
                    ? new Color(1f, 0.85f, 0.3f, 1f)
                    : new Color(0f, 0f, 0f, 0.5f);
        }
    }

    void ShowBoss(BossController boss)
    {
        bossHealth = boss.BossHealth;
        if (bossPanel != null) bossPanel.SetActive(true);
        if (bossName != null) bossName.text = boss.BossName;
        bossHealth.OnHealthChanged.AddListener(SetBossHealth);
        SetBossHealth(bossHealth.Percent);
    }

    void SetHealth(float percent)
    {
        if (healthFill != null) healthFill.fillAmount = percent;
    }

    void SetMana(float percent)
    {
        if (manaFill != null) manaFill.fillAmount = percent;
    }

    void SetBossHealth(float percent)
    {
        if (bossFill != null) bossFill.fillAmount = percent;
    }
}
