using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "FusionDatabase", menuName = "Cards/Fusion Database")]
public class FusionDatabase : ScriptableObject
{
    [System.Serializable]
    public class FusionEntry
    {
        public List<CardData> ingredients;
        public CardData result;
    }

    public List<FusionEntry> recipes = new List<FusionEntry>();

    private Dictionary<string, List<CardData>> _cache;

    // 🔥 NUEVO: Normaliza una combinación para comparar sin importar el orden
    private string KeyFor(IList<CardData> list)
    {
        var ids = list.Select(c => c.id).OrderBy(s => s).ToList();
        return string.Join("|", ids);
    }

    private string KeyFor(FusionEntry entry)
    {
        return KeyFor(entry.ingredients);
    }

    // 🔥 MODIFICADO: Construir caché con TODAS las posibles combinaciones
    private void BuildCache()
    {
        _cache = new Dictionary<string, List<CardData>>();

        foreach (var entry in recipes)
        {
            if (entry.result == null) continue;
            string key = KeyFor(entry);

            if (!_cache.ContainsKey(key))
            {
                _cache[key] = new List<CardData>();
            }

            // Agregar resultado a la lista si no existe
            if (!_cache[key].Contains(entry.result))
            {
                _cache[key].Add(entry.result);
            }
        }

        Debug.Log($"[FusionDatabase] Caché construido: {_cache.Count} combinaciones únicas");

        // Log de combinaciones con múltiples resultados
        foreach (var kvp in _cache.Where(x => x.Value.Count > 1))
        {
            Debug.Log($"[FusionDatabase] '{kvp.Key}' tiene {kvp.Value.Count} resultados: " +
                      string.Join(", ", kvp.Value.Select(c => c.displayName)));
        }
    }

    // 🔥 NUEVO: Retorna TODOS los posibles resultados para una combinación
    public List<CardData> TryFuseMultiple(IList<CardData> selection)
    {
        if (selection == null || selection.Count == 0)
            return null;

        if (_cache == null || _cache.Count == 0)
        {
            BuildCache();
        }

        string key = KeyFor(selection);

        if (_cache.TryGetValue(key, out List<CardData> results))
        {
            Debug.Log($"[FusionDatabase] Fusión encontrada para '{key}': " +
                      $"{results.Count} resultado(s) - {string.Join(", ", results.Select(c => c.displayName))}");
            return results;
        }

        Debug.Log($"[FusionDatabase] No se encontró fusión para '{key}'");
        return null;
    }

    // 🔥 MANTENER: Método legacy para compatibilidad (retorna primer resultado)
    public CardData TryFuse(IList<CardData> selection)
    {
        var results = TryFuseMultiple(selection);
        return results != null && results.Count > 0 ? results[0] : null;
    }
}