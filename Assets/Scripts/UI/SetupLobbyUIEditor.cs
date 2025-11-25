#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class SetupLobbyUIEditor : EditorWindow
{
    [MenuItem("Tools/Setup Lobby UI")]
    public static void SetupLobbyUI()
    {
        CreateLobbyUI(false);
    }

    [MenuItem("Tools/Setup Lobby UI (Force Recreate)")]
    public static void SetupLobbyUIForceRecreate()
    {
        CreateLobbyUI(true);
    }

    private static void CreateLobbyUI(bool forceRecreate = false)
    {
        // Buscar o crear Canvas principal
        Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("LobbyCanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("[SetupLobbyUI] Canvas principal creado");
        }

        // Crear EventSystem si no existe
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[SetupLobbyUI] EventSystem creado");
        }

        // === MAIN MENU PANEL ===
        GameObject mainMenuPanel = FindOrCreatePanel("MainMenuPanel", mainCanvas.transform, forceRecreate);
        SetupFullScreenPanel(mainMenuPanel, new Color(0.15f, 0.15f, 0.2f, 1f));

        // Title
        GameObject mainTitle = FindOrCreateText("Title", mainMenuPanel.transform, "LOBBY MULTIJUGADOR", forceRecreate);
        SetupTitleText(mainTitle, new Vector2(0, 0.7f), new Vector2(1, 0.9f));

        // Player Name Input
        GameObject playerNameInput = FindOrCreateInputField("PlayerNameInput", mainMenuPanel.transform, "Ingresa tu nombre...", forceRecreate);
        SetupCenteredElement(playerNameInput, new Vector2(0.3f, 0.55f), new Vector2(0.7f, 0.62f));

        // Create Room Button
        GameObject createRoomBtn = FindOrCreateButton("CreateRoomButton", mainMenuPanel.transform, "CREAR SALA", forceRecreate);
        SetupCenteredElement(createRoomBtn, new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.48f));
        createRoomBtn.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f, 1f);

        // Join Room Button
        GameObject joinRoomBtn = FindOrCreateButton("JoinRoomButton", mainMenuPanel.transform, "BUSCAR SALA", forceRecreate);
        SetupCenteredElement(joinRoomBtn, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.38f));
        joinRoomBtn.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.8f, 1f);

        // === CREATE ROOM PANEL ===
        GameObject createRoomPanel = FindOrCreatePanel("CreateRoomPanel", mainCanvas.transform, forceRecreate);
        SetupFullScreenPanel(createRoomPanel, new Color(0.15f, 0.2f, 0.15f, 1f));
        createRoomPanel.SetActive(false);

        // Title
        GameObject createTitle = FindOrCreateText("Title", createRoomPanel.transform, "CREAR SALA", forceRecreate);
        SetupTitleText(createTitle, new Vector2(0, 0.7f), new Vector2(1, 0.9f));

        // Max Players Dropdown
        GameObject maxPlayersDropdown = FindOrCreateDropdown("MaxPlayersDropdown", createRoomPanel.transform, forceRecreate);
        SetupCenteredElement(maxPlayersDropdown, new Vector2(0.3f, 0.5f), new Vector2(0.7f, 0.57f));

        // Confirm Button   
        GameObject confirmCreateBtn = FindOrCreateButton("ConfirmCreateButton", createRoomPanel.transform, "CONFIRMAR", forceRecreate);
        SetupCenteredElement(confirmCreateBtn, new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.43f));
        confirmCreateBtn.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f, 1f);

        // Back Button
        GameObject backFromCreateBtn = FindOrCreateButton("BackFromCreateButton", createRoomPanel.transform, "VOLVER", forceRecreate);
        SetupCenteredElement(backFromCreateBtn, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.28f));
        backFromCreateBtn.GetComponent<Image>().color = new Color(0.6f, 0.3f, 0.3f, 1f);

        // === JOIN ROOM PANEL ===
        GameObject joinRoomPanel = FindOrCreatePanel("JoinRoomPanel", mainCanvas.transform, forceRecreate);
        SetupFullScreenPanel(joinRoomPanel, new Color(0.15f, 0.15f, 0.25f, 1f));
        joinRoomPanel.SetActive(false);

        // Title
        GameObject joinTitle = FindOrCreateText("Title", joinRoomPanel.transform, "UNIRSE A SALA", forceRecreate);
        SetupTitleText(joinTitle, new Vector2(0, 0.7f), new Vector2(1, 0.9f));

        // Room Code Input
        GameObject roomCodeInput = FindOrCreateInputField("RoomCodeInput", joinRoomPanel.transform, 
    "Código de sala (déjalo vacío para buscar automáticamente)", forceRecreate);
        SetupCenteredElement(roomCodeInput, new Vector2(0.3f, 0.5f), new Vector2(0.7f, 0.57f));

        // Configurar input para mayúsculas
        TMP_InputField tmpInput = roomCodeInput.GetComponent<TMP_InputField>();
        if (tmpInput != null)
        {
            tmpInput.characterLimit = 6;
            tmpInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            tmpInput.contentType = TMP_InputField.ContentType.Alphanumeric;
        }

        // Confirm Join Button
        GameObject confirmJoinBtn = FindOrCreateButton("ConfirmJoinButton", joinRoomPanel.transform, "UNIRSE", forceRecreate);
        SetupCenteredElement(confirmJoinBtn, new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.43f));
        confirmJoinBtn.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.8f, 1f);

        // Back Button
        GameObject backFromJoinBtn = FindOrCreateButton("BackFromJoinButton", joinRoomPanel.transform, "VOLVER", forceRecreate);
        SetupCenteredElement(backFromJoinBtn, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.28f));
        backFromJoinBtn.GetComponent<Image>().color = new Color(0.6f, 0.3f, 0.3f, 1f);

        // === WAITING ROOM PANEL ===
        GameObject waitingRoomPanel = FindOrCreatePanel("WaitingRoomPanel", mainCanvas.transform, forceRecreate);
        SetupFullScreenPanel(waitingRoomPanel, new Color(0.2f, 0.15f, 0.2f, 1f));
        waitingRoomPanel.SetActive(false);

        // Room ID Text
        GameObject roomIdText = FindOrCreateText("RoomIdText", waitingRoomPanel.transform, "Código de Sala: ------", forceRecreate);
        SetupCenteredElement(roomIdText, new Vector2(0.2f, 0.8f), new Vector2(0.8f, 0.87f));
        TMP_Text roomIdTMP = roomIdText.GetComponent<TMP_Text>();
        if (roomIdTMP != null)
        {
            roomIdTMP.fontSize = 32;
            roomIdTMP.fontStyle = FontStyles.Bold;
            roomIdTMP.color = Color.yellow;
        }

        // Players Count Text
        GameObject playersCountText = FindOrCreateText("PlayersCountText", waitingRoomPanel.transform, "Jugadores: 0/4", forceRecreate);
        SetupCenteredElement(playersCountText, new Vector2(0.2f, 0.7f), new Vector2(0.8f, 0.77f));
        TMP_Text countTMP = playersCountText.GetComponent<TMP_Text>();
        if (countTMP != null)
        {
            countTMP.fontSize = 24;
        }

        // Players List Container
        GameObject playersListContainer = FindOrCreateScrollView("PlayersListContainer", waitingRoomPanel.transform, forceRecreate);
        SetupCenteredElement(playersListContainer, new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.65f));

        // Player Item Prefab (crearlo como prefab separado)
        CreatePlayerItemPrefab();

        // Start Game Button
        GameObject startGameBtn = FindOrCreateButton("StartGameButton", waitingRoomPanel.transform, "INICIAR JUEGO", forceRecreate);
        SetupCenteredElement(startGameBtn, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.28f));
        startGameBtn.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f, 1f);

        // Leave Room Button
        GameObject leaveRoomBtn = FindOrCreateButton("LeaveRoomButton", waitingRoomPanel.transform, "SALIR", forceRecreate);
        SetupCenteredElement(leaveRoomBtn, new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.16f));
        leaveRoomBtn.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.3f, 1f);

        // Añadir LobbyUIManager al Canvas si no existe
        LobbyUIManager lobbyManager = mainCanvas.GetComponent<LobbyUIManager>();
        if (lobbyManager == null)
        {
            lobbyManager = mainCanvas.gameObject.AddComponent<LobbyUIManager>();
            Debug.Log("[SetupLobbyUI] LobbyUIManager añadido al Canvas");
        }

        // Asignar referencias automáticamente
        AssignReferences(lobbyManager, mainCanvas.transform);

        // ⬇️⬇️⬇️ CAMBIOS AQUÍ ⬇️⬇️⬇️

        // Marcar como modificado
        EditorUtility.SetDirty(mainCanvas.gameObject);

        // 🔥 FORZAR GUARDADO DE LA ESCENA 🔥
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        // Refrescar base de datos de assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupLobbyUI] ✓ UI del Lobby configurada y GUARDADA correctamente");

        EditorUtility.DisplayDialog("Setup Lobby UI",
            "La UI del Lobby ha sido configurada y guardada correctamente.\n\n" +
            "✓ Referencias asignadas\n" +
            "✓ Escena guardada\n" +
            "✓ Prefab en Resources\n\n" +
            "Ahora puedes entrar en Play Mode.",
            "OK");
    }

    private static void AssignReferences(LobbyUIManager manager, Transform canvasTransform)
    {
        SerializedObject so = new SerializedObject(manager);

        // Panels
        so.FindProperty("mainMenuPanel").objectReferenceValue = canvasTransform.Find("MainMenuPanel")?.gameObject;
        so.FindProperty("createRoomPanel").objectReferenceValue = canvasTransform.Find("CreateRoomPanel")?.gameObject;
        so.FindProperty("waitingRoomPanel").objectReferenceValue = canvasTransform.Find("WaitingRoomPanel")?.gameObject;
        so.FindProperty("joinRoomPanel").objectReferenceValue = canvasTransform.Find("JoinRoomPanel")?.gameObject;

        // Main Menu
        so.FindProperty("createRoomButton").objectReferenceValue = canvasTransform.Find("MainMenuPanel/CreateRoomButton")?.GetComponent<Button>();
        so.FindProperty("joinRoomButton").objectReferenceValue = canvasTransform.Find("MainMenuPanel/JoinRoomButton")?.GetComponent<Button>();
        so.FindProperty("playerNameInput").objectReferenceValue = canvasTransform.Find("MainMenuPanel/PlayerNameInput")?.GetComponent<TMP_InputField>();

        // Create Room
        so.FindProperty("maxPlayersDropdown").objectReferenceValue = canvasTransform.Find("CreateRoomPanel/MaxPlayersDropdown")?.GetComponent<TMP_Dropdown>();
        so.FindProperty("confirmCreateButton").objectReferenceValue = canvasTransform.Find("CreateRoomPanel/ConfirmCreateButton")?.GetComponent<Button>();
        so.FindProperty("backFromCreateButton").objectReferenceValue = canvasTransform.Find("CreateRoomPanel/BackFromCreateButton")?.GetComponent<Button>();

        // Waiting Room
        so.FindProperty("roomIdText").objectReferenceValue = canvasTransform.Find("WaitingRoomPanel/RoomIdText")?.GetComponent<TMP_Text>();
        so.FindProperty("playersCountText").objectReferenceValue = canvasTransform.Find("WaitingRoomPanel/PlayersCountText")?.GetComponent<TMP_Text>();

        Transform scrollView = canvasTransform.Find("WaitingRoomPanel/PlayersListContainer/Viewport/Content");
        so.FindProperty("playersListContainer").objectReferenceValue = scrollView;

        // 🔥 INTENTAR PRIMERO DESDE RESOURCES, LUEGO DESDE PREFABS 🔥
        GameObject prefab = Resources.Load<GameObject>("PlayerItemPrefab");

        if (prefab == null)
        {
            // Fallback: buscar en Assets/Prefabs
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerItemPrefab.prefab");

            if (prefab != null)
            {
                Debug.LogWarning("[SetupLobbyUI] Prefab encontrado en Assets/Prefabs. Considera moverlo a Assets/Resources.");
            }
        }

        if (prefab != null)
        {
            so.FindProperty("playerItemPrefab").objectReferenceValue = prefab;
            Debug.Log($"[SetupLobbyUI] ✓ PlayerItemPrefab asignado correctamente: {AssetDatabase.GetAssetPath(prefab)}");
        }
        else
        {
            Debug.LogError("[SetupLobbyUI] ❌ No se pudo encontrar PlayerItemPrefab en ninguna ubicación");
        }

        so.FindProperty("startGameButton").objectReferenceValue = canvasTransform.Find("WaitingRoomPanel/StartGameButton")?.GetComponent<Button>();
        so.FindProperty("leaveRoomButton").objectReferenceValue = canvasTransform.Find("WaitingRoomPanel/LeaveRoomButton")?.GetComponent<Button>();

        // Join Room
        so.FindProperty("roomCodeInput").objectReferenceValue = canvasTransform.Find("JoinRoomPanel/RoomCodeInput")?.GetComponent<TMP_InputField>();
        so.FindProperty("confirmJoinButton").objectReferenceValue = canvasTransform.Find("JoinRoomPanel/ConfirmJoinButton")?.GetComponent<Button>();
        so.FindProperty("backFromJoinButton").objectReferenceValue = canvasTransform.Find("JoinRoomPanel/BackFromJoinButton")?.GetComponent<Button>();

        so.ApplyModifiedProperties();
        Debug.Log("[SetupLobbyUI] Referencias asignadas automáticamente");
    }

    private static void CreatePlayerItemPrefab()
    {
        string prefabPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string fullPath = prefabPath + "/PlayerItemPrefab.prefab";

        // 🔥 ELIMINAR PREFAB EXISTENTE SI EXISTE PARA RECREARLO CORRECTAMENTE
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        if (existingPrefab != null)
        {
            Debug.Log("[SetupLobbyUI] Eliminando prefab anterior para recrearlo...");
            AssetDatabase.DeleteAsset(fullPath);
        }

        // Crear prefab
        GameObject playerItem = new GameObject("PlayerItemPrefab");

        RectTransform rect = playerItem.AddComponent<RectTransform>();

        // 🔥 CRÍTICO: Configuración correcta para funcionar dentro de Vertical Layout Group
        rect.anchorMin = new Vector2(0, 1); // Anclado arriba-izquierda
        rect.anchorMax = new Vector2(1, 1); // Expandir horizontalmente
        rect.pivot = new Vector2(0.5f, 1); // Pivot arriba-centro

        // 🔥 NUEVO: Configurar tamaño fijo
        rect.sizeDelta = new Vector2(0, 60); // Ancho automático (0), alto 60
        rect.anchoredPosition = Vector2.zero; // 🔥 MUY IMPORTANTE: Posición relativa al padre

        Image bgImage = playerItem.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.35f, 0.8f);

        // 🔥 CRÍTICO: Layout Element para controlar tamaño dentro del Layout Group
        LayoutElement layoutElement = playerItem.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 60;
        layoutElement.minHeight = 60;
        layoutElement.flexibleWidth = 1; // Expandir para ocupar ancho disponible

        // Texto del jugador
        GameObject textGO = new GameObject("PlayerText");
        textGO.transform.SetParent(playerItem.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Jugador 1";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;

        // Guardar como prefab
        PrefabUtility.SaveAsPrefabAsset(playerItem, fullPath);
        Debug.Log($"[SetupLobbyUI] ✅ PlayerItemPrefab creado en: {fullPath}");

        Object.DestroyImmediate(playerItem);

        // 🔥 COPIAR A RESOURCES
        string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string resourcesFullPath = resourcesPath + "/PlayerItemPrefab.prefab";

        // Eliminar copia anterior en Resources si existe
        if (AssetDatabase.LoadAssetAtPath<GameObject>(resourcesFullPath) != null)
        {
            AssetDatabase.DeleteAsset(resourcesFullPath);
        }

        AssetDatabase.CopyAsset(fullPath, resourcesFullPath);
        Debug.Log($"[SetupLobbyUI] ✓ Prefab copiado a Resources: {resourcesFullPath}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject FindOrCreatePanel(string name, Transform parent, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupLobbyUI] Panel '{name}' ya existe, reutilizando");
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
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        Debug.Log($"[SetupLobbyUI] Panel '{name}' creado");
        return panel;
    }

    private static GameObject FindOrCreateText(string name, Transform parent, string text, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupLobbyUI] Texto '{name}' ya existe, reutilizando");
            return existing.gameObject;
        }

        if (existing != null && forceRecreate)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        Debug.Log($"[SetupLobbyUI] Texto '{name}' creado");
        return textGO;
    }

    private static GameObject FindOrCreateButton(string name, Transform parent, string text, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupLobbyUI] Botón '{name}' ya existe, reutilizando");
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
        img.color = new Color(0.4f, 0.4f, 0.5f, 1f);

        Button btn = buttonGO.AddComponent<Button>();

        // Colores del botón
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.4f, 0.4f, 0.5f, 1f);
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.6f, 1f);
        colors.pressedColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        colors.selectedColor = new Color(0.45f, 0.45f, 0.55f, 1f);
        btn.colors = colors;

        // Texto del botón
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

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

        Debug.Log($"[SetupLobbyUI] Botón '{name}' creado");
        return buttonGO;
    }

    private static GameObject FindOrCreateInputField(string name, Transform parent, string placeholder, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupLobbyUI] InputField '{name}' ya existe, reutilizando");
            return existing.gameObject;
        }

        if (existing != null && forceRecreate)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject inputFieldGO = new GameObject(name);
        inputFieldGO.transform.SetParent(parent, false);

        RectTransform rect = inputFieldGO.AddComponent<RectTransform>();

        Image bgImage = inputFieldGO.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        TMP_InputField inputField = inputFieldGO.AddComponent<TMP_InputField>();

        // Text Area
        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputFieldGO.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 6);
        textAreaRect.offsetMax = new Vector2(-10, -6);

        RectMask2D mask = textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(textArea.transform, false);
        RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 20;
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

        // Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(textArea.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        inputField.textViewport = textAreaRect;
        inputField.textComponent = tmp;
        inputField.placeholder = placeholderText;

        Debug.Log($"[SetupLobbyUI] InputField '{name}' creado");
        return inputFieldGO;
    }

    private static GameObject FindOrCreateDropdown(string name, Transform parent, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupLobbyUI] Dropdown '{name}' ya existe, reutilizando");
            return existing.gameObject;
        }

        if (existing != null && forceRecreate)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject dropdownGO = new GameObject(name);
        dropdownGO.transform.SetParent(parent, false);

        RectTransform rect = dropdownGO.AddComponent<RectTransform>();

        Image bgImage = dropdownGO.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        TMP_Dropdown dropdown = dropdownGO.AddComponent<TMP_Dropdown>();

        // Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(dropdownGO.transform, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 6);
        labelRect.offsetMax = new Vector2(-25, -6);

        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = "2 Jugadores";
        labelText.fontSize = 20;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;

        // Arrow
        GameObject arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(dropdownGO.transform, false);
        RectTransform arrowRect = arrowGO.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);

        Image arrowImage = arrowGO.AddComponent<Image>();
        arrowImage.color = Color.white;

        // Template
        GameObject templateGO = new GameObject("Template");
        templateGO.transform.SetParent(dropdownGO.transform, false);
        RectTransform templateRect = templateGO.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 150);

        Image templateImage = templateGO.AddComponent<Image>();
        templateImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        ScrollRect scrollRect = templateGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 10;

        // Viewport
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(templateGO.transform, false);
        RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;

        viewportGO.AddComponent<RectMask2D>();

        // Content
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 28);

        // Item
        GameObject itemGO = new GameObject("Item");
        itemGO.transform.SetParent(contentGO.transform, false);
        RectTransform itemRect = itemGO.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 20);

        Toggle itemToggle = itemGO.AddComponent<Toggle>();

        GameObject itemBgGO = new GameObject("Item Background");
        itemBgGO.transform.SetParent(itemGO.transform, false);
        RectTransform itemBgRect = itemBgGO.AddComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.sizeDelta = Vector2.zero;

        Image itemBgImage = itemBgGO.AddComponent<Image>();
        itemBgImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);

        GameObject itemLabelGO = new GameObject("Item Label");
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        RectTransform itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(10, 1);
        itemLabelRect.offsetMax = new Vector2(-10, -1);

        TextMeshProUGUI itemLabelText = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemLabelText.fontSize = 18;
        itemLabelText.color = Color.white;
        itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = itemBgImage;
        itemToggle.isOn = true;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;

        dropdown.targetGraphic = bgImage;
        dropdown.template = templateRect;
        dropdown.captionText = labelText;
        dropdown.itemText = itemLabelText;

        templateGO.SetActive(false);

        Debug.Log($"[SetupLobbyUI] Dropdown '{name}' creado");
        return dropdownGO;
    }

    private static GameObject FindOrCreateScrollView(string name, Transform parent, bool forceRecreate)
    {
        Transform existing = parent.Find(name);
        if (existing != null && !forceRecreate)
        {
            Debug.Log($"[SetupLobbyUI] ScrollView '{name}' ya existe, reutilizando");
            return existing.gameObject;
        }

        if (existing != null && forceRecreate)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject scrollViewGO = new GameObject(name);
        scrollViewGO.transform.SetParent(parent, false);

        RectTransform rect = scrollViewGO.AddComponent<RectTransform>();

        Image bgImage = scrollViewGO.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

        ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Viewport
        GameObject viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollViewGO.transform, false);
        RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;

        viewportGO.AddComponent<RectMask2D>();

        // Content
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;

        Debug.Log($"[SetupLobbyUI] ScrollView '{name}' creado");
        return scrollViewGO;
    }

    private static void SetupFullScreenPanel(GameObject panel, Color color)
    {
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = panel.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
        }
    }

    private static void SetupCenteredElement(GameObject element, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = element.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetupTitleText(GameObject titleGO, Vector2 anchorMin, Vector2 anchorMax)
    {
        SetupCenteredElement(titleGO, anchorMin, anchorMax);

        TMP_Text tmp = titleGO.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.fontSize = 48;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
#endif
