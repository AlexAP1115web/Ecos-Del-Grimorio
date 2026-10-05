using System;
using System.Collections;
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
    public const string PrologueScene = "Prologo";
    public const string EpilogueScene = "Epilogo";
    public const int PagesPerLevel = 3;
    public const float HealthPerPage = 5f;

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
    public int PagesFound => pages.Count;

    private readonly HashSet<Elemento> unlockedSpells = new HashSet<Elemento> { Elemento.Arcano };
    private readonly HashSet<TipoItem> upgrades = new HashSet<TipoItem>();
    private readonly HashSet<string> collectibles = new HashSet<string>();
    private readonly HashSet<string> takenPickups = new HashSet<string>();
    private readonly HashSet<string> pages = new HashSet<string>();

    private float levelStartTime;
    private bool tookDamageThisLevel;
    private string checkpointScene;
    private Vector3 checkpointPosition;
    private float hitStopUntil;

    // Datos del nivel actual para la pantalla de resultados
    private int enemigosDerrotados;
    private float danoRecibido;
    private int secretosEncontrados;
    private int secretosTotales;

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
        EnemyBase.Died += _ => enemigosDerrotados++;
        Rompible.SecretoEncontrado += () => secretosEncontrados++;
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

        if (GameObject.FindGameObjectWithTag("Player") == null) { SetState(GameState.MenuPrincipal); return; }

        SetState(GameState.Jugando);
        levelStartTime = Time.time;
        tookDamageThisLevel = false;
        enemigosDerrotados = 0;
        danoRecibido = 0f;
        secretosEncontrados = 0;
        secretosTotales = 0;
        foreach (var r in FindObjectsByType<Rompible>())
            if (r.EsSecreto) secretosTotales++;
        if (checkpointScene != scene.name) checkpointScene = null;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (checkpointScene != null) player.transform.position = checkpointPosition;

        var health = player.GetComponent<Health>();
        if (health != null)
        {
            health.OnDeath.RemoveListener(OnPlayerDeath);
            health.OnDeath.AddListener(OnPlayerDeath);
            health.Damaged += d => { tookDamageThisLevel = true; danoRecibido += d; };
        }
    }

    void Update()
    {
        // Fin de la pausa breve de impacto
        if (hitStopUntil > 0f && Time.unscaledTime >= hitStopUntil)
        {
            hitStopUntil = 0f;
            bool reading = UIManager.Instance != null && UIManager.Instance.InDialogue;
            if (State == GameState.Jugando && !reading) Time.timeScale = 1f;
        }

        if (!Controles.PausaPresionado) return;
        if (State == GameState.Jugando) Pause();
        else if (State == GameState.Pausa) Resume();
    }

    // Congela el juego una fracción de segundo para dar peso a un golpe
    public void HitStop(float seconds)
    {
        if (State != GameState.Jugando || Time.timeScale == 0f) return;
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
        bool reading = UIManager.Instance != null && UIManager.Instance.InDialogue;
        Time.timeScale = reading ? 0f : 1f;
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
        AudioManager.Play(Sfx.GameOver);
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
        pages.Clear();
        Fragments = 0;
        HasRunicKey = false;
        checkpointScene = null;
        PlayerPrefs.SetString("eg_nivel", FirstLevel);
        Save();
        SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(PrologueScene) ? PrologueScene : FirstLevel);
    }

    public bool HasSavedGame => PlayerPrefs.HasKey("eg_nivel");
    public bool GameCompleted => PlayerPrefs.GetInt("eg_terminado", 0) == 1;
    private bool finishing;

    // Se llama al derrotar al Eco de Elenora (después de su diálogo final):
    // pantalla de victoria, recompensas, epílogo y créditos.
    public void FinishGame()
    {
        if (finishing) return;
        finishing = true;
        StartCoroutine(FinalSequence());
    }

    IEnumerator FinalSequence()
    {
        string scene = SceneManager.GetActiveScene().name;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetControlsEnabled(false);
            var sc = player.GetComponent<SpellCaster>();
            if (sc != null) sc.SetInputEnabled(false);
            var h = player.GetComponent<Health>();
            if (h != null) h.SetInvulnerable(60f);
        }

        // Las recompensas del jefe final se entregan aunque Lira no haya alcanzado a recogerlas
        foreach (var p in FindObjectsByType<Pickup>())
            if (p.Item != null && (p.Item.itemName == "Fragmento de Grimorio V" || p.Item.itemName == "Grimorio Completo"))
                Destroy(p.gameObject);
        string idFragmento = scene + ":Fragmento de Grimorio V";
        if (!IsPickupTaken(idFragmento)) { MarkPickupTaken(idFragmento); AddFragment(); }
        MarkPickupTaken(scene + ":Grimorio Completo");
        AddCollectible("Grimorio Completo");

        AudioManager.Play(Sfx.Victoria);
        CameraFollow.Shake(0.2f, 0.4f);
        if (UIManager.Instance != null) UIManager.Instance.ShowBigTitle("¡VICTORIA!", "El grimorio vuelve a estar en equilibrio");
        yield return new WaitForSecondsRealtime(5f);

        LevelCompleted?.Invoke(scene, Time.time - levelStartTime, tookDamageThisLevel);
        PlayerPrefs.SetInt("eg_terminado", 1);
        PlayerPrefs.DeleteKey("eg_nivel");   // la partida terminó: "Continuar" ya no regresa a la batalla final
        Save();
        GameFinished?.Invoke();

        finishing = false;
        SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(EpilogueScene) ? EpilogueScene : CreditsScene);
    }

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

    // Páginas Perdidas: el id incluye la escena para poder contarlas por ala
    public bool AddPage(string id)
    {
        if (!pages.Add(id)) return false;
        Save();
        return true;
    }

    public int PagesInScene(string scene)
    {
        int count = 0;
        foreach (var p in pages) if (p.StartsWith(scene + ":")) count++;
        return count;
    }

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
        float segundos = Time.time - levelStartTime;

        // Si algún script que escucha estos eventos falla, el nivel debe terminar de todos modos
        try { LevelCompleted?.Invoke(current, segundos, tookDamageThisLevel); } catch (Exception e) { Debug.LogException(e); }
        try { if (unlocksSpell) UnlockSpell(spellToUnlock); } catch (Exception e) { Debug.LogException(e); }
        checkpointScene = null;
        if (nextScene == CreditsScene) { try { GameFinished?.Invoke(); } catch (Exception e) { Debug.LogException(e); } }

        bool puedeCargar = !string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene);
        if (puedeCargar && nextScene != CreditsScene) PlayerPrefs.SetString("eg_nivel", nextScene);
        Save();

        void Continuar()
        {
            if (puedeCargar) SceneManager.LoadScene(nextScene);
        }

        // Pantalla de resultados del ala antes de pasar a la siguiente
        var ui = UIManager.Instance;
        if (ui == null || !ui.HasResultsScreen)
        {
            Continuar();
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetControlsEnabled(false);
            var h = player.GetComponent<Health>();
            if (h != null) h.SetInvulnerable(999f);
        }
        try
        {
            SetState(GameState.NivelCompletado);
            if (!ui.ShowResults(ui.LevelTitle, Resumen(current, segundos, unlocksSpell, spellToUnlock), Continuar)) Continuar();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Continuar();
        }
    }

    string Resumen(string scene, float segundos, bool unlocksSpell, Elemento spell)
    {
        int min = Mathf.FloorToInt(segundos / 60f), seg = Mathf.FloorToInt(segundos % 60f);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Tiempo:  {min:00}:{seg:00}");
        sb.AppendLine($"Enemigos derrotados:  {enemigosDerrotados}");
        sb.AppendLine(tookDamageThisLevel ? $"Daño recibido:  {Mathf.RoundToInt(danoRecibido)}" : "Daño recibido:  ninguno  ★");
        sb.AppendLine($"Páginas Perdidas del ala:  {PagesInScene(scene)}/{PagesPerLevel}");
        if (secretosTotales > 0) sb.AppendLine($"Salas secretas:  {secretosEncontrados}/{secretosTotales}");
        sb.AppendLine($"Fragmentos de grimorio:  {Fragments}/5");
        if (unlocksSpell)
        {
            sb.AppendLine();
            sb.Append($"<color=#FFD27F>Nuevo hechizo: {spell}</color>");
            if (unlockedSpells.Count > 3) sb.Append($"\n<size=26>Solo caben 3 equipados: cámbialo con Q o R2</size>");
        }
        return sb.ToString();
    }

    // ---------- Guardado ----------

    void Save()
    {
        PlayerPrefs.SetString("eg_hechizos", string.Join(",", unlockedSpells));
        PlayerPrefs.SetString("eg_mejoras", string.Join(",", upgrades));
        PlayerPrefs.SetString("eg_coleccionables", string.Join("|", collectibles));
        PlayerPrefs.SetString("eg_recogidos", string.Join("|", takenPickups));
        PlayerPrefs.SetString("eg_paginas", string.Join("|", pages));
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
        foreach (var s in PlayerPrefs.GetString("eg_paginas", "").Split('|'))
            if (s.Length > 0) pages.Add(s);
        Fragments = PlayerPrefs.GetInt("eg_fragmentos", 0);
        HasRunicKey = PlayerPrefs.GetInt("eg_llave", 0) == 1;
    }

    // ---------- Mensajes (los muestra UIManager) ----------

    public void ShowMessage(string text, float seconds = 3f)
    {
        MessageShown?.Invoke(text, seconds);
    }
}
