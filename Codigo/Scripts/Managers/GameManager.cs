using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MenuPrincipal,
    Jugando,
    Pausa,
    Logros,
    GameOver,
    NivelCompletado,
    Creditos
}

// Controla los estados del juego (sección 2.5) y guarda el progreso entre niveles:
// nivel alcanzado, hechizos, mejoras, fragmentos y coleccionables.
// El progreso se guarda con PlayerPrefs para la opción "Continuar" del menú.
public class GameManager : MonoBehaviour
{
    public const string MenuScene = "MenuPrincipal";
    public const string FirstLevel = "Nivel1_AlaDeAprendizaje";
    public const string CreditsScene = "Creditos";

    public static GameManager Instance { get; private set; }

    public event Action<GameState> StateChanged;
    public event Action<int> FragmentsChanged;
    public event Action<Elemento> SpellUnlocked;
    public event Action<string, float, bool> LevelCompleted; // escena, segundos, recibió daño
    public event Action GameFinished;
    public event Action<string, float> MessageShown;

    public GameState State { get; private set; } = GameState.Jugando;
    public int Fragments { get; private set; }
    public bool HasRunicKey { get; private set; }
    public IEnumerable<Elemento> UnlockedSpells => unlockedSpells;
    public IEnumerable<TipoItem> Upgrades => upgrades;
    public int UnlockedSpellCount => unlockedSpells.Count;
    public int PotionsUsedInBossFight { get; private set; }
    public bool HasCollectible(string itemName) => collectibles.Contains(itemName);

    private readonly HashSet<Elemento> unlockedSpells = new HashSet<Elemento> { Elemento.Arcano };
    private readonly HashSet<TipoItem> upgrades = new HashSet<TipoItem>();
    private readonly HashSet<string> collectibles = new HashSet<string>();
    private readonly HashSet<string> takenPickups = new HashSet<string>();

    private float levelStartTime;
    private bool tookDamageThisLevel;
    private string checkpointScene;
    private Vector3 checkpointPosition;
    private float hitStopUntil;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        SceneManager.sceneLoaded += OnSceneLoaded;
        BossController.BossActivated += OnBossActivated;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        BossController.BossActivated -= OnBossActivated;
    }

    void Start()
    {
        PrepareScene(SceneManager.GetActiveScene());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PrepareScene(scene);
    }

    void PrepareScene(Scene scene)
    {
        Time.timeScale = 1f;
        hitStopUntil = 0f;

        if (scene.name == MenuScene) { SetState(GameState.MenuPrincipal); return; }
        if (scene.name == CreditsScene) { SetState(GameState.Creditos); return; }

        SetState(GameState.Jugando);
        levelStartTime = Time.time;
        tookDamageThisLevel = false;
        if (checkpointScene != scene.name) checkpointScene = null;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (checkpointScene != null) player.transform.position = checkpointPosition;

        var health = player.GetComponent<Health>();
        if (health != null)
        {
            health.OnDeath.RemoveListener(OnPlayerDeath);
            health.OnDeath.AddListener(OnPlayerDeath);
            health.Damaged += _ => tookDamageThisLevel = true;
        }
    }

    void Update()
    {
        // Fin de la pausa breve de impacto
        if (hitStopUntil > 0f && Time.unscaledTime >= hitStopUntil)
        {
            hitStopUntil = 0f;
            if (State == GameState.Jugando) Time.timeScale = 1f;
        }

        if (!Controles.PausaPresionado) return;
        if (State == GameState.Jugando) Pause();
        else if (State == GameState.Pausa) Resume();
    }

    // Congela el juego una fracción de segundo para dar peso a un golpe
    public void HitStop(float seconds)
    {
        if (State != GameState.Jugando) return;
        Time.timeScale = 0.05f;
        hitStopUntil = Time.unscaledTime + seconds;
    }

    void SetState(GameState newState)
    {
        State = newState;
        StateChanged?.Invoke(newState);
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        SetState(GameState.Pausa);
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        hitStopUntil = 0f;
        SetState(GameState.Jugando);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuScene);
    }

    void OnPlayerDeath()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.SetControlsEnabled(false);
        }
        SetState(GameState.GameOver);
        CameraFollow.Shake(0.3f, 0.4f);
        Controles.Vibrar(0.8f, 0.8f, 0.4f);
    }

    void OnBossActivated(BossController boss)
    {
        PotionsUsedInBossFight = 0;
    }

    // ---------- Partida ----------

    public void NewGame()
    {
        unlockedSpells.Clear();
        unlockedSpells.Add(Elemento.Arcano);
        upgrades.Clear();
        collectibles.Clear();
        takenPickups.Clear();
        Fragments = 0;
        HasRunicKey = false;
        checkpointScene = null;
        PlayerPrefs.SetString("eg_nivel", FirstLevel);
        Save();
        SceneManager.LoadScene(FirstLevel);
    }

    public bool HasSavedGame => PlayerPrefs.HasKey("eg_nivel");

    public void ContinueGame()
    {
        SceneManager.LoadScene(PlayerPrefs.GetString("eg_nivel", FirstLevel));
    }

    public void UnlockSpell(Elemento element)
    {
        if (unlockedSpells.Add(element)) SpellUnlocked?.Invoke(element);
    }

    public void AddUpgrade(TipoItem upgrade) => upgrades.Add(upgrade);

    public void AddFragment()
    {
        Fragments++;
        FragmentsChanged?.Invoke(Fragments);
    }

    public void AddCollectible(string itemName) => collectibles.Add(itemName);

    public void GiveRunicKey() => HasRunicKey = true;

    public void NotifyPotionUsed() => PotionsUsedInBossFight++;

    public bool IsPickupTaken(string id) => takenPickups.Contains(id);

    public void MarkPickupTaken(string id) => takenPickups.Add(id);

    public void SetCheckpoint(Vector3 position)
    {
        checkpointScene = SceneManager.GetActiveScene().name;
        checkpointPosition = position;
        ShowMessage("Punto de reaparición activado");
    }

    // Porcentaje de coleccionables: 5 fragmentos + diario + nota + grimorio completo + llave
    public float CompletionPercent
    {
        get
        {
            int total = 9, got = Mathf.Min(Fragments, 5);
            foreach (var c in new[] { "Diario de la Archimaga Elenora", "Nota cifrada de Elenora", "Grimorio Completo" })
                if (collectibles.Contains(c)) got++;
            if (HasRunicKey) got++;
            return got / (float)total;
        }
    }

    // Se llama al llegar a la salida de un nivel
    public void CompleteLevel(string nextScene, Elemento spellToUnlock, bool unlocksSpell)
    {
        string current = SceneManager.GetActiveScene().name;
        LevelCompleted?.Invoke(current, Time.time - levelStartTime, tookDamageThisLevel);

        if (unlocksSpell)
        {
            UnlockSpell(spellToUnlock);
            ShowMessage(unlockedSpells.Count > 3
                ? $"Hechizo de {spellToUnlock} desbloqueado. Usa Q para equiparlo."
                : $"Hechizo de {spellToUnlock} desbloqueado", 5f);
        }

        checkpointScene = null;

        if (nextScene == CreditsScene) GameFinished?.Invoke();

        if (!string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene))
        {
            if (nextScene != CreditsScene) PlayerPrefs.SetString("eg_nivel", nextScene);
            Save();
            SceneManager.LoadScene(nextScene);
            return;
        }

        Save();
        SetState(GameState.NivelCompletado);
    }

    // ---------- Guardado ----------

    void Save()
    {
        PlayerPrefs.SetString("eg_hechizos", string.Join(",", unlockedSpells));
        PlayerPrefs.SetString("eg_mejoras", string.Join(",", upgrades));
        PlayerPrefs.SetString("eg_coleccionables", string.Join("|", collectibles));
        PlayerPrefs.SetString("eg_recogidos", string.Join("|", takenPickups));
        PlayerPrefs.SetInt("eg_fragmentos", Fragments);
        PlayerPrefs.SetInt("eg_llave", HasRunicKey ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Load()
    {
        foreach (var s in PlayerPrefs.GetString("eg_hechizos", "").Split(','))
            if (Enum.TryParse(s, out Elemento e)) unlockedSpells.Add(e);
        foreach (var s in PlayerPrefs.GetString("eg_mejoras", "").Split(','))
            if (Enum.TryParse(s, out TipoItem t)) upgrades.Add(t);
        foreach (var s in PlayerPrefs.GetString("eg_coleccionables", "").Split('|'))
            if (s.Length > 0) collectibles.Add(s);
        foreach (var s in PlayerPrefs.GetString("eg_recogidos", "").Split('|'))
            if (s.Length > 0) takenPickups.Add(s);
        Fragments = PlayerPrefs.GetInt("eg_fragmentos", 0);
        HasRunicKey = PlayerPrefs.GetInt("eg_llave", 0) == 1;
    }

    // ---------- Mensajes (los muestra UIManager) ----------

    public void ShowMessage(string text, float seconds = 3f)
    {
        MessageShown?.Invoke(text, seconds);
    }
}
