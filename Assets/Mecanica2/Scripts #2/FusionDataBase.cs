using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "FusionDatabase", menuName = "Cards/Fusion Database")]
public class FusionDatabase : ScriptableObject
{
    [System.Serializable]
    public class FusionEntry
    {
        public List<CardData> ingredients; // 2 o 3 cartas
        public CardData result;
    }

    public List<FusionEntry> recipes = new List<FusionEntry>();

    // Normaliza una combinación para comparar sin importar el orden
    private string KeyFor(IList<CardData> list)
    {
        var ids = list.Select(c => c.id).OrderBy(x => x);
        return string.Join("|", ids);
    }

    private string KeyFor(FusionEntry entry) => KeyFor(entry.ingredients);

    private Dictionary<string, CardData> _cache;
    private void BuildCache()
    {
        _cache = new Dictionary<string, CardData>();
        foreach (var r in recipes)
        {
            if (r == null || r.result == null || r.ingredients == null || r.ingredients.Count == 0) continue;
            var k = KeyFor(r);
            if (!_cache.ContainsKey(k)) _cache.Add(k, r.result);
        }
    }

    public CardData TryFuse(IList<CardData> selection)
    {
        if (_cache == null) BuildCache();
        var key = KeyFor(selection);
        return _cache.TryGetValue(key, out var outCard) ? outCard : null;
    }
}
