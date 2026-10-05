using UnityEngine;

// particulas simples con sprites (chispas, polvo, fantasmas del esquive)
public class Particula : MonoBehaviour
{
    Vector2 velocity;
    float gravity;
    float life;
    float age;
    float startScale;
    SpriteRenderer sr;
    Color color;
    bool shrink;

    public static void Rafaga(Vector2 pos, Color color, int cantidad, float velocidad, float tamano, float vida, float gravedad = 0f, int orden = 20)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(0.3f, 1f) * velocidad;
            Crear(pos, dir, color, tamano * Random.Range(0.6f, 1.2f), vida * Random.Range(0.7f, 1.2f), gravedad, orden, true);
        }
    }

    public static void Polvo(Vector2 pos, Color color)
    {
        for (int i = 0; i < 8; i++)
        {
            float x = Random.Range(-1f, 1f);
            Crear(pos + new Vector2(x * 0.3f, 0f), new Vector2(x * 2.5f, Random.Range(0.3f, 1.2f)), color, Random.Range(0.12f, 0.25f), 0.45f, 0f, 12, true);
        }
    }

    public static Particula Crear(Vector2 pos, Vector2 vel, Color color, float tamano, float vida, float gravedad, int orden, bool encoger)
    {
        var go = new GameObject("Particula");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * tamano;
        var p = go.AddComponent<Particula>();
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sprite = AreaEffect.GetCircleSprite();
        p.sr.sortingOrder = orden;
        p.sr.color = color;
        p.color = color;
        p.velocity = vel;
        p.life = vida;
        p.gravity = gravedad;
        p.startScale = tamano;
        p.shrink = encoger;
        return p;
    }

    // copia del sprite que se desvanece (esquive de Lira)
    // silueta de un sprite completo
    public static void Fantasma(Sprite sprite, Vector3 posicion, Vector3 escala, bool voltear, int orden, Color tinte, float vida)
    {
        if (sprite == null) return;
        var go = new GameObject("Fantasma");
        go.transform.position = posicion;
        go.transform.localScale = new Vector3(Mathf.Abs(escala.x), Mathf.Abs(escala.y), 1f);
        var p = go.AddComponent<Particula>();
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sprite = sprite;
        p.sr.flipX = voltear;
        p.sr.sortingOrder = orden;
        p.sr.color = tinte;
        p.color = tinte;
        p.life = vida;
        p.startScale = go.transform.localScale.x;
        p.shrink = false;
    }

    public static void Fantasma(SpriteRenderer origen, Color tinte, float vida)
    {
        if (origen == null || origen.sprite == null) return;
        var go = new GameObject("Fantasma");
        go.transform.position = origen.transform.position;
        go.transform.rotation = origen.transform.rotation;
        go.transform.localScale = origen.transform.lossyScale;
        var p = go.AddComponent<Particula>();
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sprite = origen.sprite;
        p.sr.flipX = origen.flipX;
        p.sr.sortingOrder = origen.sortingOrder - 1;
        p.sr.color = tinte;
        p.color = tinte;
        p.life = vida;
        p.startScale = go.transform.localScale.x;
        p.shrink = false;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / life;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        var c = color;
        c.a = color.a * (1f - t);
        sr.color = c;
        if (shrink) transform.localScale = Vector3.one * startScale * (1f - t * 0.7f);
    }
}
