using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CardView minimalista (usa sprite completo en background).
/// Incluye SetSelectedFromManager para que FusionManager pueda forzar selecci�n.
/// </summary>
[RequireComponent(typeof(Button))]
public class CardView : MonoBehaviour
{
    [Header("Refs")]
    public Image background;   // Image del root que contendr� el sprite completo de la carta

    [Header("Data")]
    public CardData data;      // asignado en Setup()

    bool _selected;
    FusionManager _manager;

    public bool IsSelected => _selected;

    void Awake()
    {
        // Auto-conectar bot�n al OnClick local (seguro aunque ya lo tengas en inspector)
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClick);
            btn.onClick.AddListener(OnClick);
        }
    }

    // Inicializa la carta
    public void Setup(CardData cardData, FusionManager manager)
    {
        data = cardData;
        _manager = manager;
        _selected = false;

        if (background && data != null && data.artwork != null)
        {
            background.sprite = data.artwork;
            background.color = Color.white;
            background.enabled = true;
        }
        else if (background)
        {
            background.sprite = null;
            background.enabled = false;
        }

        UpdateVisualSelection();
    }

    // Click desde el Button
    public void OnClick()
    {
        if (_manager == null || data == null) return;

        _selected = !_selected;
        UpdateVisualSelection();
        _manager.NotifySelectionChanged(this, _selected);
    }

    // M�todo p�blico para que el manager fuerce selecci�n/deselecci�n
    public void SetSelectedFromManager(bool on)
    {
        _selected = on;
        UpdateVisualSelection();
    }

    // Actualiza el outline / alpha u otro feedback visual
    void UpdateVisualSelection()
    {
        // Si tienes Outline en el mismo GameObject, lo activamos/desactivamos
        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = _selected;

        // Ejemplo simple: bajar la alpha si est� seleccionado (ajusta a tu gusto)
        if (background != null)
        {
            var c = background.color;
            c.a = _selected ? 0.8f : 1f;
            background.color = c;
        }
    }
}
