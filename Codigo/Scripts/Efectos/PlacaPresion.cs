using UnityEngine;

// Placa en el piso: cuando una caja (Empujable) queda encima abre las puertas rúnicas asignadas.
[RequireComponent(typeof(Collider2D))]
public class PlacaPresion : MonoBehaviour
{
    [SerializeField] private PuertaRunica[] puertas = new PuertaRunica[0];
    [SerializeField] private SpriteRenderer runa;

    private bool activada;

    void OnTriggerStay2D(Collider2D other)
    {
        if (activada || other.GetComponentInParent<Empujable>() == null) return;
        activada = true;
        if (runa != null) runa.color = new Color(0.6f, 1f, 0.8f, 1f);
        AudioManager.Play(Sfx.Checkpoint, 0.8f, 1.2f);
        Particula.Rafaga(transform.position, new Color(0.6f, 1f, 0.8f, 1f), 14, 3f, 0.12f, 0.6f, -2f);
        foreach (var p in puertas) if (p != null) p.Abrir();
    }
}
