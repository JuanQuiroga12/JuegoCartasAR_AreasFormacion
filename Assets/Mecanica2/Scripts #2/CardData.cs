using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string id;          // ID único (ej: "FUEGO", "AGUA01")
    public string displayName; // Nombre visible
    public Sprite artwork;     // Imagen de la carta
    [Range(1, 5)] public int rarity = 1;
    [Header("Prototype")]
    public Color baseColor = Color.gray;  // <- color de la carta para el prototipo
}
