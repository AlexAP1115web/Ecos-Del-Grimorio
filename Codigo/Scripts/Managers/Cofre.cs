using UnityEngine;

// cofre de madera escondido en los niveles. Se abre con E / Triangulo y suelta su contenido
public class Cofre : MonoBehaviour
{
    [SerializeField] private GameObject[] contenido = new GameObject[0];
    [SerializeField] private float openRange = 1.8f;
    [SerializeField] private SpriteRenderer tapa;

    private Transform player;
    private bool abierto;
    private bool promptShown;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (abierto || player == null) return;

        bool near = Vector2.Distance(player.position, transform.position) <= openRange;
        var ui = UIManager.Instance;
        if (ui != null && near != promptShown)
        {
            if (near) ui.ShowPrompt($"{Controles.TextoInteractuar}  Abrir cofre");
            else ui.HidePrompt();
            promptShown = near;
        }
        if (near && Controles.InteractuarPresionado && Time.timeScale > 0f) Abrir();
    }

    void Abrir()
    {
        abierto = true;
        if (UIManager.Instance != null) UIManager.Instance.HidePrompt();
        AudioManager.Play(Sfx.Cofre);
        Particula.Rafaga(transform.position + Vector3.up * 0.4f, new Color(1f, 0.85f, 0.4f, 1f), 18, 4f, 0.15f, 0.6f, 2f);

        if (tapa != null)
        {
            tapa.transform.localPosition += new Vector3(-0.25f, 0.35f, 0f);
            tapa.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        }

        for (int i = 0; i < contenido.Length; i++)
        {
            if (contenido[i] == null) continue;
            float x = (i - (contenido.Length - 1) / 2f) * 0.9f;
            Instantiate(contenido[i], transform.position + new Vector3(x, 1.1f, 0f), Quaternion.identity);
        }
    }
}
