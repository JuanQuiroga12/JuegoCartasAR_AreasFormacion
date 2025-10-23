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

    // 🔥 NUEVO: Referencia al MultipleImagesTrackingManager
    private MultipleImagesTrackingManager imageTrackingManager;

    private Dictionary<string, CardData> imageToCardMapping = new Dictionary<string, CardData>();

    void Awake()
    {
        Debug.Log("[ARScanManager] ⚙️ Awake iniciando...");

        // Buscar referencias si no están asignadas
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

        // 🔥 NUEVO: Suscribirse a eventos del MultipleImagesTrackingManager
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

        // 🔥 CRÍTICO: Inicializar el mapeo de imágenes
        Debug.Log("[ARScanManager] 🔄 Llamando InitializeImageMapping()...");
        InitializeImageMapping();
        Debug.Log("[ARScanManager] ✅ Awake completado");
    }


    private void InitializeImageMapping()
    {
        // 🔥 AGREGAR LOGS CRÍTICOS
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
                // 🔥 CORREGIDO: Usar arImageName en lugar de id
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

        // 🔥 MOSTRAR EL CONTENIDO FINAL DEL DICCIONARIO
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

        // Activar UI de escaneo
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

        // 🔥 NUEVO: Suscribirse a eventos del MultipleImagesTrackingManager
        if (imageTrackingManager != null)
        {
            imageTrackingManager.OnImageDetected += OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost += OnImageLostFromTracking;
        }
    }

    public void CancelScan()
    {
        Debug.Log("[ARScanManager] ❌ Cancelando escaneo");

        isScanning = false;
        currentScanTime = 0f;
        pendingCard = null;

        if (scanPanel != null)
        {
            scanPanel.SetActive(false);
        }

        // 🔥 NUEVO: Desuscribirse de eventos
        if (imageTrackingManager != null)
        {
            imageTrackingManager.OnImageDetected -= OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost -= OnImageLostFromTracking;
        }

        DisableAR();
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
        // Mantener AR activa (ya está comentado en tu código original)
    }

    void Update()
    {
        if (isScanning && pendingCard != null)
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

    // 🔥 NUEVO: Método que recibe notificaciones del MultipleImagesTrackingManager
    private void OnImageDetectedFromTracking(string imageName, ARTrackedImage trackedImage)
    {
        if (!isScanning) return;

        Debug.Log($"[ARScanManager] 🎯 Imagen detectada por tracker: {imageName}");

        HandleTrackedImage(imageName);
    }

    // 🔥 NUEVO: Método que recibe pérdida de tracking
    private void OnImageLostFromTracking(string imageName)
    {
        if (!isScanning) return;

        if (imageName == lastTrackedImageName)
        {
            Debug.Log($"[ARScanManager] ⚠️ Se perdió tracking de: {imageName}");
            LostTracking();
        }
    }

    // 🔥 MODIFICADO: Ahora recibe solo el nombre de la imagen
    private void HandleTrackedImage(string imageName)
    {
        if (!isScanning) return;

        // 🔥 DEBUG ULTRA DETALLADO
        Debug.Log($"═══════════════════════════════════════");
        Debug.Log($"[ARScanManager] 🔍 IMAGEN DETECTADA");
        Debug.Log($"[ARScanManager] Nombre recibido: '{imageName}'");
        Debug.Log($"[ARScanManager] Longitud del nombre: {imageName.Length}");
        
        Debug.Log($"[ARScanManager] 📋 CARTAS EN MAPPING:");
        foreach (var kvp in imageToCardMapping)
        {
            Debug.Log($"   Key: '{kvp.Key}' (len={kvp.Key.Length}) → {kvp.Value.displayName}");
            Debug.Log($"   Coincide? {kvp.Key == imageName}");
            Debug.Log($"   Coincide (IgnoreCase)? {string.Equals(kvp.Key, imageName, System.StringComparison.OrdinalIgnoreCase)}");
        }
        Debug.Log($"═══════════════════════════════════════");

        // Verificar si es una nueva carta o la misma
        if (imageName != lastTrackedImageName)
        {
            // Nueva carta detectada
            lastTrackedImageName = imageName;
            currentScanTime = 0f;

            // Buscar la CardData correspondiente
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
        if (!isScanning) return;

        Debug.Log("[ARScanManager] 🔄 Se perdió el tracking de la carta");

        // Reiniciar el escaneo
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
        if (pendingCard == null) return;

        Debug.Log($"[ARScanManager] 🎉 ¡Escaneo completado! Carta: {pendingCard.displayName}");

        isScanning = false;

        // Mostrar indicador de éxito
        if (scanSuccessIndicator != null)
        {
            scanSuccessIndicator.SetActive(true);
        }

        if (scanInstructionText != null)
        {
            scanInstructionText.text = $"¡{pendingCard.displayName} agregada a tu mano!";
        }

        // 🔥 IMPORTANTE: Agregar la carta a la mano
        if (fusionManager != null)
        {
            Debug.Log($"[ARScanManager] 📥 Agregando carta a la mano: {pendingCard.displayName}");
            fusionManager.AddCardToHand(pendingCard);
        }
        else
        {
            Debug.LogError("[ARScanManager] ❌ FusionManager es null, no se puede agregar la carta");
        }

        // Notificar al GameManager
        if (gameManager != null)
        {
            gameManager.OnCardScanned(pendingCard);
        }
        else
        {
            Debug.LogError("[ARScanManager] ❌ GameManager es null");
        }

        // 🔥 NUEVO: Desuscribirse de eventos después de completar
        if (imageTrackingManager != null)
        {
            imageTrackingManager.OnImageDetected -= OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost -= OnImageLostFromTracking;
        }

        // Cerrar el panel después de un delay
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
        // 🔥 NUEVO: Limpiar suscripciones al destruir
        if (imageTrackingManager != null)
        {
            imageTrackingManager.OnImageDetected -= OnImageDetectedFromTracking;
            imageTrackingManager.OnImageLost -= OnImageLostFromTracking;
        }
    }
}