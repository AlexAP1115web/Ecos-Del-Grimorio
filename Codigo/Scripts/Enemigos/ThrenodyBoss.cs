using System.Collections;
using UnityEngine;

// Threnody, el Guardián del Viento (jefe del Nivel 4).
// Vuela sobre las plataformas, lanza ráfagas que empujan a Lira, embiste en picada
// e invoca Aves de Tormenta. En su segunda fase invoca más aves y embiste más rápido.
public class ThrenodyBoss : BossController
{
    [Header("Threnody")]
    [SerializeField] private float flySpeed = 4f;
    [SerializeField] private float gustForce = 12f;
    [SerializeField] private float diveSpeed = 11f;
    [SerializeField] private int maxMinions = 3;

    private bool diving;
    private float side = 4f;
    private int minionsPerSummon = 1;

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0f;
    }

    protected override void Move()
    {
        if (diving) return;
        HoverAbovePlayer(side, flySpeed);
    }

    protected override void PerformAttack()
    {
        float r = Random.value;
        if (r < 0.4f) Gust();
        else if (r < 0.75f) StartCoroutine(Dive());
        else Summon();
        side = -side; // cambia de lado para el siguiente ataque
    }

    void Gust()
    {
        if (player == null) return;
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        AreaEffect.Spawn(player.position, 1.5f, new Color(0.7f, 1f, 0.8f, 0.6f), 0.5f);

        var controller = player.GetComponent<PlayerController>();
        if (controller != null) controller.Push(new Vector2(dir * gustForce, 4f));

        ShootAtPlayer(9f, 8f, Elemento.Viento);
    }

    IEnumerator Dive()
    {
        if (player == null) yield break;
        diving = true;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f);

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float end = Time.time + 0.7f;
        while (Time.time < end)
        {
            rb.linearVelocity = dir * diveSpeed;
            yield return null;
        }
        diving = false;
    }

    void Summon()
    {
        int alive = FindObjectsByType<FlyingEnemyAI>().Length;
        for (int i = 0; i < minionsPerSummon && alive < maxMinions; i++, alive++)
            SummonMinion(new Vector2(Random.Range(-2f, 2f), 1f));
    }

    protected override void OnPhaseEnter(int phase)
    {
        minionsPerSummon = 2;
        diveSpeed *= 1.3f;
        attackInterval *= 0.75f;
    }
}
