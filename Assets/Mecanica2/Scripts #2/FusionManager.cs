using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FusionManager : MonoBehaviour
{
    [Header("DB & Prefabs")]
    public FusionDatabase fusionDatabase;
    public CardView cardViewPrefab;

    [Header("UI Slots")]
    public Transform handPanel;   // donde instanciamos cartas iniciales
    public Transform resultPanel; // donde mostramos el resultado
    public Button fusionButton;   // botón "Fusionar"

    [Header("Mano inicial")]
    public List<CardData> startingHand = new List<CardData>(); // arrastra 3 cartas aquí

    private readonly List<CardView> _handViews = new();
    private readonly HashSet<CardView> _selected = new();

    void Start()
    {
        SetupHand();
        RefreshFusionButton();
        ClearResultPanel();
    }

    private void SetupHand()
    {
        // Limpia mano previa
        foreach (Transform t in handPanel) Destroy(t.gameObject);
        _handViews.Clear();
        _selected.Clear();

        foreach (var c in startingHand)
        {
            var v = Instantiate(cardViewPrefab, handPanel);
            v.Setup(c, this);
            _handViews.Add(v);
        }
    }

    public void NotifySelectionChanged(CardView view, bool selected)
    {
        if (selected) _selected.Add(view);
        else _selected.Remove(view);
        RefreshFusionButton();
    }

    private void RefreshFusionButton()
    {
        // Habilita si se seleccionaron 3 cartas (o >=2 si prefieres)
        fusionButton.interactable = _selected.Count >= 3;
    }

    public void OnClickFusionar()
    {
        if (_selected.Count < 2) return;

        // Construye la lista de datos seleccionados
        var chosenData = new List<CardData>();
        foreach (var v in _selected) chosenData.Add(v.data);

        // Intenta fusionar
        var result = fusionDatabase ? fusionDatabase.TryFuse(chosenData) : null;

        // Si no hay receta, podrías cancelar o crear "Carta Fallida"
        if (result == null)
        {
            Debug.Log("No hay receta para esa combinación.");
            ShowResult(null, "Sin receta");
            return;
        }

        // Muestra resultado
        ShowResult(result, "¡Fusión exitosa!");

        // (Opcional) Reemplazar la mano por la carta resultante
        // startingHand = new List<CardData>{ result };
        // SetupHand();
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
