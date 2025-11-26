using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VictoryManager : MonoBehaviour
{
    [Header("🏆 Paneles de Victoria/Derrota")]
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private GameObject loserPanel;

    [Header("🎨 Textos del Winner Panel")]
    [SerializeField] private TextMeshProUGUI winnerTextTitle; // "¡GANASTE!"
    [SerializeField] private TextMeshProUGUI winnerTextContent; // "Conseguiste ganar el torneo..."
    [SerializeField] private TextMeshProUGUI winnerTextAreaFormacion; // "Area de formacion"

    [Header("📝 Textos del Loser Panel")]
    [SerializeField] private TextMeshProUGUI loserTextTitle; // "Perdiste :/"
    [SerializeField] private TextMeshProUGUI loserTextContent; // "Perdiste en el torneo..."

    [Header("⚙️ Configuración Efecto Neón")]
    [SerializeField] private float neonPulseSpeed = 2f; // Velocidad de pulsación
    [SerializeField] private float minIntensity = 0.5f; // Intensidad mínima
    [SerializeField] private float maxIntensity = 2f; // Intensidad máxima
    [SerializeField] private float glowSize = 0.3f; // Tamaño del brillo

    // 🎨 Colores de cada área de formación
    private readonly Color violetaColor = new Color(0.58f, 0.29f, 0.78f, 1f); // PM - Violeta
    private readonly Color azulColor = new Color(0.2f, 0.6f, 1f, 1f); // SI - Azul
    private readonly Color amarilloColor = new Color(1f, 0.92f, 0.02f, 1f); // PAIM - Amarillo
    private readonly Color naranjaColor = new Color(1f, 0.55f, 0f, 1f); // TD - Naranja

    // 🎯 IDs de las cartas ganadoras
    private readonly string[] winningCardIDs = { "PM", "SI", "PAIM", "TD" };

    private Coroutine neonEffectCoroutine;
    private Material textMaterial;

    void Awake()
    {
        // Ocultar paneles al inicio
        if (winnerPanel) winnerPanel.SetActive(false);
        if (loserPanel) loserPanel.SetActive(false);

        // Crear material duplicado para el texto de área de formación
        if (winnerTextAreaFormacion != null)
        {
            textMaterial = new Material(winnerTextAreaFormacion.fontMaterial);
            winnerTextAreaFormacion.fontMaterial = textMaterial;
            
            // Habilitar emisión en el material
            textMaterial.EnableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// Verifica si una carta es una carta ganadora (área de formación final)
    /// </summary>
    public bool IsWinningCard(string cardID)
    {
        foreach (string winningID in winningCardIDs)
        {
            if (cardID == winningID)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Muestra el panel de victoria con efectos neón
    /// </summary>
    public void ShowVictoryPanel(CardData winningCard)
    {
        Debug.Log($"[VictoryManager] 🏆 Mostrando panel de victoria para: {winningCard.displayName}");

        if (winnerPanel == null)
        {
            Debug.LogError("[VictoryManager] ❌ WinnerPanel no asignado!");
            return;
        }

        // Activar panel de victoria
        winnerPanel.SetActive(true);

        // Configurar textos
        if (winnerTextTitle)
        {
            winnerTextTitle.text = "¡GANASTE!";
        }

        if (winnerTextContent)
        {
            winnerTextContent.text = "Conseguiste ganar el torneo Multinexus y llegaste al área de formación:";
        }

        if (winnerTextAreaFormacion)
        {
            winnerTextAreaFormacion.text = winningCard.displayName;
            
            // Aplicar color según la carta
            Color neonColor = GetColorForCard(winningCard.id);
            winnerTextAreaFormacion.color = neonColor;

            // Iniciar efecto neón
            if (neonEffectCoroutine != null)
            {
                StopCoroutine(neonEffectCoroutine);
            }
            neonEffectCoroutine = StartCoroutine(NeonPulseEffect(neonColor));
        }

        Debug.Log("[VictoryManager] ✅ Panel de victoria mostrado con efecto neón");
    }

    /// <summary>
    /// Muestra el panel de derrota
    /// </summary>
    public void ShowDefeatPanel()
    {
        Debug.Log("[VictoryManager] 😢 Mostrando panel de derrota");

        if (loserPanel == null)
        {
            Debug.LogError("[VictoryManager] ❌ LoserPanel no asignado!");
            return;
        }

        // Activar panel de derrota
        loserPanel.SetActive(true);

        // Configurar textos
        if (loserTextTitle)
        {
            loserTextTitle.text = "Perdiste :/";
        }

        if (loserTextContent)
        {
            loserTextContent.text = "Perdiste en el torneo Multinexus, sigue practicando para alzarte con la victoria.";
        }

        Debug.Log("[VictoryManager] ✅ Panel de derrota mostrado");
    }

    /// <summary>
    /// Obtiene el color correspondiente a cada carta ganadora
    /// </summary>
    private Color GetColorForCard(string cardID)
    {
        switch (cardID)
        {
            case "PM":
                return violetaColor; // Producción Multimedia - Violeta
            case "SI":
                return azulColor; // Sistemas Interactivos - Azul
            case "PAIM":
                return amarilloColor; // Procesamiento y Análisis - Amarillo
            case "TD":
                return naranjaColor; // Transformación Digital - Naranja
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Efecto de pulsación neón para el texto
    /// </summary>
    private IEnumerator NeonPulseEffect(Color neonColor)
    {
        if (textMaterial == null)
        {
            Debug.LogWarning("[VictoryManager] ⚠️ Material de texto no disponible");
            yield break;
        }

        float time = 0f;

        while (true)
        {
            // Calcular intensidad de pulsación usando seno
            float pulse = Mathf.Lerp(minIntensity, maxIntensity, 
                (Mathf.Sin(time * neonPulseSpeed) + 1f) / 2f);

            // Aplicar color emisivo al material
            Color emissionColor = neonColor * pulse;
            textMaterial.SetColor("_EmissionColor", emissionColor);

            // También aplicar un efecto de brillo
            textMaterial.SetFloat("_GlowPower", pulse * glowSize);

            time += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Detiene el efecto neón
    /// </summary>
    public void StopNeonEffect()
    {
        if (neonEffectCoroutine != null)
        {
            StopCoroutine(neonEffectCoroutine);
            neonEffectCoroutine = null;
        }

        if (textMaterial != null)
        {
            textMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    /// <summary>
    /// Oculta ambos paneles
    /// </summary>
    public void HidePanels()
    {
        StopNeonEffect();

        if (winnerPanel) winnerPanel.SetActive(false);
        if (loserPanel) loserPanel.SetActive(false);

        Debug.Log("[VictoryManager] Paneles ocultados");
    }

    void OnDestroy()
    {
        // Limpiar material duplicado
        if (textMaterial != null)
        {
            Destroy(textMaterial);
        }
    }
}