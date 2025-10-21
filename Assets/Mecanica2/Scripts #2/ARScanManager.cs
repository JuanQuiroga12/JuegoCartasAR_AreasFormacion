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
    [SerializeField] private float scanDuration = 2f; // Tiempo que debe mantenerse la carta visible
    [SerializeField] private CardDatabase cardDatabase;

    private bool isScanning = false;
    private float currentScanTime = 0f;
    private string lastTrackedImageName = "";
    private CardData pendingCard = null;
    private GameManager gameManager;
    private FusionManager fusionManager;

    // Diccionario para mapear nombres de imágenes AR a CardData
    private Dictionary<string, CardData> imageToCardMapping = new Dictionary<string, CardData>();

    // Reemplaza el uso de 'trackedImagesChanged' por 'trackablesChanged' y ajusta la firma del método manejador

    void Awake()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
        fusionManager = Object.FindFirstObjectByType<FusionManager>();

        // Usar el nuevo evento 'trackablesChanged' y la firma correcta del manejador
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }

        // Configurar botón de cancelar
        if (cancelScanButton != null)
        {
            cancelScanButton.onClick.AddListener(CancelScan);
        }

        InitializeImageMapping();
    }

    void OnDestroy()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        }
    }

    private void InitializeImageMapping()
    {
        // Mapear nombres de imágenes AR a CardData
        if (cardDatabase != null)
        {
            foreach (var card in cardDatabase.allCards)
            {
                if (card != null)
                {
                    // Usar el ID de la carta como clave para el mapeo
                    imageToCardMapping[card.id] = card;
                    Debug.Log($"[ARScanManager] Mapeando imagen '{card.id}' a carta '{card.displayName}'");
                }
            }
        }
    }

    public void StartScanning()
    {
        if (isScanning) return;

        Debug.Log("[ARScanManager] Iniciando escaneo AR");

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

        // Activar AR si no está activa
        EnableAR();
    }

    public void CancelScan()
    {
        Debug.Log("[ARScanManager] Cancelando escaneo");

        isScanning = false;
        currentScanTime = 0f;
        pendingCard = null;

        if (scanPanel != null)
        {
            scanPanel.SetActive(false);
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
        // Opcionalmente desactivar AR para ahorrar recursos
        // Comentado por si quieres mantener AR activa todo el tiempo
        /*
        if (arSession != null)
        {
            arSession.enabled = false;
        }
        
        if (trackedImageManager != null)
        {
            trackedImageManager.enabled = false;
        }
        */
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

    // Cambia la firma del método para usar el nuevo tipo de evento
    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        if (!isScanning) return;

        // Procesar nuevas imágenes detectadas
        foreach (var trackedImage in eventArgs.added)
        {
            HandleTrackedImage(trackedImage);
        }

        // Procesar imágenes actualizadas
        foreach (var trackedImage in eventArgs.updated)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                HandleTrackedImage(trackedImage);
            }
            else if (trackedImage.trackingState == TrackingState.Limited ||
                     trackedImage.trackingState == TrackingState.None)
            {
                // La imagen se perdió
                if (trackedImage.referenceImage.name == lastTrackedImageName)
                {
                    LostTracking();
                }
            }
        }
    }

    private void HandleTrackedImage(ARTrackedImage trackedImage)
    {
        if (!isScanning) return;

        string imageName = trackedImage.referenceImage.name;

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

                Debug.Log($"[ARScanManager] Carta detectada: {card.displayName}");

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
                Debug.LogWarning($"[ARScanManager] Imagen detectada '{imageName}' no tiene carta asociada");

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

        Debug.Log("[ARScanManager] Se perdió el tracking de la carta");

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

        Debug.Log($"[ARScanManager] ¡Escaneo completado! Carta: {pendingCard.displayName}");

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

        // Agregar la carta a la mano
        if (fusionManager != null)
        {
            fusionManager.AddCardToHand(pendingCard);
        }

        // Notificar al GameManager si es necesario
        if (gameManager != null)
        {
            gameManager.OnCardScanned(pendingCard);
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

    // Método público para verificar si una carta específica puede ser escaneada
    public bool CanScanCard(string cardId)
    {
        // Aquí podrías agregar lógica para evitar escanear cartas duplicadas
        // o cartas que ya están en la mano
        return imageToCardMapping.ContainsKey(cardId);
    }

    // Método para obtener estadísticas del escaneo
    public int GetScannableCardsCount()
    {
        return imageToCardMapping.Count;
    }
   
}