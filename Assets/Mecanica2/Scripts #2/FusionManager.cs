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
    public Transform handPanel;   // contenedor de las 3 cartas
    public Transform resultPanel; // contenedor de la carta resultante
    public Button fusionButton;

    [Header("Mano inicial (3 cartas)")]
    public List<CardData> startingHand = new List<CardData>(); // arrastra 3 aquí

    private readonly List<CardView> _handViews = new();
    private readonly List<CardView> _selectionOrder = new();   // mantiene orden de selección
    private readonly HashSet<CardView> _selected = new();      // conjunto para consulta rápida

    void Start()
    {
        SetupHand();
        RefreshFusionButton();
        ClearResultPanel();
    }

    private void SetupHand()
    {
        foreach (Transform t in handPanel) Destroy(t.gameObject);
        _handViews.Clear();
        _selected.Clear();
        _selectionOrder.Clear();

        foreach (var c in startingHand.Take(3))
        {
            var v = Instantiate(cardViewPrefab, handPanel);
            v.Setup(c, this);
            _handViews.Add(v);
        }
    }

    public void NotifySelectionChanged(CardView view, bool selected)
    {
        if (selected)
        {
            // Añadir y respetar límite de 2
            if (!_selected.Contains(view))
            {
                _selected.Add(view);
                _selectionOrder.Add(view);

                // Si ahora hay 3, deselecciona la más antigua
                if (_selected.Count > 2)
                {
                    var oldest = _selectionOrder[0];
                    _selectionOrder.RemoveAt(0);
                    _selected.Remove(oldest);
                    oldest.SetSelectedFromManager(false); // fuerza visual y lógico
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

    private void RefreshFusionButton()
    {
        // Debe haber exactamente 2 seleccionadas
        if (_selected.Count == 2 && fusionDatabase != null)
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

    private List<CardData> GetSelectedData()
    {
        var list = new List<CardData>();
        foreach (var v in _selected) list.Add(v.data);
        return list;
    }

    public void OnClickFusionar()
    {
        if (_selected.Count != 2 || fusionDatabase == null) return;

        var duo = GetSelectedData();
        var result = fusionDatabase.TryFuse(duo);

        if (result == null)
        {
            Debug.Log("Combinación no válida (sin receta).");
            return;
        }

        ShowResult(result, "¡Fusión exitosa!");

        // (Opcional) limpiar selección tras fusionar
        foreach (var v in _selected.ToList())
            v.SetSelectedFromManager(false);
        _selected.Clear();
        _selectionOrder.Clear();

        RefreshFusionButton();
    }

    private void ShowResult(CardData data, string logMsg)
    {
        ClearResultPanel();
        if (data != null)
        {
            var v = Instantiate(cardViewPrefab, resultPanel);
            v.Setup(data, this);
        }
        Debug.Log(logMsg);
    }

    private void ClearResultPanel()
    {
        foreach (Transform t in resultPanel) Destroy(t.gameObject);
    }
}
