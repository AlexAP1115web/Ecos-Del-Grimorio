using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TipoLogro
{
    PrimerHechizo,
    MaestraElemental,
    ComboPerfecto,
    SinUnRasguno,
    ColeccionistaDelGrimorio,
    CazadoraDeEcos,
    VelocistaArcana,
    ElGrimorioCompleto,
    SecretoRevelado,
    SinPiedad
}

[Serializable]
public class Logro
{
    public TipoLogro tipo;
    public string nombre;
    [TextArea] public string condicion;
    public Sprite icono;
}

// Sistema de logros de la sección 2.15. Funciona como observador: escucha los eventos
// que ya existen (hechizos, combos, jefes, ítems, niveles) y desbloquea cada logro una vez.
// Los logros se guardan con PlayerPrefs y no se borran al empezar una partida nueva.
public class AchievementManager : MonoBehaviour
{
    [SerializeField] private Logro[] logros = new Logro[0];
    [SerializeField] private float toastSeconds = 4f;

    public static AchievementManager Instance { get; private set; }
    public static event Action<Logro> Unlocked;

    public IReadOnlyList<Logro> Logros => logros;

    private readonly HashSet<TipoCombo> combosUsed = new HashSet<TipoCombo>();
    private readonly HashSet<string> bossesDefeated = new HashSet<string>();
    private Logro toast;
    private float toastUntil;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        foreach (var s in PlayerPrefs.GetString("eg_combos", "").Split(','))
            if (Enum.TryParse(s, out TipoCombo c)) combosUsed.Add(c);
        foreach (var s in PlayerPrefs.GetString("eg_jefes", "").Split('|'))
            if (s.Length > 0) bossesDefeated.Add(s);
    }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SpellUnlocked += _ => { if (gm.UnlockedSpellCount >= 4) Unlock(TipoLogro.MaestraElemental); };
            gm.FragmentsChanged += n => { if (n >= 5) Unlock(TipoLogro.ColeccionistaDelGrimorio); };
            gm.LevelCompleted += OnLevelCompleted;
            gm.GameFinished += OnGameFinished;
        }

        BossController.BossDefeated += OnBossDefeated;
        ModoArchimaga.Activated += OnArchmage;
        SceneManager.sceneLoaded += OnSceneLoaded;
        HookPlayer();
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        BossController.BossDefeated -= OnBossDefeated;
        ModoArchimaga.Activated -= OnArchmage;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HookPlayer();

    void HookPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var caster = player.GetComponent<SpellCaster>();
        if (caster == null) return;
        caster.SpellCast += _ => Unlock(TipoLogro.PrimerHechizo);
        caster.ComboCast += OnCombo;
    }

    void OnCombo(TipoCombo combo)
    {
        if (!combosUsed.Add(combo)) return;
        PlayerPrefs.SetString("eg_combos", string.Join(",", combosUsed));
        if (combosUsed.Count >= 4) Unlock(TipoLogro.ComboPerfecto);
    }

    void OnBossDefeated(string bossName)
    {
        bossesDefeated.Add(bossName);
        PlayerPrefs.SetString("eg_jefes", string.Join("|", bossesDefeated));

        if (bossesDefeated.Contains("Kaelor") && bossesDefeated.Contains("Isolde") && bossesDefeated.Contains("Threnody"))
            Unlock(TipoLogro.CazadoraDeEcos);

        if (bossName.Contains("Elenora") && GameManager.Instance != null && GameManager.Instance.PotionsUsedInBossFight == 0)
            Unlock(TipoLogro.SinPiedad);
    }

    void OnLevelCompleted(string scene, float seconds, bool tookDamage)
    {
        if (!tookDamage) Unlock(TipoLogro.SinUnRasguno);
        if (scene == GameManager.FirstLevel && seconds < 300f) Unlock(TipoLogro.VelocistaArcana);
    }

    void OnGameFinished()
    {
        if (GameManager.Instance != null && GameManager.Instance.CompletionPercent >= 1f)
            Unlock(TipoLogro.ElGrimorioCompleto);
    }

    void OnArchmage() => Unlock(TipoLogro.SecretoRevelado);

    public bool IsUnlocked(TipoLogro tipo) => PlayerPrefs.GetInt("logro_" + tipo, 0) == 1;

    public void Unlock(TipoLogro tipo)
    {
        if (IsUnlocked(tipo)) return;
        PlayerPrefs.SetInt("logro_" + tipo, 1);
        PlayerPrefs.Save();

        var logro = Array.Find(logros, l => l.tipo == tipo);
        if (logro == null) return;
        toast = logro;
        toastUntil = Time.unscaledTime + toastSeconds;
        Unlocked?.Invoke(logro);
    }

    void OnGUI()
    {
        if (toast == null || Time.unscaledTime > toastUntil) return;

        var rect = new Rect(Screen.width - 420, Screen.height - 130, 400, 100);
        GUI.Box(rect, GUIContent.none);
        if (toast.icono != null)
            GUI.DrawTexture(new Rect(rect.x + 10, rect.y + 10, 80, 80), SpriteTexture(toast.icono), ScaleMode.ScaleToFit);

        var title = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        var body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        GUI.Label(new Rect(rect.x + 100, rect.y + 10, 290, 24), "Logro desbloqueado: " + toast.nombre, title);
        GUI.Label(new Rect(rect.x + 100, rect.y + 36, 290, 60), toast.condicion, body);
    }

    // Lista de logros (pantalla de logros desde la pausa)
    public void DrawList(Rect area)
    {
        GUI.Box(area, "LOGROS  (L para cerrar)");
        float y = area.y + 30;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
        foreach (var l in logros)
        {
            bool got = IsUnlocked(l.tipo);
            var old = GUI.color;
            GUI.color = got ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            if (l.icono != null) GUI.DrawTexture(new Rect(area.x + 10, y, 40, 40), SpriteTexture(l.icono), ScaleMode.ScaleToFit);
            GUI.Label(new Rect(area.x + 60, y, area.width - 70, 44), $"{l.nombre}: {l.condicion}", style);
            GUI.color = old;
            y += 46;
        }
    }

    static Texture SpriteTexture(Sprite s) => s.texture;
}
