using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System.Collections;

namespace TCGARPrototype
{
    /// <summary>
    /// Gestiona la UI del prototipo AR y muestra información de debug
    /// </summary>
    public class ARUIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private GameObject scanningPanel;
        [SerializeField] private GameObject cardDetectedPanel;
        [SerializeField] private Button resetButton;
        [SerializeField] private Toggle debugToggle;

        [Header("AR References")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARTrackedImageManager trackedImageManager;
        [SerializeField] private ARCardTracker cardTracker;

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private float updateInterval = 0.5f;

        private int cardsDetected = 0;
        private float fps;
        private float deltaTime;
        private Coroutine updateCoroutine;

        void Start()
        {
            // Configurar eventos de UI
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetARSession);
            }

            if (debugToggle != null)
            {
                debugToggle.onValueChanged.AddListener(ToggleDebugInfo);
                debugToggle.isOn = showDebugInfo;
            }

            // Iniciar actualizaciones
            updateCoroutine = StartCoroutine(UpdateUICoroutine());

            // Mostrar panel de escaneo inicial
            ShowScanningUI();
        }

        void OnEnable()
        {
            if (trackedImageManager != null)
            {
                trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
            }
        }

        void OnDisable()
        {
            if (trackedImageManager != null)
            {
                trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
            }

            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }

        void Update()
        {
            // Calcular FPS
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fps = 1.0f / deltaTime;
        }

        private IEnumerator UpdateUICoroutine()
        {
            WaitForSeconds wait = new WaitForSeconds(updateInterval);

            while (true)
            {
                UpdateStatusText();

                if (showDebugInfo)
                {
                    UpdateDebugText();
                }

                yield return wait;
            }
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;

            string status = "Estado AR: ";

            if (ARSession.state == ARSessionState.SessionTracking)
            {
                status += "<color=green>Activo</color>\n";
                status += $"Cartas detectadas: {cardsDetected}";
            }
            else if (ARSession.state == ARSessionState.SessionInitializing)
            {
                status += "<color=yellow>Inicializando...</color>";
            }
            else
            {
                status += "<color=red>No disponible</color>";
            }

            statusText.text = status;
        }

        private void UpdateDebugText()
        {
            if (debugText == null) return;

            string debug = $"<b>Debug Info</b>\n";
            debug += $"FPS: {Mathf.Ceil(fps)}\n";
            debug += $"AR State: {ARSession.state}\n";
            debug += $"Tracking: {GetTrackingQuality()}\n";
            debug += $"Images Tracked: {cardsDetected}\n";
            debug += $"Camera Pos: {FormatVector3(Camera.main.transform.position)}\n";
            debug += $"Time: {Time.time:F1}s";

            debugText.text = debug;
            debugText.gameObject.SetActive(showDebugInfo);
        }

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            // Actualizar contador de cartas
            cardsDetected = eventArgs.added.Count + eventArgs.updated.Count;

            // Mostrar UI apropiada
            if (cardsDetected > 0)
            {
                ShowCardDetectedUI();
            }
            else
            {
                ShowScanningUI();
            }
        }

        private void ShowScanningUI()
        {
            if (scanningPanel != null)
            {
                scanningPanel.SetActive(true);
            }

            if (cardDetectedPanel != null)
            {
                cardDetectedPanel.SetActive(false);
            }
        }

        private void ShowCardDetectedUI()
        {
            if (scanningPanel != null)
            {
                scanningPanel.SetActive(false);
            }

            if (cardDetectedPanel != null)
            {
                cardDetectedPanel.SetActive(true);

                // Opcional: Animar la aparición
                StartCoroutine(AnimateCardDetectedPanel());
            }
        }

        private IEnumerator AnimateCardDetectedPanel()
        {
            if (cardDetectedPanel == null) yield break;

            CanvasGroup canvasGroup = cardDetectedPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = cardDetectedPanel.AddComponent<CanvasGroup>();
            }

            // Fade in
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;

            // Auto-hide después de 2 segundos
            yield return new WaitForSeconds(2f);

            // Fade out
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            cardDetectedPanel.SetActive(false);
        }

        private void ResetARSession()
        {
            // Limpiar animaciones
            if (cardTracker != null)
            {
                cardTracker.ClearAllAnimations();
            }

            // Reset AR Session
            if (arSession != null)
            {
                arSession.Reset();
            }

            cardsDetected = 0;
            ShowScanningUI();
        }

        private void ToggleDebugInfo(bool value)
        {
            showDebugInfo = value;

            if (debugText != null)
            {
                debugText.gameObject.SetActive(value);
            }
        }

        private string GetTrackingQuality()
        {
            // Aquí podrías implementar una evaluación más detallada
            if (ARSession.state == ARSessionState.SessionTracking)
            {
                return "<color=green>Buena</color>";
            }
            else if (ARSession.state == ARSessionState.SessionInitializing)
            {
                return "<color=yellow>Inicializando</color>";
            }
            else
            {
                return "<color=red>Sin tracking</color>";
            }
        }

        private string FormatVector3(Vector3 v)
        {
            return $"({v.x:F1}, {v.y:F1}, {v.z:F1})";
        }

        /// <summary>
        /// Muestra un mensaje temporal en pantalla
        /// </summary>
        public void ShowMessage(string message, float duration = 2f)
        {
            StartCoroutine(ShowMessageCoroutine(message, duration));
        }

        private IEnumerator ShowMessageCoroutine(string message, float duration)
        {
            if (statusText != null)
            {
                string originalText = statusText.text;
                statusText.text = message;
                yield return new WaitForSeconds(duration);
                statusText.text = originalText;
            }
        }

        /// <summary>
        /// Método público para actualizar el contador de cartas
        /// </summary>
        public void UpdateCardCount(int count)
        {
            cardsDetected = count;
        }
    }
}