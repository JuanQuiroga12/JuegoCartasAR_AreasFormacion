// Assets/Scripts/Network/NetworkManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Firebase")]
    private FirebaseAuth auth;
    private DatabaseReference databaseRef;
    private FirebaseDatabase database;

    [Header("Room Settings")]
    public string currentRoomId;
    public bool isHost = false;
    public string playerId;
    public int playerNumber = -1;

    // 🔥 NUEVO: Campo para almacenar el nombre del jugador
    private string _playerName; // ⬅️ AGREGADO

    public event Action<RoomData> OnRoomUpdated;
    public event Action<string> OnPlayerJoined;
    public event Action<string> OnPlayerLeft;
    public event Action OnGameStarted;
    public event Action<GameStateData> OnGameStateUpdated;
    public event Action OnEndTurnReceived;

    [Header("References")]
    private DatabaseReference roomsRef;
    public DatabaseReference currentRoomRef;
    private List<System.Object> roomListeners = new List<System.Object>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                database = FirebaseDatabase.DefaultInstance;
                databaseRef = database.RootReference;
                roomsRef = databaseRef.Child("rooms");
                auth = FirebaseAuth.DefaultInstance;

                SignInAnonymously();

                Debug.Log("[NetworkManager] Firebase inicializado correctamente");
            }
            else
            {
                Debug.LogError($"[NetworkManager] No se pudo inicializar Firebase: {task.Result}");
            }
        });
    }

    private async Task<bool> WaitForAuthentication(float timeout = 10f)
    {
        float elapsed = 0f;

        while (string.IsNullOrEmpty(playerId) && elapsed < timeout)
        {
            await Task.Delay(100);
            elapsed += 0.1f;
        }

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[NetworkManager] ⏱️ Timeout esperando autenticación");
            return false;
        }

        return true;
    }

    private void SignInAnonymously()
    {
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[NetworkManager] Error al autenticar: " + task.Exception);
                return;
            }

            playerId = auth.CurrentUser.UserId;
            Debug.Log($"[NetworkManager] Usuario autenticado: {playerId}");
        });
    }

    public async Task<string> CreateRoom(int maxPlayers, string hostName)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.Log("[NetworkManager] ⏳ Esperando autenticación...");
            bool authenticated = await WaitForAuthentication();

            if (!authenticated)
            {
                Debug.LogError("[NetworkManager] ❌ No se pudo autenticar al usuario");
                return null;
            }

            Debug.Log("[NetworkManager] ✅ Usuario autenticado exitosamente");
        }

        if (!string.IsNullOrEmpty(currentRoomId))
        {
            Debug.LogWarning($"[NetworkManager] Ya estás en la sala {currentRoomId}. Sal primero antes de crear una nueva.");
            return currentRoomId;
        }

        string roomId = GenerateRoomId();

        currentRoomRef = roomsRef.Child(roomId);

        currentRoomId = roomId;
        isHost = true;
        playerNumber = 0;

        // 🔥 GUARDAR nombre del jugador
        _playerName = hostName;

        RoomData roomData = new RoomData
        {
            roomId = roomId,
            hostId = playerId,
            hostName = hostName,
            maxPlayers = maxPlayers,
            currentPlayers = 1,
            gameStarted = false,
            createdAt = ServerValue.Timestamp
        };

        var playerData = new PlayerData
        {
            playerId = playerId,
            playerName = hostName,
            playerNumber = 0,
            isHost = true,
            isReady = true,
            joinedAt = ServerValue.Timestamp
        };

        try
        {
            await currentRoomRef.SetValueAsync(roomData.ToDictionary());
            await currentRoomRef.Child("players").Child(playerId).SetValueAsync(playerData.ToDictionary());

            SetupRoomListeners();

            Debug.Log($"[NetworkManager] ✓ Sala creada: {roomId}");
            return roomId;
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] ❌ Error al crear sala: {e.Message}");
            Debug.LogError($"[NetworkManager] StackTrace: {e.StackTrace}");

            currentRoomId = null;
            currentRoomRef = null;
            isHost = false;
            playerNumber = -1;

            return null;
        }
    }

    public async Task<string> JoinRoom(string roomId = null, string playerName = null)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.Log("[NetworkManager] ⏳ Esperando autenticación...");
            bool authenticated = await WaitForAuthentication();

            if (!authenticated)
            {
                Debug.LogError("[NetworkManager] ❌ No se pudo autenticar al usuario");
                return null;
            }

            Debug.Log("[NetworkManager] ✅ Usuario autenticado exitosamente");
        }

        if (string.IsNullOrEmpty(roomId))
        {
            var availableRoom = await FindAvailableRoom();
            if (availableRoom == null)
            {
                Debug.Log("[NetworkManager] No hay salas disponibles");
                return null;
            }
            roomId = availableRoom.roomId;
        }

        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogError("[NetworkManager] ❌ roomId es null o vacío, no se puede unir");
            return null;
        }

        if (!string.IsNullOrEmpty(currentRoomId))
        {
            if (currentRoomId == roomId)
            {
                Debug.LogWarning($"[NetworkManager] Ya estás en la sala {roomId}");
                return roomId;
            }
            else
            {
                Debug.LogWarning($"[NetworkManager] Ya estás en la sala {currentRoomId}. Saliendo primero...");
                await LeaveRoom();
            }
        }

        currentRoomId = roomId;
        currentRoomRef = roomsRef.Child(roomId);

        var roomSnapshot = await currentRoomRef.GetValueAsync();
        if (!roomSnapshot.Exists)
        {
            Debug.LogError($"[NetworkManager] La sala {roomId} no existe");
            currentRoomId = null;
            currentRoomRef = null;
            return null;
        }

        var roomData = RoomData.FromSnapshot(roomSnapshot);

        if (roomData.gameStarted)
        {
            Debug.LogWarning("[NetworkManager] El juego ya ha comenzado");
            currentRoomId = null;
            currentRoomRef = null;
            return null;
        }

        if (roomData.currentPlayers >= roomData.maxPlayers)
        {
            Debug.LogWarning("[NetworkManager] La sala está llena");
            currentRoomId = null;
            currentRoomRef = null;
            return null;
        }

        var playersSnapshot = await currentRoomRef.Child("players").GetValueAsync();
        playerNumber = GetNextAvailablePlayerNumber(playersSnapshot);

        if (playerNumber == -1)
        {
            Debug.LogError("[NetworkManager] No hay slots disponibles");
            currentRoomId = null;
            currentRoomRef = null;
            return null;
        }

        string finalPlayerName = string.IsNullOrEmpty(playerName) ?
            $"Jugador {playerNumber + 1}" : playerName;

        // 🔥 GUARDAR nombre del jugador
        _playerName = finalPlayerName;

        var playerData = new PlayerData
        {
            playerId = playerId,
            playerName = finalPlayerName,
            playerNumber = playerNumber,
            isHost = false,
            isReady = false,
            joinedAt = ServerValue.Timestamp
        };

        await currentRoomRef.Child("players").Child(playerId).SetValueAsync(playerData.ToDictionary());

        await currentRoomRef.Child("currentPlayers").SetValueAsync(roomData.currentPlayers + 1);

        SetupRoomListeners();

        Debug.Log($"[NetworkManager] Unido a sala: {roomId} como {finalPlayerName}");
        return roomId;
    }

    private async Task<RoomData> FindAvailableRoom()
    {
        var snapshot = await roomsRef.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.Log("[NetworkManager] No hay salas en Firebase");
            return null;
        }

        foreach (var roomSnapshot in snapshot.Children)
        {
            var roomData = RoomData.FromSnapshot(roomSnapshot);

            if (string.IsNullOrEmpty(roomData.roomId))
            {
                Debug.LogWarning($"[NetworkManager] Sala con roomId inválido encontrada, saltando...");
                continue;
            }

            if (!roomData.gameStarted && roomData.currentPlayers < roomData.maxPlayers)
            {
                Debug.Log($"[NetworkManager] ✓ Sala disponible encontrada: {roomData.roomId} ({roomData.currentPlayers}/{roomData.maxPlayers})");
                return roomData;
            }
        }

        Debug.Log("[NetworkManager] No hay salas disponibles");
        return null;
    }

    private int GetNextAvailablePlayerNumber(DataSnapshot playersSnapshot)
    {
        bool[] takenNumbers = new bool[4];

        if (playersSnapshot.Exists)
        {
            foreach (var playerSnapshot in playersSnapshot.Children)
            {
                var playerData = PlayerData.FromSnapshot(playerSnapshot);
                if (playerData.playerNumber >= 0 && playerData.playerNumber < 4)
                {
                    takenNumbers[playerData.playerNumber] = true;
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            if (!takenNumbers[i])
                return i;
        }

        return -1;
    }

    public async Task StartGame()
    {
        if (!isHost)
        {
            Debug.LogWarning("[NetworkManager] Solo el host puede iniciar el juego");
            return;
        }

        await currentRoomRef.Child("gameStarted").SetValueAsync(true);
        await currentRoomRef.Child("gameState").Child("currentTurn").SetValueAsync(0);
        await currentRoomRef.Child("gameState").Child("currentRound").SetValueAsync(1);

        Debug.Log("[NetworkManager] Juego iniciado");
    }

    private void SetupRoomListeners()
    {
        if (currentRoomRef == null)
            return;

        currentRoomRef.ValueChanged += HandleRoomValueChanged;
        roomListeners.Add(currentRoomRef);

        currentRoomRef.Child("gameState").ValueChanged += HandleGameStateChanged;
        roomListeners.Add(currentRoomRef.Child("gameState"));

        currentRoomRef.Child("actions").Child("endTurn").ValueChanged += HandleEndTurnChanged;
        roomListeners.Add(currentRoomRef.Child("actions").Child("endTurn"));

        currentRoomRef.Child("preparation").ValueChanged += HandlePreparationChanged;
        roomListeners.Add(currentRoomRef.Child("preparation"));
    }

    private void HandlePreparationChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null || !e.Snapshot.Exists)
            return;

        var prepState = PreparationStateData.FromSnapshot(e.Snapshot);
        OnPreparationStateUpdated?.Invoke(prepState);
    }

    private void HandleEndTurnChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null || !e.Snapshot.Exists)
            return;

        var endTurnData = e.Snapshot.Value as Dictionary<string, object>;
        if (endTurnData != null && endTurnData.ContainsKey("playerId"))
        {
            string endTurnPlayerId = endTurnData["playerId"].ToString();

            if (endTurnPlayerId != playerId)
            {
                Debug.Log($"[NetworkManager] 🔔 Fin de turno recibido de otro jugador");
                OnEndTurnReceived?.Invoke();
            }
        }
    }

    private void HandleRoomValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError($"[NetworkManager] Error en sala: {e.DatabaseError.Message}");
            return;
        }

        if (e.Snapshot.Exists)
        {
            var roomData = RoomData.FromSnapshot(e.Snapshot);
            OnRoomUpdated?.Invoke(roomData);

            if (roomData.gameStarted && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("Mecanica 2"))
            {
                OnGameStarted?.Invoke();
                UnityEngine.SceneManagement.SceneManager.LoadScene("Mecanica 2");
            }

            // 🔥 NUEVO: Detectar fin de juego
            var gameStateSnapshot = e.Snapshot.Child("gameState");
            if (gameStateSnapshot.Exists)
            {
                var gameEndSnapshot = gameStateSnapshot.Child("gameEnd");
                if (gameEndSnapshot.Exists)
                {
                    HandleGameEnd(gameEndSnapshot);
                }
            }
        }
    }

    private void HandleGameStateChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null || !e.Snapshot.Exists)
            return;

        var gameState = GameStateData.FromSnapshot(e.Snapshot);
        OnGameStateUpdated?.Invoke(gameState);

        if (e.Snapshot.Child("turnTimeRemaining").Exists)
        {
            float timeRemaining = Convert.ToSingle(e.Snapshot.Child("turnTimeRemaining").Value);
            OnTurnTimerUpdated?.Invoke(timeRemaining);
        }
    }

    public async Task SendCardFusion(string[] cardIds, string resultId)
    {
        if (currentRoomRef == null) return;

        var fusionData = new Dictionary<string, object>
        {
            {"playerId", playerId},
            {"cardIds", cardIds},
            {"resultId", resultId},
            {"timestamp", ServerValue.Timestamp}
        };

        await currentRoomRef.Child("actions").Child("fusions").Push().SetValueAsync(fusionData);
    }

    public async Task SendCardScan(string cardId)
    {
        if (currentRoomRef == null) return;

        var scanData = new Dictionary<string, object>
        {
            {"playerId", playerId},
            {"cardId", cardId},
            {"timestamp", ServerValue.Timestamp}
        };

        await currentRoomRef.Child("actions").Child("scans").Push().SetValueAsync(scanData);
    }

    public async Task SendEndTurn()
    {
        if (currentRoomRef == null) return;

        await currentRoomRef.Child("actions").Child("endTurn").SetValueAsync(new Dictionary<string, object>
        {
            {"playerId", playerId},
            {"timestamp", ServerValue.Timestamp}
        });
    }

    private string GenerateRoomId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        var result = new char[6];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }

        return new string(result);
    }

    public async Task LeaveRoom()
    {
        if (string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(playerId))
            return;

        await currentRoomRef.Child("players").Child(playerId).RemoveValueAsync();

        var roomSnapshot = await currentRoomRef.GetValueAsync();
        if (roomSnapshot.Exists)
        {
            var roomData = RoomData.FromSnapshot(roomSnapshot);
            await currentRoomRef.Child("currentPlayers").SetValueAsync(roomData.currentPlayers - 1);

            if (isHost || roomData.currentPlayers <= 1)
            {
                await currentRoomRef.RemoveValueAsync();
            }
        }

        foreach (var listener in roomListeners)
        {
            if (listener is DatabaseReference dbRef)
            {
                dbRef.ValueChanged -= HandleRoomValueChanged;
                dbRef.ValueChanged -= HandleGameStateChanged;
            }
        }
        roomListeners.Clear();

        currentRoomId = null;
        currentRoomRef = null;
        isHost = false;
        playerNumber = -1;
    }

    void OnDestroy()
    {
        LeaveRoom().ContinueWith(task => { });
    }

    public async Task SendTurnTimer(float timeRemaining)
    {
        if (currentRoomRef == null) return;

        await currentRoomRef.Child("gameState").Child("turnTimeRemaining").SetValueAsync(timeRemaining);
    }

    public event Action<float> OnTurnTimerUpdated;

    public event Action<PreparationStateData> OnPreparationStateUpdated;

    public async Task SendPreparationCardScan(string cardId, int scannedCount)
    {
        if (currentRoomRef == null) return;

        var scanData = new Dictionary<string, object>
    {
        {"playerId", playerId},
        {"cardId", cardId},
        {"scannedCount", scannedCount},
        {"timestamp", ServerValue.Timestamp}
    };

        await currentRoomRef.Child("preparation").Child("scans").Child(playerId).SetValueAsync(scanData);
    }

    public async Task SendPlayerPreparationComplete()
    {
        if (currentRoomRef == null) return;

        await currentRoomRef.Child("preparation").Child("playersReady").Child(playerId).SetValueAsync(true);
    }

    public async Task SendPreparationTimer(float timeRemaining)
    {
        if (currentRoomRef == null) return;

        await currentRoomRef.Child("preparation").Child("timeRemaining").SetValueAsync(timeRemaining);
    }

    // 🔥 NUEVO: Notificar fin del juego
    public async Task SendGameEnd(string winningCardID)
    {
        if (currentRoomRef == null)
        {
            Debug.LogError("[NetworkManager] No hay sala activa");
            return;
        }

        try
        {
            var gameEndData = new Dictionary<string, object>
        {
            { "winningCardID", winningCardID },
            { "winnerPlayerID", playerId },
            { "winnerPlayerName", _playerName }, // ⬅️ USAR CAMPO PRIVADO
            { "timestamp", ServerValue.Timestamp }
        };

            await currentRoomRef.Child("gameState").Child("gameEnd").SetValueAsync(gameEndData);

            Debug.Log($"[NetworkManager] 🏆 Fin de juego notificado: {winningCardID}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkManager] Error al notificar fin de juego: {e.Message}");
        }
    }

    // 🔥 NUEVO: Manejar fin de juego
    private void HandleGameEnd(DataSnapshot gameEndSnapshot)
    {
        if (!gameEndSnapshot.Exists) return;

        string winningCardID = gameEndSnapshot.Child("winningCardID").Value?.ToString();
        string winnerPlayerID = gameEndSnapshot.Child("winnerPlayerID").Value?.ToString();
        string winnerPlayerName = gameEndSnapshot.Child("winnerPlayerName").Value?.ToString();

        Debug.Log($"[NetworkManager] 🎮 Fin de juego recibido: {winnerPlayerName} ganó con {winningCardID}");

        // 🔥 CORRECCIÓN: Usar UnityEngine.Object explícitamente
        VictoryManager victoryManager = UnityEngine.Object.FindFirstObjectByType<VictoryManager>();
        CardDatabase cardDatabase = UnityEngine.Object.FindFirstObjectByType<CardDatabase>();

        if (victoryManager != null && cardDatabase != null)
        {
            if (winnerPlayerID != playerId)
            {
                Debug.Log("[NetworkManager] 😢 No ganaste esta partida");
                victoryManager.ShowDefeatPanel();
            }
            else
            {
                // 🔥 CORRECCIÓN: Usar el método correcto del CardDatabase
                CardData winningCard = cardDatabase.allCards.Find(c => c.id == winningCardID);

                if (winningCard != null)
                {
                    Debug.Log("[NetworkManager] 🏆 ¡Eres el ganador!");
                    victoryManager.ShowVictoryPanel(winningCard);
                }
                else
                {
                    Debug.LogError($"[NetworkManager] No se encontró la carta con ID: {winningCardID}");
                }
            }
        }
    }
}

[Serializable]
public class PreparationStateData
{
    public string phase;
    public float timeRemaining;
    public int playersReady;

    public static PreparationStateData FromSnapshot(DataSnapshot snapshot)
    {
        var prepState = new PreparationStateData
        {
            phase = snapshot.Child("phase").Value?.ToString() ?? "preparation",
            timeRemaining = Convert.ToSingle(snapshot.Child("timeRemaining").Value ?? 60f),
            playersReady = 0
        };

        var readySnapshot = snapshot.Child("playersReady");
        if (readySnapshot.Exists)
        {
            foreach (var child in readySnapshot.Children)
            {
                if (Convert.ToBoolean(child.Value ?? false))
                {
                    prepState.playersReady++;
                }
            }
        }

        return prepState;
    }
}

[Serializable]
public class RoomData
{
    public string roomId;
    public string hostId;
    public string hostName;
    public int maxPlayers;
    public int currentPlayers;
    public bool gameStarted;
    public object createdAt;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            {"roomId", roomId},
            {"hostId", hostId},
            {"hostName", hostName},
            {"maxPlayers", maxPlayers},
            {"currentPlayers", currentPlayers},
            {"gameStarted", gameStarted},
            {"createdAt", createdAt}
        };
    }

    public static RoomData FromSnapshot(DataSnapshot snapshot)
    {
        return new RoomData
        {
            roomId = snapshot.Child("roomId").Value?.ToString(),
            hostId = snapshot.Child("hostId").Value?.ToString(),
            hostName = snapshot.Child("hostName").Value?.ToString() ?? "Host",
            maxPlayers = Convert.ToInt32(snapshot.Child("maxPlayers").Value ?? 2),
            currentPlayers = Convert.ToInt32(snapshot.Child("currentPlayers").Value ?? 0),
            gameStarted = Convert.ToBoolean(snapshot.Child("gameStarted").Value ?? false),
            createdAt = snapshot.Child("createdAt").Value
        };
    }
}

[Serializable]
public class PlayerData
{
    public string playerId;
    public string playerName;
    public int playerNumber;
    public bool isHost;
    public bool isReady;
    public object joinedAt;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            {"playerId", playerId},
            {"playerName", playerName},
            {"playerNumber", playerNumber},
            {"isHost", isHost},
            {"isReady", isReady},
            {"joinedAt", joinedAt}
        };
    }

    public static PlayerData FromSnapshot(DataSnapshot snapshot)
    {
        return new PlayerData
        {
            playerId = snapshot.Child("playerId").Value?.ToString(),
            playerName = snapshot.Child("playerName").Value?.ToString() ?? "Jugador",
            playerNumber = Convert.ToInt32(snapshot.Child("playerNumber").Value ?? -1),
            isHost = Convert.ToBoolean(snapshot.Child("isHost").Value ?? false),
            isReady = Convert.ToBoolean(snapshot.Child("isReady").Value ?? false),
            joinedAt = snapshot.Child("joinedAt").Value
        };
    }
}

[Serializable]
public class GameStateData
{
    public int currentTurn;
    public int currentRound;
    public List<string> playerOrder;

    public static GameStateData FromSnapshot(DataSnapshot snapshot)
    {
        var gameState = new GameStateData
        {
            currentTurn = Convert.ToInt32(snapshot.Child("currentTurn").Value ?? 0),
            currentRound = Convert.ToInt32(snapshot.Child("currentRound").Value ?? 1),
            playerOrder = new List<string>()
        };

        var orderSnapshot = snapshot.Child("playerOrder");
        if (orderSnapshot.Exists)
        {
            foreach (var child in orderSnapshot.Children)
            {
                gameState.playerOrder.Add(child.Value.ToString());
            }
        }

        return gameState;
    }
}