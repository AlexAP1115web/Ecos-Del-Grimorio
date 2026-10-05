using UnityEngine;
using UnityEngine.EventSystems;

// sonido al moverse entre botones y al presionarlos
public class SonidoBoton : MonoBehaviour, ISelectHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
{
    private static float ultimo;

    void Mover()
    {
        if (Time.unscaledTime - ultimo < 0.05f) return;
        ultimo = Time.unscaledTime;
        AudioManager.Play(Sfx.MenuMover, 0.6f);
    }

    public void OnSelect(BaseEventData e) => Mover();
    public void OnPointerEnter(PointerEventData e) => Mover();
    public void OnSubmit(BaseEventData e) => AudioManager.Play(Sfx.MenuAceptar);
    public void OnPointerClick(PointerEventData e) => AudioManager.Play(Sfx.MenuAceptar);
}
