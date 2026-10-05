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
    private int progress;
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
        if (kb == null || !kb.anyKey.wasPressedThisFrame) return;

        Key expected = code[progress];
        bool pressedExpected = kb[expected].wasPressedThisFrame || (expected == Key.Enter && kb.numpadEnterKey.wasPressedThisFrame);

        if (pressedExpected)
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
            // Si se equivoca vuelve a empezar (pero la F cuenta como primer paso)
            progress = kb.fKey.wasPressedThisFrame ? 1 : 0;
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
