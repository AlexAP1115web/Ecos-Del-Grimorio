using UnityEngine;

// Estela de partículas detrás de los proyectiles
public class EstelaProyectil : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float cada = 0.03f;
    [SerializeField] private float tamano = 0.25f;

    float siguiente;

    public void SetColor(Color c) => color = c;

    void Update()
    {
        if (Time.time < siguiente) return;
        siguiente = Time.time + cada;
        Particula.Crear((Vector2)transform.position + Random.insideUnitCircle * 0.08f, Vector2.zero, color, tamano, 0.3f, 0f, 14, true);
    }
}
