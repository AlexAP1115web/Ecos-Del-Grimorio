using UnityEngine;

// stats reutilizables por tipo de enemigo. Varios prefabs pueden compartir el mismo asset
[CreateAssetMenu(fileName = "NuevoEnemigo", menuName = "Ecos del Grimorio/Enemigo")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Enemigo";

    [Header("Vida")]
    public float maxHealth = 30f;
    [Tooltip("Si es mayor a 0, el enemigo muere tras ese número de impactos sin importar el daño")]
    public int hitsToKill = 0;

    [Header("Combate")]
    public float contactDamage = 10f;
    public bool hasImmunity = false;
    public Elemento immuneTo = Elemento.Hielo;

    [Header("Movimiento")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float detectionRange = 5f;

    [Header("Botín")]
    public GameObject dropPrefab;
    [Range(0f, 1f)] public float dropChance = 0f;
}
