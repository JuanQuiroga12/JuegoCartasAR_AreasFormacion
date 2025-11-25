using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using TMPro;

public class ARScanManager : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private ARSession arSession;

    [Header("UI Referencias")]
    [SerializeField] private GameObject scanPanel;
    [SerializeField] private TextMeshProUGUI scanInstructionText;
    [SerializeField] private Button cancelScanButton;
    [SerializeField] private Image scanProgressBar;
    [SerializeField] private GameObject scanSuccessIndicator;

    [Header("Configuración")]
    [SerializeField] private float scanDuration = 2f;
    [SerializeField] private CardDatabase cardDatabase;

    private bool isScanning = false;
    private float currentScanTime = 0f;
    private string lastTrackedImageName = "";
    private CardData pendingCard = null;
    private GameManager gameManager;
    private FusionManager fusionManager;

    private MultipleImagesTrackingManager imageTrackingManager;

    private Dictionary<string, CardData> imageToCardMapping = new Dictionary<string, CardData>();

    // 🔥 NUEVO: Flag para prevenir procesamiento duplicado
    private bool isProcessingCard = false;

    void Awake()
    {
        Debug.Log("[ARScanManager] ⚙️ Awake iniciando...");

        if (arSession == null)
        {
            arSession = Object.FindFirstObjectByType<ARSession>();
            Debug.Log($"[ARScanManager] ARSession encontrado: {arSession != null}");
        }

        if (trackedImageManager == null)
        {
            trackedImageManager = Object.FindFirstObjectByType<ARTrackedImageManager>();
            Debug.Log($"[ARScanManager] ARTrackedImageManager encontrado: {trackedImageManager != null}");
        }

        if (fusionManager == null)
        {
            fusionManager = Object.FindFirstObjectByType<FusionManager>();
            Debug.Log($"[ARScanManager] FusionManager encontrado: {fusionManager != null}");
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
            Debug.Log($"[ARScanManager] GameManager encontrado: {gameManager != null}");
        }

        imageTrackingManager = Object.FindFirstObjectByType<MultipleImagesTrackingManager>();
        if (imageTrackingManager != null)
        {
            Debug.Log("[ARScanManager] ✅ MultipleImagesTrackingManager encontrado, suscribiendo eventos");
            imageTrackingManager.OnImageDetected += OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost += OnImageLostFromTracking;
        }
        else
        {
            Debug.LogError("[ARScanManager] ❌ MultipleImagesTrackingManager NO encontrado!");
        }

        // 🔥 CRÍTICO: Conectar el botón de cancelar
        if (cancelScanButton != null)
        {
            cancelScanButton.onClick.RemoveAllListeners(); // Limpiar listeners previos
            cancelScanButton.onClick.AddListener(CancelScan);
            Debug.Log("[ARScanManager] ✅ Botón cancelar conectado");
        }
        else
        {
            Debug.LogError("[ARScanManager] ❌ cancelScanButton es NULL! No se puede conectar el listener");
        }

        Debug.Log("[ARScanManager] 🔄 Llamando InitializeImageMapping()...");
        InitializeImageMapping();
        Debug.Log("[ARScanManager] ✅ Awake completado");
    }


    private void InitializeImageMapping()
    {
        Debug.Log($"[ARScanManager] 🔧 INICIALIZANDO MAPEO");

        if (cardDatabase == null)
        {
            Debug.LogError("[ARScanManager] ❌❌❌ CardDatabase es NULL! No se puede mapear ninguna carta!");
            return;
        }

        Debug.Log($"[ARScanManager] CardDatabase encontrado: {cardDatabase.name}");
        Debug.Log($"[ARScanManager] Total de cartas en database: {cardDatabase.allCards.Count}");

        if (cardDatabase.allCards.Count == 0)
        {
            Debug.LogError("[ARScanManager] ❌❌❌ CardDatabase está VACÍO! No hay cartas para mapear!");
            return;
        }

        foreach (var card in cardDatabase.allCards)
        {
            if (card != null)
            {
                string mapKey = string.IsNullOrEmpty(card.arImageName) ? card.id : card.arImageName;
                imageToCardMapping[mapKey] = card;

                Debug.Log($"[ARScanManager] ✅ Mapeado:");
                Debug.Log($"   - ID: '{card.id}'");
                Debug.Log($"   - AR Name: '{card.arImageName}'");
                Debug.Log($"   - Map Key usado: '{mapKey}'");
                Debug.Log($"   - Display Name: '{card.displayName}'");
            }
            else
            {
                Debug.LogWarning("[ARScanManager] ⚠️ Carta NULL encontrada en allCards");
            }
        }

        Debug.Log($"[ARScanManager] 📊 Total mapeado: {imageToCardMapping.Count} cartas");

        Debug.Log("[ARScanManager] 📋 Contenido final de imageToCardMapping:");
        foreach (var kvp in imageToCardMapping)
        {
            Debug.Log($"   '{kvp.Key}' → {kvp.Value.displayName}");
        }
    }

    public void StartScanning()
    {
        if (isScanning) return;

        Debug.Log("[ARScanManager] 🔍 Iniciando escaneo AR");

        isScanning = true;
        currentScanTime = 0f;
        pendingCard = null;
        lastTrackedImageName = "";
        isProcessingCard = false; // 🔥 RESETEAR FLAG

        if (scanPanel != null)
        {
            scanPanel.SetActive(true);
        }

        if (scanInstructionText != null)
        {
            scanInstructionText.text = "Apunta a una carta para escanearla...";
        }

        if (scanProgressBar != null)
        {
            scanProgressBar.fillAmount = 0f;
            scanProgressBar.gameObject.SetActive(false);
        }

        if (scanSuccessIndicator != null)
        {
            scanSuccessIndicator.SetActive(false);
        }

        EnableAR();

        if (imageTrackingManager != null)
        {
            imageTrackingManager.OnImageDetected += OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost += OnImageLostFromTracking;
        }
    }

    public void CancelScan()
    {
        Debug.Log("[ARScanManager] ========================================");
        Debug.Log("[ARScanManager] 🚫 CancelScan() LLAMADO");
        Debug.Log("[ARScanManager] ========================================");

        if (!isScanning)
        {
            Debug.LogWarning("[ARScanManager] No hay escaneo activo para cancelar");

            // 🔥 FORZAR CIERRE DEL PANEL aunque no esté escaneando
            if (scanPanel != null && scanPanel.activeSelf)
            {
                Debug.Log("[ARScanManager] 🔧 Forzando cierre de panel activo");
                scanPanel.SetActive(false);
            }

            return;
        }

        Debug.Log("[ARScanManager] 🔄 Deteniendo escaneo activo...");

        // Detener escaneo
        isScanning = false;
        currentScanTime = 0f;
        pendingCard = null;
        isProcessingCard = false;

        // Resetear UI
        if (scanProgressBar != null)
        {
            scanProgressBar.fillAmount = 0f;
            scanProgressBar.gameObject.SetActive(false);
            Debug.Log("[ARScanManager] ✓ Barra de progreso reseteada");
        }

        if (scanInstructionText != null)
        {
            scanInstructionText.text = "Escaneo cancelado";
            Debug.Log("[ARScanManager] ✓ Texto de instrucciones actualizado");
        }

        if (scanSuccessIndicator != null)
        {
            scanSuccessIndicator.SetActive(false);
            Debug.Log("[ARScanManager] ✓ Indicador de éxito ocultado");
        }

        // 🔥 CRÍTICO: Cerrar el panel
        if (scanPanel != null)
        {
            Debug.Log("[ARScanManager] 🔒 Cerrando panel de escaneo...");
            scanPanel.SetActive(false);
            Debug.Log("[ARScanManager] ✅ Panel cerrado correctamente");
        }
        else
        {
            Debug.LogError("[ARScanManager] ❌ scanPanel es NULL, no se puede cerrar!");
        }

        // Desuscribir eventos
        if (imageTrackingManager != null)
        {
            imageTrackingManager.OnImageDetected -= OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost -= OnImageLostFromTracking;
            Debug.Log("[ARScanManager] ✓ Eventos de tracking desuscritos");
        }

        Debug.Log("[ARScanManager] ========================================");
        Debug.Log("[ARScanManager] ✅ Escaneo cancelado exitosamente");
        Debug.Log("[ARScanManager] ========================================");
    }

    private void EnableAR()
    {
        if (arSession != null && !arSession.enabled)
        {
            arSession.enabled = true;
        }

        if (trackedImageManager != null && !trackedImageManager.enabled)
        {
            trackedImageManager.enabled = true;
        }
    }

    private void DisableAR()
    {
        // Mantener AR activa
    }

    void Update()
    {
        if (isScanning && pendingCard != null && !isProcessingCard) // 🔥 VERIFICAR FLAG
        {
            UpdateScanProgress();
        }
    }

    private void UpdateScanProgress()
    {
        currentScanTime += Time.deltaTime;

        float progress = currentScanTime / scanDuration;

        if (scanProgressBar != null)
        {
            scanProgressBar.fillAmount = progress;
        }

        if (currentScanTime >= scanDuration)
        {
            CompleteScan();
        }
    }

    private void OnImageDetectedFromTracking(string imageName, ARTrackedImage trackedImage)
    {
        if (!isScanning || isProcessingCard) return; // 🔥 VERIFICAR FLAG

        Debug.Log($"[ARScanManager] 🎯 Imagen detectada por tracker: {imageName}");

        HandleTrackedImage(imageName);
    }

    private void OnImageLostFromTracking(string imageName)
    {
        if (!isScanning || isProcessingCard) return; // 🔥 VERIFICAR FLAG

        if (imageName == lastTrackedImageName)
        {
            Debug.Log($"[ARScanManager] ⚠️ Se perdió tracking de: {imageName}");
            LostTracking();
        }
    }

    private void HandleTrackedImage(string imageName)
    {
        if (!isScanning || isProcessingCard) return; // 🔥 VERIFICAR FLAG

        // Verificar si es una nueva carta o la misma
        if (imageName != lastTrackedImageName)
        {
            lastTrackedImageName = imageName;
            currentScanTime = 0f;

            if (imageToCardMapping.TryGetValue(imageName, out CardData card))
            {
                pendingCard = card;

                Debug.Log($"[ARScanManager] ✅ Carta detectada: {card.displayName}");

                if (scanInstructionText != null)
                {
                    scanInstructionText.text = $"Escaneando: {card.displayName}\nMantén la carta visible...";
                }

                if (scanProgressBar != null)
                {
                    scanProgressBar.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning($"[ARScanManager] ⚠️ Imagen detectada '{imageName}' no tiene carta asociada");

                if (scanInstructionText != null)
                {
                    scanInstructionText.text = "Carta no reconocida. Intenta con otra carta.";
                }
            }
        }
    }

    private void LostTracking()
    {
        if (!isScanning || isProcessingCard) return; // 🔥 VERIFICAR FLAG

        Debug.Log("[ARScanManager] 🔄 Se perdió el tracking de la carta");

        currentScanTime = 0f;

        if (scanProgressBar != null)
        {
            scanProgressBar.fillAmount = 0f;
            scanProgressBar.gameObject.SetActive(false);
        }

        if (scanInstructionText != null)
        {
            scanInstructionText.text = "Carta perdida. Vuelve a apuntar a la carta...";
        }
    }

    private void CompleteScan()
    {
        if (pendingCard == null || isProcessingCard) return;

        isProcessingCard = true;
        isScanning = false;

        Debug.Log($"[ARScanManager] 🎉 ¡Escaneo completado! Carta: {pendingCard.displayName}");

        if (scanSuccessIndicator != null)
        {
            scanSuccessIndicator.SetActive(true);
        }

        if (scanInstructionText != null)
        {
            scanInstructionText.text = $"¡{pendingCard.displayName} agregada a tu mano!";
        }

        // 🔥 CRÍTICO: Notificar al GameManager
        if (gameManager != null)
        {
            Debug.Log($"[ARScanManager] 📤 Notificando GameManager: {pendingCard.displayName}");
            gameManager.OnCardScanned(pendingCard);
            Debug.Log($"[ARScanManager] ✅ GameManager.OnCardScanned() ejecutado correctamente");
        }
        else
        {
            Debug.LogError("[ARScanManager] ❌ GameManager es null, NO SE PUEDE AGREGAR LA CARTA");
        }

        // Desuscribir eventos
        if (imageTrackingManager != null)
        {
            imageTrackingManager.OnImageDetected -= OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost -= OnImageLostFromTracking;
        }

        StartCoroutine(CloseScanPanelDelayed());
    }

    private IEnumerator CloseScanPanelDelayed()
    {
        yield return new WaitForSeconds(1.5f);

        if (scanPanel != null)
        {
            scanPanel.SetActive(false);
        }

        // Reiniciar variables
        pendingCard = null;
        lastTrackedImageName = "";
        currentScanTime = 0f;
        isProcessingCard = false; // 🔥 RESETEAR FLAG AL FINAL
    }

    public bool CanScanCard(string cardId)
    {
        return imageToCardMapping.ContainsKey(cardId);
    }

    public int GetScannableCardsCount()
    {
        return imageToCardMapping.Count;
    }

    void OnDestroy()
    {
        if (imageTrackingManager != null)
        {
            imageTrackingManager.OnImageDetected -= OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost -= OnImageLostFromTracking;
        }
    }
}