using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    [System.Serializable]
    public class CardInfo
    {
        public string id;
        public string displayName;
        public string artwork;
    }

    public List<CardData> allCards = new List<CardData>();

    void Awake()
    {
        LoadCardsFromCSV();
    }

    void LoadCardsFromCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("cards"); // cards.csv en Resources
        if (csvFile == null)
        {
            Debug.LogError("No se encontró cards.csv en Resources!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // saltar cabecera
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');

            string id = cols[0].Trim();
            string name = cols[1].Trim();
            string artworkName = cols[2].Trim();

            // crear CardData
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.id = id;
            card.displayName = name;
            card.artwork = Resources.Load<Sprite>("Sprites/" + artworkName);

            allCards.Add(card);
        }

        Debug.Log($"Cargadas {allCards.Count} cartas desde CSV");
    }
}
