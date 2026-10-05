using UnityEngine;

// Gargola de Runa: estatua hasta que Lira se acerca, luego dispara runas
public class GargolaAI : EnemyBase
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float wakeRange = 6f;
    [SerializeField] private float flySpeed = 3f;
    [SerializeField] private float hoverHeight = 3.2f;
    [SerializeField] private float fireInterval = 2.4f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private Color colorPiedra = new Color(0.55f, 0.55f, 0.6f, 1f);

    private bool awake;
    private float nextFire;
    private float seed;

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0f;
    }

    protected override void Start()
    {
        base.Start();
        seed = Random.Range(0f, 10f);
        SetBaseColor(colorPiedra);
        health.Damaged += _ => { if (!awake) Wake(); };
    }

    protected override void Think()
    {
        if (!awake)
        {
            rb.linearVelocity = Vector2.zero;
            if (player != null && DistanceToPlayer < wakeRange) Wake();
            return;
        }
        if (player == null) return;

        Vector2 target = (Vector2)player.position + new Vector2(Mathf.Sin(Time.time * 0.8f + seed) * 3f, hoverHeight);
        Vector2 dir = target - (Vector2)transform.position;
        rb.linearVelocity = Vector2.ClampMagnitude(dir * 2f, flySpeed * SpeedMultiplier);
        FaceTowards(player.position.x);

        if (Time.time >= nextFire && projectilePrefab != null)
        {
            nextFire = Time.time + fireInterval;
            Vector2 aim = ((Vector2)player.position - (Vector2)transform.position).normalized;
            for (int i = -1; i <= 1; i++)
            {
                var go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                var p = go.GetComponent<EnemyProjectile>();
                if (p != null) p.Init(Quaternion.Euler(0f, 0f, i * 14f) * aim, projectileSpeed, projectileDamage, Elemento.Arcano);
            }
            AudioManager.Play(Sfx.Arcano, 0.5f, 0.7f);
        }
    }

    void Wake()
    {
        awake = true;
        SetBaseColor(Color.white);
        nextFire = Time.time + 1.2f;
        Particula.Rafaga(transform.position, new Color(0.5f, 0.8f, 1f, 1f), 20, 4f, 0.15f, 0.6f);
        Particula.Rafaga(transform.position, new Color(0.6f, 0.6f, 0.6f, 0.9f), 12, 3f, 0.12f, 0.6f, 4f);
        AudioManager.Play(Sfx.Romper, 0.7f, 1.3f);
        CameraFollow.Shake(0.1f, 0.15f);
    }
}
