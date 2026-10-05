using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Interfaz de los niveles hecha con UGUI: mensajes, título del nivel, diálogos con retrato,
// menú de pausa, pantalla de logros, Game Over y aviso de logro desbloqueado.
// Se puede navegar con teclado, mouse o control.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private Font font;
    [SerializeField] private string levelTitle = "";

    [Header("Mensajes")]
    [SerializeField] private CanvasGroup messageGroup;
    [SerializeField] private Text messageText;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Text promptText;

    [Header("Diálogo")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image dialoguePortrait;
    [SerializeField] private Text dialogueName;
    [SerializeField] private Text dialogueBody;
    [SerializeField] private Text dialogueHint;

    [Header("Pantallas")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseFirst;
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private Transform achievementsList;
    [SerializeField] private GameObject achievementsFirst;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameOverFirst;

    [Header("Logro")]
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private Image toastIcon;
    [SerializeField] private Text toastText;

    private float messageUntil;
    private readonly Queue<Logro> toasts = new Queue<Logro>();
    private float toastUntil;

    // Diálogo actual
    private string[] lines;
    private int lineIndex;
    private int visibleChars;
    private float typeTimer;
    private Action onDialogueEnd;
    private int dialogueStartFrame;
    private PlayerController playerController;
    private SpellCaster playerCaster;

    public bool InDialogue => dialoguePanel != null && dialoguePanel.activeSelf;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerCaster = player.GetComponent<SpellCaster>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged += OnStateChanged;
            GameManager.Instance.MessageShown += ShowMessage;
        }
        AchievementManager.Unlocked += OnAchievement;

        SetActive(dialoguePanel, false);
        SetActive(pausePanel, false);
        SetActive(achievementsPanel, false);
        SetActive(gameOverPanel, false);
        if (messageGroup != null) messageGroup.alpha = 0f;
        if (toastGroup != null) toastGroup.alpha = 0f;
        HidePrompt();

        if (titleGroup != null)
        {
            titleText.text = levelTitle;
            StartCoroutine(ShowTitle());
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= OnStateChanged;
            GameManager.Instance.MessageShown -= ShowMessage;
        }
        AchievementManager.Unlocked -= OnAchievement;
        if (Instance == this) Instance = null;
    }

    static void SetActive(GameObject go, bool value)
    {
        if (go != null) go.SetActive(value);
    }

    void Select(GameObject go)
    {
        if (EventSystem.current != null && go != null) EventSystem.current.SetSelectedGameObject(go);
    }

    IEnumerator ShowTitle()
    {
        titleGroup.alpha = 0f;
        yield return new WaitForSeconds(0.3f);
        for (float t = 0; t < 1f; t += Time.deltaTime * 2f) { titleGroup.alpha = t; yield return null; }
        titleGroup.alpha = 1f;
        yield return new WaitForSeconds(2f);
        for (float t = 1f; t > 0; t -= Time.deltaTime) { titleGroup.alpha = t; yield return null; }
        titleGroup.alpha = 0f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (messageGroup != null)
            messageGroup.alpha = Mathf.MoveTowards(messageGroup.alpha, Time.unscaledTime < messageUntil ? 1f : 0f, dt * 4f);

        UpdateToast(dt);
        UpdateDialogue(dt);
    }

    // ---------- Mensajes ----------

    public void ShowMessage(string text, float seconds)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageUntil = Time.unscaledTime + seconds;
    }

    public void ShowPrompt(string text)
    {
        if (promptText == null) return;
        promptText.text = text;
        promptText.enabled = true;
    }

    public void HidePrompt()
    {
        if (promptText != null) promptText.enabled = false;
    }

    // ---------- Logros ----------

    void OnAchievement(Logro logro) => toasts.Enqueue(logro);

    void UpdateToast(float dt)
    {
        if (toastGroup == null) return;

        if (Time.unscaledTime >= toastUntil && toasts.Count > 0)
        {
            var logro = toasts.Dequeue();
            toastIcon.sprite = logro.icono;
            toastText.text = $"<b>Logro desbloqueado</b>\n{logro.nombre}";
            toastUntil = Time.unscaledTime + 4f;
        }

        toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, Time.unscaledTime < toastUntil ? 1f : 0f, dt * 3f);
    }

    // ---------- Diálogos ----------

    public void StartDialogue(string speaker, Sprite portrait, string[] dialogueLines, Action onEnd)
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        lines = dialogueLines;
        lineIndex = 0;
        visibleChars = 0;
        onDialogueEnd = onEnd;
        dialogueStartFrame = Time.frameCount;

        dialogueName.text = speaker;
        dialoguePortrait.sprite = portrait;
        dialoguePortrait.enabled = portrait != null;
        dialogueHint.text = $"{Controles.TextoInteractuar} para continuar";
        SetActive(dialoguePanel, true);
        HidePrompt();

        if (playerController != null) playerController.SetControlsEnabled(false);
        if (playerCaster != null) playerCaster.SetInputEnabled(false);
    }

    void UpdateDialogue(float dt)
    {
        if (!InDialogue || Time.timeScale == 0f) return;

        string line = lines[lineIndex];
        typeTimer += dt * 55f;
        while (typeTimer >= 1f && visibleChars < line.Length)
        {
            typeTimer -= 1f;
            visibleChars++;
        }
        dialogueBody.text = line.Substring(0, visibleChars);

        if (Time.frameCount == dialogueStartFrame) return;

        bool advance = Controles.InteractuarPresionado || Controles.AceptarPresionado || Controles.LanzarPresionado;
        if (!advance) return;

        if (visibleChars < line.Length)
        {
            visibleChars = line.Length; // muestra la línea completa
            return;
        }

        lineIndex++;
        visibleChars = 0;
        if (lineIndex >= lines.Length) EndDialogue();
    }

    void EndDialogue()
    {
        SetActive(dialoguePanel, false);
        if (playerController != null) playerController.SetControlsEnabled(true);
        if (playerCaster != null) playerCaster.SetInputEnabled(true);
        var callback = onDialogueEnd;
        onDialogueEnd = null;
        callback?.Invoke();
    }

    // ---------- Pausa / Game Over ----------

    void OnStateChanged(GameState state)
    {
        SetActive(pausePanel, state == GameState.Pausa);
        SetActive(achievementsPanel, false);
        SetActive(gameOverPanel, state == GameState.GameOver);

        if (state == GameState.Pausa) Select(pauseFirst);
        if (state == GameState.GameOver) Select(gameOverFirst);
    }

    public void BotonReanudar()
    {
        if (GameManager.Instance != null) GameManager.Instance.Resume();
    }

    public void BotonReiniciar()
    {
        if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
    }

    public void BotonMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
    }

    public void BotonLogros()
    {
        SetActive(pausePanel, false);
        SetActive(achievementsPanel, true);
        AchievementsList.Fill(achievementsList, font);
        Select(achievementsFirst);
    }

    public void BotonVolverPausa()
    {
        SetActive(achievementsPanel, false);
        SetActive(pausePanel, true);
        Select(pauseFirst);
    }
}
