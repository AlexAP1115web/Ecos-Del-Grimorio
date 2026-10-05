using UnityEngine;

// desvanece un sprite y luego lo destruye
public class Desvanecer : MonoBehaviour
{
    [SerializeField] private float duracion = 0.8f;

    private SpriteRenderer sr;
    private Color inicio;
    private float t;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) inicio = sr.color;
    }

    void Update()
    {
        t += Time.deltaTime / duracion;
        if (sr != null) sr.color = new Color(inicio.r, inicio.g, inicio.b, inicio.a * (1f - t));
        if (t >= 1f) Destroy(gameObject);
    }
}
