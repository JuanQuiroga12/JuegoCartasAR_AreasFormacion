// Assets/Scripts/UI/LobbyUIManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject createRoomPanel;
    public GameObject waitingRoomPanel;

    [Header("Main Menu")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public TMP_InputField playerNameInput;

    [Header("Create Room")]
    public TMP_Dropdown maxPlayersDropdown;
    public Button confirmCreateButton;
    public Button backFromCreateButton;

    [Header("Waiting Room")]
    public TMP_Text roomIdText;
    public TMP_Text playersCountText;
    public Transform playersListContainer;
    public GameObject playerItemPrefab;
    public Button startGameButton;
    public Button leaveRoomButton;

    [Header("Join Room")]
    public GameObject joinRoomPanel;
    public TMP_InputField roomCodeInput;
    public Button confirmJoinButton;
    public Button backFromJoinButton;

    private NetworkManager networkManager;
    private Dictionary<string, GameObject> playerListItems = new Dictionary<string, GameObject>();

    void Start()
    {
        networkManager = NetworkManager.Instance;

        if (networkManager == null)
        {
            Debug.LogError("[LobbyUI] NetworkManager no encontrado");
            return;
        }

        // ⬇️ VALIDACIÓN MEJORADA ⬇️
        if (playerItemPrefab == null)
        {
            Debug.LogWarning("[LobbyUI] PlayerItemPrefab NO asignado en el Inspector. Intentando carga desde Resources...");

            // Intentar cargarlo desde Resources
            playerItemPrefab = Resources.Load<GameObject>("PlayerItemPrefab");

            if (playerItemPrefab == null)
            {
                Debug.LogError("[LobbyUI] ❌ CRÍTICO: No se pudo cargar PlayerItemPrefab desde Resources.");
                Debug.LogError("[LobbyUI] ❌ Verifica que el prefab esté en Assets/Resources/PlayerItemPrefab.prefab");
                Debug.LogError("[LobbyUI] ❌ O ejecuta Tools > Setup Lobby UI para regenerar todo.");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log("[LobbyUI] ✓ PlayerItemPrefab cargado desde Resources como fallback.");
            }
        }
        else
        {
            Debug.Log("[LobbyUI] ✓ PlayerItemPrefab ya está asignado desde el Inspector.");
        }

        SetupUI();
        ShowMainMenu();

        // Suscribirse a eventos
        networkManager.OnRoomUpdated += OnRoomUpdated;
        networkManager.OnGameStarted += OnGameStarted;
    }

    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnRoomUpdated -= OnRoomUpdated;
            networkManager.OnGameStarted -= OnGameStarted;
        }
    }

    void SetupUI()
    {
        // Main Menu
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        // Create Room
        confirmCreateButton.onClick.AddListener(OnConfirmCreateRoom);
        backFromCreateButton.onClick.AddListener(ShowMainMenu);

        // Waiting Room
        startGameButton.onClick.AddListener(OnStartGameClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

        // Join Room
        confirmJoinButton.onClick.AddListener(OnConfirmJoinRoom);
        backFromJoinButton.onClick.AddListener(ShowMainMenu);

        // Configurar dropdown de jugadores
        maxPlayersDropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();
        for (int i = 2; i <= 4; i++)
        {
            options.Add(new TMP_Dropdown.OptionData($"{i} Jugadores"));
        }
        maxPlayersDropdown.AddOptions(options);
    }

    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        createRoomPanel.SetActive(false);
        waitingRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
    }

    void OnCreateRoomClicked()
    {
        mainMenuPanel.SetActive(false);
        createRoomPanel.SetActive(true);
    }

    void OnJoinRoomClicked()
    {
        // 🔥 CAMBIADO: Siempre mostrar el panel de unirse
        Debug.Log("[LobbyUI] Abriendo panel para unirse a sala...");

        mainMenuPanel.SetActive(false);
        joinRoomPanel.SetActive(true);

        // 🔥 NUEVO: Limpiar el campo de código de sala
        if (roomCodeInput != null)
        {
            roomCodeInput.text = "";
            roomCodeInput.placeholder.GetComponent<TMP_Text>().text =
                "Código de sala (déjalo vacío para buscar automáticamente)";
        }
    }

    async void OnConfirmCreateRoom()
    {
        string playerName = string.IsNullOrEmpty(playerNameInput.text) ?
            "Jugador 1" : playerNameInput.text;

        int maxPlayers = maxPlayersDropdown.value + 2; // 2-4 jugadores

        confirmCreateButton.interactable = false;

        // 🔥 NUEVO: Mostrar indicador de carga 🔥
        if (playersCountText != null)
        {
            playersCountText.text = "⏳ Conectando...";
        }

        string roomId = await networkManager.CreateRoom(maxPlayers, playerName);

        if (!string.IsNullOrEmpty(roomId))
        {
            ShowWaitingRoom(roomId);
        }
        else
        {
            confirmCreateButton.interactable = true;

            // 🔥 NUEVO: Mostrar mensaje de error 🔥
            if (playersCountText != null)
            {
                playersCountText.text = "❌ Error al crear sala";
            }

            Debug.LogError("[LobbyUI] Error al crear sala");
        }
    }

    async void OnConfirmJoinRoom()
    {
        // 🔥 CRÍTICO: Obtener nombre del jugador del campo principal
        string playerName = string.IsNullOrEmpty(playerNameInput.text) ?
            "Jugador" : playerNameInput.text;

        string roomCode = roomCodeInput.text.ToUpper().Trim();

        confirmJoinButton.interactable = false;

        // 🔥 NUEVO: Mostrar indicador de carga
        if (roomCodeInput != null)
        {
            roomCodeInput.placeholder.GetComponent<TMP_Text>().text = "⏳ Conectando...";
        }

        string roomId;

        // 🔥 Si hay código, intentar unirse a esa sala específica
        if (!string.IsNullOrEmpty(roomCode))
        {
            Debug.Log($"[LobbyUI] Intentando unirse a sala: {roomCode}");
            roomId = await networkManager.JoinRoom(roomCode, playerName); // 🔥 Pasar el nombre
        }
        else
        {
            // 🔥 Si está vacío, buscar cualquier sala disponible
            Debug.Log($"[LobbyUI] Buscando sala disponible automáticamente...");
            roomId = await networkManager.JoinRoom(null, playerName); // 🔥 Pasar el nombre
        }

        if (!string.IsNullOrEmpty(roomId))
        {
            Debug.Log($"[LobbyUI] ✅ Unido a sala: {roomId} como {playerName}");
            ShowWaitingRoom(roomId);
        }
        else
        {
            // 🔥 Manejar error
            confirmJoinButton.interactable = true;

            if (roomCodeInput != null)
            {
                if (!string.IsNullOrEmpty(roomCode))
                {
                    roomCodeInput.placeholder.GetComponent<TMP_Text>().text =
                        "❌ Sala no encontrada. Intenta con otro código.";
                }
                else
                {
                    roomCodeInput.placeholder.GetComponent<TMP_Text>().text =
                        "❌ No hay salas disponibles. Crea una nueva.";
                }
            }

            Debug.LogError("[LobbyUI] Error al unirse a la sala");
        }
    }

    void ShowWaitingRoom(string roomId)
    {
        mainMenuPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        waitingRoomPanel.SetActive(true);

        roomIdText.text = $"Código de Sala: {roomId}";
        startGameButton.gameObject.SetActive(networkManager.isHost);
    }

    void OnRoomUpdated(RoomData roomData)
    {
        if (roomData == null) return;

        playersCountText.text = $"Jugadores: {roomData.currentPlayers}/{roomData.maxPlayers}";

        // Solo el host puede iniciar si hay al menos 2 jugadores
        if (networkManager.isHost)
        {
            startGameButton.interactable = roomData.currentPlayers >= 2;
        }

        // Actualizar lista de jugadores (necesitaría más implementación para mostrar nombres)
        UpdatePlayersList(roomData);
    }

    async void UpdatePlayersList(RoomData roomData)
    {
        Debug.Log("[LobbyUI] ========================================");
        Debug.Log("[LobbyUI] 🔄 Actualizando lista de jugadores...");
        Debug.Log("[LobbyUI] ========================================");

        // 🔥 LIMPIAR LISTA ACTUAL
        foreach (var item in playerListItems.Values)
        {
            if (item != null)
            {
                Debug.Log($"[LobbyUI] 🗑️ Destruyendo item: {item.name}");
                Destroy(item);
            }
        }
        playerListItems.Clear();

        // 🔥 VALIDAR QUE playersListContainer NO SEA NULL
        if (playersListContainer == null)
        {
            Debug.LogError("[LobbyUI] ❌ playersListContainer es NULL!");
            return;
        }

        Debug.Log($"[LobbyUI] ✓ playersListContainer asignado: {playersListContainer.name}");

        // Cargar prefab si no está asignado
        if (playerItemPrefab == null)
        {
            Debug.LogWarning("[LobbyUI] ⚠️ playerItemPrefab es NULL, cargando desde Resources...");
            playerItemPrefab = Resources.Load<GameObject>("PlayerItemPrefab");

            if (playerItemPrefab == null)
            {
                Debug.LogError("[LobbyUI] ❌ PlayerItemPrefab no encontrado en Resources");
                return;
            }

            Debug.Log("[LobbyUI] ✓ PlayerItemPrefab cargado desde Resources");
        }

        // 🔥 OBTENER JUGADORES REALES DESDE FIREBASE
        if (networkManager == null || networkManager.currentRoomRef == null)
        {
            Debug.LogWarning("[LobbyUI] ⚠️ No se puede cargar jugadores: sala no inicializada");
            return;
        }

        try
        {
            var playersSnapshot = await networkManager.currentRoomRef.Child("players").GetValueAsync();

            if (!playersSnapshot.Exists || !playersSnapshot.HasChildren)
            {
                Debug.LogWarning("[LobbyUI] ⚠️ No hay jugadores en la sala");
                return;
            }

            // Crear lista ordenada por playerNumber
            var playersList = new System.Collections.Generic.List<PlayerData>();

            foreach (var playerSnap in playersSnapshot.Children)
            {
                var playerData = PlayerData.FromSnapshot(playerSnap);
                playersList.Add(playerData);
            }

            // Ordenar por playerNumber
            playersList.Sort((a, b) => a.playerNumber.CompareTo(b.playerNumber));

            Debug.Log($"[LobbyUI] 📋 Total de jugadores a mostrar: {playersList.Count}");

            // 🔥 CRÍTICO: Instanciar solo dentro del contenedor correcto
            foreach (var playerData in playersList)
            {
                Debug.Log($"[LobbyUI] ➕ Instanciando item para: {playerData.playerName}");

                // 🔥 IMPORTANTE: Instanciar con 'worldPositionStays: false'
                var playerItem = Instantiate(playerItemPrefab, playersListContainer, false);

                // 🔥 VERIFICAR QUE SE INSTANCIÓ CORRECTAMENTE
                if (playerItem == null)
                {
                    Debug.LogError($"[LobbyUI] ❌ No se pudo instanciar item para {playerData.playerName}");
                    continue;
                }

                Debug.Log($"[LobbyUI] ✓ Item instanciado: {playerItem.name}");
                Debug.Log($"[LobbyUI] 📍 Padre: {playerItem.transform.parent?.name ?? "NULL"}");

                // 🔥 RESETEAR TRANSFORMACIÓN PARA ASEGURAR POSICIONAMIENTO CORRECTO
                RectTransform itemRect = playerItem.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    itemRect.anchoredPosition = Vector2.zero;
                    itemRect.localScale = Vector3.one;
                    itemRect.localRotation = Quaternion.identity;

                    Debug.Log($"[LobbyUI] ✓ Transform reseteado para {playerData.playerName}");
                }

                // Configurar texto
                var text = playerItem.GetComponentInChildren<TMP_Text>();

                if (text != null)
                {
                    string playerText = playerData.playerName;

                    if (playerData.isHost)
                        playerText += " (Host)";

                    if (playerData.playerId == networkManager.playerId)
                        playerText += " (Tú)";

                    text.text = playerText;

                    Debug.Log($"[LobbyUI] ✓ Texto configurado: {playerText}");
                }
                else
                {
                    Debug.LogWarning($"[LobbyUI] ⚠️ No se encontró TMP_Text en el prefab para {playerData.playerName}");
                }

                playerListItems.Add(playerData.playerId, playerItem);
            }

            Debug.Log("[LobbyUI] ========================================");
            Debug.Log($"[LobbyUI] ✅ Lista de jugadores actualizada: {playerListItems.Count} jugadores");
            Debug.Log("[LobbyUI] ========================================");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LobbyUI] ❌ Error al actualizar lista de jugadores: {e.Message}");
            Debug.LogError($"[LobbyUI] StackTrace: {e.StackTrace}");
        }
    }

    async void OnStartGameClicked()
    {
        if (!networkManager.isHost)
        {
            Debug.LogWarning("[LobbyUI] Solo el host puede iniciar el juego");
            return;
        }

        startGameButton.interactable = false;
        await networkManager.StartGame();
    }

    async void OnLeaveRoomClicked()
    {
        leaveRoomButton.interactable = false;
        await networkManager.LeaveRoom();
        ShowMainMenu();
        leaveRoomButton.interactable = true;
    }

    void OnGameStarted()
    {
        Debug.Log("[LobbyUI] Juego iniciado, cargando escena...");
        // La escena se carga automáticamente desde NetworkManager
    }
}