using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// "Sentido arcano": el grimorio de Lira vibra cuando hay algo escondido cerca.
// Ayuda a encontrar las Páginas Perdidas, los Fragmentos de Grimorio y los muros
// agrietados que tapan salas secretas:
//  - Salen chispas azules del secreto (se ven aunque esté detrás de la pared).
//  - Suena un tintineo que se repite más rápido mientras más cerca está.
//  - La primera vez que Lira se acerca a cada secreto aparece un aviso.
// Se crea sola al iniciar el juego, igual que CapturaPantalla.
public class SentidoArcano : MonoBehaviour
{
    const float Radio = 7f;          // distancia a la que el grimorio empieza a vibrar
    const float RadioAviso = 5f;     // distancia para mostrar el mensaje

    static SentidoArcano instancia;

    enum Tipo { Pagina, Fragmento, Muro }

    class Secreto
    {
        public Transform t;
        public Collider2D col;
        public Tipo tipo;
        public Health vida;
    }

    private readonly List<Secreto> secretos = new List<Secreto>();
    private readonly HashSet<Transform> avisados = new HashSet<Transform>();
    private Transform lira;
    private float siguienteBusqueda, siguienteBrillo, siguienteSonido;

    static readonly Color Azul = new Color(0.55f, 0.85f, 1f, 0.9f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("[SentidoArcano]");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<SentidoArcano>();
    }

    void OnEnable() => SceneManager.sceneLoaded += AlCargar;
    void OnDisable() => SceneManager.sceneLoaded -= AlCargar;

    void AlCargar(Scene escena, LoadSceneMode modo)
    {
        avisados.Clear();
        siguienteBusqueda = 0f;
    }

    // Vuelve a buscar cada cierto tiempo porque los objetos se recogen o se rompen
    void Buscar()
    {
        secretos.Clear();
        var p = GameObject.FindGameObjectWithTag("Player");
        lira = p != null ? p.transform : null;
        if (lira == null) return;

        foreach (var pickup in FindObjectsByType<Pickup>())
        {
            if (pickup.Item == null) continue;
            var tipo = pickup.Item.type;
            if (tipo == TipoItem.PaginaPerdida)
                secretos.Add(new Secreto { t = pickup.transform, col = pickup.GetComponent<Collider2D>(), tipo = Tipo.Pagina });
            else if (tipo == TipoItem.FragmentoGrimorio)
                secretos.Add(new Secreto { t = pickup.transform, col = pickup.GetComponent<Collider2D>(), tipo = Tipo.Fragmento });
        }

        foreach (var muro in FindObjectsByType<Rompible>())
            if (muro.EsSecreto)
                secretos.Add(new Secreto { t = muro.transform, col = muro.GetComponent<Collider2D>(), tipo = Tipo.Muro, vida = muro.GetComponent<Health>() });
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (Time.time >= siguienteBusqueda)
        {
            siguienteBusqueda = Time.time + 1.5f;
            Buscar();
        }
        if (lira == null) return;

        // El secreto más cercano que siga ahí
        Secreto cercano = null;
        float distancia = Radio;
        foreach (var s in secretos)
        {
            if (s.t == null || (s.vida != null && s.vida.IsDead)) continue;
            float d = Vector2.Distance(lira.position, Centro(s));
            if (d < distancia) { distancia = d; cercano = s; }
        }
        if (cercano == null) return;

        float k = 1f - distancia / Radio;   // 0 = lejos, 1 = encima
        Vector2 centro = Centro(cercano);

        // Chispas en el secreto y en el libro de Lira (orden alto para verse sobre la pared que lo tapa)
        if (Time.time >= siguienteBrillo)
        {
            siguienteBrillo = Time.time + Mathf.Lerp(0.45f, 0.12f, k);
            Particula.Rafaga(centro + Random.insideUnitCircle * 0.5f, Azul, 2, 0.7f, 0.09f, 0.7f, -0.6f, 45);
            Particula.Rafaga((Vector2)lira.position + new Vector2(0f, 0.3f), Azul, 1, 0.8f, 0.07f, 0.4f, -1f, 45);
        }

        // Tintineo: más seguido y más agudo mientras más cerca
        if (Time.time >= siguienteSonido)
        {
            siguienteSonido = Time.time + Mathf.Lerp(3.2f, 1.1f, k);
            AudioManager.Play(Sfx.Secreto, 0.25f + 0.45f * k, 1f + 0.25f * k);
        }

        // Aviso de texto una sola vez por secreto
        if (distancia <= RadioAviso && avisados.Add(cercano.t) && GameManager.Instance != null)
            GameManager.Instance.ShowMessage(Mensaje(cercano.tipo), 3f);
    }

    static Vector2 Centro(Secreto s) => s.col != null ? (Vector2)s.col.bounds.center : (Vector2)s.t.position;

    static string Mensaje(Tipo tipo)
    {
        switch (tipo)
        {
            case Tipo.Muro: return "El grimorio vibra... este muro agrietado esconde algo. Prueba con un hechizo.";
            case Tipo.Fragmento: return "El grimorio vibra... un Fragmento de Grimorio está cerca.";
            default: return "El grimorio vibra... hay una Página Perdida cerca.";
        }
    }
}
