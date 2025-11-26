using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class GameManager : MonoBehaviour
{
    [Header("Configuración del Juego")]
    [SerializeField] private float turnDuration = 60f;
    [SerializeField] private float preparationDuration = 60f;
    [SerializeField] private int maxHandSize = 3; // ⬅️ Debe terminar con 3
    [SerializeField] private int absoluteMaxHandSize = 4; // 🔥 NUEVO: Máximo absoluto
    [SerializeField] private int initialCardsToScan = 3;
    [SerializeField] private int maxScansPerTurn = 2; // 🔥 NUEVO: Máximo escaneos por turno
    [SerializeField] private int maxFusionsPerTurn = 1; // 🔥 NUEVO: Máximo fusiones por turno

    [Header("Referencias")]
    [SerializeField] private FusionManager fusionManager;
    [SerializeField] private Button scanButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private GameObject scanPromptPanel;

    // 🔥 AGREGAR AL INICIO DE LA CLASE (después de las variables privadas existentes)

    [Header("🏆 Sistema de Victoria")]
    [SerializeField] private VictoryManager victoryManager;

    [Header("🔥 UI Contadores de Turno")]
    [SerializeField] private TextMeshProUGUI scansCountText; // 🔥 NUEVO
    [SerializeField] private TextMeshProUGUI fusionsCountText; // 🔥 NUEVO

    [Header("🔥 UI Fase de Preparación - Solo Textos")]
    [SerializeField] private TextMeshProUGUI preparationInstructionText;
    [SerializeField] private TextMeshProUGUI scannedCardsCountText;

    [Header("Sistema AR")]
    [SerializeField] private ARScanManager arScanManager;
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARTrackedImageManager arTrackedImageManager;

    [Header("Network")]
    [SerializeField] private bool isMultiplayer = true;
    private NetworkManager networkManager;

    [Header("Jugadores (Solo para modo local)")]
    [SerializeField] private List<string> playerNames = new List<string>() { "Jugador 1", "Jugador 2", "Jugador 3", "Jugador 4" };

    // Estado de la fase de preparación
    private enum GamePhase
    {
        Preparation,
        Playing
    }

    private GamePhase currentPhase = GamePhase.Preparation;
    private float preparationTimeRemaining;
    private int scannedCardsCount = 0;
    private bool isPreparationActive = false;

    // Estado del juego
    private int currentPlayerIndex = 0;
    private int currentRound = 1;
    private float currentTurnTime;
    private bool isTurnActive = false;
    private List<CardView> currentHand = new List<CardView>();

    // 🔥 NUEVO: Contadores de turno
    private int scansUsedThisTurn = 0;
    private int fusionsUsedThisTurn = 0;

    // Multijugador
    private bool isMyTurn = false;
    private List<PlayerData> allPlayers = new List<PlayerData>();

    private bool gameEnded = false; // 🔥 NUEVO: Flag para evitar múltiples victorias

    // Eventos
    public delegate void OnTurnChanged(int playerIndex);
    public static event OnTurnChanged TurnChanged;

    public delegate void OnRoundChanged(int round);
    public static event OnRoundChanged RoundChanged;

    public delegate void OnPreparationPhaseComplete();
    public static event OnPreparationPhaseComplete PreparationPhaseComplete;

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

        if (arSession == null)
        {
            arSession = Object.FindFirstObjectByType<ARSession>();
        }

        if (arTrackedImageManager == null)
        {
            arTrackedImageManager = Object.FindFirstObjectByType<ARTrackedImageManager>();
        }

        StartCoroutine(EnsureAREnabled());
    }

    private IEnumerator EnsureAREnabled()
    {
        yield return new WaitForSeconds(0.5f);

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
        if (isPreparationActive)
        {
            UpdatePreparationTimer();
        }
        else if (isTurnActive)
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
            networkManager.OnEndTurnReceived += OnNetworkEndTurnReceived;
            networkManager.OnPreparationStateUpdated += OnNetworkPreparationStateUpdated;
        }
    }

    void OnDisable()
    {
        if (isMultiplayer && networkManager != null)
        {
            networkManager.OnGameStateUpdated -= OnNetworkGameStateUpdated;
            networkManager.OnTurnTimerUpdated -= OnNetworkTurnTimerUpdated;
            networkManager.OnEndTurnReceived -= OnNetworkEndTurnReceived;
            networkManager.OnPreparationStateUpdated -= OnNetworkPreparationStateUpdated;
        }
    }

    private void InitializeGame()
    {
        // Ocultar TODOS los elementos de UI al inicio
        if (scanButton) scanButton.gameObject.SetActive(false);
        if (discardButton) discardButton.gameObject.SetActive(false);
        if (endTurnButton) endTurnButton.gameObject.SetActive(false);
        if (timerText) timerText.gameObject.SetActive(false);
        if (roundText) roundText.gameObject.SetActive(false);
        if (currentPlayerText) currentPlayerText.gameObject.SetActive(false);
        if (preparationInstructionText) preparationInstructionText.gameObject.SetActive(false);
        if (scannedCardsCountText) scannedCardsCountText.gameObject.SetActive(false);
        if (scansCountText) scansCountText.gameObject.SetActive(false); // 🔥 NUEVO
        if (fusionsCountText) fusionsCountText.gameObject.SetActive(false); // 🔥 NUEVO

        // Configurar listeners de botones
        if (endTurnButton) endTurnButton.onClick.AddListener(OnEndTurnClick);
        if (scanButton) scanButton.onClick.AddListener(OnScanClick);

        if (isMultiplayer)
        {
            LoadPlayersFromNetwork();
        }
        else
        {
            StartPreparationPhase();
        }
    }

    private async void LoadPlayersFromNetwork()
    {
        if (networkManager == null) return;

        Debug.Log("[GameManager] 🔄 Cargando jugadores desde Firebase...");

        await Task.Delay(500);

        if (networkManager.currentRoomRef != null)
        {
            var playersSnapshot = await networkManager.currentRoomRef.Child("players").GetValueAsync();

            playerNames.Clear();
            allPlayers.Clear();

            if (playersSnapshot.Exists)
            {
                var playersList = new System.Collections.Generic.List<PlayerData>();

                foreach (var playerSnap in playersSnapshot.Children)
                {
                    var playerData = PlayerData.FromSnapshot(playerSnap);
                    playersList.Add(playerData);
                    allPlayers.Add(playerData);
                }

                playersList.Sort((a, b) => a.playerNumber.CompareTo(b.playerNumber));

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

                for (int i = 0; i < 4; i++)
                {
                    playerNames.Add($"Jugador {i + 1}");
                }
            }
        }
        else
        {
            Debug.LogError("[GameManager] ❌ currentRoomRef es null, no se pueden cargar jugadores");

            playerNames.Clear();
            for (int i = 0; i < 4; i++)
            {
                playerNames.Add($"Jugador {i + 1}");
            }
        }

        currentPlayerIndex = 0;

        Debug.Log($"[GameManager] 🎮 Juego multijugador iniciado. Soy jugador #{networkManager.playerNumber}");
        Debug.Log($"[GameManager] 📋 Jugadores en partida: {string.Join(", ", playerNames)}");

        StartPreparationPhase();
    }

    // ========== FASE DE PREPARACIÓN ==========

    private void StartPreparationPhase()
    {
        Debug.Log("[GameManager] 📋 Iniciando Fase de Preparación");

        currentPhase = GamePhase.Preparation;
        isPreparationActive = true;
        preparationTimeRemaining = preparationDuration;
        scannedCardsCount = 0;

        Debug.Log("[GameManager] 🎨 Configurando UI de preparación...");

        // Mostrar timer
        if (timerText)
        {
            timerText.gameObject.SetActive(true);
            Debug.Log("[GameManager] ✓ Timer activado");
        }

        // Mostrar texto de fase
        if (roundText)
        {
            roundText.text = "FASE DE PREPARACIÓN";
            roundText.gameObject.SetActive(true);
            Debug.Log("[GameManager] ✓ Texto de fase configurado");
        }

        // Ocultar texto de jugador actual
        if (currentPlayerText)
        {
            currentPlayerText.gameObject.SetActive(false);
            Debug.Log("[GameManager] ✓ Texto de jugador ocultado");
        }

        // Configurar botón de escaneo
        if (scanButton)
        {
            scanButton.onClick.RemoveAllListeners();
            scanButton.onClick.AddListener(OnPreparationScanClick);
            scanButton.gameObject.SetActive(true);
            scanButton.interactable = true;
            Debug.Log("[GameManager] ✓ Botón de escaneo configurado y activado");
        }

        // Mostrar textos específicos de preparación
        if (preparationInstructionText)
        {
            preparationInstructionText.text = $"Escanea {initialCardsToScan} cartas del mazo físico para comenzar";
            preparationInstructionText.gameObject.SetActive(true);
            Debug.Log("[GameManager] ✓ Texto de instrucciones activado");
        }

        UpdateScannedCardsCount();

        // 🔥 CRÍTICO: Asegurar que botones de juego estén ocultos
        if (discardButton) discardButton.gameObject.SetActive(false);
        if (endTurnButton) endTurnButton.gameObject.SetActive(false);
        if (scansCountText) scansCountText.gameObject.SetActive(false); // 🔥 NUEVO
        if (fusionsCountText) fusionsCountText.gameObject.SetActive(false); // 🔥 NUEVO

        // Sincronizar con Firebase
        if (isMultiplayer && networkManager != null && networkManager.isHost)
        {
            _ = SyncPreparationPhaseToFirebase();
        }

        Debug.Log("[GameManager] ✅ Fase de preparación iniciada completamente");

        if (SFXManager.Instance != null)
            SFXManager.Instance.StartTimerLoop();
    }

    private void OnPreparationScanClick()
    {
        if (scannedCardsCount >= initialCardsToScan)
        {
            ShowWarning($"Ya escaneaste las {initialCardsToScan} cartas iniciales");
            return;
        }

        Debug.Log("[GameManager] 🔍 Iniciando escaneo en fase de preparación...");

        if (arScanManager != null)
        {
            arScanManager.StartScanning();
        }
        else
        {
            ShowWarning("ARScanManager no está configurado");
        }
    }

    public async void OnPreparationCardScanned(CardData scannedCard)
    {
        if (!isPreparationActive) return;

        if (scannedCardsCount >= initialCardsToScan)
        {
            ShowWarning($"Ya escaneaste las {initialCardsToScan} cartas iniciales");
            return;
        }

        Debug.Log($"[GameManager] 🎴 Carta escaneada en preparación: {scannedCard.displayName}");

        if (fusionManager != null)
        {
            fusionManager.AddCardToHand(scannedCard);
        }

        scannedCardsCount++;
        UpdateScannedCardsCount();

        if (isMultiplayer && networkManager != null)
        {
            await networkManager.SendPreparationCardScan(scannedCard.id, scannedCardsCount);
        }

        if (scannedCardsCount >= initialCardsToScan)
        {
            OnPlayerCompletedPreparation();
        }
    }

    private void UpdateScannedCardsCount()
    {
        if (scannedCardsCountText != null)
        {
            scannedCardsCountText.text = $"Cartas escaneadas: {scannedCardsCount}/{initialCardsToScan}";
            scannedCardsCountText.gameObject.SetActive(true);
        }
    }

    private async void OnPlayerCompletedPreparation()
    {
        Debug.Log("[GameManager] ✅ Jugador completó fase de preparación");

        if (scanButton)
        {
            scanButton.interactable = false;
        }

        if (preparationInstructionText)
        {
            preparationInstructionText.text = "¡Listo! Esperando a otros jugadores...";
        }

        if (isMultiplayer && networkManager != null)
        {
            await networkManager.SendPlayerPreparationComplete();
        }

        if (!isMultiplayer)
        {
            CheckIfAllPlayersReady();
        }
    }

    private void UpdatePreparationTimer()
    {
        preparationTimeRemaining -= Time.deltaTime;

        if (timerText)
        {
            int minutes = Mathf.FloorToInt(preparationTimeRemaining / 60);
            int seconds = Mathf.FloorToInt(preparationTimeRemaining % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";

            if (preparationTimeRemaining <= 10f)
            {
                timerText.color = Color.red;
            }
            else if (preparationTimeRemaining <= 30f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }

        if (isMultiplayer && networkManager != null && networkManager.isHost)
        {
            _ = networkManager.SendPreparationTimer(preparationTimeRemaining);
        }

        if (preparationTimeRemaining <= 0)
        {
            OnPreparationTimeOut();
        }
    }

    private void OnPreparationTimeOut()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.StopTimerLoop();

        Debug.Log("[GameManager] ⏰ Tiempo de preparación agotado");

        isPreparationActive = false;

        if (isMultiplayer && networkManager != null && networkManager.isHost)
        {
            _ = SyncPreparationCompleteToFirebase();
        }

        EndPreparationPhase();
    }

    private void CheckIfAllPlayersReady()
    {
        Debug.Log("[GameManager] ✅ Todos los jugadores listos");

        if (isMultiplayer && networkManager != null && networkManager.isHost)
        {
            _ = SyncPreparationCompleteToFirebase();
        }

        EndPreparationPhase();
    }

    private void EndPreparationPhase()
    {
        Debug.Log("[GameManager] 🎉 Finalizando fase de preparación");

        isPreparationActive = false;
        currentPhase = GamePhase.Playing;

        // Ocultar elementos de preparación
        if (preparationInstructionText)
        {
            preparationInstructionText.gameObject.SetActive(false);
            Debug.Log("[GameManager] ✓ Ocultando instrucciones de preparación");
        }

        if (scannedCardsCountText)
        {
            scannedCardsCountText.gameObject.SetActive(false);
            Debug.Log("[GameManager] ✓ Ocultando contador de cartas");
        }

        // Ocultar botón de escaneo
        if (scanButton)
        {
            scanButton.gameObject.SetActive(false);
            scanButton.interactable = true;
            scanButton.onClick.RemoveAllListeners();
            Debug.Log("[GameManager] ✓ Ocultando botón de escaneo");
        }

        // Resetear color del timer
        if (timerText)
        {
            timerText.color = Color.white;
        }

        // Validar cartas
        if (fusionManager != null && fusionManager.GetHandCount() == 0)
        {
            ShowWarning("No escaneaste ninguna carta. Se agregarán cartas aleatorias.");
        }

        PreparationPhaseComplete?.Invoke();

        Debug.Log("[GameManager] 🎮 Transicionando a primera ronda...");

        StartNewRound();
    }

    private async Task SyncPreparationPhaseToFirebase()
    {
        if (networkManager == null || networkManager.currentRoomRef == null) return;

        try
        {
            await networkManager.currentRoomRef.Child("gameState").Child("phase").SetValueAsync("preparation");
            await networkManager.currentRoomRef.Child("gameState").Child("preparationTimeRemaining")
                .SetValueAsync(preparationTimeRemaining);

            Debug.Log("[GameManager] ✅ Fase de preparación sincronizada en Firebase");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] ❌ Error al sincronizar preparación: {e.Message}");
        }
    }

    private async Task SyncPreparationCompleteToFirebase()
    {
        if (networkManager == null || networkManager.currentRoomRef == null) return;

        try
        {
            await networkManager.currentRoomRef.Child("gameState").Child("phase").SetValueAsync("playing");
            await networkManager.currentRoomRef.Child("gameState").Child("preparationComplete").SetValueAsync(true);

            Debug.Log("[GameManager] ✅ Fin de preparación sincronizado en Firebase");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] ❌ Error al sincronizar fin de preparación: {e.Message}");
        }
    }

    private void OnNetworkPreparationStateUpdated(PreparationStateData prepState)
    {
        if (!isMultiplayer) return;

        Debug.Log($"[GameManager] 🔄 Estado de preparación actualizado: Fase={prepState.phase}, Listos={prepState.playersReady}/{playerNames.Count}");

        preparationTimeRemaining = prepState.timeRemaining;

        if (prepState.phase == "playing" || prepState.playersReady >= playerNames.Count)
        {
            if (isPreparationActive)
            {
                EndPreparationPhase();
            }
        }
    }

    // ========== FIN FASE DE PREPARACIÓN ==========

    private void StartNewRound()
    {
        Debug.Log($"[GameManager] 🎮 Iniciando Ronda {currentRound}");

        if (roundText)
        {
            roundText.text = $"Ronda {currentRound}";
            roundText.gameObject.SetActive(true);
            Debug.Log($"[GameManager] ✓ Mostrando texto de Ronda {currentRound}");
        }

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
        Debug.Log($"[GameManager] 🎯 Iniciando turno de {playerNames[currentPlayerIndex]}");

        if (isMultiplayer && networkManager != null)
        {
            isMyTurn = (currentPlayerIndex == networkManager.playerNumber);
        }
        else
        {
            isMyTurn = true;
        }

        if (currentPlayerText)
        {
            string turnText = $"Turno: {playerNames[currentPlayerIndex]}";
            if (isMultiplayer && isMyTurn)
            {
                turnText += " (TÚ)";
            }
            currentPlayerText.text = turnText;
            currentPlayerText.gameObject.SetActive(true);
            Debug.Log($"[GameManager] ✓ Mostrando texto de jugador: {turnText}");
        }

        currentTurnTime = turnDuration;
        isTurnActive = true;

        // 🔥 NUEVO: Resetear contadores de turno
        scansUsedThisTurn = 0;
        fusionsUsedThisTurn = 0;
        UpdateTurnCounters();

        if (SFXManager.Instance != null)
            SFXManager.Instance.StartTimerLoop();

        if (fusionManager != null)
        {
            fusionManager.SetInteractionEnabled(isMyTurn);
            fusionManager.ResetFusionCounter(); // 🔥 NUEVO
        }

        ConfigureTurnButtons();
        UpdateCurrentHand();

        TurnChanged?.Invoke(currentPlayerIndex);

        Debug.Log($"[GameManager] ✅ Turno iniciado correctamente");
    }

    // 🔥 NUEVO: Actualizar textos de contadores
    private void UpdateTurnCounters()
    {
        if (scansCountText != null)
        {
            scansCountText.text = $"Escaneos: {scansUsedThisTurn}/{maxScansPerTurn}";
            scansCountText.gameObject.SetActive(isMyTurn);
        }

        if (fusionsCountText != null)
        {
            fusionsCountText.text = $"Fusiones: {fusionsUsedThisTurn}/{maxFusionsPerTurn}";
            fusionsCountText.gameObject.SetActive(isMyTurn);
        }
    }

    private void ConfigureTurnButtons()
    {
        bool canInteract = isMyTurn;

        Debug.Log($"[GameManager] 🔧 Configurando botones de turno (canInteract={canInteract})");

        // 🔥 MODIFICADO: Botón de escaneo disponible SIEMPRE (desde ronda 1)
        // Pero solo si:
        // 1. No se han usado todos los escaneos
        // 2. La mano no está llena (< 4 cartas)
        bool canScan = canInteract &&
                       scansUsedThisTurn < maxScansPerTurn &&
                       currentHand.Count < absoluteMaxHandSize;

        if (scanButton)
        {
            if (canScan)
            {
                scanButton.onClick.RemoveAllListeners();
                scanButton.onClick.AddListener(OnScanClick);
                scanButton.gameObject.SetActive(true);
                scanButton.interactable = true;
                Debug.Log($"[GameManager] ✓ Botón de escaneo activado ({scansUsedThisTurn}/{maxScansPerTurn} usados, {currentHand.Count}/{absoluteMaxHandSize} cartas)");
            }
            else
            {
                scanButton.gameObject.SetActive(false);

                if (scansUsedThisTurn >= maxScansPerTurn)
                {
                    Debug.Log($"[GameManager] ✓ Botón de escaneo ocultado (límite alcanzado: {scansUsedThisTurn}/{maxScansPerTurn})");
                }
                else if (currentHand.Count >= absoluteMaxHandSize)
                {
                    Debug.Log($"[GameManager] ✓ Botón de escaneo ocultado (mano llena: {currentHand.Count}/{absoluteMaxHandSize})");
                }
            }
        }

        UpdateDiscardButton();

        // Mostrar botón de finalizar turno
        if (endTurnButton)
        {
            endTurnButton.gameObject.SetActive(true);
            endTurnButton.interactable = canInteract;
            Debug.Log($"[GameManager] ✓ Botón finalizar turno (activo={canInteract})");
        }
    }

    private void OnNetworkEndTurnReceived()
    {
        if (!isMultiplayer) return;

        Debug.Log("[GameManager] 🔄 Fin de turno recibido desde Firebase");

        if (!isMyTurn)
        {
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
                Debug.Log($"[GameManager] ✓ Botón descartar activado ({currentHand.Count} cartas)");
            }
        }
    }

    private void OnScanClick()
    {
        // 🔥 NUEVA VALIDACIÓN: Verificar límite de escaneos
        if (scansUsedThisTurn >= maxScansPerTurn)
        {
            ShowWarning($"Ya usaste todos tus escaneos este turno ({maxScansPerTurn} máximo)");
            return;
        }

        // 🔥 NUEVA VALIDACIÓN: Verificar que no tenga 4 cartas
        if (currentHand.Count >= absoluteMaxHandSize)
        {
            ShowWarning($"No puedes escanear con {absoluteMaxHandSize} cartas. Fusiona o descarta primero.");
            return;
        }

        Debug.Log($"[GameManager] 🔍 Iniciando escaneo AR ({scansUsedThisTurn + 1}/{maxScansPerTurn})...");

        if (arScanManager != null)
        {
            arScanManager.StartScanning();
        }
        else if (scanPromptPanel)
        {
            scanPromptPanel.SetActive(true);
            StartCoroutine(SimulateCardScan());
        }
    }

    public async void OnCardScanned(CardData scannedCard)
    {
        if (scannedCard == null)
        {
            Debug.LogError("[GameManager] ❌ Carta escaneada es NULL");
            return;
        }

        Debug.Log($"[GameManager] 🎴 Carta escaneada: {scannedCard.displayName}");
        Debug.Log($"[GameManager] 📍 Fase actual: {currentPhase}");
        Debug.Log($"[GameManager] 📍 Ronda actual: {currentRound}");

        // Si estamos en fase de preparación, manejar de forma especial
        if (currentPhase == GamePhase.Preparation)
        {
            Debug.Log("[GameManager] 🔧 Procesando escaneo en FASE DE PREPARACIÓN");
            OnPreparationCardScanned(scannedCard);
            return;
        }

        // 🔥 VALIDACIÓN: No exceder límite de escaneos
        if (scansUsedThisTurn >= maxScansPerTurn)
        {
            ShowWarning($"Ya usaste todos tus escaneos este turno ({maxScansPerTurn} máximo)");
            return;
        }

        // 🔥 VALIDACIÓN: No exceder límite de cartas
        if (currentHand.Count >= absoluteMaxHandSize)
        {
            ShowWarning($"No puedes tener más de {absoluteMaxHandSize} cartas");
            return;
        }

        Debug.Log("[GameManager] 🔧 Procesando escaneo en JUEGO NORMAL");

        // 1. Agregar carta a la mano visualmente
        if (fusionManager != null)
        {
            Debug.Log($"[GameManager] ➕ Agregando '{scannedCard.displayName}' al FusionManager");
            fusionManager.AddCardToHand(scannedCard);
        }
        else
        {
            Debug.LogError("[GameManager] ❌ FusionManager es NULL! No se puede agregar la carta");
        }

        // 2. Incrementar contador de escaneos
        scansUsedThisTurn++;
        UpdateTurnCounters();
        Debug.Log($"[GameManager] 📊 Escaneos usados este turno: {scansUsedThisTurn}/{maxScansPerTurn}");

        // 3. Actualizar estado local
        UpdateCurrentHand();
        UpdateDiscardButton();

        // 4. Reconfigurar botones (ocultar escaneo si llegó al límite o si tiene 4 cartas)
        ConfigureTurnButtons();

        // 5. Sincronizar con Firebase si es multijugador
        if (isMultiplayer && networkManager != null)
        {
            Debug.Log("[GameManager] 📡 Sincronizando escaneo con Firebase...");
            await networkManager.SendCardScan(scannedCard.id);
        }

        Debug.Log($"[GameManager] ✅ Carta '{scannedCard.displayName}' procesada correctamente");
        Debug.Log($"[GameManager] 📊 Total de cartas en mano: {fusionManager?.GetHandCount() ?? 0}");
    }

    private IEnumerator SimulateCardScan()
    {
        yield return new WaitForSeconds(2f);

        if (scanPromptPanel) scanPromptPanel.SetActive(false);

        CardData randomCard = GetRandomCard();
        if (randomCard != null)
        {
            AddCardToHand(randomCard);
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
            fusionManager.AddCardToHand(cardData);
            UpdateCurrentHand();
            UpdateDiscardButton();
        }
    }

    private CardData GetRandomCard()
    {
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
        EnableDiscardMode();
    }

    private void EnableDiscardMode()
    {
        foreach (var card in currentHand)
        {
            if (card != null)
            {
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

            if (currentHand.Count <= maxHandSize)
            {
                DisableDiscardMode();
            }

            // 🔥 NUEVO: Reconfigurar botones después de descartar
            ConfigureTurnButtons();
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
        if (!isMyTurn)
        {
            ShowWarning("No es tu turno.");
            return;
        }

        UpdateCurrentHand();

        // 🔥 MODIFICADO: Debe terminar con EXACTAMENTE 3 cartas
        if (currentHand.Count != maxHandSize)
        {
            ShowWarning($"Debes terminar tu turno con exactamente {maxHandSize} cartas. Tienes {currentHand.Count}.");
            return;
        }

        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }

        if (isMultiplayer && networkManager != null)
        {
            await networkManager.SendEndTurn();
        }

        EndCurrentTurn();
    }

    private void OnNetworkTurnTimerUpdated(float timeRemaining)
    {
        if (!isMultiplayer || isMyTurn) return;

        currentTurnTime = timeRemaining;

        if (timerText)
        {
            int minutes = Mathf.FloorToInt(currentTurnTime / 60);
            int seconds = Mathf.FloorToInt(currentTurnTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";

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

    private float lastSyncTime = 0f;
    private const float SYNC_INTERVAL = 1f;

    private void UpdateTimer()
    {
        currentTurnTime -= Time.deltaTime;

        if (timerText)
        {
            int minutes = Mathf.FloorToInt(currentTurnTime / 60);
            int seconds = Mathf.FloorToInt(currentTurnTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";

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

        if (isMyTurn && isMultiplayer && networkManager != null)
        {
            lastSyncTime += Time.deltaTime;
            if (lastSyncTime >= SYNC_INTERVAL)
            {
                lastSyncTime = 0f;
                _ = networkManager.SendTurnTimer(currentTurnTime);
            }
        }

        if (currentTurnTime <= 0)
        {
            OnTimeOut();
        }
    }

    private void OnTimeOut()
    {
        Debug.Log("[GameManager] ¡Tiempo agotado!");

        if (SFXManager.Instance != null)
            SFXManager.Instance.StopTimerLoop();

        if (!isMyTurn && isMultiplayer)
        {
            Debug.LogWarning("[GameManager] Timeout ignorado: no es mi turno");
            return;
        }

        // 🔥 MODIFICADO: Descartar hasta tener EXACTAMENTE 3 cartas
        while (currentHand.Count > maxHandSize)
        {
            int randomIndex = Random.Range(0, currentHand.Count);
            DiscardCard(currentHand[randomIndex]);
        }

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

        if (fusionManager != null)
        {
            fusionManager.SetInteractionEnabled(false);
        }

        // 🔥 NUEVO: Ocultar contadores al terminar turno
        if (scansCountText) scansCountText.gameObject.SetActive(false);
        if (fusionsCountText) fusionsCountText.gameObject.SetActive(false);

        currentPlayerIndex++;

        if (currentPlayerIndex >= playerNames.Count)
        {
            currentPlayerIndex = 0;
            currentRound++;

            if (isMultiplayer && networkManager != null && networkManager.isHost)
            {
                _ = SyncRoundToFirebase();
            }

            Debug.Log($"[GameManager] 🎉 Nueva Ronda: {currentRound}");
        }

        if (isMultiplayer && networkManager != null && networkManager.isHost)
        {
            _ = SyncTurnToFirebase();
        }

        StartCoroutine(DelayedNextTurn());
    }

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
        if (fusionManager)
        {
            currentHand = fusionManager.GetCurrentHand();
        }
    }

    private void ShowWarning(string message)
    {
        Debug.LogWarning($"[GameManager] {message}");
    }

    // 🔥 MODIFICADO: Incrementar contador de fusiones
    public async void OnCardFused()
    {
        fusionsUsedThisTurn++;
        UpdateTurnCounters();

        Debug.Log($"[GameManager] ✅ Fusión completada ({fusionsUsedThisTurn}/{maxFusionsPerTurn})");

        UpdateCurrentHand();
        UpdateDiscardButton();
        ConfigureTurnButtons();

        // 🔥 NUEVO: Verificar si la última fusión resultó en una carta ganadora
        if (fusionManager != null && victoryManager != null && !gameEnded)
        {
            CardData lastFusionResult = fusionManager.GetLastFusionResult();

            if (lastFusionResult != null && victoryManager.IsWinningCard(lastFusionResult.id))
            {
                Debug.Log($"[GameManager] 🏆 ¡CARTA GANADORA DETECTADA! {lastFusionResult.displayName}");

                // Marcar juego como terminado
                gameEnded = true;

                // Detener el timer
                isTurnActive = false;
                if (SFXManager.Instance != null)
                    SFXManager.Instance.StopTimerLoop();

                // Deshabilitar interacción
                if (fusionManager != null)
                {
                    fusionManager.SetInteractionEnabled(false);
                }

                // Esperar un momento para que el jugador vea la carta
                await Task.Delay(1500);

                // Mostrar panel de victoria para el jugador actual
                if (isMyTurn || !isMultiplayer)
                {
                    victoryManager.ShowVictoryPanel(lastFusionResult);
                }

                // Notificar a otros jugadores en multijugador
                if (isMultiplayer && networkManager != null)
                {
                    await networkManager.SendGameEnd(lastFusionResult.id);
                }

                return; // Salir para evitar procesamiento adicional
            }
        }

        // Continuar con la sincronización normal si no es carta ganadora
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

                var result = fusionManager.GetLastFusionResult();
                if (result != null)
                {
                    await networkManager.SendCardFusion(cardIds, result.id);
                }
            }
        }
    }

    private void OnNetworkGameStateUpdated(GameStateData gameState)
    {
        if (!isMultiplayer) return;

        if (currentPhase == GamePhase.Preparation)
        {
            Debug.Log($"[GameManager] ⏸️ Evento de gameState ignorado (estamos en preparación)");
            return;
        }

        Debug.Log($"[GameManager] Estado de juego actualizado: Turno={gameState.currentTurn}, Ronda={gameState.currentRound}");

        bool turnChanged = (currentPlayerIndex != gameState.currentTurn);
        bool roundChanged = (currentRound != gameState.currentRound);

        currentRound = gameState.currentRound;
        currentPlayerIndex = gameState.currentTurn;

        if (roundText) roundText.text = $"Ronda {currentRound}";

        if (networkManager != null)
        {
            isMyTurn = (currentPlayerIndex == networkManager.playerNumber);
        }

        if (turnChanged)
        {
            Debug.Log("[GameManager] 🔄 Cambio de turno detectado desde Firebase");

            StopAllCoroutines();

            StartPlayerTurn();
        }
        else
        {
            ConfigureTurnButtons();
        }
    }

    public delegate void OnCardScannedDelegate(CardData card, int playerIndex);
    public static event OnCardScannedDelegate OnCardScannedEvent;

    private void LogPlayerAction(string action)
    {
        Debug.Log($"[GameLog] {action}");
    }
}