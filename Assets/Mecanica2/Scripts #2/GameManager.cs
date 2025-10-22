using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        }
    }

    void OnDisable()
    {
        if (isMultiplayer && networkManager != null)
        {
            networkManager.OnGameStateUpdated -= OnNetworkGameStateUpdated;
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

        // Esperar un momento para que Firebase se sincronice
        await Task.Delay(500);

        // Cargar jugadores desde Firebase
        // Por ahora usamos playerNames basados en playerNumber
        playerNames.Clear();

        // El orden de turnos es por playerNumber (0, 1, 2, 3)
        for (int i = 0; i < 4; i++)
        {
            playerNames.Add($"Jugador {i + 1}");
        }

        // Determinar índice del jugador actual
        currentPlayerIndex = 0; // El host inicia en Firebase

        Debug.Log($"[GameManager] Juego multijugador iniciado. Soy jugador #{networkManager.playerNumber}");

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

        // Botón de finalizar turno solo activo si es mi turno
        if (endTurnButton)
        {
            endTurnButton.interactable = canInteract;
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

        // Validar que la mano tenga exactamente 3 cartas
        if (currentHand.Count != maxHandSize)
        {
            ShowWarning($"Debes tener exactamente {maxHandSize} cartas para terminar tu turno. Tienes {currentHand.Count}.");

            // Si tiene más de 3, forzar descarte
            if (currentHand.Count > maxHandSize)
            {
                EnableDiscardMode();
            }
            return;
        }

        // Sincronizar fin de turno con Firebase
        if (isMultiplayer && networkManager != null)
        {
            await networkManager.SendEndTurn();
        }

        EndCurrentTurn();
    }

    private void UpdateTimer()
    {
        // Solo contar tiempo si es mi turno
        if (!isMyTurn && isMultiplayer) return;

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

        // Si se acaba el tiempo
        if (currentTurnTime <= 0)
        {
            OnTimeOut();
        }
    }

    private void OnTimeOut()
    {
        Debug.Log("[GameManager] ¡Tiempo agotado!");

        // Si tiene más de 3 cartas, descartar al azar
        while (currentHand.Count > maxHandSize)
        {
            int randomIndex = Random.Range(0, currentHand.Count);
            DiscardCard(currentHand[randomIndex]);
        }

        EndCurrentTurn();
    }

    private void EndCurrentTurn()
    {
        Debug.Log($"[GameManager] Finalizando turno de {playerNames[currentPlayerIndex]}");

        isTurnActive = false;
        DisableDiscardMode();

        // Pasar al siguiente jugador
        currentPlayerIndex++;

        // Si todos los jugadores jugaron, nueva ronda
        if (currentPlayerIndex >= playerNames.Count)
        {
            currentPlayerIndex = 0;
            currentRound++;
            isFirstRoundComplete = true;
        }

        // Pequeña pausa antes del siguiente turno
        StartCoroutine(DelayedNextTurn());
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
    private void OnNetworkGameStateUpdated(GameStateData gameState)
    {
        if (!isMultiplayer) return;

        Debug.Log($"[GameManager] Estado de juego actualizado: Turno={gameState.currentTurn}, Ronda={gameState.currentRound}");

        // Actualizar el estado del juego local basado en Firebase
        currentRound = gameState.currentRound;
        currentPlayerIndex = gameState.currentTurn;

        // Actualizar UI
        if (roundText) roundText.text = $"Ronda {currentRound}";

        // Verificar si es mi turno
        if (networkManager != null)
        {
            isMyTurn = (currentPlayerIndex == networkManager.playerNumber);
        }

        // Reconfigurar botones
        ConfigureTurnButtons();
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