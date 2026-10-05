using UnityEngine;

// datos de cada hechizo para moverlos desde el inspector
[CreateAssetMenu(fileName = "NuevoHechizo", menuName = "Ecos del Grimorio/Hechizo")]
public class SpellData : ScriptableObject
{
    public string spellName = "Hechizo";
    public Elemento element = Elemento.Arcano;
    public Sprite icon;

    [Header("Costo y daño")]
    public float manaCost = 10f;
    public float damage = 15f;
    public float cooldown = 0.3f;

    [Header("Proyectil")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
    public float lifetime = 2.5f;
    public Color color = Color.white;

    [Header("Efectos extra")]
    [Tooltip("1 = no ralentiza. 0.5 = el enemigo se mueve a la mitad de velocidad")]
    [Range(0f, 1f)] public float slowFactor = 1f;
    public float slowDuration = 0f;
    [Tooltip("Fuerza de empuje (hechizo de Viento)")]
    public float knockback = 0f;
}
