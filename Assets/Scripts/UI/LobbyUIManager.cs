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
        if (string.IsNullOrEmpty(roomCodeInput.text))
        {
            // Buscar sala automáticamente
            JoinAnyAvailableRoom();
        }
        else
        {
            mainMenuPanel.SetActive(false);
            joinRoomPanel.SetActive(true);
        }
    }

    async void OnConfirmCreateRoom()
    {
        string playerName = string.IsNullOrEmpty(playerNameInput.text) ?
            "Jugador 1" : playerNameInput.text;

        int maxPlayers = maxPlayersDropdown.value + 2; // 2-4 jugadores

        confirmCreateButton.interactable = false;

        string roomId = await networkManager.CreateRoom(maxPlayers, playerName);

        if (!string.IsNullOrEmpty(roomId))
        {
            ShowWaitingRoom(roomId);
        }
        else
        {
            confirmCreateButton.interactable = true;
            Debug.LogError("[LobbyUI] Error al crear sala");
        }
    }

    async void OnConfirmJoinRoom()
    {
        string roomCode = roomCodeInput.text.ToUpper();

        if (string.IsNullOrEmpty(roomCode))
        {
            Debug.LogWarning("[LobbyUI] Código de sala vacío");
            return;
        }

        confirmJoinButton.interactable = false;

        string roomId = await networkManager.JoinRoom(roomCode);

        if (!string.IsNullOrEmpty(roomId))
        {
            ShowWaitingRoom(roomId);
        }
        else
        {
            confirmJoinButton.interactable = true;
            Debug.LogError("[LobbyUI] Error al unirse a la sala");
        }
    }

    async void JoinAnyAvailableRoom()
    {
        joinRoomButton.interactable = false;

        string roomId = await networkManager.JoinRoom();

        if (!string.IsNullOrEmpty(roomId))
        {
            ShowWaitingRoom(roomId);
        }
        else
        {
            // No hay salas disponibles, crear una nueva
            OnCreateRoomClicked();
        }

        joinRoomButton.interactable = true;
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

    void UpdatePlayersList(RoomData roomData)
    {
        // Limpiar lista actual
        foreach (var item in playerListItems.Values)
        {
            Destroy(item);
        }
        playerListItems.Clear();

        // Esta es una versión simplificada
        // En producción, deberías obtener la lista real de jugadores desde Firebase
        for (int i = 0; i < roomData.currentPlayers; i++)
        {
            var playerItem = Instantiate(playerItemPrefab, playersListContainer);
            var text = playerItem.GetComponentInChildren<TMP_Text>();

            if (text != null)
            {
                string playerText = $"Jugador {i + 1}";
                if (i == 0) playerText += " (Host)";
                if (i == networkManager.playerNumber) playerText += " (Tú)";
                text.text = playerText;
            }

            playerListItems.Add($"player_{i}", playerItem);
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