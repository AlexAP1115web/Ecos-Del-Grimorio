using UnityEngine;

// Ecos Menores (Nivel 5): copias débiles que mezclan patrones de enemigos anteriores.
// Hereda la patrulla y persecución de EnemyAI y además dispara de vez en cuando
// un proyectil de un elemento al azar, como los Cristales Vivientes.
public class EcoMenorAI : EnemyAI
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 3f;
    [SerializeField] private float projectileDamage = 8f;

    private float nextFireTime;

    protected override void Think()
    {
        base.Think();

        if (player == null || projectilePrefab == null || DistanceToPlayer > detectionRange) return;
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireRate + Random.Range(0f, 1f);
        var element = (Elemento)Random.Range(0, 4);
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var p = go.GetComponent<EnemyProjectile>();
        if (p != null) p.Init(dir, 7f, projectileDamage, element);
    }
}
