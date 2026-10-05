using UnityEngine;

// enemigo de suelo: patrulla y persigue a Lira
public class EnemyAI : EnemyBase
{
    [Header("Patrulla")]
    [Tooltip("Distancia que camina a cada lado de su posición inicial")]
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [Tooltip("Diferencia máxima de altura para que persiga (evita que persiga a Lira si está en otra plataforma)")]
    [SerializeField] private float maxHeightDifference = 2f;

    [Header("Salto de ataque (Salamandras)")]
    [SerializeField] private bool jumpAttack = false;
    [SerializeField] private float jumpAttackForce = 7f;
    [SerializeField] private float jumpAttackCooldown = 2.5f;

    private Vector2 startPosition;
    private int patrolDirection = 1;
    private float nextJumpTime;

    protected override void Start()
    {
        base.Start();
        startPosition = transform.position;
    }

    protected override void ApplyData()
    {
        base.ApplyData();
        chaseSpeed = data.chaseSpeed;
    }

    protected override void Think()
    {
        bool canSeePlayer = player != null
            && DistanceToPlayer < detectionRange
            && Mathf.Abs(player.position.y - transform.position.y) < maxHeightDifference;

        if (canSeePlayer) Chase();
        else Patrol();
    }

    void Patrol()
    {
        float leftLimit = startPosition.x - patrolDistance;
        float rightLimit = startPosition.x + patrolDistance;

        if (transform.position.x >= rightLimit) patrolDirection = -1;
        else if (transform.position.x <= leftLimit) patrolDirection = 1;

        rb.linearVelocity = new Vector2(patrolDirection * moveSpeed * SpeedMultiplier, rb.linearVelocity.y);
        FaceTowards(transform.position.x + patrolDirection);
    }

    void Chase()
    {
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        FaceTowards(player.position.x);

        if (jumpAttack && Time.time >= nextJumpTime && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
        {
            // salto + embestida en linea recta hacia Lira
            rb.linearVelocity = new Vector2(dir * chaseSpeed * 2f, jumpAttackForce);
            nextJumpTime = Time.time + jumpAttackCooldown;
            return;
        }

        rb.linearVelocity = new Vector2(dir * chaseSpeed * SpeedMultiplier, rb.linearVelocity.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Vector3 origin = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin + Vector3.left * patrolDistance, origin + Vector3.right * patrolDistance);
    }
}
