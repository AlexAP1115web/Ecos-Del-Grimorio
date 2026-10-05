using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Todas las entradas del juego en un solo lugar: teclado y control (PS4 / Xbox).
// El Input System reconoce el DualShock 4 por USB o Bluetooth sin instalar nada.
//
//  Acción              Teclado            Control PS4
//  Moverse             A/D o flechas      Stick izquierdo o cruceta
//  Saltar              Espacio / W        X
//  Lanzar hechizo      J / clic           Cuadrado
//  Hechizo 1, 2, 3     1, 2, 3            L1 / R1 (anterior / siguiente)
//  Cambiar hechizo     Q                  R2
//  Esquive             Shift / K          Círculo
//  Hablar / abrir      E                  Triángulo
//  Pausa               Esc                Options
public static class Controles
{
    static Keyboard Kb => Keyboard.current;
    static Gamepad Pad => Gamepad.current;
    static Mouse Mouse => Mouse.current;

    public static bool HayControl => Pad != null;

    public static float Horizontal
    {
        get
        {
            float h = 0f;
            if (Kb != null)
            {
                if (Kb.aKey.isPressed || Kb.leftArrowKey.isPressed) h -= 1f;
                if (Kb.dKey.isPressed || Kb.rightArrowKey.isPressed) h += 1f;
            }
            if (Pad != null)
            {
                float stick = Pad.leftStick.x.ReadValue();
                if (Mathf.Abs(stick) > 0.25f) h = stick;
                if (Pad.dpad.left.isPressed) h = -1f;
                if (Pad.dpad.right.isPressed) h = 1f;
            }
            return Mathf.Clamp(h, -1f, 1f);
        }
    }

    public static bool Arriba =>
        (Kb != null && (Kb.upArrowKey.isPressed)) ||
        (Pad != null && (Pad.leftStick.y.ReadValue() > 0.6f || Pad.dpad.up.isPressed));

    public static bool SaltarPresionado =>
        (Kb != null && (Kb.spaceKey.wasPressedThisFrame || Kb.wKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

    public static bool SaltarSostenido =>
        (Kb != null && (Kb.spaceKey.isPressed || Kb.wKey.isPressed)) ||
        (Pad != null && Pad.buttonSouth.isPressed);

    public static bool LanzarPresionado =>
        (Kb != null && Kb.jKey.wasPressedThisFrame) ||
        (Mouse != null && Mouse.leftButton.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonWest.wasPressedThisFrame);

    public static bool EspacioPresionado(int slot)
    {
        if (Kb == null) return false;
        switch (slot)
        {
            case 0: return Kb.digit1Key.wasPressedThisFrame;
            case 1: return Kb.digit2Key.wasPressedThisFrame;
            case 2: return Kb.digit3Key.wasPressedThisFrame;
            default: return false;
        }
    }

    public static bool SiguienteEspacio => Pad != null && Pad.rightShoulder.wasPressedThisFrame;
    public static bool AnteriorEspacio => Pad != null && Pad.leftShoulder.wasPressedThisFrame;

    public static bool CambiarHechizo =>
        (Kb != null && Kb.qKey.wasPressedThisFrame) ||
        (Pad != null && Pad.rightTrigger.wasPressedThisFrame);

    public static bool EsquivePresionado =>
        (Kb != null && (Kb.leftShiftKey.wasPressedThisFrame || Kb.kKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.buttonEast.wasPressedThisFrame);

    public static bool InteractuarPresionado =>
        (Kb != null && Kb.eKey.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonNorth.wasPressedThisFrame);

    public static bool PausaPresionado =>
        (Kb != null && Kb.escapeKey.wasPressedThisFrame) ||
        (Pad != null && Pad.startButton.wasPressedThisFrame);

    public static bool AceptarPresionado =>
        (Kb != null && (Kb.enterKey.wasPressedThisFrame || Kb.numpadEnterKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

    // Texto de ayuda según lo que esté usando el jugador
    public static string TextoInteractuar => HayControl ? "Triángulo" : "E";
    public static string TextoAceptar => HayControl ? "X" : "Enter";

    // ---------- Vibración del control ----------

    static VibracionControl runner;

    public static void Vibrar(float bajo, float alto, float segundos)
    {
        if (Pad == null) return;
        if (runner == null)
        {
            var go = new GameObject("Vibracion");
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<VibracionControl>();
        }
        runner.Vibrar(Pad, bajo, alto, segundos);
    }
}
