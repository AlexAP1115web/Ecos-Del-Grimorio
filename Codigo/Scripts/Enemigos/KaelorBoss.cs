using System.Collections;
using UnityEngine;

// Kaelor, el Guardián de Fuego (jefe del Nivel 2).
// Fase 1: embestida incandescente y oleadas de brasas.
// Fase 2 (menos de la mitad de vida): más rápido y ataca más seguido.
// Sirve de ejemplo para crear IsoldeBoss, ThrenodyBoss y ElenoraBoss heredando de BossController.
public class KaelorBoss : BossController
{
    [Header("Kaelor")]
    [SerializeField] private float chargeSpeed = 9f;
    [SerializeField] private float chargeDuration = 0.8f;
    [SerializeField] private float emberRadius = 3f;
    [SerializeField] private float emberDamage = 15f;
    [SerializeField] private float phase2SpeedMultiplier = 1.5f;

    private bool charging;
    private float speedBonus = 1f;

    protected override void Move()
    {
        if (charging || player == null) return;
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed * speedBonus * SpeedMultiplier, rb.linearVelocity.y);
    }

    protected override void PerformAttack()
    {
        if (Random.value < 0.5f) StartCoroutine(Charge());
        else StartCoroutine(EmberWave());
    }

    IEnumerator Charge()
    {
        if (player == null) yield break;
        charging = true;
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.4f); // aviso antes de embestir

        float end = Time.time + chargeDuration;
        while (Time.time < end)
        {
            rb.linearVelocity = new Vector2(dir * chargeSpeed * speedBonus, rb.linearVelocity.y);
            yield return null;
        }
        charging = false;
    }

    IEnumerator EmberWave()
    {
        int waves = CurrentPhase >= 1 ? 2 : 1;
        for (int i = 0; i < waves; i++)
        {
            AreaEffect.Spawn(transform.position, emberRadius, new Color(1f, 0.4f, 0.05f, 0.7f), 0.5f);
            AreaEffect.Damage(transform.position, emberRadius, emberDamage, Elemento.Fuego, true);
            yield return new WaitForSeconds(0.6f);
        }
    }

    protected override void OnPhaseEnter(int phase)
    {
        if (phase == 1)
        {
            speedBonus = phase2SpeedMultiplier;
            attackInterval *= 0.7f;
            Debug.Log($"{bossName} entra en su segunda fase");
        }
    }
}
