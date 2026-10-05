using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Sfx
{
    Salto, Aterrizaje, Esquive,
    Arcano, Fuego, Hielo, Viento, Combo,
    GolpeEnemigo, MuerteEnemigo, DanoLira,
    Objeto, ObjetoEspecial, Pagina, Checkpoint, Cofre, Romper,
    MenuMover, MenuAceptar, Logro, Portal, GameOver, JefeAparece, Paso, Victoria, Geiser, Hielazo, Viento2
}

public enum VozLira { Dano, Esfuerzo, Caida }

// Voz de un personaje: sílabas cortas que suenan mientras aparece su texto en los diálogos
[Serializable]
public class VozPersonaje
{
    public string nombre;
    public AudioClip[] silabas = new AudioClip[0];
    [Range(0f, 1f)] public float volumen = 0.6f;
}

// Música y efectos de sonido. Vive en el prefab del GameManager, así que pasa de una
// escena a otra sin cortarse. Cada escena tiene su tema; al activarse un jefe cambia
// a la música de combate y al derrotarlo regresa la del nivel (con un fundido).
// También tiene las voces de los personajes y los quejidos de Lira.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Música")]
    [SerializeField] private AudioClip musicaMenu;
    [Tooltip("Una por nivel, en orden: Aprendizaje, Fuego, Hielo, Viento, Corazón")]
    [SerializeField] private AudioClip[] musicaNiveles = new AudioClip[5];
    [SerializeField] private AudioClip musicaJefe;
    [SerializeField] private AudioClip musicaJefeFinal;
    [SerializeField] private AudioClip musicaCreditos;

    [Header("Efectos (en el orden del enum Sfx)")]
    [SerializeField] private AudioClip[] efectos = new AudioClip[0];

    [Header("Voces")]
    [Tooltip("El orden importa: se busca el primer nombre que aparezca en el nombre de quien habla")]
    [SerializeField] private VozPersonaje[] voces = new VozPersonaje[0];
    [SerializeField] private AudioClip[] liraDano = new AudioClip[0];
    [SerializeField] private AudioClip[] liraEsfuerzo = new AudioClip[0];
    [SerializeField] private AudioClip liraCaida;
    [Range(0f, 1f)] [SerializeField] private float volumenVoces = 0.55f;

    [Header("Volumen")]
    [Range(0f, 1f)] [SerializeField] private float volumenMusica = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float volumenEfectos = 0.8f;
    [SerializeField] private float fundido = 1.2f;

    private AudioSource[] musica = new AudioSource[2];
    private int musicaActiva;
    private AudioSource[] canales;
    private int siguienteCanal;
    private AudioClip musicaNivel;
    private float duck = 1f, duckObjetivo = 1f;
    private Coroutine fundidoActual;
    private readonly float[] ultimoSonido = new float[32];
    private AudioSource voz;
    private float ultimaQueja;

    public float VolumenMusica
    {
        get => volumenMusica;
        set { volumenMusica = Mathf.Clamp01(value); PlayerPrefs.SetFloat("eg_vol_musica", volumenMusica); }
    }

    public float VolumenEfectos
    {
        get => volumenEfectos;
        set { volumenEfectos = Mathf.Clamp01(value); PlayerPrefs.SetFloat("eg_vol_efectos", volumenEfectos); }
    }

    void Awake()
    {
        // El GameManager duplicado se destruye solo; aquí solo evitamos inicializarlo
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;

        volumenMusica = PlayerPrefs.GetFloat("eg_vol_musica", volumenMusica);
        volumenEfectos = PlayerPrefs.GetFloat("eg_vol_efectos", volumenEfectos);

        for (int i = 0; i < 2; i++)
        {
            musica[i] = gameObject.AddComponent<AudioSource>();
            musica[i].loop = true;
            musica[i].playOnAwake = false;
            musica[i].volume = 0f;
            musica[i].ignoreListenerPause = true;
        }
        canales = new AudioSource[10];
        for (int i = 0; i < canales.Length; i++)
        {
            canales[i] = gameObject.AddComponent<AudioSource>();
            canales[i].playOnAwake = false;
            canales[i].ignoreListenerPause = true;
        }

        voz = gameObject.AddComponent<AudioSource>();
        voz.playOnAwake = false;
        voz.ignoreListenerPause = true;

        // Sin un AudioListener no se escucha nada: el AudioManager lleva el suyo
        gameObject.AddComponent<AudioListener>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        BossController.BossActivated += OnBossActivated;
        BossController.BossDefeated += OnBossDefeated;
        AchievementManager.Unlocked += OnAchievement;
    }

    void Start()
    {
        if (Instance == this) OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        BossController.BossActivated -= OnBossActivated;
        BossController.BossDefeated -= OnBossDefeated;
        AchievementManager.Unlocked -= OnAchievement;
        Instance = null;
    }

    void Update()
    {
        // Baja la música en pausa y en Game Over
        if (GameManager.Instance != null)
        {
            var st = GameManager.Instance.State;
            duckObjetivo = st == GameState.Pausa ? 0.45f : st == GameState.GameOver ? 0.3f : 1f;
        }
        duck = Mathf.MoveTowards(duck, duckObjetivo, Time.unscaledDeltaTime * 2f);
        if (fundidoActual == null)
        {
            musica[musicaActiva].volume = volumenMusica * duck;
            musica[1 - musicaActiva].volume = 0f;
        }
    }

    // ---------- Música ----------

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Solo debe haber un AudioListener: se quitan los de las cámaras
        foreach (var cam in Camera.allCameras)
        {
            var l = cam.GetComponent<AudioListener>();
            if (l != null) Destroy(l);
        }

        AudioClip clip = musicaMenu;
        string n = scene.name;
        if (n == GameManager.CreditsScene || n == GameManager.EpilogueScene) clip = musicaCreditos;
        else if (n.StartsWith("Nivel"))
        {
            int idx = n.Length > 5 && char.IsDigit(n[5]) ? n[5] - '1' : 0;
            if (musicaNiveles != null && idx >= 0 && idx < musicaNiveles.Length && musicaNiveles[idx] != null)
                clip = musicaNiveles[idx];
        }
        musicaNivel = clip;
        PlayMusic(clip);
    }

    void OnBossActivated(BossController boss)
    {
        Play(Sfx.JefeAparece);
        PlayMusic(boss is ElenoraBoss && musicaJefeFinal != null ? musicaJefeFinal : musicaJefe);
    }

    void OnBossDefeated(string bossName)
    {
        PlayMusic(musicaNivel);
    }

    void OnAchievement(Logro logro) => Play(Sfx.Logro);

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        var actual = musica[musicaActiva];
        if (actual.clip == clip && actual.isPlaying) return;

        if (fundidoActual != null) StopCoroutine(fundidoActual);
        fundidoActual = StartCoroutine(Fundido(clip));
    }

    IEnumerator Fundido(AudioClip clip)
    {
        var sale = musica[musicaActiva];
        musicaActiva = 1 - musicaActiva;
        var entra = musica[musicaActiva];
        entra.clip = clip;
        entra.volume = 0f;
        entra.Play();

        float inicioSale = sale.volume;
        for (float t = 0f; t < fundido; t += Time.unscaledDeltaTime)
        {
            float k = t / fundido;
            sale.volume = Mathf.Lerp(inicioSale, 0f, k);
            entra.volume = Mathf.Lerp(0f, volumenMusica * duck, k);
            yield return null;
        }
        sale.Stop();
        sale.volume = 0f;
        entra.volume = volumenMusica * duck;
        fundidoActual = null;
    }

    // ---------- Voces ----------

    // Una sílaba de la voz de quien habla (se llama mientras se escribe el diálogo)
    public static void Hablar(string personaje, float tono = 1f)
    {
        if (Instance != null) Instance.HablarInternal(personaje, tono);
    }

    void HablarInternal(string personaje, float tono)
    {
        var v = BuscarVoz(personaje);
        if (v == null || v.silabas == null || v.silabas.Length == 0) return;
        var clip = v.silabas[UnityEngine.Random.Range(0, v.silabas.Length)];
        if (clip == null) return;
        voz.pitch = tono * UnityEngine.Random.Range(0.96f, 1.05f);
        voz.PlayOneShot(clip, v.volumen * volumenVoces);
    }

    VozPersonaje BuscarVoz(string personaje)
    {
        if (string.IsNullOrEmpty(personaje) || voces == null) return null;
        foreach (var v in voces)
            if (v != null && string.Equals(v.nombre, personaje, StringComparison.OrdinalIgnoreCase)) return v;
        foreach (var v in voces)
            if (v != null && !string.IsNullOrEmpty(v.nombre) && personaje.IndexOf(v.nombre, StringComparison.OrdinalIgnoreCase) >= 0) return v;
        return null;
    }

    public static void Lira(VozLira tipo)
    {
        if (Instance != null) Instance.LiraInternal(tipo);
    }

    void LiraInternal(VozLira tipo)
    {
        AudioClip clip = null;
        switch (tipo)
        {
            case VozLira.Dano:
                if (Time.unscaledTime - ultimaQueja < 0.5f) return;
                if (liraDano.Length > 0) clip = liraDano[UnityEngine.Random.Range(0, liraDano.Length)];
                break;
            case VozLira.Esfuerzo:
                if (Time.unscaledTime - ultimaQueja < 0.8f) return;
                if (liraEsfuerzo.Length > 0) clip = liraEsfuerzo[UnityEngine.Random.Range(0, liraEsfuerzo.Length)];
                break;
            case VozLira.Caida:
                clip = liraCaida;
                break;
        }
        if (clip == null) return;
        ultimaQueja = Time.unscaledTime;
        voz.pitch = UnityEngine.Random.Range(0.97f, 1.04f);
        voz.PlayOneShot(clip, volumenVoces);
    }

    // ---------- Efectos ----------

    // Se puede llamar desde cualquier script aunque no haya AudioManager en la escena
    public static void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
    {
        if (Instance != null) Instance.PlayInternal(sfx, volume, pitch);
    }

    void PlayInternal(Sfx sfx, float volume, float pitch)
    {
        int i = (int)sfx;
        if (efectos == null || i >= efectos.Length || efectos[i] == null) return;

        // Evita que el mismo sonido se encime muchas veces en el mismo instante
        if (i < ultimoSonido.Length)
        {
            if (Time.unscaledTime - ultimoSonido[i] < 0.04f) return;
            ultimoSonido[i] = Time.unscaledTime;
        }

        var src = canales[siguienteCanal];
        siguienteCanal = (siguienteCanal + 1) % canales.Length;
        src.pitch = pitch * UnityEngine.Random.Range(0.95f, 1.05f);
        src.PlayOneShot(efectos[i], volume * volumenEfectos);
    }
}
