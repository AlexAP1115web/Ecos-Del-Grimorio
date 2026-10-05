using UnityEngine;

// Objeto que se rompe con los hechizos de Lira: vasijas con botín y muros agrietados
// que esconden pasadizos secretos. Al romperse puede soltar ítems, mostrar un mensaje
// y desvanecer la "roca" que tapaba la sala escondida.
[RequireComponent(typeof(Health))]
public class Rompible : MonoBehaviour
{
    [Header("Botín")]
    [SerializeField] private GameObject[] botin = new GameObject[0];
    [Range(0f, 1f)] [SerializeField] private float probabilidadBotin = 0.75f;

    [Header("Secreto")]
    [SerializeField] private string mensaje = "";
    [Tooltip("Sprites que tapan la sala secreta y se desvanecen al romper el muro")]
    [SerializeField] private SpriteRenderer[] revelar = new SpriteRenderer[0];

    [Header("Efecto")]
    [SerializeField] private Color colorParticulas = new Color(0.8f, 0.6f, 0.4f, 1f);
    [Tooltip("Las vasijas también se rompen si Lira las atraviesa con el esquive")]
    [SerializeField] private bool rompeConEsquive;

    private Health health;
    private SpriteRenderer[] renderers;
    private Color[] colores;
    private Vector3 basePos;
    private float sacudida;

    void Awake()
    {
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        colores = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) colores[i] = renderers[i].color;
        basePos = transform.position;
    }

    void Start()
    {
        health.OnDeath.AddListener(Romper);
        health.Damaged += _ => { sacudida = 1f; AudioManager.Play(Sfx.GolpeEnemigo, 0.5f, 0.8f); };
    }

    void Update()
    {
        // Tiembla y se ilumina un instante al recibir un golpe
        if (sacudida <= 0f) return;
        sacudida = Mathf.MoveTowards(sacudida, 0f, Time.deltaTime * 5f);
        transform.position = basePos + (Vector3)(Random.insideUnitCircle * 0.06f * sacudida);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = Color.Lerp(colores[i], Color.white, sacudida * 0.6f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!rompeConEsquive || !other.CompareTag("Player")) return;
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && pc.IsDashing) health.Kill();
    }

    void Romper()
    {
        Vector2 centro = transform.position;
        var col = GetComponent<Collider2D>();
        if (col != null) centro = col.bounds.center;

        Particula.Rafaga(centro, colorParticulas, 20, 5f, 0.18f, 0.7f, 3f);
        Particula.Polvo(centro, new Color(0.8f, 0.75f, 0.7f, 0.6f));
        CameraFollow.Shake(revelar.Length > 0 ? 0.25f : 0.08f, 0.2f);
        AudioManager.Play(Sfx.Romper, revelar.Length > 0 ? 1f : 0.7f, revelar.Length > 0 ? 0.8f : 1.1f);

        if (botin != null && botin.Length > 0 && Random.value <= probabilidadBotin)
        {
            var prefab = botin[Random.Range(0, botin.Length)];
            if (prefab != null) Instantiate(prefab, centro + Vector2.up * 0.3f, Quaternion.identity);
        }

        if (revelar.Length > 0)
        {
            AudioManager.Play(Sfx.ObjetoEspecial, 0.7f);
            foreach (var sr in revelar)
                if (sr != null) sr.gameObject.AddComponent<Desvanecer>();
        }

        if (!string.IsNullOrEmpty(mensaje) && GameManager.Instance != null)
            GameManager.Instance.ShowMessage(mensaje, 3.5f);
    }
}
