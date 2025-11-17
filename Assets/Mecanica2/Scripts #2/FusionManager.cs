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

    [Header("Configuración")]
    public int maxHandSize = 3;
    public int maxSelectionSize = 2; // Máximo de cartas seleccionables para fusión

    // 🔥 ELIMINADO: startingHand ya no es necesario
    // La mano se construirá escaneando cartas en la fase de preparación


    private readonly List<CardView> _handViews = new();
    private readonly List<CardView> _selectionOrder = new();
    private readonly HashSet<CardView> _selected = new();

    private GameManager gameManager;
    private bool isDiscardMode = false;

    // Variable para almacenar el último resultado de fusión (para multijugador)
    private CardData lastFusionResult = null;

    // 🔥 NUEVO: Variable para controlar si el jugador puede interactuar
    private bool canInteract = true;

    void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();

        // 🔥 ELIMINADO: SetupHand() ya no se llama aquí
        // La mano se llenará dinámicamente durante la fase de preparación

        RefreshFusionButton();
        ClearResultPanel();

        Debug.Log("[FusionManager] ✅ FusionManager iniciado (sin mano inicial)");
    }

    // 🔥 NUEVO: Método para habilitar/deshabilitar interacciones
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

        // 🔥 MODIFICADO: Límite flexible durante preparación
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
        if (canInteract && _selected.Count == 2 && fusionDatabase != null)
        {
            var duo = GetSelectedData();
            var canFuse = fusionDatabase.TryFuse(duo) != null;
            fusionButton.interactable = canFuse;
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

    public void OnClickFusionar()
    {
        if (!canInteract)
        {
            Debug.LogWarning("[FusionManager] No puedes fusionar cuando no es tu turno");
            return;
        }

        if (_selected.Count != 2 || fusionDatabase == null) return;

        var duo = GetSelectedData();
        var result = fusionDatabase.TryFuse(duo);

        if (result == null)
        {
            Debug.Log("Combinación no válida (sin receta).");
            return;
        }

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
        // Si tienes FanHandLayout, actualizar el layout
        var fanLayout = handPanel.GetComponent<FanHandLayout>();
        if (fanLayout != null)
        {
            // El FanHandLayout se encargará de reorganizar las cartas automáticamente
        }
    }

    // ========== MÉTODOS PARA MULTIJUGADOR ==========

    /// <summary>
    /// Obtiene el último resultado de fusión realizado.
    /// Se usa para sincronizar la fusión con Firebase en modo multijugador.
    /// </summary>
    /// <returns>La CardData del último resultado de fusión, o null si no hay ninguno</returns>
    public CardData GetLastFusionResult()
    {
        return lastFusionResult;
    }

    /// <summary>
    /// Limpia el último resultado de fusión almacenado.
    /// Útil para resetear el estado después de sincronizar.
    /// </summary>
    public void ClearLastFusionResult()
    {
        lastFusionResult = null;
    }
}