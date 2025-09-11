using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [Header("Refs")]
    public Image background;
    public Image artwork;
    public Text title;
    public Outline selectionOutline;

    [Header("Data")]
    public CardData data;

    private bool _selected;
    private FusionManager _manager;
    private Color _baseColor; // guarda el color original de la carta

    public bool IsSelected => _selected;

    public void Setup(CardData data, FusionManager manager)
    {
        this.data = data;
        _manager = manager;

        if (title) title.text = data ? data.displayName : "";

        // Artwork
        if (artwork)
        {
            if (data && data.artwork)
            {
                artwork.sprite = data.artwork;
                artwork.enabled = true;
            }
            else
            {
                artwork.sprite = null;
                artwork.enabled = false;
            }
        }

        // Color de fondo desde CardData
        if (background)
        {
            background.sprite = null; // fondo plano
            _baseColor = data ? data.baseColor : Color.gray;
            _baseColor.a = 1f;
            background.color = _baseColor;
            background.enabled = true;
        }

        SetSelected(false);
        Debug.Log($"[CardView] Setup '{data?.displayName}' color={background?.color}");
    }

    public void OnClick()
    {
        if (_manager == null || data == null) return;

        _selected = !_selected;
        SetSelected(_selected);

        _manager.NotifySelectionChanged(this, _selected);
    }

    private void SetSelected(bool on)
    {
        if (selectionOutline)
        {
            selectionOutline.enabled = on;
            selectionOutline.effectDistance = on ? new Vector2(6, 6) : Vector2.zero;
        }

        if (background)
        {
            if (on)
            {
                // brillo leve sobre el color base
                background.color = _baseColor * 1.1f;
            }
            else
            {
                background.color = _baseColor;
            }
        }
    }
}
