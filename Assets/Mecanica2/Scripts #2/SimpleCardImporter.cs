// Assets/Editor/SimpleCardImporter.cs
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SimpleCardImporter : EditorWindow
{
    string cardsCsvPath = "Assets/cards.csv";
    string recipesCsvPath = "Assets/recipes.csv";
    string outputFolder = "Assets/ScriptableObjects/Cards";
    FusionDatabase targetDb;

    [MenuItem("Tools/Simple Card Importer")]
    public static void Open() => GetWindow<SimpleCardImporter>("Simple Card Importer");

    void OnGUI()
    {
        GUILayout.Label("Importador simple de cartas y recetas", EditorStyles.boldLabel);
        cardsCsvPath = EditorGUILayout.TextField("Cards CSV", cardsCsvPath);
        recipesCsvPath = EditorGUILayout.TextField("Recipes CSV", recipesCsvPath);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        targetDb = (FusionDatabase)EditorGUILayout.ObjectField("FusionDatabase", targetDb, typeof(FusionDatabase), false);

        if (GUILayout.Button("Importar"))
        {
            if (!File.Exists(cardsCsvPath)) { Debug.LogError("No existe " + cardsCsvPath); return; }
            if (!File.Exists(recipesCsvPath)) { Debug.LogError("No existe " + recipesCsvPath); return; }
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
            if (targetDb == null) FindOrCreateDb();
            ImportCards(cardsCsvPath);
            ImportRecipes(recipesCsvPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Importación finalizada.");
        }
    }

    void FindOrCreateDb()
    {
        var ids = AssetDatabase.FindAssets("t:FusionDatabase");
        if (ids.Length > 0) targetDb = AssetDatabase.LoadAssetAtPath<FusionDatabase>(AssetDatabase.GUIDToAssetPath(ids[0]));
        else
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Cards"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Cards");
            string path = "Assets/ScriptableObjects/FusionDatabase.asset";
            targetDb = ScriptableObject.CreateInstance<FusionDatabase>();
            AssetDatabase.CreateAsset(targetDb, path);
            Debug.Log("Creado FusionDatabase en " + path);
        }
    }

    void ImportCards(string path)
    {
        var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length <= 1) { Debug.LogWarning("cards.csv vacío o solo encabezado."); return; }

        var headers = lines[0].Split(',').Select(s => s.Trim()).ToArray();
        int idxId = System.Array.IndexOf(headers, "id");
        int idxName = System.Array.IndexOf(headers, "displayName");
        int idxArt = System.Array.IndexOf(headers, "artworkPath");

        for (int i = 1; i < lines.Length; i++)
        {
            var cells = lines[i].Split(',').Select(s => s.Trim()).ToArray();
            if (cells.Length <= idxId) continue;
            string id = cells[idxId];
            if (string.IsNullOrEmpty(id)) continue;
            string displayName = idxName >= 0 && idxName < cells.Length ? cells[idxName] : id;
            string artPath = idxArt >= 0 && idxArt < cells.Length ? cells[idxArt] : "";

            // buscar por id
            CardData cd = FindCardById(id);
            if (cd == null)
            {
                cd = ScriptableObject.CreateInstance<CardData>();
                cd.id = id;
                cd.displayName = displayName;
                string safe = MakeSafe(id);
                string assetPath = $"{outputFolder}/{safe}.asset";
                AssetDatabase.CreateAsset(cd, assetPath);
                Debug.Log("Creado CardData: " + id);
            }
            else
            {
                cd.displayName = displayName;
            }

            if (!string.IsNullOrEmpty(artPath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                if (sprite != null)
                {
                    cd.artwork = sprite;
                }
                else
                {
                    Debug.LogWarning($"No se encontró sprite para '{id}' en ruta: {artPath}");
                }
            }
            EditorUtility.SetDirty(cd);
        }
    }

    void ImportRecipes(string path)
    {
        var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length == 0) { Debug.LogWarning("recipes.csv vacío."); return; }

        targetDb.recipes.Clear();

        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("ingredientIds")) continue; // saltar encabezado
            var parts = line.Split(',').Select(s => s.Trim()).ToArray();
            if (parts.Length < 2) continue;
            var ingIds = parts[0].Split('|').Select(s => s.Trim()).Where(s => s != "").ToList();
            var resultId = parts[1];

            List<CardData> ingredients = new List<CardData>();
            foreach (var iid in ingIds)
            {
                var cd = FindCardById(iid);
                if (cd == null)
                {
                    cd = ScriptableObject.CreateInstance<CardData>();
                    cd.id = iid;
                    cd.displayName = iid;
                    AssetDatabase.CreateAsset(cd, $"{outputFolder}/{MakeSafe(iid)}.asset");
                    Debug.Log("Creado placeholder: " + iid);
                }
                ingredients.Add(cd);
            }

            var res = FindCardById(resultId);
            if (res == null)
            {
                res = ScriptableObject.CreateInstance<CardData>();
                res.id = resultId;
                res.displayName = resultId;
                AssetDatabase.CreateAsset(res, $"{outputFolder}/{MakeSafe(resultId)}.asset");
                Debug.Log("Creado placeholder result: " + resultId);
            }

            var entry = new FusionDatabase.FusionEntry();
            entry.ingredients = ingredients;
            entry.result = res;
            targetDb.recipes.Add(entry);
        }
        EditorUtility.SetDirty(targetDb);
        Debug.Log("Recetas importadas: " + targetDb.recipes.Count);
    }

    static CardData FindCardById(string id)
    {
        var guids = AssetDatabase.FindAssets("t:CardData");
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var cd = AssetDatabase.LoadAssetAtPath<CardData>(p);
            if (cd != null && string.Equals(cd.id, id, System.StringComparison.InvariantCultureIgnoreCase))
                return cd;
        }
        return null;
    }

    static string MakeSafe(string s)
    {
        var clean = new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
        return string.IsNullOrEmpty(clean) ? "card" : clean;
    }
}
