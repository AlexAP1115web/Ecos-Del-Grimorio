using System.Collections;
using UnityEngine;

public enum TipoGolpe { Zarpazo, Estocada, Embestida }

// arma de un enemigo: cuando Lira esta cerca, el enemigo
// la usa. El arte del arma aparece frente a el y daña lo que este en esa zona
// garras de Tinta Corrosiva -> Espectros de tinta (zarpazo)
// lanza Incandescente       -> Centinelas de Ceniza (estocada)
// embestida Rocosa          -> Golems de Piedra Suspendida (embestida)
[RequireComponent(typeof(EnemyBase))]
public class ArmaEnemigo : MonoBehaviour
{
    [SerializeField] private Sprite arte;
    [SerializeField] private TipoGolpe tipo = TipoGolpe.Zarpazo;
    [SerializeField] private float alcance = 1.8f;
    [SerializeField] private float dano = 12f;
    [SerializeField] private float recarga = 2f;
    [SerializeField] private float tamano = 1.2f;
    [Tooltip("Giro del dibujo para que apunte hacia adelante (la lanza viene en diagonal)")]
    [SerializeField] private float rotacionArte = 0f;
    [SerializeField] private float aviso = 0.35f;
    [SerializeField] private bool conElemento;
    [SerializeField] private Elemento elemento = Elemento.Fuego;

    private EnemyBase enemigo;
    private Health vida;
    private Transform player;
    private float siguiente;
    private bool atacando;

    void Start()
    {
        enemigo = GetComponent<EnemyBase>();
        vida = GetComponent<Health>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        siguiente = Time.time + Random.Range(0.5f, recarga);
    }

    void Update()
    {
        if (player == null || atacando || Time.time < siguiente || vida.IsDead || enemigo.Aturdido) return;
        float dx = player.position.x - transform.position.x;
        float dy = player.position.y - transform.position.y;
        if (Mathf.Abs(dx) < alcance + 0.4f && Mathf.Abs(dy) < 1.6f) StartCoroutine(Atacar(Mathf.Sign(dx)));
    }

    IEnumerator Atacar(float dir)
    {
        atacando = true;

        // aviso: un destello del color del arma
        Particula.Rafaga((Vector2)transform.position + new Vector2(dir * 0.5f, 0.3f), new Color(1f, 0.9f, 0.7f, 0.8f), 5, 1.5f, 0.08f, aviso);
        yield return new WaitForSeconds(aviso);
        if (vida == null || vida.IsDead) yield break;

        var go = new GameObject("Arma");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = arte;
        sr.sortingOrder = 12;
        sr.flipX = dir < 0f;
        float escala = arte != null ? tamano / Mathf.Max(arte.bounds.size.x, arte.bounds.size.y) : 1f;
        float giro = dir < 0f ? -rotacionArte : rotacionArte;
        AudioManager.Play(Sfx.Esquive, 0.7f, tipo == TipoGolpe.Embestida ? 0.6f : 0.85f);

        bool golpeo = false;
        const float dur = 0.3f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            Vector2 offset;
            float s = escala;
            switch (tipo)
            {
                case TipoGolpe.Estocada:
                    offset = new Vector2(dir * Mathf.Lerp(0.3f, alcance * 0.7f, Mathf.Sin(k * Mathf.PI)), 0.1f);
                    break;
                case TipoGolpe.Embestida:
                    offset = new Vector2(dir * alcance * 0.5f, 0f);
                    s = escala * Mathf.Lerp(0.6f, 1.15f, k);
                    giro += dir * -360f * Time.deltaTime;
                    break;
                default:
                    offset = new Vector2(dir * alcance * 0.55f, 0.2f - k * 0.4f);
                    s = escala * Mathf.Lerp(0.7f, 1.1f, k);
                    break;
            }
            if (go == null) yield break;
            go.transform.position = (Vector2)transform.position + offset;
            go.transform.localScale = new Vector3(s, s, 1f) * 1f / Mathf.Max(transform.lossyScale.x, 0.01f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, giro);
            sr.color = new Color(1f, 1f, 1f, k < 0.7f ? 1f : (1f - k) / 0.3f);

            if (!golpeo && k > 0.3f) golpeo = Golpear(dir);
            yield return null;
        }
        if (go != null) Destroy(go);

        siguiente = Time.time + recarga;
        atacando = false;
    }

    bool Golpear(float dir)
    {
        Vector2 centro = (Vector2)transform.position + new Vector2(dir * alcance * 0.55f, 0f);
        foreach (var col in Physics2D.OverlapBoxAll(centro, new Vector2(alcance, 1.6f), 0f))
        {
            if (!col.CompareTag("Player")) continue;
            var h = col.GetComponentInParent<Health>();
            if (h == null || h.IsInvulnerable) return true;
            if (conElemento) h.TakeDamage(dano, elemento); else h.TakeDamage(dano);
            var pc = col.GetComponentInParent<PlayerController>();
            if (pc != null) pc.Push(new Vector2(dir * 9f, 5f));
            return true;
        }
        return false;
    }
}
