using System.Collections;
using UnityEngine;

// Isolde, la Guardiana de Hielo (jefa del Nivel 3).
// Congela el suelo para limitar el movimiento de Lira e invoca esquirlas de hielo en área.
// En su segunda fase congela más suelo y lanza más esquirlas.
public class IsoldeBoss : BossController
{
    [Header("Isolde")]
    [SerializeField] private float groundY = -4f;
    [SerializeField] private float shardDamage = 12f;
    [SerializeField] private float frozenFloorDuration = 4f;

    private int shardCount = 5;
    private float floorWidth = 4f;

    protected override void Move()
    {
        if (player == null) return;
        // Mantiene distancia: se acerca si Lira está lejos y retrocede si está muy cerca
        float dx = player.position.x - transform.position.x;
        float dir = Mathf.Abs(dx) > 6f ? Mathf.Sign(dx) : (Mathf.Abs(dx) < 3f ? -Mathf.Sign(dx) : 0f);
        float x = Mathf.Clamp(transform.position.x + dir, arenaMinX, arenaMaxX);
        if (Mathf.Approximately(x, transform.position.x)) dir = 0f;
        rb.linearVelocity = new Vector2(dir * moveSpeed * SpeedMultiplier, rb.linearVelocity.y);
    }

    protected override void PerformAttack()
    {
        if (Random.value < 0.5f) FreezeFloor();
        else StartCoroutine(ShardRain());
    }

    void FreezeFloor()
    {
        if (player == null) return;
        SlowZone.Spawn(new Vector2(player.position.x, groundY + 0.1f), floorWidth, frozenFloorDuration);
        AreaEffect.Spawn(new Vector2(player.position.x, groundY + 0.5f), floorWidth / 2f, new Color(0.7f, 0.95f, 1f, 0.5f), 0.6f);
        ShootAtPlayer(8f, shardDamage, Elemento.Hielo);
    }

    IEnumerator ShardRain()
    {
        if (player == null) yield break;
        float centerX = player.position.x;
        for (int i = 0; i < shardCount; i++)
        {
            float x = centerX + Random.Range(-4f, 4f);
            Shoot(new Vector2(x, groundY + 9f), Vector2.down, 9f, shardDamage, Elemento.Hielo);
            yield return new WaitForSeconds(0.15f);
        }
    }

    protected override void OnPhaseEnter(int phase)
    {
        shardCount = 9;
        floorWidth = 7f;
        attackInterval *= 0.75f;
        moveSpeed *= 1.3f;
    }
}
