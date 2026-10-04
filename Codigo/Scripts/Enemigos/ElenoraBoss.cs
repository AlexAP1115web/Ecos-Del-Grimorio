using System.Collections;
using UnityEngine;

// Eco de la Archimaga Elenora (jefe final, Nivel 5).
// Cuatro fases elementales (Fuego, Hielo, Viento, Arcano) y una fase final que combina
// los cuatro tipos de daño. En cada fase resiste el elemento que está usando.
public class ElenoraBoss : BossController
{
    [Header("Elenora")]
    [SerializeField] private float flySpeed = 3.5f;
    [SerializeField] private float groundY = -4f;
    [SerializeField] private float projectileDamage = 12f;

    private readonly Elemento[] phaseElements = { Elemento.Fuego, Elemento.Hielo, Elemento.Viento, Elemento.Arcano };
    private float side = 5f;

    bool IsFinalPhase => CurrentPhase >= phaseElements.Length;
    Elemento CurrentElement => phaseElements[Mathf.Min(CurrentPhase, phaseElements.Length - 1)];

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0f;
    }

    protected override void Start()
    {
        base.Start();
        ApplyPhaseResistance();
    }

    protected override void Move()
    {
        HoverAbovePlayer(side, flySpeed);
    }

    protected override void PerformAttack()
    {
        side = -side;

        if (IsFinalPhase)
        {
            // Fase final: combina dos ataques de elementos distintos
            int a = Random.Range(0, 4);
            int b = (a + Random.Range(1, 4)) % 4;
            Attack(phaseElements[a]);
            StartCoroutine(Delayed(phaseElements[b], 0.6f));
        }
        else
        {
            Attack(CurrentElement);
        }
    }

    IEnumerator Delayed(Elemento element, float delay)
    {
        yield return new WaitForSeconds(delay);
        Attack(element);
    }

    void Attack(Elemento element)
    {
        if (player == null) return;

        switch (element)
        {
            case Elemento.Fuego:
                // Abanico de bolas de fuego
                for (int i = -2; i <= 2; i++) ShootAtPlayer(8f, projectileDamage, Elemento.Fuego, i * 12f);
                break;

            case Elemento.Hielo:
                // Lluvia de esquirlas y suelo congelado
                for (int i = 0; i < 6; i++)
                {
                    float x = player.position.x + Random.Range(-4f, 4f);
                    Shoot(new Vector2(x, groundY + 9f), Vector2.down, 9f, projectileDamage, Elemento.Hielo);
                }
                SlowZone.Spawn(new Vector2(player.position.x, groundY + 0.1f), 5f, 3f);
                break;

            case Elemento.Viento:
                // Ráfaga que empuja a Lira y proyectiles horizontales
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                var controller = player.GetComponent<PlayerController>();
                if (controller != null) controller.Push(new Vector2(dir * 12f, 5f));
                for (int i = 0; i < 3; i++)
                    Shoot((Vector2)transform.position + Vector2.down * i, new Vector2(dir, 0f), 10f, projectileDamage * 0.7f, Elemento.Viento);
                break;

            case Elemento.Arcano:
                // Estallido de proyectiles en círculo
                for (int i = 0; i < 10; i++)
                {
                    float angle = i * 36f * Mathf.Deg2Rad;
                    Shoot(transform.position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), 7f, projectileDamage, Elemento.Arcano);
                }
                break;
        }

        AreaEffect.Spawn(transform.position, 1.8f, ElementoColor.Get(element) * new Color(1f, 1f, 1f, 0.6f), 0.5f);
    }

    void ApplyPhaseResistance()
    {
        foreach (var e in phaseElements) health.SetResistance(e, 1f);
        if (!IsFinalPhase) health.SetResistance(CurrentElement, 0.3f);
    }

    protected override void OnPhaseEnter(int phase)
    {
        ApplyPhaseResistance();
        if (IsFinalPhase)
        {
            attackInterval *= 0.8f;
            flySpeed *= 1.3f;
        }
        Debug.Log($"{bossName}: fase {(IsFinalPhase ? "final" : CurrentElement.ToString())}");
    }
}
