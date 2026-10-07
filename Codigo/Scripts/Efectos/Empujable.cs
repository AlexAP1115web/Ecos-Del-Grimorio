using UnityEngine;

// Caja u objeto suelto que se mueve con el hechizo de Viento (sección 2.11).
// Los demás hechizos apenas la mueven. Se usa con PlacaPresion para abrir puertas.
[RequireComponent(typeof(Rigidbody2D))]
public class Empujable : MonoBehaviour
{
    [SerializeField] private float fuerzaViento = 7f;
    [SerializeField] private float fuerzaOtros = 0.6f;

    private Rigidbody2D rb;
    private Vector3 inicio;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inicio = transform.position;
    }

    // Si la caja se cae del nivel regresa a su lugar, para que el acertijo siempre se pueda resolver
    void FixedUpdate()
    {
        if (transform.position.y > inicio.y - 4f) return;
        rb.linearVelocity = Vector2.zero;
        transform.position = inicio;
        Particula.Rafaga(inicio, new Color(0.8f, 0.75f, 0.65f, 0.8f), 12, 3f, 0.15f, 0.5f);
        if (GameManager.Instance != null) GameManager.Instance.ShowMessage("La caja regresó a su lugar.", 2.5f);
    }

    public void Empujar(Vector2 direccion, Elemento elemento)
    {
        float fuerza = elemento == Elemento.Viento ? fuerzaViento : fuerzaOtros;
        float dir = Mathf.Abs(direccion.x) > 0.1f ? Mathf.Sign(direccion.x) : 0f;
        rb.linearVelocity = new Vector2(dir * fuerza, elemento == Elemento.Viento ? 2f : rb.linearVelocity.y);
        if (elemento == Elemento.Viento)
        {
            AudioManager.Play(Sfx.Viento2, 0.7f, 1.2f);
            Particula.Polvo((Vector2)transform.position + Vector2.down * 0.5f, new Color(0.8f, 0.75f, 0.65f, 0.6f));
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowMessage("Es muy pesada... tal vez el viento la mueva.", 2.5f);
        }
    }
}
