using UnityEngine;

// Adorno que se mueve despacio de un lado al otro del nivel (aves, nubes, niebla).
// Cuando sale por un extremo regresa por el otro. Opcionalmente sube y baja y aletea.
public class Deriva : MonoBehaviour
{
    [SerializeField] private float velocidad = 1f;
    [SerializeField] private float desde = -30f;
    [SerializeField] private float hasta = 150f;
    [SerializeField] private float ondulacion = 0f;
    [Tooltip("Aves: estira y encoge el dibujo en Y como si moviera las alas")]
    [SerializeField] private bool aletea = false;

    private float baseY;
    private Vector3 escala;
    private float fase;

    void Start()
    {
        baseY = transform.localPosition.y;
        escala = transform.localScale;
        fase = Random.Range(0f, 6f);
    }

    void Update()
    {
        var p = transform.localPosition;
        p.x += velocidad * Time.deltaTime;
        if (velocidad > 0f && p.x > hasta) p.x = desde;
        else if (velocidad < 0f && p.x < desde) p.x = hasta;
        if (ondulacion != 0f) p.y = baseY + Mathf.Sin(Time.time * 1.5f + fase) * ondulacion;
        transform.localPosition = p;

        if (aletea)
            transform.localScale = new Vector3(escala.x, escala.y * (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 7f + fase))), escala.z);
    }
}
