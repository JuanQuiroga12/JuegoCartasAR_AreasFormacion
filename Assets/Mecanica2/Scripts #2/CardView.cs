using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CardView : MonoBehaviour
{
    [Header("Refs")]
    public Image background;

    [Header("Data")]
    public CardData data;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.8f, 0.9f, 1f, 1f);
    public Color discardColor = new Color(1f, 0.7f, 0.7f, 1f);
    public GameObject selectionIndicator;
    public GameObject discardIndicator;

    private bool _selected;
    private bool _discardMode;
    private FusionManager _manager;
    private GameManager _gameManager;
    private Outline _outline;

    public bool IsSelected => _selected;
    public bool IsInDiscardMode => _discardMode;

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClick);
            btn.onClick.AddListener(OnClick);
        }

        // Obtener o agregar componente Outline
        _outline = GetComponent<Outline>();
        if (_outline == null)
        {
            _outline = gameObject.AddComponent<Outline>();
            _outline.effectColor = Color.yellow;
            _outline.effectDistance = new Vector2(3, -3);
        }

       
        _gameManager = Object.FindFirstObjectByType<GameManager>();

        // aseguramos que arranca deseleccionada
        _selected = false;
        _discardMode = false;
        UpdateVisualState();
    }

    public void Setup(CardData data, FusionManager manager)
    {
        this.data = data;
        _manager = manager;

        if (background && data != null && data.artwork != null)
        {
            background.sprite = data.artwork;
            background.color = normalColor;
        }

        SetSelectedFromManager(false);
        SetDiscardMode(false);
    }

    public void OnClick()
    {
        if (_manager == null || data == null) return;

        // Si estamos en modo descarte, manejar diferente
        if (_discardMode)
        {
            HandleDiscardClick();
            return;
        }

        // Comportamiento normal de selección
        _selected = !_selected;
        UpdateVisualState();
        _manager.NotifySelectionChanged(this, _selected);

        Debug.Log($"[CardView] {data.displayName} seleccionado={_selected}");
    }

    private void HandleDiscardClick()
    {
        Debug.Log($"[CardView] Descartando {data.displayName}");

        // Efecto visual de descarte
        StartCoroutine(DiscardAnimation());
    }

    private System.Collections.IEnumerator DiscardAnimation()
    {
        // Animación simple de fade out
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (background)
            {
                Color col = background.color;
                col.a = Mathf.Lerp(1f, 0f, t);
                background.color = col;
            }

            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            yield return null;
        }

        // Notificar al manager que esta carta debe ser descartada
        if (_gameManager != null)
        {
            _gameManager.DiscardCard(this);
        }
        else if (_manager != null)
        {
            _manager.RemoveCardFromHand(this);
        }
    }

    public void SetSelectedFromManager(bool on)
    {
        _selected = on;
        UpdateVisualState();
    }

    public void SetDiscardMode(bool on)
    {
        _discardMode = on;
        UpdateVisualState();

        // Si entra en modo descarte, deseleccionar
        if (on)
        {
            _selected = false;
        }
    }

    private void UpdateVisualState()
    {
        // Prioridad: Modo descarte > Seleccionado > Normal

        if (_discardMode)
        {
            // Modo descarte - resaltar en rojo
            if (_outline != null)
            {
                _outline.enabled = true;
                _outline.effectColor = Color.red;
            }

            if (background != null)
            {
                background.color = discardColor;
            }

            if (discardIndicator != null)
            {
                discardIndicator.SetActive(true);
            }
        }
        else if (_selected)
        {
            // Seleccionado para fusión
            if (_outline != null)
            {
                _outline.enabled = true;
                _outline.effectColor = Color.yellow;
            }

            if (background != null)
            {
                background.color = selectedColor;
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
            }

            if (discardIndicator != null)
            {
                discardIndicator.SetActive(false);
            }
        }
        else
        {
            // Estado normal
            if (_outline != null)
            {
                _outline.enabled = false;
            }

            if (background != null)
            {
                background.color = normalColor;
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            if (discardIndicator != null)
            {
                discardIndicator.SetActive(false);
            }
        }

        // Efecto de elevación si está seleccionado
        UpdateElevation();
    }

    private void UpdateElevation()
    {
        var fanLayout = GetComponentInParent<FanHandLayout>();
        if (fanLayout != null)
        {
            // El FanHandLayout manejará la elevación
            return;
        }

        // Si no hay FanHandLayout, aplicar elevación simple
        Vector3 targetPos = transform.localPosition;
        if (_selected && !_discardMode)
        {
            targetPos.y += 20f;
        }
        else
        {
            targetPos.y = 0f;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * 10f);
    }

    void OnDestroy()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnClick);
        }
    }
}