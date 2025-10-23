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

    [Header("Mano inicial (3 cartas)")]
    public List<CardData> startingHand = new List<CardData>();

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
        SetupHand();
        RefreshFusionButton();
        ClearResultPanel();
    }

    // 🔥 NUEVO: Método para habilitar/deshabilitar interacciones
    public void SetInteractionEnabled(bool enabled)
    {
        canInteract = enabled;
        RefreshFusionButton();

        // Deshabilitar selección de cartas si no es el turno
        if (!enabled)
        {
            // Deseleccionar todas las cartas
            foreach (var card in _selected.ToList())
            {
                card.SetSelectedFromManager(false);
            }
            _selected.Clear();
            _selectionOrder.Clear();
        }
    }

    private void SetupHand()
    {
        foreach (Transform t in handPanel) Destroy(t.gameObject);
        _handViews.Clear();
        _selected.Clear();
        _selectionOrder.Clear();

        foreach (var c in startingHand.Take(maxHandSize))
        {
            CreateCardView(c);
        }

        UpdateHandLayout();
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
        if (cardData == null) return;

        // Verificar límite de mano extendida (4 cartas después de primera ronda)
        if (_handViews.Count >= 4)
        {
            Debug.LogWarning("[FusionManager] La mano ya tiene el máximo de cartas permitidas");
            return;
        }

        var newCard = CreateCardView(cardData);
        UpdateHandLayout();

        Debug.Log($"[FusionManager] Carta agregada: {cardData.displayName}. Total en mano: {_handViews.Count}");
    }

    public void RemoveCardFromHand(CardView card)
    {
        if (_handViews.Contains(card))
        {
            _handViews.Remove(card);

            // Si estaba seleccionada, remover de la selección
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
        // 🔥 NUEVO: No permitir selección si no es el turno del jugador
        if (!canInteract)
        {
            Debug.LogWarning("[FusionManager] No puedes seleccionar cartas cuando no es tu turno");
            view.SetSelectedFromManager(false); // Forzar deselección
            return;
        }

        // Si estamos en modo descarte, manejar diferente
        if (isDiscardMode)
        {
            if (selected && gameManager != null)
            {
                gameManager.DiscardCard(view);
                isDiscardMode = false; // Salir del modo descarte después de descartar
            }
            return;
        }

        // Lógica normal de selección para fusión
        if (selected)
        {
            if (!_selected.Contains(view))
            {
                _selected.Add(view);
                _selectionOrder.Add(view);

                // Límite de selección para fusión
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

        // Deseleccionar todas las cartas cuando entramos en modo descarte
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
        // 🔥 MODIFICADO: Botón activo solo si:
        // 1. Es el turno del jugador (canInteract)
        // 2. Hay exactamente 2 cartas seleccionadas
        // 3. La combinación existe en la base de datos
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
        // 🔥 NUEVO: Verificar que sea el turno del jugador
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

        // Guardar el último resultado de fusión para multijugador
        lastFusionResult = result;

        // Remover las cartas fusionadas
        var cardsToRemove = _selected.ToList();
        foreach (var card in cardsToRemove)
        {
            RemoveCardFromHand(card);
        }

        // Agregar la carta resultante a la mano
        AddCardToHand(result);

        ShowResult(result, "¡Fusión exitosa!");

        // Limpiar selección
        _selected.Clear();
        _selectionOrder.Clear();

        RefreshFusionButton();

        // Notificar al GameManager
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

            // Animación de aparición
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