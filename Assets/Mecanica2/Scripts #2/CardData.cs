using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite artwork;
    public Color baseColor = Color.gray;

    [Header("Extra solo para resultados")]
    public string resultDescription;  //  Texto extra que aparecerá si es resultado
}
