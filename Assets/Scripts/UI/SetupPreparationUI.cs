#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class SetupPreparationUI : EditorWindow
{
    [MenuItem("Tools/Setup Preparation Phase UI")]
    public static void SetupUI()
    {
        Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("[SetupPreparationUI] No se encontró Canvas en la escena");
            return;
        }

        // Crear Panel de Preparación
        GameObject prepPanel = new GameObject("PreparationPanel");
        prepPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform prepRect = prepPanel.AddComponent<RectTransform>();
        prepRect.anchorMin = Vector2.zero;
        prepRect.anchorMax = Vector2.one;
        prepRect.offsetMin = Vector2.zero;
        prepRect.offsetMax = Vector2.zero;

        Image prepBg = prepPanel.AddComponent<Image>();
        prepBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Contenedor Central
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(prepPanel.transform, false);

        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.2f, 0.2f);
        contentRect.anchorMax = new Vector2(0.8f, 0.8f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // Título
        CreateText("Title", contentGO.transform, "FASE DE PREPARACIÓN",
            new Vector2(0, 0.85f), new Vector2(1, 0.95f), 36, FontStyles.Bold);

        // Timer
        CreateText("PreparationTimerText", contentGO.transform, "Tiempo: 01:00",
            new Vector2(0.3f, 0.75f), new Vector2(0.7f, 0.83f), 28, FontStyles.Normal);

        // Instrucciones
        CreateText("PreparationInstructionText", contentGO.transform,
            "Escanea 3 cartas del mazo físico para comenzar",
            new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.72f), 22, FontStyles.Normal);

        // Contador de cartas
        CreateText("ScannedCardsCountText", contentGO.transform, "Cartas escaneadas: 0/3",
            new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.58f), 24, FontStyles.Bold);

        // Botón Escanear
        GameObject scanBtn = CreateButton("PreparationScanButton", contentGO.transform,
            "ESCANEAR CARTA", new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.45f));
        scanBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);

        // Asignar referencias al GameManager
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            SerializedObject so = new SerializedObject(gm);

            so.FindProperty("preparationPanel").objectReferenceValue = prepPanel;
            so.FindProperty("preparationTimerText").objectReferenceValue =
                contentGO.transform.Find("PreparationTimerText")?.GetComponent<TMP_Text>();
            so.FindProperty("preparationInstructionText").objectReferenceValue =
                contentGO.transform.Find("PreparationInstructionText")?.GetComponent<TMP_Text>();
            so.FindProperty("scannedCardsCountText").objectReferenceValue =
                contentGO.transform.Find("ScannedCardsCountText")?.GetComponent<TMP_Text>();
            so.FindProperty("preparationScanButton").objectReferenceValue =
                scanBtn.GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(mainCanvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupPreparationUI] ✅ UI de preparación creada correctamente");
        EditorUtility.DisplayDialog("Setup Preparation UI", "La UI de fase de preparación ha sido creada.", "OK");
    }

    private static GameObject CreateText(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyles fontStyle)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return textGO;
    }

    private static GameObject CreateButton(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        RectTransform rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.4f, 0.4f, 0.5f, 1f);

        Button btn = btnGO.AddComponent<Button>();

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnGO;
    }
}
#endif