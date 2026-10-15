using UnityEngine;

// Zona debajo del nivel: si Lira (o un enemigo) cae aquí, muere.
[RequireComponent(typeof(Collider2D))]
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponentInParent<Health>();
        if (health == null || health.IsDead) return;
        // Sonido de caída al vacío (solo para Lira)
        if (health.CompareTag("Player")) AudioManager.Play(Sfx.Vacio, 0.9f);
        health.Kill();
    }
}
