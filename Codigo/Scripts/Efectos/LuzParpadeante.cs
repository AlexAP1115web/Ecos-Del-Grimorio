using UnityEngine;

// Antorchas y cristales: la luz parpadea un poco para que el escenario se sienta vivo.
public class LuzParpadeante : MonoBehaviour
{
    [SerializeField] private float intensidadBase = 1f;
    [SerializeField] private float variacion = 0.25f;
    [SerializeField] private float velocidad = 6f;
    [SerializeField] private bool echaChispas = true;
    [SerializeField] private Color colorChispa = new Color(1f, 0.6f, 0.2f, 0.9f);

    Component luz;
    System.Reflection.PropertyInfo intensidad;
    float semilla;
    float siguienteChispa;

    void Start()
    {
        semilla = Random.Range(0f, 100f);
        var tipo = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime");
        if (tipo == null) return;
        luz = GetComponent(tipo);
        intensidad = tipo.GetProperty("intensity");
    }

    void Update()
    {
        if (luz != null && intensidad != null)
        {
            float n = Mathf.PerlinNoise(semilla, Time.time * velocidad);
            intensidad.SetValue(luz, intensidadBase + (n - 0.5f) * 2f * variacion);
        }

        if (echaChispas && Time.time >= siguienteChispa)
        {
            siguienteChispa = Time.time + Random.Range(0.15f, 0.5f);
            Particula.Crear((Vector2)transform.position + Random.insideUnitCircle * 0.1f,
                new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.6f, 1.2f)), colorChispa, Random.Range(0.05f, 0.1f), 1f, 0f, 6, true);
        }
    }
}
