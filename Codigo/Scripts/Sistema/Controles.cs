using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Todas las entradas del juego en un solo lugar: teclado y control (PS4 / Xbox).
// El Input System reconoce el DualShock 4 por USB o Bluetooth sin instalar nada.
//
// El teclado está pensado para laptop: la mano izquierda en W A S D y la derecha en J K L,
// sin necesitar mouse, flechas ni la fila de números (aunque también funcionan).
//
//  Acción              Teclado (laptop)        Control PS4
//  Moverse             A / D                   Stick izquierdo o cruceta
//  Saltar / planear    Espacio                 X
//  Apuntar arriba      W (con A/D = diagonal)  Stick derecho (360°) o stick izq. arriba
//  Agacharse           S                       Stick o cruceta abajo
//  Hechizo 1, 2, 3     J, K, L  (o 1, 2, 3)    Cuadrado, R1, L1
//  Combo rápido        I  (o C)                L2
//  Cambiar hechizo     U  (o Q)                R2
//  Esquive             Shift                   Círculo
//  Hablar / abrir      E                       Triángulo
//  Pausa               Esc o P                 Options
//  Saltar nivel        Mantén N                Mantén L3 + R3 (o en la pausa)
//
// En el celular (versión web) aparecen botones en pantalla: ControlesTactiles.cs.
//  Modo Archimaga      F H V A + Enter         L1 + R1 + Triángulo (en cualquier orden)
public static class Controles
{
    static Keyboard Kb => Keyboard.current;
    static Gamepad Pad => Gamepad.current;
    static Mouse Mouse => Mouse.current;

    // Los textos de ayuda muestran los botones del último dispositivo que se usó:
    // si hay control conectado pero se juega con el teclado, aparecen las teclas.
    static bool usandoControl = true;
    public static bool HayControl => Pad != null && usandoControl && !UsandoTactil;
    public static bool UsandoTactil => ControlesTactiles.Activo;

    // Se llama una vez por cuadro (DetectorEntrada) para saber qué está usando el jugador
    public static void ActualizarDispositivo()
    {
        if (Kb != null && Kb.anyKey.wasPressedThisFrame) usandoControl = false;
        else if (Mouse != null && Mouse.leftButton.wasPressedThisFrame) usandoControl = false;
        else if (Pad != null && (Pad.buttonSouth.wasPressedThisFrame || Pad.buttonEast.wasPressedThisFrame ||
                 Pad.buttonWest.wasPressedThisFrame || Pad.buttonNorth.wasPressedThisFrame ||
                 Pad.leftShoulder.wasPressedThisFrame || Pad.rightShoulder.wasPressedThisFrame ||
                 Pad.leftTrigger.wasPressedThisFrame || Pad.rightTrigger.wasPressedThisFrame ||
                 Pad.startButton.wasPressedThisFrame || Pad.leftStick.ReadValue().magnitude > 0.5f ||
                 Pad.rightStick.ReadValue().magnitude > 0.5f || Pad.dpad.ReadValue().magnitude > 0.5f))
            usandoControl = true;
    }

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
            if (ControlesTactiles.Sostenido(BotonTactil.Izquierda)) h = -1f;
            if (ControlesTactiles.Sostenido(BotonTactil.Derecha)) h = 1f;
            return Mathf.Clamp(h, -1f, 1f);
        }
    }

    public static bool Arriba =>
        (Kb != null && (Kb.wKey.isPressed || Kb.upArrowKey.isPressed)) ||
        (Pad != null && (Pad.leftStick.y.ReadValue() > 0.6f || Pad.dpad.up.isPressed)) ||
        ControlesTactiles.Sostenido(BotonTactil.Arriba);

    // Stick derecho para apuntar en cualquier dirección (cero si no se está usando)
    public static Vector2 ApuntarStick
    {
        get
        {
            if (Pad == null) return Vector2.zero;
            var v = Pad.rightStick.ReadValue();
            return v.magnitude > 0.45f ? v.normalized : Vector2.zero;
        }
    }

    // Posición del mouse en pantalla (para apuntar con el clic)
    public static Vector2 PosicionMouse => Mouse != null ? Mouse.position.ReadValue() : Vector2.zero;
    // En el celular el navegador convierte los toques en clics: ahí no cuenta el clic del mouse
    public static bool ClicMouse => Mouse != null && Mouse.leftButton.wasPressedThisFrame && !UsandoTactil;

    public static bool Abajo =>
        (Kb != null && (Kb.sKey.isPressed || Kb.downArrowKey.isPressed)) ||
        (Pad != null && (Pad.leftStick.y.ReadValue() < -0.6f || Pad.dpad.down.isPressed)) ||
        ControlesTactiles.Sostenido(BotonTactil.Abajo);

    // Truco con el control: L1 + R1 + Triángulo juntos, en cualquier orden
    public static bool TrucoControl =>
        Pad != null && Pad.leftShoulder.isPressed && Pad.rightShoulder.isPressed && Pad.buttonNorth.isPressed &&
        (Pad.leftShoulder.wasPressedThisFrame || Pad.rightShoulder.wasPressedThisFrame || Pad.buttonNorth.wasPressedThisFrame);

    public static bool SaltarPresionado =>
        (Kb != null && Kb.spaceKey.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonSouth.wasPressedThisFrame) ||
        ControlesTactiles.Presionado(BotonTactil.Saltar);

    public static bool SaltarSostenido =>
        (Kb != null && Kb.spaceKey.isPressed) ||
        (Pad != null && Pad.buttonSouth.isPressed) ||
        ControlesTactiles.Sostenido(BotonTactil.Saltar);

    // Avanza diálogos (y en el teclado lanza el hechizo seleccionado)
    public static bool LanzarPresionado =>
        (Kb != null && Kb.jKey.wasPressedThisFrame) ||
        ClicMouse ||
        (Pad != null && Pad.buttonWest.wasPressedThisFrame) ||
        ControlesTactiles.ToqueLibre || ControlesTactiles.Presionado(BotonTactil.Hechizo1);

    // Clic: lanza el hechizo del espacio seleccionado apuntando hacia el mouse
    public static bool LanzarSeleccionado => ClicMouse;

    // Cada espacio tiene su propio botón para poder encadenar combos rápido:
    // teclado J, K, L (o 1, 2, 3) / control Cuadrado, R1, L1
    public static bool EspacioPresionado(int slot)
    {
        switch (slot)
        {
            case 0: return (Kb != null && (Kb.jKey.wasPressedThisFrame || Kb.digit1Key.wasPressedThisFrame)) || (Pad != null && Pad.buttonWest.wasPressedThisFrame) || ControlesTactiles.Presionado(BotonTactil.Hechizo1);
            case 1: return (Kb != null && (Kb.kKey.wasPressedThisFrame || Kb.digit2Key.wasPressedThisFrame)) || (Pad != null && Pad.rightShoulder.wasPressedThisFrame && !Pad.leftShoulder.isPressed) || ControlesTactiles.Presionado(BotonTactil.Hechizo2);
            case 2: return (Kb != null && (Kb.lKey.wasPressedThisFrame || Kb.digit3Key.wasPressedThisFrame)) || (Pad != null && Pad.leftShoulder.wasPressedThisFrame && !Pad.rightShoulder.isPressed) || ControlesTactiles.Presionado(BotonTactil.Hechizo3);
            default: return false;
        }
    }

    // Combo rápido: lanza el combo de dos hechizos equipados con un solo botón
    public static bool ComboRapido =>
        (Kb != null && (Kb.iKey.wasPressedThisFrame || Kb.cKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.leftTrigger.wasPressedThisFrame) ||
        ControlesTactiles.Presionado(BotonTactil.Combo);

    public static string TextoEspacio(int slot) =>
        UsandoTactil ? (slot + 1).ToString() :
        HayControl ? (slot == 0 ? "□" : slot == 1 ? "R1" : "L1") : (slot == 0 ? "J" : slot == 1 ? "K" : "L");

    public static string TextoCombo => UsandoTactil ? "Combo" : HayControl ? "L2" : "I";
    public static string TextoCambiar => UsandoTactil ? "Cambiar" : HayControl ? "R2" : "U";

    public static bool CambiarHechizo =>
        (Kb != null && (Kb.uKey.wasPressedThisFrame || Kb.qKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.rightTrigger.wasPressedThisFrame) ||
        ControlesTactiles.Presionado(BotonTactil.Cambiar);

    public static bool EsquivePresionado =>
        (Kb != null && (Kb.leftShiftKey.wasPressedThisFrame || Kb.rightShiftKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.buttonEast.wasPressedThisFrame) ||
        ControlesTactiles.Presionado(BotonTactil.Esquivar);

    public static bool InteractuarPresionado =>
        (Kb != null && Kb.eKey.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonNorth.wasPressedThisFrame) ||
        ControlesTactiles.Presionado(BotonTactil.Usar);

    public static bool PausaPresionado =>
        (Kb != null && (Kb.escapeKey.wasPressedThisFrame || Kb.pKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.startButton.wasPressedThisFrame) ||
        ControlesTactiles.Presionado(BotonTactil.Pausa);

    public static bool AceptarPresionado =>
        (Kb != null && (Kb.enterKey.wasPressedThisFrame || Kb.numpadEnterKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

    // Saltar nivel: se mantiene presionado (lo revisa GameManager)
    public static bool SaltarNivelSostenido =>
        (Kb != null && Kb.nKey.isPressed) ||
        (Pad != null && Pad.leftStickButton.isPressed && Pad.rightStickButton.isPressed);

    public static string TextoSaltarNivel => UsandoTactil ? "Pausa > Saltar nivel" : HayControl ? "L3 + R3" : "N";

    // Texto de ayuda según lo que esté usando el jugador
    public static string TextoInteractuar => UsandoTactil ? "Usar" : HayControl ? "Triángulo" : "E";
    public static string TextoAceptar => UsandoTactil ? "Toca la pantalla para" : HayControl ? "X" : "Enter";

    // ---------- Vibración del control ----------

    static VibracionControl runner;

    public static bool VibracionActiva
    {
        get => PlayerPrefs.GetInt("eg_vibracion", 1) == 1;
        set => PlayerPrefs.SetInt("eg_vibracion", value ? 1 : 0);
    }

    // Ayuda para apuntar: los hechizos buscan al enemigo más cercano a la mira (se cambia en Opciones)
    public static bool AyudaApuntar
    {
        get => PlayerPrefs.GetInt("eg_ayuda_apuntar", 1) == 1;
        set => PlayerPrefs.SetInt("eg_ayuda_apuntar", value ? 1 : 0);
    }

    public static void Vibrar(float bajo, float alto, float segundos)
    {
        if (Pad == null || !VibracionActiva) return;
        if (runner == null)
        {
            var go = new GameObject("Vibracion");
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<VibracionControl>();
        }
        runner.Vibrar(Pad, bajo, alto, segundos);
    }
}
