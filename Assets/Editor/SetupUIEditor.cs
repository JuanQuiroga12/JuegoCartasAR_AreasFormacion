#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class SetupUIEditor : EditorWindow
{
    [MenuItem("Tools/Setup Game UI")]
    public static void ShowWindow()
    {
        SetupGameUI();
    }

    [MenuItem("Tools/Setup Game UI (Force Recreate)")]
    public static void ShowWindowForceRecreate()
    {
        SetupGameUI(true);
    }

    private static void SetupGameUI(bool forceRecreate = false)
    {
        // Buscar o crear Canvas principal
        Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("[SetupUI] Canvas principal creado");
        }

        // === Panel Superior (Info del Juego) ===
        GameObject topPanel = FindOrCreatePanel("TopPanel", mainCanvas.transform, forceRecreate);
        RectTransform topRect = topPanel.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 0.9f);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.offsetMin = Vector2.zero;
        topRect.offsetMax = Vector2.zero;

        // Timer
        GameObject timerGO = FindOrCreateText("TimerText", topPanel.transform, "00:00", forceRecreate);
        RectTransform timerRect = timerGO.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0, 0);
        timerRect.anchorMax = new Vector2(0.33f, 1);
        timerRect.offsetMin = new Vector2(20, 10);
        timerRect.offsetMax = new Vector2(-10, -10);

        // Texto de Ronda
        GameObject roundGO = FindOrCreateText("RoundText", topPanel.transform, "Ronda 1", forceRecreate);
        RectTransform roundRect = roundGO.GetComponent<RectTransform>();
        roundRect.anchorMin = new Vector2(0.33f, 0);
        roundRect.anchorMax = new Vector2(0.66f, 1);
        roundRect.offsetMin = new Vector2(10, 10);
        roundRect.offsetMax = new Vector2(-10, -10);

        // Texto de Jugador Actual
        GameObject playerGO = FindOrCreateText("CurrentPlayerText", topPanel.transform, "Turno: Jugador 1", forceRecreate);
        RectTransform playerRect = playerGO.GetComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.66f, 0);
        playerRect.anchorMax = new Vector2(1, 1);
        playerRect.offsetMin = new Vector2(10, 10);
        playerRect.offsetMax = new Vector2(-20, -10);

        // === Panel de Botones de Acción ===
        GameObject actionPanel = FindOrCreatePanel("ActionPanel", mainCanvas.transform, forceRecreate);
        RectTransform actionRect = actionPanel.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.7f, 0.4f);
        actionRect.anchorMax = new Vector2(0.95f, 0.6f);
        actionRect.offsetMin = Vector2.zero;
        actionRect.offsetMax = Vector2.zero;

        // Layout vertical para botones
        VerticalLayoutGroup vlg = actionPanel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = actionPanel.AddComponent<VerticalLayoutGroup>();
        }
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = true;
        vlg.childForceExpandWidth = true;

        // Botón Escanear
        GameObject scanBtn = FindOrCreateButton("ScanButton", actionPanel.transform, "ESCANEAR CARTA", forceRecreate);
        scanBtn.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);

        // Botón Descartar
        GameObject discardBtn = FindOrCreateButton("DiscardButton", actionPanel.transform, "DESCARTAR", forceRecreate);
        discardBtn.GetComponent<Image>().color = new Color(1f, 0.6f, 0.2f);

        // Botón Finalizar Turno
        GameObject endTurnBtn = FindOrCreateButton("EndTurnButton", actionPanel.transform, "FINALIZAR TURNO", forceRecreate);
        endTurnBtn.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f);

        // === Panel de Escaneo AR ===
        GameObject scanPanel = FindOrCreatePanel("ScanPanel", mainCanvas.transform, forceRecreate);
        RectTransform scanRect = scanPanel.GetComponent<RectTransform>();
        scanRect.anchorMin = Vector2.zero;
        scanRect.anchorMax = Vector2.one;
        scanRect.offsetMin = Vector2.zero;
        scanRect.offsetMax = Vector2.zero;
        scanPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        scanPanel.SetActive(false);

        // Contenedor central para elementos de escaneo
        GameObject scanContent = FindOrCreatePanel("ScanContent", scanPanel.transform, forceRecreate);
        RectTransform scanContentRect = scanContent.GetComponent<RectTransform>();
        scanContentRect.anchorMin = new Vector2(0.2f, 0.3f);
        scanContentRect.anchorMax = new Vector2(0.8f, 0.7f);
        scanContentRect.offsetMin = Vector2.zero;
        scanContentRect.offsetMax = Vector2.zero;
        scanContent.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Texto de instrucciones
        GameObject instructionText = FindOrCreateText("ScanInstructionText", scanContent.transform,
            "Apunta la cámara hacia una carta para escanearla", forceRecreate);
        TMP_Text tmpInstruct = instructionText.GetComponent<TMP_Text>();
        if (tmpInstruct != null)
        {
            tmpInstruct.fontSize = 24;
        }
        RectTransform instructRect = instructionText.GetComponent<RectTransform>();
        instructRect.anchorMin = new Vector2(0, 0.6f);
        instructRect.anchorMax = new Vector2(1, 0.9f);
        instructRect.offsetMin = new Vector2(20, 0);
        instructRect.offsetMax = new Vector2(-20, 0);

        // Barra de progreso
        GameObject progressBar = FindOrCreateProgressBar("ScanProgressBar", scanContent.transform, forceRecreate);
        RectTransform progressRect = progressBar.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.1f, 0.4f);
        progressRect.anchorMax = new Vector2(0.9f, 0.5f);
        progressRect.offsetMin = Vector2.zero;
        progressRect.offsetMax = Vector2.zero;

        // Indicador de éxito
        GameObject successIndicator = FindOrCreateText("ScanSuccessIndicator", scanContent.transform, "✓", forceRecreate);
        TMP_Text tmpSuccess = successIndicator.GetComponent<TMP_Text>();
        if (tmpSuccess != null)
        {
            tmpSuccess.fontSize = 72;
            tmpSuccess.color = Color.green;
        }
        RectTransform successRect = successIndicator.GetComponent<RectTransform>();
        successRect.anchorMin = new Vector2(0.4f, 0.2f);
        successRect.anchorMax = new Vector2(0.6f, 0.4f);
        successRect.offsetMin = Vector2.zero;
        successRect.offsetMax = Vector2.zero;
        successIndicator.SetActive(false);

        // Botón cancelar
        GameObject cancelBtn = FindOrCreateButton("CancelScanButton", scanContent.transform, "CANCELAR", forceRecreate);
        RectTransform cancelRect = cancelBtn.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.3f, 0.05f);
        cancelRect.anchorMax = new Vector2(0.7f, 0.15f);
        cancelRect.offsetMin = Vector2.zero;
        cancelRect.offsetMax = Vector2.zero;
        cancelBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f);

        // Marcar como modificado para guardar
        EditorUtility.SetDirty(mainCanvas.gameObject);

        Debug.Log("[SetupUI] ✓ UI del juego configurada correctamente");
        EditorUtility.DisplayDialog("Setup UI", "La UI del juego ha sido configurada correctamente.", "OK");
    }

    private static GameObject FindOrCreatePanel(string name, Transform parent, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupUI] Panel '{name}' ya existe, reutilizando");
            return existing.gameObject;
        }

        if (existing != null && forceRecreate)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        Debug.Log($"[SetupUI] Panel '{name}' creado");
        return panel;
    }

    private static GameObject FindOrCreateText(string name, Transform parent, string text, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupUI] Texto '{name}' ya existe, reutilizando");
            TMP_Text tmpComponent = existing.GetComponent<TMP_Text>();
            if (tmpComponent != null)
            {
                tmpComponent.text = text;
            }
            return existing.gameObject;
        }

        if (existing != null && forceRecreate)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 18;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        Debug.Log($"[SetupUI] Texto '{name}' creado");
        return textGO;
    }

    private static GameObject FindOrCreateButton(string name, Transform parent, string text, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupUI] Botón '{name}' ya existe, reutilizando");
            Transform textChild = existing.Find(name + "_Text");
            if (textChild != null)
            {
                TMP_Text tmpComponent = textChild.GetComponent<TMP_Text>();
                if (tmpComponent != null)
                {
                    tmpComponent.text = text;
                }
            }
            return existing.gameObject;
        }

        if (existing != null && forceRecreate)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        Image img = buttonGO.AddComponent<Image>();
        Button btn = buttonGO.AddComponent<Button>();

        // Texto del botón
        GameObject textGO = new GameObject(name + "_Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 18;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        Debug.Log($"[SetupUI] Botón '{name}' creado");
        return buttonGO;
    }

    private static GameObject FindOrCreateProgressBar(string name, Transform parent, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupUI] Barra de progreso '{name}' ya existe, reutilizando");
            return existing.gameObject;
        }

        if (existing != null && forceRecreate)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject barGO = new GameObject(name);
        barGO.transform.SetParent(parent, false);

        RectTransform rect = barGO.AddComponent<RectTransform>();

        // Fondo
        Image bgImg = barGO.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Fill
        GameObject fillGO = new GameObject(name + "_Fill");
        fillGO.transform.SetParent(barGO.transform, false);

        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = Color.green;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0;

        Debug.Log($"[SetupUI] Barra de progreso '{name}' creada");
        return barGO;
    }
}
#endif