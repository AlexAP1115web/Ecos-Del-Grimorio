using UnityEngine;

// Carámbano del Ala de Hielo: cuelga debajo de una plataforma y, cuando Lira pasa
// por abajo, tiembla un momento y cae. Se rompe al tocar el suelo.
public class Carambano : MonoBehaviour
{
    [SerializeField] private float dano = 15f;
    [SerializeField] private float temblor = 0.45f;

    private Transform player;
    private Rigidbody2D rb;
    private Vector3 inicio;
    private float cayendoDesde = -1f;
    private bool cayendo;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        inicio = transform.position;
    }

    void Update()
    {
        if (cayendo || player == null) return;

        if (cayendoDesde < 0f)
        {
            bool debajo = Mathf.Abs(player.position.x - inicio.x) < 1.2f && player.position.y < inicio.y && inicio.y - player.position.y < 7f;
            if (debajo) cayendoDesde = Time.time;
            return;
        }

        transform.position = inicio + new Vector3(Mathf.Sin(Time.time * 60f) * 0.05f, 0f, 0f);
        if (Time.time - cayendoDesde >= temblor)
        {
            cayendo = true;
            transform.position = inicio;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!cayendo) return;
        if (other.CompareTag("Player"))
        {
            var h = other.GetComponentInParent<Health>();
            if (h != null) h.TakeDamage(dano, Elemento.Hielo);
            Romper();
        }
        else if (other.CompareTag("Ground") && !other.isTrigger)
        {
            Romper();
        }
    }

    void Romper()
    {
        Particula.Rafaga(transform.position, new Color(0.8f, 0.95f, 1f, 1f), 14, 4f, 0.1f, 0.5f, 6f);
        AudioManager.Play(Sfx.Hielazo, 0.8f);
        Destroy(gameObject);
    }
}
