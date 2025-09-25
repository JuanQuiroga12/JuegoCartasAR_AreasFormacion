using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite artwork;
}
