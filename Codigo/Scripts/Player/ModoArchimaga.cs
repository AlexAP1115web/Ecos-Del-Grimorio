using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Código secreto de la sección 2.16: F-H-V-A + Intro (o L1 + R1 + Triángulo en el control).
// Durante 60 segundos da maná ilimitado y quita el cooldown de los hechizos.
public class ModoArchimaga : MonoBehaviour
{
    [SerializeField] private float duration = 60f;

    public static event Action Activated;

    private readonly Key[] code = { Key.F, Key.H, Key.V, Key.A, Key.Enter };
    private readonly Key[] codeKeys = { Key.F, Key.H, Key.V, Key.A, Key.Enter };
    private int progress;
    private float lastKeyTime;
    private float endTime;
    private bool active;
    private SpellCaster caster;

    public bool IsActive => active;
    public float TimeLeft => active ? Mathf.Max(0f, endTime - Time.time) : 0f;

    void Awake()
    {
        caster = GetComponent<SpellCaster>();
    }

    void Update()
    {
        if (active && Time.time >= endTime)
        {
            active = false;
            if (caster != null) caster.SetArchmageMode(false);
            if (GameManager.Instance != null) GameManager.Instance.ShowMessage("El Modo Archimaga se terminó");
        }

        if (Time.timeScale > 0f && Controles.TrucoControl)
        {
            progress = 0;
            Activate();
            return;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        // Si pasa mucho tiempo entre letras se reinicia el código
        if (progress > 0 && Time.unscaledTime - lastKeyTime > 4f) progress = 0;

        // Solo cuentan las teclas del código: moverse, saltar o lanzar mientras se escribe no lo reinicia
        foreach (var key in codeKeys)
        {
            bool pressed = kb[key].wasPressedThisFrame || (key == Key.Enter && kb.numpadEnterKey.wasPressedThisFrame);
            if (!pressed) continue;

            lastKeyTime = Time.unscaledTime;
            if (key == code[progress])
            {
                progress++;
                if (progress == code.Length)
                {
                    progress = 0;
                    Activate();
                }
            }
            else
            {
                progress = key == Key.F ? 1 : 0;
            }
            break;
        }
    }

    void Activate()
    {
        active = true;
        endTime = Time.time + duration;
        if (caster != null) caster.SetArchmageMode(true);
        Activated?.Invoke();
        AudioManager.Play(Sfx.ObjetoEspecial);
        Particula.Rafaga(transform.position, new Color(0.8f, 0.6f, 1f, 1f), 30, 6f, 0.2f, 0.8f, 3f);
        if (GameManager.Instance != null)
            GameManager.Instance.ShowMessage($"Modo Archimaga: maná infinito y sin espera durante {Mathf.RoundToInt(duration)} segundos", 4f);
    }
}
