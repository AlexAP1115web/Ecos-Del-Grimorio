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
    Coleccionable,
    PaginaPerdida   // 3 escondidas en cada ala: cada una da +5 de vida máxima
}

// datos de un item de la tabla 2.13
[CreateAssetMenu(fileName = "NuevoItem", menuName = "Ecos del Grimorio/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "Item";
    public TipoItem type = TipoItem.Mana;
    [Tooltip("Cantidad de maná o vida que recupera (si aplica)")]
    public float amount = 25f;
    public Sprite icon;
    [TextArea] public string description;
    [Tooltip("Texto de historia que se muestra al recogerlo (fragmentos, diario, nota)")]
    [TextArea(2, 5)] public string[] lore = new string[0];
}
