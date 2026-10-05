using System.Collections;
using UnityEngine;

// Coloso de Raíz: lento y muy resistente. Camina hacia Lira y cada cierto tiempo
// golpea el suelo: la onda de choque daña a quien esté parado cerca, así que
// se esquiva saltando o con el esquive. El fuego le hace más daño.
public class ColosoAI : EnemyBase
{
    [Header("Pisotón")]
    [SerializeField] private float stompInterval = 3.2f;
    [SerializeField] private float stompRange = 3.5f;
    [SerializeField] private float stompRadius = 3.2f;
    [SerializeField] private float stompDamage = 18f;
    [SerializeField] private float aviso = 0.7f;

    private float nextStomp;
    private bool stomping;

    protected override void Think()
    {
        if (player == null || stomping) return;

        float dist = DistanceToPlayer;
        if (dist > detectionRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        FaceTowards(player.position.x);
        if (dist < stompRange && Time.time >= nextStomp)
        {
            StartCoroutine(Stomp());
            return;
        }
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed * SpeedMultiplier, rb.linearVelocity.y);
    }

    IEnumerator Stomp()
    {
        stomping = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        float feet = spriteRenderer != null ? spriteRenderer.bounds.min.y : transform.position.y;
        Vector2 suelo = new Vector2(transform.position.x, feet);

        // Aviso: tiembla y salen hojas del suelo
        for (float t = 0; t < aviso; t += 0.1f)
        {
            Particula.Rafaga(suelo + Vector2.up * 0.1f, new Color(0.5f, 0.8f, 0.3f, 0.8f), 3, 2f, 0.1f, 0.4f, 2f);
            yield return new WaitForSeconds(0.1f);
        }

        if (!health.IsDead && !IsStunned)
        {
            AreaEffect.Spawn(suelo, stompRadius, new Color(0.55f, 0.4f, 0.2f, 0.6f), 0.4f);
            Particula.Polvo(suelo, new Color(0.7f, 0.6f, 0.45f, 0.8f));
            CameraFollow.Shake(0.25f, 0.3f);
            Controles.Vibrar(0.6f, 0.3f, 0.2f);
            AudioManager.Play(Sfx.Romper, 1f, 0.6f);

            // Solo daña si Lira está cerca y en el suelo (saltar la esquiva)
            if (player != null)
            {
                float dx = Mathf.Abs(player.position.x - transform.position.x);
                var ph = player.GetComponent<Health>();
                var pc = player.GetComponent<PlayerController>();
                bool grounded = pc == null || pc.IsGrounded;
                if (dx < stompRadius && grounded && ph != null && !ph.IsInvulnerable)
                {
                    ph.TakeDamage(stompDamage);
                    if (pc != null) pc.Push(new Vector2(Mathf.Sign(player.position.x - transform.position.x) * 10f, 7f));
                }
            }
        }

        nextStomp = Time.time + stompInterval;
        yield return new WaitForSeconds(0.4f);
        stomping = false;
    }
}
