using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResultAppear : MonoBehaviour
{
    [Header("Configuración de Animación")]
    [SerializeField] private float appearDuration = 0.5f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float disappearDuration = 0.5f;

    [Header("Efectos")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool rotateOnAppear = true;
    [SerializeField] private float rotationAmount = 360f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Obtener o agregar CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Iniciar oculto
        canvasGroup.alpha = 0;
        transform.localScale = Vector3.zero;
    }

    public void Play()
    {
        StartCoroutine(AnimateResult());
    }

    private IEnumerator AnimateResult()
    {
        // Fase 1: Aparecer
        yield return StartCoroutine(AppearAnimation());

        // Fase 2: Mostrar
        yield return new WaitForSeconds(displayDuration);

        // Fase 3: Desaparecer
        yield return StartCoroutine(DisappearAnimation());

        // Destruir el objeto
        Destroy(gameObject);
    }

    private IEnumerator AppearAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        float startRotation = 0f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;

            // Escala
            float scaleValue = scaleCurve.Evaluate(t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, scaleValue);

            // Alpha
            float alphaValue = alphaCurve.Evaluate(t);
            canvasGroup.alpha = alphaValue;

            // Rotación opcional
            if (rotateOnAppear && rectTransform != null)
            {
                float rotation = Mathf.Lerp(startRotation, rotationAmount, t);
                rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
            }

            yield return null;
        }

        // Asegurar valores finales
        transform.localScale = targetScale;
        canvasGroup.alpha = 1f;

        if (rotateOnAppear && rectTransform != null)
        {
            rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        // Efecto de "bounce" opcional
        yield return StartCoroutine(BounceEffect());
    }

    private IEnumerator BounceEffect()
    {
        float bounceDuration = 0.2f;
        float elapsed = 0f;

        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;

            // Curva de bounce
            float bounceScale = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
            transform.localScale = Vector3.one * bounceScale;

            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    private IEnumerator DisappearAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = Vector3.zero;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disappearDuration;

            // Escala hacia abajo
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            // Fade out
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            // Rotación opcional al desaparecer
            if (rotateOnAppear && rectTransform != null)
            {
                float rotation = Mathf.Lerp(0, -rotationAmount * 0.5f, t);
                rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
            }

            yield return null;
        }

        transform.localScale = targetScale;
        canvasGroup.alpha = 0f;
    }

    // Método público para detener la animación si es necesario
    public void Stop()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}