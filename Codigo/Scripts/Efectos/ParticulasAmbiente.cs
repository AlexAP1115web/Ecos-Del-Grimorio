using UnityEngine;

// Partículas de ambiente alrededor de la cámara: polvo, brasas, nieve, hojas o motas arcanas.
public class ParticulasAmbiente : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.9f, 0.6f, 0.5f);
    [SerializeField] private Vector2 velocidad = new Vector2(0.2f, 0.4f);
    [SerializeField] private float porSegundo = 10f;
    [SerializeField] private float tamano = 0.08f;
    [SerializeField] private float vida = 5f;
    [SerializeField] private bool desdeArriba = false;

    Camera cam;
    float acumulado;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) return;
        acumulado += porSegundo * Time.deltaTime;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector2 c = cam.transform.position;

        while (acumulado >= 1f)
        {
            acumulado -= 1f;
            Vector2 pos = desdeArriba
                ? new Vector2(c.x + Random.Range(-w - 2f, w + 2f), c.y + h + 0.5f)
                : new Vector2(c.x + Random.Range(-w, w), c.y + Random.Range(-h, h));
            Vector2 vel = velocidad + Random.insideUnitCircle * 0.3f;
            var col = color;
            col.a *= Random.Range(0.4f, 1f);
            Particula.Crear(pos, vel, col, tamano * Random.Range(0.6f, 1.5f), vida, 0f, 25, false);
        }
    }
}
