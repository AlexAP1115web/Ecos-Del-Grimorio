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

    public static AchievementManager Instance { get; private set; }
    public static event Action<Logro> Unlocked;

    public IReadOnlyList<Logro> Logros => logros;

    private readonly HashSet<TipoCombo> combosUsed = new HashSet<TipoCombo>();
    private readonly HashSet<string> bossesDefeated = new HashSet<string>();

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
        Unlocked?.Invoke(logro);
    }

}
