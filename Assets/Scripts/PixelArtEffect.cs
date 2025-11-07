// Assets/Scripts/Shaders/PixelArtEffect.cs
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PixelArtEffect : MonoBehaviour
{
    [Header("Pixel Art Settings")]
    [SerializeField, Range(8, 256)] private int pixelResolution = 64;
    [SerializeField] private bool maintainAspectRatio = true;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField, Range(0, 1)] private float outlineThickness = 0.1f;
    [SerializeField] private bool enableDithering = false;
    [SerializeField, Range(0, 1)] private float ditheringStrength = 0.5f;

    private Material pixelArtMaterial;
    private Renderer objectRenderer;
    private MaterialPropertyBlock propertyBlock;

    // Shader property IDs for optimization
    private static readonly int PixelResolutionID = Shader.PropertyToID("_PixelResolution");
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessID = Shader.PropertyToID("_OutlineThickness");
    private static readonly int DitheringEnabledID = Shader.PropertyToID("_DitheringEnabled");
    private static readonly int DitheringStrengthID = Shader.PropertyToID("_DitheringStrength");

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        // Crear instancia del material para no afectar otros objetos
        if (objectRenderer.sharedMaterial != null)
        {
            pixelArtMaterial = new Material(objectRenderer.sharedMaterial);
            objectRenderer.material = pixelArtMaterial;
        }

        UpdateShaderProperties();
    }

    void OnValidate()
    {
        if (Application.isPlaying && pixelArtMaterial != null)
        {
            UpdateShaderProperties();
        }
    }

    public void UpdateShaderProperties()
    {
        if (objectRenderer == null || propertyBlock == null) return;

        objectRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(PixelResolutionID, pixelResolution);
        propertyBlock.SetColor(OutlineColorID, outlineColor);
        propertyBlock.SetFloat(OutlineThicknessID, outlineThickness);
        propertyBlock.SetFloat(DitheringEnabledID, enableDithering ? 1f : 0f);
        propertyBlock.SetFloat(DitheringStrengthID, ditheringStrength);

        objectRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetPixelResolution(int resolution)
    {
        pixelResolution = Mathf.Clamp(resolution, 8, 256);
        UpdateShaderProperties();
    }

    public void SetOutline(Color color, float thickness)
    {
        outlineColor = color;
        outlineThickness = Mathf.Clamp01(thickness);
        UpdateShaderProperties();
    }

    void OnDestroy()
    {
        if (pixelArtMaterial != null)
        {
            Destroy(pixelArtMaterial);
        }
    }
}