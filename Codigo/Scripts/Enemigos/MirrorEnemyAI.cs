using System.Collections;
using UnityEngine;

// Guardián Espejo (Nivel 5): imita el último hechizo que lanzó Lira y se lo regresa.
public class MirrorEnemyAI : EnemyBase
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float reflectDelay = 0.5f;
    [SerializeField] private float projectileSpeed = 9f;
    [SerializeField] private float projectileDamage = 12f;
    [SerializeField] private float keepDistance = 5f;

    private SpellCaster playerCaster;

    protected override void Start()
    {
        base.Start();
        if (player != null)
        {
            playerCaster = player.GetComponent<SpellCaster>();
            if (playerCaster != null) playerCaster.SpellCast += OnPlayerCast;
        }
    }

    void OnDestroy()
    {
        if (playerCaster != null) playerCaster.SpellCast -= OnPlayerCast;
    }

    void OnPlayerCast(Elemento element)
    {
        if (this == null || IsStunned || DistanceToPlayer > detectionRange) return;
        StartCoroutine(Reflect(element));
    }

    IEnumerator Reflect(Elemento element)
    {
        yield return new WaitForSeconds(reflectDelay);
        if (player == null || projectilePrefab == null) yield break;

        AreaEffect.Spawn(transform.position, 0.8f, ElementoColor.Get(element), 0.3f);
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var p = go.GetComponent<EnemyProjectile>();
        if (p != null) p.Init(dir, projectileSpeed, projectileDamage, element);
    }

    protected override void Think()
    {
        if (player == null || DistanceToPlayer > detectionRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Se mantiene a cierta distancia de Lira, como un reflejo
        float dx = player.position.x - transform.position.x;
        float dir = Mathf.Abs(dx) > keepDistance + 1f ? Mathf.Sign(dx) : (Mathf.Abs(dx) < keepDistance - 1f ? -Mathf.Sign(dx) : 0f);
        rb.linearVelocity = new Vector2(dir * moveSpeed * SpeedMultiplier, rb.linearVelocity.y);
        FaceTowards(player.position.x);
    }
}
