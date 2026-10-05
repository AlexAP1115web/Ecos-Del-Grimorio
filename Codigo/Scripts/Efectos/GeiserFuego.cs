using UnityEngine;

// geiser del ala de fuego, avisa con chispas y luego sale la llama
public class GeiserFuego : MonoBehaviour
{
    [SerializeField] private float intervalo = 3.5f;
    [SerializeField] private float aviso = 1f;
    [SerializeField] private float erupcion = 0.9f;
    [SerializeField] private float altura = 4f;
    [SerializeField] private float ancho = 1.1f;
    [SerializeField] private float dano = 12f;
    [SerializeField] private SpriteRenderer llama;

    private float t;
    private float nextHit;
    private Transform player;
    private Health playerHealth;

    void Start()
    {
        t = Random.Range(0f, intervalo);
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { player = p.transform; playerHealth = p.GetComponent<Health>(); }
        if (llama != null) llama.enabled = false;
    }

    void Update()
    {
        float ciclo = intervalo + aviso + erupcion;
        float antes = t;
        t = (t + Time.deltaTime) % ciclo;

        bool avisando = t >= intervalo && t < intervalo + aviso;
        bool activo = t >= intervalo + aviso;

        if (avisando && Random.value < 0.4f)
            Particula.Rafaga((Vector2)transform.position + Vector2.up * 0.2f, new Color(1f, 0.6f, 0.1f, 0.9f), 1, 2.5f, 0.07f, 0.4f, -3f);

        if (activo && antes < intervalo + aviso)
        {
            AudioManager.Play(Sfx.Geiser, 0.8f);
            if (player != null && Mathf.Abs(player.position.x - transform.position.x) < 12f) CameraFollow.Shake(0.06f, 0.2f);
        }

        if (llama != null)
        {
            llama.enabled = activo;
            if (activo)
            {
                float k = Mathf.Clamp01((t - intervalo - aviso) / 0.15f);
                // se escala el pivote para que la llama crezca desde el suelo
                var pivote = llama.transform.parent != transform ? llama.transform.parent : llama.transform;
                pivote.localScale = new Vector3(1f + Mathf.Sin(Time.time * 40f) * 0.08f, k, 1f);
            }
        }

        if (!activo || player == null || playerHealth == null || Time.time < nextHit) return;
        float dx = Mathf.Abs(player.position.x - transform.position.x);
        float dy = player.position.y - transform.position.y;
        if (dx < ancho * 0.6f && dy > -0.5f && dy < altura)
        {
            playerHealth.TakeDamage(dano, Elemento.Fuego);
            nextHit = Time.time + 0.4f;
        }
    }
}
