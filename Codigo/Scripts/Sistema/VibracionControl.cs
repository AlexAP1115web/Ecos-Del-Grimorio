using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// apaga la vibracion del control despues de un momento
public class VibracionControl : MonoBehaviour
{
    Coroutine actual;

    public void Vibrar(Gamepad pad, float bajo, float alto, float segundos)
    {
        if (actual != null) StopCoroutine(actual);
        actual = StartCoroutine(Rutina(pad, bajo, alto, segundos));
    }

    IEnumerator Rutina(Gamepad pad, float bajo, float alto, float segundos)
    {
        pad.SetMotorSpeeds(bajo, alto);
        yield return new WaitForSecondsRealtime(segundos);
        pad.SetMotorSpeeds(0f, 0f);
        actual = null;
    }

    void OnDisable()
    {
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}
