using UnityEngine;

// Enemigo inmóvil que dispara a distancia (Cristales Vivientes: esquirlas de hielo).
public class RangedEnemyAI : EnemyBase
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1.8f;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float projectileDamage = 8f;
    [SerializeField] private Elemento projectileElement = Elemento.Hielo;

    private float nextFireTime;

    protected override void Awake()
    {
        base.Awake();
        rb.bodyType = RigidbodyType2D.Kinematic; // no se desplaza
    }

    protected override void Think()
    {
        if (player == null || DistanceToPlayer > detectionRange) return;

        FaceTowards(player.position.x);

        if (Time.time >= nextFireTime)
        {
            Fire();
            // Si está ralentizado dispara más lento
            nextFireTime = Time.time + fireRate / SpeedMultiplier;
        }
    }

    void Fire()
    {
        if (projectilePrefab == null) return;

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)player.position - origin).normalized;

        var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
        var projectile = go.GetComponent<EnemyProjectile>();
        if (projectile != null) projectile.Init(dir, projectileSpeed, projectileDamage, projectileElement);
    }
}
