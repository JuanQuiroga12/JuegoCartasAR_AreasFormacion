using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FusionManager : MonoBehaviour
{
    [Header("DB & Prefabs")]
    public FusionDatabase fusionDatabase;
    public CardView cardViewPrefab;

    [Header("UI Slots")]
    public Transform handPanel;
    public Transform resultPanel;
    public Button fusionButton;

    [Header("🔥 Selector de Resultados")]
    public FusionResultSelector resultSelector; // ⬅️ DEBE estar asignado desde el Inspector

    [Header("Configuración")]
    public int maxHandSize = 3;
    public int maxSelectionSize = 2;

    private readonly List<CardView> _handViews = new();
    private readonly List<CardView> _selectionOrder = new();
    private readonly HashSet<CardView> _selected = new();

    private GameManager gameManager;
    private bool isDiscardMode = false;
    private CardData lastFusionResult = null;
    private bool canInteract = true;

    // 🔥 NUEVO: Flag para evitar múltiples fusiones simultáneas
    private bool isFusing = false;

    void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();

        // 🔥 CRÍTICO: Validar que resultSelector esté asignado MANUALMENTE en el Inspector
        if (resultSelector == null)
        {
            Debug.LogError("[FusionManager] ❌❌❌ FusionResultSelector NO ESTÁ ASIGNADO EN EL INSPECTOR!");
            Debug.LogError("[FusionManager] Por favor, arrastra el FusionResultSelectorPanel al campo 'Result Selector' en el Inspector");

            // 🔥 INTENTO DE BÚSQUEDA FORZADA (incluyendo objetos inactivos)
            resultSelector = Object.FindFirstObjectByType<FusionResultSelector>(FindObjectsInactive.Include);

            if (resultSelector != null)
            {
                Debug.LogWarning($"[FusionManager] ⚠️ Encontrado mediante búsqueda forzada: {resultSelector.gameObject.name}");
                Debug.LogWarning("[FusionManager] ⚠️ IMPORTANTE: Asigna esto manualmente en el Inspector para evitar este warning");
            }
            else
            {
                Debug.LogError("[FusionManager] ❌ NO SE ENCONTRÓ FusionResultSelector ni siquiera en objetos inactivos!");
            }
        }
        else
        {
            Debug.Log($"[FusionManager] ✅ FusionResultSelector asignado correctamente: {resultSelector.gameObject.name}");
        }

        RefreshFusionButton();
        ClearResultPanel();

        Debug.Log("[FusionManager] ✅ FusionManager iniciado");
    }

    public void SetInteractionEnabled(bool enabled)
    {
        canInteract = enabled;
        RefreshFusionButton();

        if (!enabled)
        {
            foreach (var card in _selected.ToList())
            {
                card.SetSelectedFromManager(false);
            }
            _selected.Clear();
            _selectionOrder.Clear();
        }
    }

    private CardView CreateCardView(CardData cardData)
    {
        var v = Instantiate(cardViewPrefab, handPanel);
        v.Setup(cardData, this);
        _handViews.Add(v);
        return v;
    }

    public void AddCardToHand(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("[FusionManager] ⚠️ Intentando agregar carta NULL");
            return;
        }

        if (_handViews.Count >= 4)
        {
            Debug.LogWarning("[FusionManager] La mano ya tiene el máximo de cartas permitidas");
            return;
        }

        var newCard = CreateCardView(cardData);
        UpdateHandLayout();

        Debug.Log($"[FusionManager] ✅ Carta agregada: {cardData.displayName}. Total en mano: {_handViews.Count}");
    }

    public void RemoveCardFromHand(CardView card)
    {
        if (_handViews.Contains(card))
        {
            _handViews.Remove(card);

            if (_selected.Contains(card))
            {
                _selected.Remove(card);
                _selectionOrder.Remove(card);
            }

            Destroy(card.gameObject);
            UpdateHandLayout();
            RefreshFusionButton();

            Debug.Log($"[FusionManager] Carta removida. Total en mano: {_handViews.Count}");
        }
    }

    public List<CardView> GetCurrentHand()
    {
        return new List<CardView>(_handViews);
    }

    public int GetHandCount()
    {
        return _handViews.Count;
    }

    public void NotifySelectionChanged(CardView view, bool selected)
    {
        if (!canInteract)
        {
            Debug.LogWarning("[FusionManager] No puedes seleccionar cartas cuando no es tu turno");
            view.SetSelectedFromManager(false);
            return;
        }

        if (isDiscardMode)
        {
            if (selected && gameManager != null)
            {
                gameManager.DiscardCard(view);
                isDiscardMode = false;
            }
            return;
        }

        if (selected)
        {
            if (!_selected.Contains(view))
            {
                _selected.Add(view);
                _selectionOrder.Add(view);

                if (_selected.Count > maxSelectionSize)
                {
                    var oldest = _selectionOrder[0];
                    if (oldest != view)
                    {
                        _selectionOrder.RemoveAt(0);
                        _selected.Remove(oldest);
                        oldest.SetSelectedFromManager(false);
                    }
                }
            }
        }
        else
        {
            if (_selected.Contains(view))
            {
                _selected.Remove(view);
                _selectionOrder.Remove(view);
            }
        }

        RefreshFusionButton();
    }

    public void SetDiscardMode(bool enabled)
    {
        isDiscardMode = enabled;

        if (enabled)
        {
            foreach (var card in _selected.ToList())
            {
                card.SetSelectedFromManager(false);
            }
            _selected.Clear();
            _selectionOrder.Clear();
            RefreshFusionButton();
        }
    }

    private void RefreshFusionButton()
    {
        if (canInteract && _selected.Count == 2 && fusionDatabase != null && !isFusing)
        {
            var duo = GetSelectedData();
            var results = fusionDatabase.TryFuseMultiple(duo);
            fusionButton.interactable = (results != null && results.Count > 0);
        }
        else
        {
            fusionButton.interactable = false;
        }
    }

    public List<CardData> GetSelectedData()
    {
        return _selected.Select(v => v.data).ToList();
    }

    // 🔥 MODIFICADO: Método principal de fusión con soporte para múltiples resultados
    public void OnClickFusionar()
    {
        Debug.Log("[FusionManager] ========================================");
        Debug.Log("[FusionManager] 🎯 OnClickFusionar() INICIADO");
        Debug.Log("[FusionManager] ========================================");

        if (!canInteract)
        {
            Debug.LogWarning("[FusionManager] No puedes fusionar cuando no es tu turno");
            return;
        }

        if (isFusing)
        {
            Debug.LogWarning("[FusionManager] Ya hay una fusión en progreso");
            return;
        }

        if (_selected.Count != 2)
        {
            Debug.LogWarning($"[FusionManager] Se necesitan 2 cartas seleccionadas. Actualmente: {_selected.Count}");
            return;
        }

        if (fusionDatabase == null)
        {
            Debug.LogError("[FusionManager] ❌ fusionDatabase es NULL!");
            return;
        }

        var duo = GetSelectedData();
        Debug.Log($"[FusionManager] 🃏 Cartas seleccionadas: {duo[0].displayName} + {duo[1].displayName}");

        var results = fusionDatabase.TryFuseMultiple(duo);

        if (results == null)
        {
            Debug.LogWarning("[FusionManager] TryFuseMultiple retornó NULL");
            return;
        }

        Debug.Log($"[FusionManager] 📊 Resultados encontrados: {results.Count}");

        if (results.Count == 0)
        {
            Debug.Log("[FusionManager] Combinación no válida (sin receta).");
            return;
        }

        // 🔥 NUEVO: Si hay múltiples resultados, mostrar selector
        if (results.Count > 1)
        {
            Debug.Log($"[FusionManager] 🎯 Múltiples resultados encontrados ({results.Count}), mostrando selector");
            Debug.Log($"[FusionManager]   Resultados: {string.Join(", ", results.Select(r => r.displayName))}");

            // 🔥 VALIDACIÓN CRÍTICA
            if (resultSelector == null)
            {
                Debug.LogError("[FusionManager] ❌❌❌ CRÍTICO: resultSelector es NULL!");
                Debug.LogError("[FusionManager] ❌ No se puede mostrar el selector de opciones");
                Debug.LogError("[FusionManager] ❌ Usando primer resultado como fallback");
                OnFusionResultSelected(results[0]);
                return;
            }

            Debug.Log($"[FusionManager] ✅ resultSelector válido: {resultSelector.gameObject.name}");
            Debug.Log($"[FusionManager] 📍 Panel activo en jerarquía: {resultSelector.gameObject.activeInHierarchy}");

            isFusing = true;
            fusionButton.interactable = false;

            Debug.Log("[FusionManager] 📤 Llamando a resultSelector.ShowOptions()...");

            resultSelector.ShowOptions(results, OnFusionResultSelected);

            Debug.Log("[FusionManager] ✅ ShowOptions() llamado exitosamente");
        }
        else
        {
            // 🔥 Solo un resultado, fusión directa
            Debug.Log($"[FusionManager] ✅ Un solo resultado, fusión directa: {results[0].displayName}");
            CompleteFusion(results[0]);
        }

        Debug.Log("[FusionManager] ========================================");
        Debug.Log("[FusionManager] 🏁 OnClickFusionar() FINALIZADO");
        Debug.Log("[FusionManager] ========================================");
    }

    // 🔥 NUEVO: Callback cuando el usuario selecciona un resultado
    private void OnFusionResultSelected(CardData selectedResult)
    {
        Debug.Log("[FusionManager] ========================================");
        Debug.Log("[FusionManager] 🎯 OnFusionResultSelected() LLAMADO");
        Debug.Log("[FusionManager] ========================================");

        isFusing = false;

        if (selectedResult == null)
        {
            Debug.Log("[FusionManager] Fusión cancelada por el usuario");
            RefreshFusionButton();
            return;
        }

        Debug.Log($"[FusionManager] Usuario seleccionó: {selectedResult.displayName}");
        CompleteFusion(selectedResult);
    }

    // 🔥 NUEVO: Método que completa la fusión con el resultado elegido
    private void CompleteFusion(CardData result)
    {
        Debug.Log($"[FusionManager] 🎉 CompleteFusion() - Resultado: {result.displayName}");

        lastFusionResult = result;

        var cardsToRemove = _selected.ToList();
        foreach (var card in cardsToRemove)
        {
            RemoveCardFromHand(card);
        }

        AddCardToHand(result);

        ShowResult(result, "¡Fusión exitosa!");

        _selected.Clear();
        _selectionOrder.Clear();

        RefreshFusionButton();

        if (gameManager != null)
        {
            gameManager.OnCardFused();
        }
    }

    private void ShowResult(CardData data, string logMsg)
    {
        ClearResultPanel();
        if (data != null)
        {
            var v = Instantiate(cardViewPrefab, resultPanel);
            v.Setup(data, this);

            var anim = v.gameObject.GetComponent<ResultAppear>() ?? v.gameObject.AddComponent<ResultAppear>();
            anim.Play();
        }
        Debug.Log(logMsg);
    }

    private void ClearResultPanel()
    {
        foreach (Transform t in resultPanel) Destroy(t.gameObject);
    }

    private void UpdateHandLayout()
    {
        var fanLayout = handPanel.GetComponent<FanHandLayout>();
        if (fanLayout != null)
        {
            // El FanHandLayout se encargará de reorganizar las cartas automáticamente
        }
    }

    public CardData GetLastFusionResult()
    {
        return lastFusionResult;
    }

    public void ClearLastFusionResult()
    {
        lastFusionResult = null;
    }
}