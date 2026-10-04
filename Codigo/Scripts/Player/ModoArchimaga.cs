using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Código secreto de la sección 2.16: F-H-V-A + Intro.
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
        Debug.Log("Modo Archimaga activado");
    }
}
