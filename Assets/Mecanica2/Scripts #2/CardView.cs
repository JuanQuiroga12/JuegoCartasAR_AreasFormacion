using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [Header("Refs")]
    public Image background;         // Image del root (marco/diseño)
    public Image artwork;            // Imagen central de la carta (opcional)
    public TMP_Text title;
    public TMP_Text resultOnlyText; // o TMP_Text
    public Outline selectionOutline;

    [Header("Data")]
    public CardData data;

    private bool _selected;
    private FusionManager _manager;

    public bool IsSelected => _selected;

    public void Setup(CardData data, FusionManager manager)
    {
        this.data = data;
        _manager = manager;

        if (title) title.text = data ? data.displayName : "";
        

        // === Fondo (NO tocar el sprite si ya existe en el prefab) ===
        if (background)
        {
            if (background.sprite == null)
            {
                // Sin sprite: usa color de respaldo
                var col = data ? data.baseColor : Color.gray;
                col.a = 1f;
                background.color = col;
            }
            else
            {
                // Con sprite de diseño: no lo tinte
                background.color = Color.white;
            }
            background.enabled = true;
        }

        // === Artwork (opcional) ===
        if (artwork)
        {
            if (data != null && data.artwork != null)
            {
                artwork.sprite = data.artwork;
                artwork.enabled = true;
                artwork.preserveAspect = true;
            }
            else
            {
                artwork.sprite = null;
                artwork.enabled = false; // no tapes el fondo con un rectángulo
            }
        }
        if (resultOnlyText)
        {
            resultOnlyText.gameObject.SetActive(false);
        }

        SetSelected(false);
    }

    public void OnClick()
    {
        if (_manager == null || data == null) return;
        _selected = !_selected;
        SetSelected(_selected);
        _manager.NotifySelectionChanged(this, _selected);
    }

    public void SetSelectedFromManager(bool on)
    {
        _selected = on;
        SetSelected(on);
    }

    private void SetSelected(bool on)
    {
        if (selectionOutline)
        {
            selectionOutline.enabled = on;
            selectionOutline.effectDistance = on ? new Vector2(3, -3) : Vector2.zero;
        }
        // Si tu fondo usa sprite, evita cambiar su color aquí.
        // (Si usas color de respaldo, puedes aplicar un leve brillo multiplicando)
        if (background && background.sprite == null)
        {
            var baseCol = data ? data.baseColor : Color.gray;
            baseCol.a = 1f;
            background.color = on ? baseCol * 1.1f : baseCol;
        }
    }
    public void ShowResultExtraText()
    {
        if (resultOnlyText && data != null && !string.IsNullOrEmpty(data.resultDescription))
        {
            resultOnlyText.text = data.resultDescription;
            resultOnlyText.gameObject.SetActive(true);
        }
    }
}
