using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string id;
    public string displayName;

    // 🔥 NUEVO: Nombre de la imagen en AR Foundation
    [Tooltip("Nombre exacto de la imagen en XR Reference Image Library")]
    public string arImageName; // ⬅️ Agregar esto

    public Sprite artwork;
}
