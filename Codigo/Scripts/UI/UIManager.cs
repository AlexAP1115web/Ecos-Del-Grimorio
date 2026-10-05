using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Interfaz de los niveles hecha con UGUI: mensajes, título del nivel, diálogos con retrato,
// menú de pausa, pantalla de logros, Game Over y aviso de logro desbloqueado.
// Se puede navegar con teclado, mouse o control.
// En los diálogos, una línea que empieza con "Nombre: " cambia de personaje y de retrato.
[Serializable]
public class RetratoPersonaje
{
    public string nombre;
    public Sprite retrato;
}

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
    [SerializeField] private RetratoPersonaje[] retratos = new RetratoPersonaje[0];

    [Header("Resultados")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Text resultsTitle;
    [SerializeField] private Text resultsBody;
    [SerializeField] private GameObject resultsFirst;

    [Header("Pantallas")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseFirst;
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private Transform achievementsList;
    [SerializeField] private GameObject achievementsFirst;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject controlsFirst;
    [SerializeField] private GameObject grimorioPanel;
    [SerializeField] private GameObject grimorioFirst;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject optionsFirst;
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
    private string defaultSpeaker;
    private Sprite defaultPortrait;
    private string currentText = "";
    private string currentSpeaker = "";
    private float nextVoiceTime;
    private Action onResultsContinue;
    private PlayerController playerController;
    private SpellCaster playerCaster;

    public bool InDialogue => dialoguePanel != null && dialoguePanel.activeSelf;
    public string LevelTitle => levelTitle;

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
        SetActive(controlsPanel, false);
        SetActive(grimorioPanel, false);
        SetActive(optionsPanel, false);
        SetActive(resultsPanel, false);
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

    // Título grande que se queda en pantalla (victoria)
    public void ShowBigTitle(string title, string subtitle)
    {
        if (titleGroup == null) return;
        StopAllCoroutines();
        titleText.text = $"{title}\n<size=46>{subtitle}</size>";
        StartCoroutine(FadeInTitle());
    }

    IEnumerator FadeInTitle()
    {
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 1.5f) { titleGroup.alpha = t; yield return null; }
        titleGroup.alpha = 1f;
    }

    IEnumerator ShowTitle()
    {
        titleGroup.alpha = 0f;
        yield return new WaitForSecondsRealtime(0.3f);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 2f) { titleGroup.alpha = t; yield return null; }
        titleGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.5f);
        for (float t = 1f; t > 0; t -= Time.unscaledDeltaTime) { titleGroup.alpha = t; yield return null; }
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
        onDialogueEnd = onEnd;
        dialogueStartFrame = Time.frameCount;
        defaultSpeaker = speaker;
        defaultPortrait = portrait;
        PrepareLine();
        dialogueHint.text = $"{Controles.TextoInteractuar} para continuar";
        SetActive(dialoguePanel, true);
        HidePrompt();

        if (playerController != null) playerController.SetControlsEnabled(false);
        if (playerCaster != null) playerCaster.SetInputEnabled(false);
        Time.timeScale = 0f; // el mundo se detiene mientras se lee
    }

    // Si la línea empieza con el nombre de un personaje conocido, cambia quién habla
    void PrepareLine()
    {
        string raw = lines[lineIndex];
        string speaker = defaultSpeaker;
        Sprite portrait = defaultPortrait;
        currentText = raw;

        int colon = raw.IndexOf(':');
        if (colon > 0 && colon < 30)
        {
            string name = raw.Substring(0, colon).Trim();
            foreach (var r in retratos)
            {
                if (r != null && string.Equals(r.nombre, name, StringComparison.OrdinalIgnoreCase))
                {
                    speaker = r.nombre;
                    portrait = r.retrato;
                    currentText = raw.Substring(colon + 1).Trim();
                    break;
                }
            }
        }

        dialogueName.text = speaker;
        currentSpeaker = speaker;
        dialoguePortrait.sprite = portrait;
        dialoguePortrait.enabled = portrait != null;
        visibleChars = 0;
        typeTimer = 0f;
    }

    void UpdateDialogue(float dt)
    {
        if (!InDialogue) return;
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Pausa) return;

        string line = currentText;
        typeTimer += dt * 55f;
        while (typeTimer >= 1f && visibleChars < line.Length)
        {
            typeTimer -= 1f;
            visibleChars++;

            // Voz del personaje: una sílaba cada pocas letras
            char ch = line[visibleChars - 1];
            if (char.IsLetter(ch) && Time.unscaledTime >= nextVoiceTime)
            {
                AudioManager.Hablar(currentSpeaker);
                nextVoiceTime = Time.unscaledTime + 0.13f;
            }
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
        AudioManager.Play(Sfx.MenuMover, 0.4f);
        if (lineIndex >= lines.Length) EndDialogue();
        else PrepareLine();
    }

    void EndDialogue()
    {
        SetActive(dialoguePanel, false);
        if (GameManager.Instance == null || GameManager.Instance.State == GameState.Jugando) Time.timeScale = 1f;
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
        SetActive(controlsPanel, false);
        SetActive(grimorioPanel, false);
        SetActive(optionsPanel, false);
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

    // Pantalla de resultados al terminar un ala
    public void ShowResults(string title, string body, Action onContinue)
    {
        onResultsContinue = onContinue;
        if (resultsTitle != null) resultsTitle.text = $"¡{title} completada!";
        if (resultsBody != null) resultsBody.text = body;
        HidePrompt();
        SetActive(dialoguePanel, false);
        SetActive(resultsPanel, true);
        Select(resultsFirst);
        Time.timeScale = 0f;
        AudioManager.Play(Sfx.Logro);
    }

    public void BotonContinuarResultados()
    {
        if (onResultsContinue == null) return;
        var callback = onResultsContinue;
        onResultsContinue = null;
        AudioManager.Play(Sfx.Portal);
        callback();
    }

    public void BotonOpciones()
    {
        SetActive(pausePanel, false);
        SetActive(optionsPanel, true);
        Select(optionsFirst);
    }

    public void BotonGrimorio()
    {
        SetActive(pausePanel, false);
        SetActive(grimorioPanel, true);
        Select(grimorioFirst);
    }

    public void BotonControles()
    {
        SetActive(pausePanel, false);
        SetActive(controlsPanel, true);
        Select(controlsFirst);
    }

    public void BotonVolverPausa()
    {
        SetActive(achievementsPanel, false);
        SetActive(controlsPanel, false);
        SetActive(grimorioPanel, false);
        SetActive(optionsPanel, false);
        SetActive(pausePanel, true);
        Select(pauseFirst);
    }
}
