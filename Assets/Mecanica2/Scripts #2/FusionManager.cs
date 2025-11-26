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
    public FusionResultSelector resultSelector;

    [Header("Configuración")]
    public int maxHandSize = 3;
    public int maxSelectionSize = 3; // 🔥 CAMBIADO DE 2 A 3

    private readonly List<CardView> _handViews = new();
    private readonly List<CardView> _selectionOrder = new();
    private readonly HashSet<CardView> _selected = new();

    private GameManager gameManager;
    private bool isDiscardMode = false;
    private CardData lastFusionResult = null;
    private bool canInteract = true;
    private bool isFusing = false;
    private int fusionsUsedThisTurn = 0;
    private int maxFusionsPerTurn = 1;

    void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();

        if (resultSelector == null)
        {
            Debug.LogError("[FusionManager] ❌❌❌ FusionResultSelector NO ESTÁ ASIGNADO EN EL INSPECTOR!");
            Debug.LogError("[FusionManager] Por favor, arrastra el FusionResultSelectorPanel al campo 'Result Selector' en el Inspector");

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

        Debug.Log("[FusionManager] ✅ FusionManager iniciado (Max selección: 3 cartas)");
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
        Debug.Log($"[FusionManager] ========================================");
        Debug.Log($"[FusionManager] 🎯 AddCardToHand LLAMADO");
        Debug.Log($"[FusionManager] ========================================");

        if (cardData == null)
        {
            Debug.LogWarning("[FusionManager] ⚠️ Intentando agregar carta NULL");
            return;
        }

        Debug.Log($"[FusionManager] 📝 Carta a agregar: '{cardData.displayName}' (ID: {cardData.id})");
        Debug.Log($"[FusionManager] 📊 Cartas actuales en mano: {_handViews.Count}");

        if (_handViews.Count >= 4)
        {
            Debug.LogWarning($"[FusionManager] ⚠️ La mano ya tiene el máximo de cartas permitidas ({_handViews.Count}/4)");
            return;
        }

        Debug.Log($"[FusionManager] 🔨 Creando CardView...");
        var newCard = CreateCardView(cardData);

        if (newCard == null)
        {
            Debug.LogError("[FusionManager] ❌ CreateCardView retornó NULL!");
            return;
        }

        Debug.Log($"[FusionManager] ✅ CardView creado exitosamente: {newCard.name}");

        UpdateHandLayout();

        Debug.Log($"[FusionManager] ========================================");
        Debug.Log($"[FusionManager] ✅ Carta '{cardData.displayName}' agregada exitosamente");
        Debug.Log($"[FusionManager] 📊 Total en mano ahora: {_handViews.Count}");
        Debug.Log($"[FusionManager] ========================================");
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

    public void ResetFusionCounter()
    {
        fusionsUsedThisTurn = 0;
        Debug.Log("[FusionManager] 🔄 Contador de fusiones reseteado");
    }

    // 🔥 MODIFICADO: Ahora verifica 2 O 3 cartas seleccionadas
    private void RefreshFusionButton()
    {
        bool wasInteractable = fusionButton != null && fusionButton.interactable;

        // 🔥 MODIFICADO: Aceptar 2 o 3 cartas
        bool hasValidSelection = _selected.Count >= 2 && _selected.Count <= 3;

        if (canInteract && hasValidSelection && fusionDatabase != null && !isFusing && fusionsUsedThisTurn < maxFusionsPerTurn)
        {
            var selectedCards = GetSelectedData();
            var results = fusionDatabase.TryFuseMultiple(selectedCards);

            bool canFuseNow = (results != null && results.Count > 0);

            if (fusionButton != null)
                fusionButton.interactable = canFuseNow;

            if (!wasInteractable && canFuseNow)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayFusionButtonEnabled();
            }
        }
        else
        {
            if (fusionButton != null)
                fusionButton.interactable = false;

            if (fusionsUsedThisTurn >= maxFusionsPerTurn)
            {
                Debug.Log($"[FusionManager] ⚠️ Límite de fusiones alcanzado ({fusionsUsedThisTurn}/{maxFusionsPerTurn})");
            }
        }
    }

    public List<CardData> GetSelectedData()
    {
        return _selected.Select(v => v.data).ToList();
    }

    // 🔥 MODIFICADO: Ahora acepta 2 o 3 cartas
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

        // 🔥 MODIFICADO: Validar 2 o 3 cartas
        if (_selected.Count < 2 || _selected.Count > 3)
        {
            Debug.LogWarning($"[FusionManager] Se necesitan 2 o 3 cartas seleccionadas. Actualmente: {_selected.Count}");
            return;
        }

        if (fusionDatabase == null)
        {
            Debug.LogError("[FusionManager] ❌ fusionDatabase es NULL!");
            return;
        }

        var selectedCards = GetSelectedData();

        // 🔥 NUEVO: Log detallado de cartas seleccionadas
        string cardNames = string.Join(" + ", selectedCards.Select(c => c.displayName));
        Debug.Log($"[FusionManager] 🃏 Cartas seleccionadas ({selectedCards.Count}): {cardNames}");

        var results = fusionDatabase.TryFuseMultiple(selectedCards);

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

        if (results.Count > 1)
        {
            Debug.Log($"[FusionManager] 🎯 Múltiples resultados encontrados ({results.Count}), mostrando selector");
            Debug.Log($"[FusionManager]   Resultados: {string.Join(", ", results.Select(r => r.displayName))}");

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
            Debug.Log($"[FusionManager] ✅ Un solo resultado, fusión directa: {results[0].displayName}");
            CompleteFusion(results[0]);
        }

        Debug.Log("[FusionManager] ========================================");
        Debug.Log("[FusionManager] 🏁 OnClickFusionar() FINALIZADO");
        Debug.Log("[FusionManager] ========================================");
    }

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

        fusionsUsedThisTurn++;
        Debug.Log($"[FusionManager] 📊 Fusiones usadas este turno: {fusionsUsedThisTurn}/{maxFusionsPerTurn}");

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