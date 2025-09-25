// Assets/Editor/CsvCardImporter.cs
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CsvCardImporter : EditorWindow
{
    string cardsCsv = "Assets/Resources/cards.csv";
    string recipesCsv = "Assets/Resources/recipes.csv";
    string outputFolder = "Assets/ScriptableObjects/Cards";
    FusionDatabase targetDb;

    [MenuItem("Tools/CSV Card Importer")]
    public static void Open() => GetWindow<CsvCardImporter>("CSV Card Importer");

    void OnGUI()
    {
        GUILayout.Label("Importer CSV -> CardData / FusionDatabase", EditorStyles.boldLabel);
        cardsCsv = EditorGUILayout.TextField("Cards CSV", cardsCsv);
        recipesCsv = EditorGUILayout.TextField("Recipes CSV", recipesCsv);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        targetDb = (FusionDatabase)EditorGUILayout.ObjectField("FusionDatabase", targetDb, typeof(FusionDatabase), false);

        if (GUILayout.Button("Import"))
        {
            if (!File.Exists(cardsCsv)) { Debug.LogError("Cards CSV not found: " + cardsCsv); return; }
            if (!File.Exists(recipesCsv)) { Debug.LogError("Recipes CSV not found: " + recipesCsv); return; }
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
            if (targetDb == null) FindOrCreateDb();
            ImportCards(cardsCsv);
            ImportRecipes(recipesCsv);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Import completed.");
        }
    }

    void FindOrCreateDb()
    {
        var guids = AssetDatabase.FindAssets("t:FusionDatabase");
        if (guids.Length > 0) targetDb = AssetDatabase.LoadAssetAtPath<FusionDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        else
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Cards"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Cards");
            string path = "Assets/ScriptableObjects/FusionDatabase.asset";
            targetDb = ScriptableObject.CreateInstance<FusionDatabase>();
            AssetDatabase.CreateAsset(targetDb, path);
            Debug.Log("Created FusionDatabase at " + path);
        }
    }

    void ImportCards(string path)
    {
        var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length <= 1) { Debug.LogWarning("cards CSV empty or only header."); return; }

        // header expected: id,displayName,artwork
        for (int i = 1; i < lines.Length; i++)
        {
            var cells = SplitCsvLine(lines[i]);
            if (cells.Length < 1) continue;
            string id = cells.Length > 0 ? cells[0].Trim() : "";
            if (string.IsNullOrEmpty(id)) continue;
            string displayName = cells.Length > 1 ? cells[1].Trim() : id;
            string artworkPath = cells.Length > 2 ? cells[2].Trim() : "";

            // find existing by id
            CardData cd = FindCardById(id);
            if (cd == null)
            {
                cd = ScriptableObject.CreateInstance<CardData>();
                cd.id = id;
                cd.displayName = displayName;
                string safe = MakeSafe(id);
                string assetPath = $"{outputFolder}/{safe}.asset";
                AssetDatabase.CreateAsset(cd, assetPath);
                Debug.Log("Created CardData: " + id);
            }
            else
            {
                cd.displayName = displayName;
            }

            if (!string.IsNullOrEmpty(artworkPath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artworkPath);
                if (sprite != null)
                {
                    cd.artwork = sprite;
                }
                else
                {
                    Debug.LogWarning($"Sprite not found at {artworkPath} for card {id}");
                }
            }

            EditorUtility.SetDirty(cd);
        }
    }

    void ImportRecipes(string path)
    {
        var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length == 0) { Debug.LogWarning("recipes CSV empty."); return; }

        targetDb.recipes.Clear();

        for (int i = 0; i < lines.Length; i++)
        {
            if (i == 0 && lines[i].Trim().StartsWith("ingredientIds")) continue; // skip header
            var cells = SplitCsvLine(lines[i]);
            if (cells.Length < 2) continue;
            string left = cells[0].Trim();
            string right = cells[1].Trim();
            var ingredientIds = left.Split('|').Select(s => s.Trim()).Where(s => s != "").ToList();
            var resultId = right;

            List<CardData> ingredients = new List<CardData>();
            foreach (var iid in ingredientIds)
            {
                var cd = FindCardById(iid);
                if (cd == null)
                {
                    cd = ScriptableObject.CreateInstance<CardData>();
                    cd.id = iid;
                    cd.displayName = iid;
                    AssetDatabase.CreateAsset(cd, $"{outputFolder}/{MakeSafe(iid)}.asset");
                    Debug.Log("Created placeholder CardData for ingredient: " + iid);
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
                Debug.Log("Created placeholder CardData for result: " + resultId);
            }

            var entry = new FusionDatabase.FusionEntry();
            entry.ingredients = ingredients;
            entry.result = res;
            targetDb.recipes.Add(entry);
        }

        EditorUtility.SetDirty(targetDb);
    }

    // simple CSV splitter (handles commas inside quotes minimally)
    static string[] SplitCsvLine(string line)
    {
        var list = new List<string>();
        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ',' && !inQuotes) { list.Add(cur.ToString()); cur.Clear(); continue; }
            cur.Append(c);
        }
        list.Add(cur.ToString());
        return list.ToArray();
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
