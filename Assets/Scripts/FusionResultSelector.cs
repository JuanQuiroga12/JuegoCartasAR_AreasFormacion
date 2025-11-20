using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FusionResultSelector : MonoBehaviour
{
    [Header("UI Referencias")]
    [SerializeField] private GameObject selectorPanel;
    [SerializeField] private ScrollRect scrollRect; // 🔥 NUEVO: ScrollRect para scrolling
    [SerializeField] private Transform optionsContainer;
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button closeButton;

    private List<CardData> availableResults;
    private System.Action<CardData> onResultSelected;
    private List<GameObject> spawnedOptions = new List<GameObject>();

    void Awake()
    {
        Debug.Log("[FusionResultSelector] ⚙️ Awake iniciando...");

        if (selectorPanel == null)
        {
            Debug.LogWarning("[FusionResultSelector] selectorPanel no asignado, usando this.gameObject");
            selectorPanel = this.gameObject;
        }

        Debug.Log($"[FusionResultSelector] ✅ selectorPanel: {selectorPanel.name}");

        if (scrollRect == null)
        {
            Debug.LogError("[FusionResultSelector] ❌ scrollRect es NULL!");
        }
        else
        {
            Debug.Log($"[FusionResultSelector] ✅ scrollRect: {scrollRect.gameObject.name}");
        }

        if (optionsContainer == null)
        {
            Debug.LogError("[FusionResultSelector] ❌ optionsContainer es NULL!");
        }
        else
        {
            Debug.Log($"[FusionResultSelector] ✅ optionsContainer: {optionsContainer.name}");
        }

        if (cardViewPrefab == null)
        {
            Debug.LogError("[FusionResultSelector] ❌ cardViewPrefab es NULL!");
        }
        else
        {
            Debug.Log($"[FusionResultSelector] ✅ cardViewPrefab: {cardViewPrefab.name}");
        }

        if (selectorPanel != null)
        {
            selectorPanel.SetActive(false);
            Debug.Log("[FusionResultSelector] Panel desactivado inicialmente");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CancelSelection);
            Debug.Log("[FusionResultSelector] ✅ Botón cerrar configurado");
        }
    }

    public void ShowOptions(List<CardData> results, System.Action<CardData> callback)
    {
        Debug.Log("========================================");
        Debug.Log($"[FusionResultSelector] 🎯 ShowOptions LLAMADO con {results?.Count ?? 0} resultados");
        Debug.Log("========================================");

        if (results == null || results.Count == 0)
        {
            Debug.LogWarning("[FusionResultSelector] ⚠️ No hay resultados para mostrar");
            return;
        }

        if (selectorPanel == null)
        {
            Debug.LogError("[FusionResultSelector] ❌ CRÍTICO: selectorPanel es NULL en ShowOptions!");
            return;
        }

        if (optionsContainer == null)
        {
            Debug.LogError("[FusionResultSelector] ❌ CRÍTICO: optionsContainer es NULL!");
            return;
        }

        if (cardViewPrefab == null)
        {
            Debug.LogError("[FusionResultSelector] ❌ CRÍTICO: cardViewPrefab es NULL!");
            return;
        }

        availableResults = results;
        onResultSelected = callback;

        ClearOptions();
        Debug.Log("[FusionResultSelector] 🧹 Opciones previas limpiadas");

        Debug.Log($"[FusionResultSelector] 🔍 Estado del panel ANTES de activar:");
        Debug.Log($"   - activeSelf: {selectorPanel.activeSelf}");
        Debug.Log($"   - activeInHierarchy: {selectorPanel.activeInHierarchy}");

        selectorPanel.SetActive(true);

        Debug.Log($"[FusionResultSelector] 🔍 Estado del panel DESPUÉS de activar:");
        Debug.Log($"   - activeSelf: {selectorPanel.activeSelf}");
        Debug.Log($"   - activeInHierarchy: {selectorPanel.activeInHierarchy}");

        Canvas canvas = selectorPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[FusionResultSelector] 🖼️ Canvas encontrado: {canvas.name}");
            Debug.Log($"   - renderMode: {canvas.renderMode}");
            Debug.Log($"   - sortingOrder: {canvas.sortingOrder}");
        }
        else
        {
            Debug.LogError("[FusionResultSelector] ❌ No se encontró Canvas padre!");
        }

        var canvasGroup = selectorPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log("[FusionResultSelector] ✅ CanvasGroup configurado");
        }

        if (instructionText != null)
        {
            instructionText.text = $"Selecciona una de las {results.Count} cartas resultantes:";
            Debug.Log($"[FusionResultSelector] ✅ Texto de instrucciones actualizado");
        }

        Debug.Log($"[FusionResultSelector] 🃏 Creando {results.Count} opciones de cartas...");
        for (int i = 0; i < results.Count; i++)
        {
            Debug.Log($"[FusionResultSelector]   [{i}] Creando carta: {results[i].displayName}");
            CreateOptionCard(results[i]);
        }

        // 🔥 NUEVO: Resetear posición del scroll al inicio
        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
            Debug.Log("[FusionResultSelector] ✅ Scroll reseteado al inicio");
        }

        Debug.Log("========================================");
        Debug.Log($"[FusionResultSelector] ✅ PANEL MOSTRADO - {results.Count} opciones creadas");
        Debug.Log("========================================");

        Canvas.ForceUpdateCanvases();
        Debug.Log("[FusionResultSelector] 🔄 Canvas.ForceUpdateCanvases() llamado");
    }

    private void CreateOptionCard(CardData cardData)
    {
        if (cardViewPrefab == null || optionsContainer == null)
        {
            Debug.LogError("[FusionResultSelector] ❌ CardViewPrefab o OptionsContainer es NULL");
            return;
        }

        Debug.Log($"[FusionResultSelector] 🔨 Instanciando carta: {cardData.displayName}");

        var cardView = Instantiate(cardViewPrefab, optionsContainer);

        if (cardView == null)
        {
            Debug.LogError("[FusionResultSelector] ❌ Falló la instanciación!");
            return;
        }

        Debug.Log($"[FusionResultSelector] ✅ Carta instanciada: {cardView.name}");

        cardView.Setup(cardData, null);
        cardView.gameObject.SetActive(true);

        var button = cardView.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnCardSelected(cardData));
            Debug.Log($"[FusionResultSelector] ✅ Listener agregado");
        }
        else
        {
            Debug.LogWarning($"[FusionResultSelector] ⚠️ No se encontró Button en {cardView.name}");
        }

        spawnedOptions.Add(cardView.gameObject);
        Debug.Log($"[FusionResultSelector] ✅ Total opciones: {spawnedOptions.Count}");
    }

    private void OnCardSelected(CardData selectedCard)
    {
        Debug.Log($"[FusionResultSelector] 🎯 Carta seleccionada: {selectedCard.displayName}");

        if (selectorPanel != null)
        {
            selectorPanel.SetActive(false);
            Debug.Log("[FusionResultSelector] Panel ocultado");
        }

        if (onResultSelected != null)
        {
            Debug.Log("[FusionResultSelector] 📞 Invocando callback...");
            onResultSelected.Invoke(selectedCard);
        }
        else
        {
            Debug.LogWarning("[FusionResultSelector] ⚠️ onResultSelected es NULL!");
        }

        ClearOptions();
    }

    private void CancelSelection()
    {
        Debug.Log("[FusionResultSelector] ❌ Selección cancelada");

        if (selectorPanel != null)
        {
            selectorPanel.SetActive(false);
        }

        onResultSelected?.Invoke(null);
        ClearOptions();
    }

    private void ClearOptions()
    {
        Debug.Log($"[FusionResultSelector] 🧹 Limpiando {spawnedOptions.Count} opciones");

        foreach (var option in spawnedOptions)
        {
            if (option != null)
            {
                Destroy(option);
            }
        }

        spawnedOptions.Clear();
        Debug.Log("[FusionResultSelector] ✅ Opciones limpiadas");
    }

    void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CancelSelection);
        }
    }
}