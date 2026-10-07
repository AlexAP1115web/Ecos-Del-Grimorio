using System.Collections;
using UnityEngine;

// Puerta de piedra con runas que bloquea un camino. Se abre con una PlacaPresion.
public class PuertaRunica : MonoBehaviour
{
    [SerializeField] private float subir = 3.5f;
    [SerializeField] private float duracion = 1.2f;

    private bool abierta;
    private float siguientePista;

    // Un hechizo la golpea: no se rompe, pero dice cómo abrirla
    public void Pista()
    {
        if (abierta || Time.time < siguientePista) return;
        siguientePista = Time.time + 3f;
        if (GameManager.Instance != null)
            GameManager.Instance.ShowMessage("La puerta rúnica no se rompe... la abre la placa de presión.", 3f);
    }

    public void Abrir()
    {
        if (abierta) return;
        abierta = true;
        StartCoroutine(Subir());
    }

    IEnumerator Subir()
    {
        if (GameManager.Instance != null) GameManager.Instance.ShowMessage("¡Una puerta rúnica se abrió!", 3f);
        AudioManager.Play(Sfx.Romper, 0.8f, 0.5f);
        CameraFollow.Shake(0.12f, duracion);
        Vector3 inicio = transform.position;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            transform.position = inicio + Vector3.up * subir * (t / duracion);
            yield return null;
        }
        transform.position = inicio + Vector3.up * subir;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.35f);
    }
}
