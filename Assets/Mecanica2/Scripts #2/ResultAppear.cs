using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ResultAppear : MonoBehaviour
{
    [Header("Ajustes")]
    public float duration = 0.35f;
    public float startScale = 0.6f;
    public float startYOffset = -80f;   // inicia un poco desde abajo (negativo = abajo)
    public float startRotation = -6f;   // leve giro inicial

    private CanvasGroup cg;
    private RectTransform rt;

    // Estados objetivo
    private Vector3 targetScale;
    private Vector2 targetPos;     // ¡Vector2 porque anchoredPosition es Vector2!
    private Vector3 targetRot;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayCo());
    }

    private IEnumerator PlayCo()
    {
        // Estados finales deseados
        targetScale = Vector3.one;
        targetPos = rt.anchoredPosition;  // Vector2
        targetRot = Vector3.zero;

        // Estados iniciales
        rt.localScale = Vector3.one * startScale;
        rt.anchoredPosition = targetPos + new Vector2(0f, startYOffset); // Vector2 + Vector2
        rt.localEulerAngles = new Vector3(0f, 0f, startRotation);
        cg.alpha = 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float e = EaseOutBack(Mathf.Clamp01(t));    // easing para “pop”
            float eAlpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            rt.localScale = Vector3.LerpUnclamped(Vector3.one * startScale, targetScale, e);
            rt.anchoredPosition = Vector2.Lerp(targetPos + new Vector2(0f, startYOffset), targetPos, e);
            rt.localEulerAngles = Vector3.Lerp(new Vector3(0f, 0f, startRotation), targetRot, e);
            cg.alpha = eAlpha;

            yield return null;
        }

        // Asegura estado final exacto
        rt.localScale = targetScale;
        rt.anchoredPosition = targetPos;
        rt.localEulerAngles = targetRot;
        cg.alpha = 1f;
    }

    // Curva tipo "back" (ligero overshoot agradable)
    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = x - 1f;
        return 1f + c3 * (p * p * p) + c1 * (p * p);
    }
}
