using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CardView : MonoBehaviour
{
    [Header("Refs")]
    public Image background;   // Image principal (sprite completo de la carta)

    [Header("Data")]
    public CardData data;

    private bool _selected;
    private FusionManager _manager;

    public bool IsSelected => _selected;

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClick);
            btn.onClick.AddListener(OnClick);
        }

        // aseguramos que arranca deseleccionada
        _selected = false;
        UpdateVisualSelection();
    }

    public void Setup(CardData data, FusionManager manager)
    {
        this.data = data;
        _manager = manager;

        if (background && data != null && data.artwork != null)
        {
            background.sprite = data.artwork;
            background.color = Color.white;
        }

        SetSelectedFromManager(false);
    }

    public void OnClick()
    {
        if (_manager == null || data == null) return;

        // cambiar estado y avisar al manager
        _selected = !_selected;
        UpdateVisualSelection();
        _manager.NotifySelectionChanged(this, _selected);

        Debug.Log($"[CardView] {(data != null ? data.displayName : "??")} seleccionado={_selected}");
    }

    public void SetSelectedFromManager(bool on)
    {
        _selected = on;
        UpdateVisualSelection(); // ❌ NO vuelvas a llamar a _manager.NotifySelectionChanged aquí
    }


    private void UpdateVisualSelection()
    {
        // Opción 1: activar Outline si existe
        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = _selected;

        // Opción 2: cambiar alpha del fondo
        if (background != null)
        {
            var col = background.color;
            col.a = _selected ? 0.7f : 1f;
            background.color = col;
        }
    }
}
