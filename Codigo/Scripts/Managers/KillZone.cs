using UnityEngine;

// Zona debajo del nivel: si Lira (o un enemigo) cae aquí, muere.
[RequireComponent(typeof(Collider2D))]
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponentInParent<Health>();
        if (health != null) health.Kill();
    }
}
