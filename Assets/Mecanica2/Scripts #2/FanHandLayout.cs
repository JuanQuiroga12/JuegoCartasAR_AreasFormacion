using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FanHandLayout : MonoBehaviour
{
    [Header("Distribución")]
    [Range(0f, 180f)] public float totalArcDegrees = 50f; // ángulo total del abanico
    public float radius = 420f;                           // radio del arco
    public float verticalOffset = 120f;                   // desplaza todo el abanico (positivo = hacia arriba)
    public float spacingJitter = 0f;                      // pequeña variación (0 = uniforme)

    [Header("Orientación del arco")]
    public bool arcAtBottom = true;   // true = arco abajo, cartas hacia arriba; false = arco arriba, cartas hacia abajo
    public bool faceCenter = true;    // orientar cartas a la tangente del arco
    [Range(0f, 30f)] public float extraTilt = 6f; // inclinación extra hacia afuera

    [Header("Escala")]
    public float minScale = 0.95f;    // bordes un poco más pequeños
    public float maxScale = 1.00f;    // centro más grande

    [Header("Selección (realce)")]
    public float raiseSelectedBy = 20f; // eleva Y si la carta está seleccionada (usa CardView.IsSelected)

    [Header("Animación")]
    public bool smooth = true;              // interpolar suavemente en Play
    [Range(0f, 20f)] public float lerpSpeed = 10f;

    RectTransform rt;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
        LayoutNow(true);
    }

    void OnTransformChildrenChanged()
    {
        LayoutNow(false);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        LayoutNow(false);
    }
#endif

    void Update()
    {
        if (!Application.isPlaying)
        {
            LayoutNow(true);
            return;
        }
        if (smooth) LayoutNow(true);
    }

    void LayoutNow(bool interpolate)
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        int n = rt.childCount;
        if (n == 0) return;

        // Centro local del panel (usamos (0,0) para trabajar en anchoredPosition)
        Vector2 panelCenter = Vector2.zero;

        // Ángulos: centramos el abanico alrededor de 0°
        float start = -totalArcDegrees * 0.5f;
        float step = (n > 1) ? totalArcDegrees / (n - 1) : 0f;

        for (int i = 0; i < n; i++)
        {
            var child = rt.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            float jitter = (spacingJitter == 0f) ? 0f : (Mathf.PerlinNoise(i * 0.37f, 0.123f) - 0.5f) * spacingJitter;
            float angleDeg = start + step * i + jitter;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // Vector en el círculo según orientación del arco
            Vector2 onCircle;
            float rotZ;
            if (arcAtBottom)
            {
                // 0° mira hacia ARRIBA (arco abajo)
                onCircle = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
                rotZ = -angleDeg; // tangente hacia arriba
            }
            else
            {
                // 0° mira hacia ABAJO (arco arriba)
                onCircle = new Vector2(Mathf.Sin(angleRad), -Mathf.Cos(angleRad));
                rotZ = angleDeg; // tangente hacia abajo
            }

            if (faceCenter) rotZ += Mathf.Sign(angleDeg) * extraTilt;

            // Posición base sobre el arco + offset vertical
            Vector2 targetPos = panelCenter + onCircle * radius + new Vector2(0f, verticalOffset);

            // Realce si está seleccionada (usa tu CardView)
            var cv = child.GetComponent<CardView>();
            if (cv && cv.IsSelected)
                targetPos.y += raiseSelectedBy;

            // Escala: centro un poco más grande
            float t = (n <= 1) ? 1f : 1f - Mathf.Abs((i - (n - 1) * 0.5f)) / ((n - 1) * 0.5f);
            float scale = Mathf.Lerp(minScale, maxScale, t);

            // Asegura pivote centrado (rotación/escala bonitas)
            child.pivot = new Vector2(0.5f, 0.5f);
            // Anchors recomendados: middle center (por defecto en UI Image)

            if (smooth && Application.isPlaying && interpolate)
            {
                child.anchoredPosition = Vector2.Lerp(child.anchoredPosition, targetPos, Time.deltaTime * lerpSpeed);
                float currentRot = child.localEulerAngles.z;
                float newRot = Mathf.LerpAngle(currentRot, rotZ, Time.deltaTime * lerpSpeed);
                child.localEulerAngles = new Vector3(0, 0, newRot);
                child.localScale = Vector3.Lerp(child.localScale, Vector3.one * scale, Time.deltaTime * lerpSpeed);
            }
            else
            {
                child.anchoredPosition = targetPos;
                child.localEulerAngles = new Vector3(0, 0, rotZ);
                child.localScale = Vector3.one * scale;
            }
        }
    }
}
