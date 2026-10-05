using UnityEngine;
using UnityEngine.UI;

// Pantalla de Opciones (menú principal y pausa, secciones 2.5 y 2.6):
// volumen de música, efectos y voces, pantalla completa y vibración del control.
public class OpcionesUI : MonoBehaviour
{
    [SerializeField] private Slider musica;
    [SerializeField] private Slider efectos;
    [SerializeField] private Slider voces;
    [SerializeField] private Toggle pantallaCompleta;
    [SerializeField] private Toggle vibracion;
    [SerializeField] private Text[] porcentajes = new Text[3];

    private bool cargando;

    void Start()
    {
        if (musica != null) musica.onValueChanged.AddListener(v => { if (!cargando && AudioManager.Instance != null) AudioManager.Instance.VolumenMusica = v; Actualizar(); });
        if (efectos != null) efectos.onValueChanged.AddListener(v =>
        {
            if (cargando) return;
            if (AudioManager.Instance != null) AudioManager.Instance.VolumenEfectos = v;
            AudioManager.Play(Sfx.MenuMover);
            Actualizar();
        });
        if (voces != null) voces.onValueChanged.AddListener(v =>
        {
            if (cargando) return;
            if (AudioManager.Instance != null) AudioManager.Instance.VolumenVoces = v;
            AudioManager.Hablar("Lira");
            Actualizar();
        });
        if (pantallaCompleta != null) pantallaCompleta.onValueChanged.AddListener(v => { if (!cargando) Screen.fullScreen = v; });
        if (vibracion != null) vibracion.onValueChanged.AddListener(v =>
        {
            if (cargando) return;
            Controles.VibracionActiva = v;
            if (v) Controles.Vibrar(0.5f, 0.5f, 0.25f);
        });
    }

    void OnEnable()
    {
        cargando = true;
        var am = AudioManager.Instance;
        if (am != null)
        {
            if (musica != null) musica.value = am.VolumenMusica;
            if (efectos != null) efectos.value = am.VolumenEfectos;
            if (voces != null) voces.value = am.VolumenVoces;
        }
        if (pantallaCompleta != null) pantallaCompleta.isOn = Screen.fullScreen;
        if (vibracion != null) vibracion.isOn = Controles.VibracionActiva;
        cargando = false;
        Actualizar();
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
    }

    void Actualizar()
    {
        var sliders = new[] { musica, efectos, voces };
        for (int i = 0; i < porcentajes.Length && i < sliders.Length; i++)
            if (porcentajes[i] != null && sliders[i] != null) porcentajes[i].text = $"{Mathf.RoundToInt(sliders[i].value * 100f)}%";
    }
}
