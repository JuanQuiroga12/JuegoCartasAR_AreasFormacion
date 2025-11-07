// Assets/Scripts/UI/ScanAreaBorder.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScanAreaBorder : MonoBehaviour
{
    [Header("Border Settings")]
    [SerializeField] private Color borderColor = Color.red;
    [SerializeField] private float borderWidth = 3f;
    [SerializeField] private bool animateBorder = true;
    [SerializeField] private float pulseSpeed = 2f;

    private Outline outline;
    private Shadow[] shadows;
    private float pulseTimer;

    void Start()
    {
        SetupOutline();
    }

    void SetupOutline()
    {
        // Añadir componente Outline si no existe
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }

        // Configurar outline
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(borderWidth, borderWidth);
        outline.useGraphicAlpha = false;
    }

    void Update()
    {
        if (animateBorder && outline != null)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float alpha = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
            Color newColor = borderColor;
            newColor.a = Mathf.Lerp(0.5f, 1f, alpha);
            outline.effectColor = newColor;
        }
    }
}