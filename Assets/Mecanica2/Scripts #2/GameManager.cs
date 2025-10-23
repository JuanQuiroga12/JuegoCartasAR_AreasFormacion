using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// 🔥 AÑADIR ESTOS USING 🔥
using UnityEngine.XR.ARFoundation;

public class GameManager : MonoBehaviour
{
    [Header("Configuración del Juego")]
    [SerializeField] private float turnDuration = 60f;
    [SerializeField] private int maxHandSize = 3;
    [SerializeField] private int extendedHandSize = 4;

    [Header("Referencias")]
    [SerializeField] private FusionManager fusionManager;
    [SerializeField] private Button scanButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private GameObject scanPromptPanel;

    [Header("Sistema AR")]
    [SerializeField] private ARScanManager arScanManager;
    // 🔥 AÑADIR REFERENCIA A LA SESIÓN AR 🔥
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARTrackedImageManager arTrackedImageManager;


    [Header("Network")]
    [SerializeField] private bool isMultiplayer = true; // Toggle para modo multijugador
    private NetworkManager networkManager;

    [Header("Jugadores (Solo para modo local)")]
    [SerializeField] private List<string> playerNames = new List<string>() { "Jugador 1", "Jugador 2", "Jugador 3", "Jugador 4" };

    // Estado del juego
    private int currentPlayerIndex = 0;
    private int currentRound = 1;
    private bool isFirstRoundComplete = false;
    private float currentTurnTime;
    private bool isTurnActive = false;
    private List<CardView> currentHand = new List<CardView>();
    private bool hasScannedThisTurn = false;

    // Multijugador
    private bool isMyTurn = false;
    private List<PlayerData> allPlayers = new List<PlayerData>();

    // Eventos
    public delegate void OnTurnChanged(int playerIndex);
    public static event OnTurnChanged TurnChanged;

    public delegate void OnRoundChanged(int round);
    public static event OnRoundChanged RoundChanged;

    void Awake()
    {
        if (isMultiplayer)
        {
            networkManager = NetworkManager.Instance;
            if (networkManager == null)
            {
                Debug.LogError("[GameManager] NetworkManager no encontrado. Cambiando a modo local.");
                isMultiplayer = false;
            }
        }

        // 🔥 BUSCAR COMPONENTES AR SI NO ESTÁN ASIGNADOS 🔥
        if (arSession == null)
        {
            arSession = Object.FindFirstObjectByType<ARSession>();
        }

        if (arTrackedImageManager == null)
        {
            arTrackedImageManager = Object.FindFirstObjectByType<ARTrackedImageManager>();
        }

        // 🔥 FORZAR ACTIVACIÓN DE AR AL INICIO 🔥
        StartCoroutine(EnsureAREnabled());
    }

    // 🔥 NUEVA COROUTINE PARA ACTIVAR AR 🔥
    private IEnumerator EnsureAREnabled()
    {
        yield return new WaitForSeconds(0.5f); // Esperar a que se inicialice la escena

        if (arSession != null)
        {
            arSession.enabled = true;
            Debug.Log("[GameManager] ✓ ARSession habilitada");
        }
        else
        {
            Debug.LogWarning("[GameManager] ⚠️ ARSession no encontrada en la escena");
        }

        if (arTrackedImageManager != null)
        {
            arTrackedImageManager.enabled = true;
            Debug.Log("[GameManager] ✓ ARTrackedImageManager habilitado");
        }
        else
        {
            Debug.LogWarning("[GameManager] ⚠️ ARTrackedImageManager no encontrado en la escena");
        }
    }

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (isTurnActive)
        {
            UpdateTimer();
        }
    }

    void OnEnable()
    {
        if (isMultiplayer && networkManager != null)
        {
            networkManager.OnGameStateUpdated += OnNetworkGameStateUpdated;
            networkManager.OnTurnTimerUpdated += OnNetworkTurnTimerUpdated;
            networkManager.OnEndTurnReceived += OnNetworkEndTurnReceived; // 🔥 NUEVO
        }
    }

    void OnDisable()
    {
        if (isMultiplayer && networkManager != null)
        {
            networkManager.OnGameStateUpdated -= OnNetworkGameStateUpdated;
            networkManager.OnTurnTimerUpdated -= OnNetworkTurnTimerUpdated;
            networkManager.OnEndTurnReceived -= OnNetworkEndTurnReceived; // 🔥 NUEVO
        }
    }

    private void InitializeGame()
    {
        // Configurar UI inicial
        if (scanButton) scanButton.gameObject.SetActive(false);
        if (discardButton) discardButton.gameObject.SetActive(false);
        if (endTurnButton) endTurnButton.onClick.AddListener(OnEndTurnClick);

        if (isMultiplayer)
        {
            // En multijugador, esperar a que el host inicie el juego
            LoadPlayersFromNetwork();
        }
        else
        {
            // Modo local: iniciar primera ronda inmediatamente
            StartNewRound();
        }
    }

    private async void LoadPlayersFromNetwork()
    {
        if (networkManager == null) return;

        Debug.Log("[GameManager] 🔄 Cargando jugadores desde Firebase...");

        // Esperar un momento para que Firebase se sincronice
        await Task.Delay(500);

        // 🔥 CARGAR JUGADORES REALES DESDE FIREBASE 🔥
        if (networkManager.currentRoomRef != null)
        {
            var playersSnapshot = await networkManager.currentRoomRef.Child("players").GetValueAsync();

            playerNames.Clear();
            allPlayers.Clear();

            if (playersSnapshot.Exists)
            {
                // Crear lista temporal para ordenar por playerNumber
                var playersList = new System.Collections.Generic.List<PlayerData>();

                foreach (var playerSnap in playersSnapshot.Children)
                {
                    var playerData = PlayerData.FromSnapshot(playerSnap);
                    playersList.Add(playerData);
                    allPlayers.Add(playerData);
                }

                // Ordenar por playerNumber
                playersList.Sort((a, b) => a.playerNumber.CompareTo(b.playerNumber));

                // Llenar playerNames con nombres reales
                foreach (var player in playersList)
                {
                    playerNames.Add(player.playerName);
                    Debug.Log($"[GameManager] ✓ Jugador cargado: {player.playerName} (#{player.playerNumber})");
                }

                Debug.Log($"[GameManager] ✓ {playerNames.Count} jugadores cargados desde Firebase");
            }
            else
            {
                Debug.LogWarning("[GameManager] ⚠️ No se encontraron jugadores en Firebase, usando nombres genéricos");

                // Fallback a nombres genéricos
                for (int i = 0; i < 4; i++)
                {
                    playerNames.Add($"Jugador {i + 1}");
                }
            }
        }
        else
        {
            Debug.LogError("[GameManager] ❌ currentRoomRef es null, no se pueden cargar jugadores");

            // Fallback a nombres genéricos
            playerNames.Clear();
            for (int i = 0; i < 4; i++)
            {
                playerNames.Add($"Jugador {i + 1}");
            }
        }

        // Determinar índice del jugador actual
        currentPlayerIndex = 0; // El host inicia en Firebase

        Debug.Log($"[GameManager] 🎮 Juego multijugador iniciado. Soy jugador #{networkManager.playerNumber}");
        Debug.Log($"[GameManager] 📋 Jugadores en partida: {string.Join(", ", playerNames)}");

        StartNewRound();
    }

    private void StartNewRound()
    {
        Debug.Log($"[GameManager] Iniciando Ronda {currentRound}");

        if (roundText) roundText.text = $"Ronda {currentRound}";

        // Si es después de la primera ronda, habilitar botones especiales
        if (currentRound > 1)
        {
            isFirstRoundComplete = true;
        }

        // Reiniciar al primer jugador si es nueva ronda
        if (currentPlayerIndex >= playerNames.Count)
        {
            currentPlayerIndex = 0;
            currentRound++;
        }

        RoundChanged?.Invoke(currentRound);
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        Debug.Log($"[GameManager] Turno de {playerNames[currentPlayerIndex]}");

        // Verificar si es mi turno
        if (isMultiplayer && networkManager != null)
        {
            isMyTurn = (currentPlayerIndex == networkManager.playerNumber);
        }
        else
        {
            // En modo local, siempre es "mi turno"
            isMyTurn = true;
        }

        // Actualizar UI
        if (currentPlayerText)
        {
            string turnText = $"Turno: {playerNames[currentPlayerIndex]}";
            if (isMultiplayer && isMyTurn)
            {
                turnText += " (TÚ)";
            }
            currentPlayerText.text = turnText;
        }

        // Reiniciar tiempo
        currentTurnTime = turnDuration;
        isTurnActive = true;
        hasScannedThisTurn = false;

        // 🔥 NUEVO: Habilitar/Deshabilitar interacciones en FusionManager
        if (fusionManager != null)
        {
            fusionManager.SetInteractionEnabled(isMyTurn);
        }

        // Configurar botones según la ronda
        ConfigureTurnButtons();

        // Obtener referencia a la mano actual
        UpdateCurrentHand();

        // Notificar cambio de turno
        TurnChanged?.Invoke(currentPlayerIndex);
    }

    private void ConfigureTurnButtons()
    {
        // Solo habilitar botones si es mi turno
        bool canInteract = isMyTurn;

        // Habilitar botón de escanear solo después de la primera ronda
        if (isFirstRoundComplete && !hasScannedThisTurn && canInteract)
        {
            if (scanButton)
            {
                scanButton.gameObject.SetActive(true);
                scanButton.onClick.RemoveAllListeners();
                scanButton.onClick.AddListener(OnScanClick);
            }
        }
        else
        {
            if (scanButton) scanButton.gameObject.SetActive(false);
        }

        // El botón de descartar se activa cuando hay más de 3 cartas
        UpdateDiscardButton();

        // 🔥 CORREGIDO: Botón de finalizar turno SOLO activo si es tu turno
        if (endTurnButton)
        {
            endTurnButton.interactable = canInteract;
        }
    }

    // 🔥 NUEVO: Método para sincronizar fin de turno desde Firebase
    private void OnNetworkEndTurnReceived()
    {
        if (!isMultiplayer) return;

        Debug.Log("[GameManager] 🔄 Fin de turno recibido desde Firebase");

        // Solo procesar si NO soy yo quien terminó el turno
        if (!isMyTurn)
        {
            // Pasar al siguiente turno
            EndCurrentTurn();
        }
    }

    private void UpdateDiscardButton()
    {
        if (discardButton)
        {
            bool shouldShowDiscard = currentHand.Count > maxHandSize && isMyTurn;
            discardButton.gameObject.SetActive(shouldShowDiscard);

            if (shouldShowDiscard)
            {
                discardButton.onClick.RemoveAllListeners();
                discardButton.onClick.AddListener(OnDiscardClick);
            }
        }
    }

    private void OnScanClick()
    {
        Debug.Log("[GameManager] Iniciando escaneo AR...");

        // Conectar con el sistema AR real
        if (arScanManager != null)
        {
            arScanManager.StartScanning();
        }
        else if (scanPromptPanel)
        {
            // Fallback si no hay AR configurado
            scanPromptPanel.SetActive(true);
            StartCoroutine(SimulateCardScan());
        }

        // Deshabilitar el botón de escanear después de usarlo
        hasScannedThisTurn = true;
        if (scanButton) scanButton.gameObject.SetActive(false);
    }

    public async void OnCardScanned(CardData scannedCard)
    {
        Debug.Log($"[GameManager] Carta escaneada: {scannedCard.displayName}");

        // Sincronizar con Firebase si es multijugador
        if (isMultiplayer && networkManager != null)
        {
            await networkManager.SendCardScan(scannedCard.id);
        }

        // La carta ya fue agregada por el ARScanManager al FusionManager
        // Solo actualizamos el estado del juego
        UpdateCurrentHand();
        UpdateDiscardButton();

        // Marcar que ya se escaneó en este turno
        hasScannedThisTurn = true;
        if (scanButton) scanButton.gameObject.SetActive(false);
    }

    private IEnumerator SimulateCardScan()
    {
        yield return new WaitForSeconds(2f);

        if (scanPromptPanel) scanPromptPanel.SetActive(false);

        // Simulación para testing sin AR
        CardData randomCard = GetRandomCard();
        if (randomCard != null)
        {
            // Agregar la carta a la mano
            AddCardToHand(randomCard);

            // Llamar a OnCardScanned para mantener consistencia con el flujo AR real
            OnCardScanned(randomCard);
        }
        else
        {
            ShowWarning("No hay cartas disponibles para escanear.");
        }
    }

    private void AddCardToHand(CardData cardData)
    {
        if (fusionManager && cardData != null)
        {
            // Agregar carta a la mano a través del FusionManager
            fusionManager.AddCardToHand(cardData);
            UpdateCurrentHand();
            UpdateDiscardButton();
        }
    }

    private CardData GetRandomCard()
    {
        // Este método debería obtener una carta random de la base de datos
        // Por ahora retorna null, se debe implementar con tu sistema
        var cardDatabase = Object.FindFirstObjectByType<CardDatabase>();
        if (cardDatabase && cardDatabase.allCards.Count > 0)
        {
            return cardDatabase.allCards[Random.Range(0, cardDatabase.allCards.Count)];
        }
        return null;
    }

    private void OnDiscardClick()
    {
        Debug.Log("[GameManager] Modo descarte activado");

        // Habilitar selección para descarte
        EnableDiscardMode();
    }

    private void EnableDiscardMode()
    {
        // Marcar todas las cartas como descartables
        foreach (var card in currentHand)
        {
            if (card != null)
            {
                // Aquí podrías cambiar el color o agregar un indicador visual
                card.SetDiscardMode(true);
            }
        }
    }

    public void DiscardCard(CardView card)
    {
        if (currentHand.Contains(card))
        {
            Debug.Log($"[GameManager] Descartando carta: {card.data.displayName}");

            fusionManager.RemoveCardFromHand(card);
            UpdateCurrentHand();
            UpdateDiscardButton();

            // Desactivar modo descarte si ya tenemos 3 cartas
            if (currentHand.Count <= maxHandSize)
            {
                DisableDiscardMode();
            }
        }
    }

    private void DisableDiscardMode()
    {
        foreach (var card in currentHand)
        {
            if (card != null)
            {
                card.SetDiscardMode(false);
            }
        }
    }

    private async void OnEndTurnClick()
    {
        // Solo permitir si es mi turno
        if (!isMyTurn)
        {
            ShowWarning("No es tu turno.");
            return;
        }

        // 🔥 ACTUALIZAR MANO ANTES DE VALIDAR 🔥
        UpdateCurrentHand();

        // 🔥 VALIDACIÓN MÁS FLEXIBLE: PERMITIR 1-3 CARTAS 🔥
        // En la primera ronda, todos empiezan con 3 cartas
        // Después de fusionar, pueden quedar 1 o 2 cartas
        if (currentHand.Count < 1 || currentHand.Count > extendedHandSize)
        {
            ShowWarning($"Debes tener entre 1 y {extendedHandSize} cartas para terminar tu turno. Tienes {currentHand.Count}.");
            return;
        }

        // 🔥 SI TIENE MÁS DE 3, FORZAR DESCARTE 🔥
        if (currentHand.Count > maxHandSize)
        {
            ShowWarning($"Tienes {currentHand.Count} cartas. Debes descartar hasta tener {maxHandSize}.");
            EnableDiscardMode();
            return;
        }

        // Deshabilitar botón para evitar clicks múltiples
        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }

        // Sincronizar fin de turno con Firebase
        if (isMultiplayer && networkManager != null)
        {
            await networkManager.SendEndTurn();
        }

        EndCurrentTurn();
    }

    // 🔥 NUEVO MÉTODO PARA RECIBIR TIEMPO SINCRONIZADO 🔥
    private void OnNetworkTurnTimerUpdated(float timeRemaining)
    {
        if (!isMultiplayer || isMyTurn) return; // Si es mi turno, yo controlo el tiempo

        // Actualizar el tiempo local desde Firebase
        currentTurnTime = timeRemaining;

        // Actualizar UI
        if (timerText)
        {
            int minutes = Mathf.FloorToInt(currentTurnTime / 60);
            int seconds = Mathf.FloorToInt(currentTurnTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Cambiar color si queda poco tiempo
            if (currentTurnTime <= 10f)
            {
                timerText.color = Color.red;
            }
            else if (currentTurnTime <= 30f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    // 🔥 MODIFICAR UpdateTimer PARA SINCRONIZAR TIEMPO 🔥
    private float lastSyncTime = 0f;
    private const float SYNC_INTERVAL = 1f; // Sincronizar cada segundo

    private void UpdateTimer()
    {
        currentTurnTime -= Time.deltaTime;

        if (timerText)
        {
            int minutes = Mathf.FloorToInt(currentTurnTime / 60);
            int seconds = Mathf.FloorToInt(currentTurnTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Cambiar color si queda poco tiempo
            if (currentTurnTime <= 10f)
            {
                timerText.color = Color.red;
            }
            else if (currentTurnTime <= 30f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }

        // 🔥 SINCRONIZAR TIEMPO CON FIREBASE (SOLO SI ES MI TURNO) 🔥
        if (isMyTurn && isMultiplayer && networkManager != null)
        {
            lastSyncTime += Time.deltaTime;
            if (lastSyncTime >= SYNC_INTERVAL)
            {
                lastSyncTime = 0f;
                _ = networkManager.SendTurnTimer(currentTurnTime);
            }
        }

        // Si se acaba el tiempo
        if (currentTurnTime <= 0)
        {
            OnTimeOut();
        }
    }

    private void OnTimeOut()
    {
        Debug.Log("[GameManager] ¡Tiempo agotado!");

        // Solo permitir timeout si es mi turno
        if (!isMyTurn && isMultiplayer)
        {
            Debug.LogWarning("[GameManager] Timeout ignorado: no es mi turno");
            return;
        }

        // Si tiene más de 3 cartas, descartar al azar
        while (currentHand.Count > maxHandSize)
        {
            int randomIndex = Random.Range(0, currentHand.Count);
            DiscardCard(currentHand[randomIndex]);
        }

        // 🔥 SINCRONIZAR FIN DE TURNO CON FIREBASE ANTES DE TERMINAR
        if (isMultiplayer && networkManager != null)
        {
            _ = networkManager.SendEndTurn();
        }

        EndCurrentTurn();
    }

    private void EndCurrentTurn()
    {
        Debug.Log($"[GameManager] Finalizando turno de {playerNames[currentPlayerIndex]}");

        isTurnActive = false;
        DisableDiscardMode();

        // 🔥 NUEVO: Deshabilitar interacciones en FusionManager
        if (fusionManager != null)
        {
            fusionManager.SetInteractionEnabled(false);
        }

        // Pasar al siguiente jugador
        currentPlayerIndex++;

        // 🔥 CORREGIDO: Si todos los jugadores jugaron, nueva ronda
        if (currentPlayerIndex >= playerNames.Count)
        {
            currentPlayerIndex = 0;
            currentRound++;
            isFirstRoundComplete = true;

            // 🔥 CORREGIDO: Acceder a isHost desde networkManager
            if (isMultiplayer && networkManager != null && networkManager.isHost)
            {
                _ = SyncRoundToFirebase();
            }

            Debug.Log($"[GameManager] 🎉 Nueva Ronda: {currentRound}");
        }

        // 🔥 CORREGIDO: Acceder a isHost desde networkManager
        if (isMultiplayer && networkManager != null && networkManager.isHost)
        {
            _ = SyncTurnToFirebase();
        }

        // Pequeña pausa antes del siguiente turno
        StartCoroutine(DelayedNextTurn());
    }

    // 🔥 NUEVO: Sincronizar turno con Firebase
    private async Task SyncTurnToFirebase()
    {
        if (networkManager == null || networkManager.currentRoomRef == null) return;

        try
        {
            await networkManager.currentRoomRef.Child("gameState").Child("currentTurn").SetValueAsync(currentPlayerIndex);
            Debug.Log($"[GameManager] ✅ Turno sincronizado en Firebase: {currentPlayerIndex}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] ❌ Error al sincronizar turno: {e.Message}");
        }
    }

    // 🔥 NUEVO: Sincronizar ronda con Firebase
    private async Task SyncRoundToFirebase()
    {
        if (networkManager == null || networkManager.currentRoomRef == null) return;

        try
        {
            await networkManager.currentRoomRef.Child("gameState").Child("currentRound").SetValueAsync(currentRound);
            Debug.Log($"[GameManager] ✅ Ronda sincronizada en Firebase: {currentRound}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] ❌ Error al sincronizar ronda: {e.Message}");
        }
    }

    private IEnumerator DelayedNextTurn()
    {
        yield return new WaitForSeconds(1f);
        StartPlayerTurn();
    }

    private void UpdateCurrentHand()
    {
        // Obtener las cartas actuales de la mano
        if (fusionManager)
        {
            currentHand = fusionManager.GetCurrentHand();
        }
    }

    private void ShowWarning(string message)
    {
        Debug.LogWarning($"[GameManager] {message}");
        // Aquí podrías mostrar un panel de UI con el mensaje
    }

    public async void OnCardFused()
    {
        // Llamado por FusionManager cuando se fusionan cartas
        UpdateCurrentHand();
        UpdateDiscardButton();

        // Sincronizar fusión con Firebase si es multijugador
        if (isMultiplayer && networkManager != null && fusionManager != null)
        {
            var selectedCards = fusionManager.GetSelectedData();
            if (selectedCards != null && selectedCards.Count > 0)
            {
                string[] cardIds = new string[selectedCards.Count];
                for (int i = 0; i < selectedCards.Count; i++)
                {
                    cardIds[i] = selectedCards[i].id;
                }

                // Obtener resultado (necesitarás implementar esto en FusionManager)
                var result = fusionManager.GetLastFusionResult();
                if (result != null)
                {
                    await networkManager.SendCardFusion(cardIds, result.id);
                }
            }
        }
    }

    // Evento que se llama cuando Firebase actualiza el estado del juego
    // 🔥 MODIFICADO: Evento que se llama cuando Firebase actualiza el estado del juego
    private void OnNetworkGameStateUpdated(GameStateData gameState)
    {
        if (!isMultiplayer) return;

        Debug.Log($"[GameManager] Estado de juego actualizado: Turno={gameState.currentTurn}, Ronda={gameState.currentRound}");

        // 🔥 NUEVO: Verificar si el turno cambió
        bool turnChanged = (currentPlayerIndex != gameState.currentTurn);
        bool roundChanged = (currentRound != gameState.currentRound);

        // Actualizar el estado del juego local basado en Firebase
        currentRound = gameState.currentRound;
        currentPlayerIndex = gameState.currentTurn;

        // Actualizar UI
        if (roundText) roundText.text = $"Ronda {currentRound}";

        // 🔥 NUEVO: Marcar si estamos en ronda 2+
        if (currentRound > 1)
        {
            isFirstRoundComplete = true;
        }

        // Verificar si es mi turno
        if (networkManager != null)
        {
            isMyTurn = (currentPlayerIndex == networkManager.playerNumber);
        }

        // 🔥 NUEVO: Si el turno cambió, reiniciar el turno local
        if (turnChanged)
        {
            Debug.Log("[GameManager] 🔄 Cambio de turno detectado desde Firebase");

            // Detener coroutine anterior si existe
            StopAllCoroutines();

            // Iniciar nuevo turno
            StartPlayerTurn();
        }
        else
        {
            // Solo reconfigurar botones si no hubo cambio de turno
            ConfigureTurnButtons();
        }
    }

    // Eventos adicionales opcionales
    public delegate void OnCardScannedDelegate(CardData card, int playerIndex);
    public static event OnCardScannedDelegate OnCardScannedEvent;

    private void LogPlayerAction(string action)
    {
        Debug.Log($"[GameLog] {action}");
        // Aquí podrías guardar en un historial de acciones para mostrar en UI
        // o para repetición de partidas
    }
}