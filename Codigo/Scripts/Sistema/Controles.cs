using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// todos los controles juntos (teclado y control de PS4)
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

    // stick derecho para apuntar en cualquier direccion
    public static Vector2 ApuntarStick
    {
        get
        {
            if (Pad == null) return Vector2.zero;
            var v = Pad.rightStick.ReadValue();
            return v.magnitude > 0.45f ? v.normalized : Vector2.zero;
        }
    }

    // posicion del mouse en pantalla (para apuntar con el clic)
    public static Vector2 PosicionMouse => Mouse != null ? Mouse.position.ReadValue() : Vector2.zero;
    public static bool ClicMouse => Mouse != null && Mouse.leftButton.wasPressedThisFrame;

    public static bool Abajo =>
        (Kb != null && (Kb.sKey.isPressed || Kb.downArrowKey.isPressed)) ||
        (Pad != null && (Pad.leftStick.y.ReadValue() < -0.6f || Pad.dpad.down.isPressed));

    // truco con el control: L1 + R1 + Triangulo juntos, en cualquier orden
    public static bool TrucoControl =>
        Pad != null && Pad.leftShoulder.isPressed && Pad.rightShoulder.isPressed && Pad.buttonNorth.isPressed &&
        (Pad.leftShoulder.wasPressedThisFrame || Pad.rightShoulder.wasPressedThisFrame || Pad.buttonNorth.wasPressedThisFrame);

    public static bool SaltarPresionado =>
        (Kb != null && (Kb.spaceKey.wasPressedThisFrame || Kb.wKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

    public static bool SaltarSostenido =>
        (Kb != null && (Kb.spaceKey.isPressed || Kb.wKey.isPressed)) ||
        (Pad != null && Pad.buttonSouth.isPressed);

    // avanza dialogos (y en el teclado lanza el hechizo seleccionado)
    public static bool LanzarPresionado =>
        (Kb != null && Kb.jKey.wasPressedThisFrame) ||
        (Mouse != null && Mouse.leftButton.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonWest.wasPressedThisFrame);

    // j o clic: lanza el hechizo del espacio seleccionado
    public static bool LanzarSeleccionado =>
        (Kb != null && Kb.jKey.wasPressedThisFrame) || ClicMouse;

    // cada espacio tiene su propio boton para poder encadenar combos rapido:
    // teclado 1, 2, 3 / control Cuadrado, R1, L1
    public static bool EspacioPresionado(int slot)
    {
        switch (slot)
        {
            case 0: return (Kb != null && Kb.digit1Key.wasPressedThisFrame) || (Pad != null && Pad.buttonWest.wasPressedThisFrame);
            case 1: return (Kb != null && Kb.digit2Key.wasPressedThisFrame) || (Pad != null && Pad.rightShoulder.wasPressedThisFrame && !Pad.leftShoulder.isPressed);
            case 2: return (Kb != null && Kb.digit3Key.wasPressedThisFrame) || (Pad != null && Pad.leftShoulder.wasPressedThisFrame && !Pad.rightShoulder.isPressed);
            default: return false;
        }
    }

    // combo rapido: lanza el combo de dos hechizos equipados con un solo boton
    public static bool ComboRapido =>
        (Kb != null && Kb.cKey.wasPressedThisFrame) ||
        (Pad != null && Pad.leftTrigger.wasPressedThisFrame);

    public static string TextoEspacio(int slot) =>
        HayControl ? (slot == 0 ? "□" : slot == 1 ? "R1" : "L1") : (slot + 1).ToString();

    public static string TextoCombo => HayControl ? "L2" : "C";

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

    // texto de ayuda segun lo que este usando el jugador
    public static string TextoInteractuar => HayControl ? "Triángulo" : "E";
    public static string TextoAceptar => HayControl ? "X" : "Enter";

    // vibracion del control

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
