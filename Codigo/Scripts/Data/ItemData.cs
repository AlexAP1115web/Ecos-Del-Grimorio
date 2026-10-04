using UnityEngine;

public enum TipoItem
{
    Mana,
    Vida,
    FragmentoGrimorio,
    NucleoDeAscua,
    AnilloDeEscarcha,
    PlumaLigera,
    LlaveRunica,
    Coleccionable
}

// Datos de un ítem de la tabla 2.13 (Cristal de Maná, Poción de Vida, etc.)
[CreateAssetMenu(fileName = "NuevoItem", menuName = "Ecos del Grimorio/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "Item";
    public TipoItem type = TipoItem.Mana;
    [Tooltip("Cantidad de maná o vida que recupera (si aplica)")]
    public float amount = 25f;
    public Sprite icon;
    [TextArea] public string description;
}
