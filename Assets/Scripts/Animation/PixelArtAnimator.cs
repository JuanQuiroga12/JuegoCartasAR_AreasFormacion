using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TCGARPrototype
{
    /// <summary>
    /// Gestiona la animación de sprites pixel art para las cartas
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelArtAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class AnimationClip
        {
            public string name;
            public Sprite[] frames;
            public float frameRate = 12f;
            public bool loop = true;
            public bool pingPong = false;
        }

        [Header("Animation Settings")]
        [SerializeField] private List<AnimationClip> animations = new List<AnimationClip>();
        [SerializeField] private string defaultAnimation = "idle";
        [SerializeField] private bool playOnStart = true;

        [Header("Sprite Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private int pixelsPerUnit = 100;
        [SerializeField] private FilterMode filterMode = FilterMode.Point; // Para mantener estética pixel art

        [Header("Effects")]
        [SerializeField] private bool enableFloatingEffect = true;
        [SerializeField] private float floatAmplitude = 0.01f;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private bool enablePulseEffect = false;
        [SerializeField] private float pulseScale = 0.1f;
        [SerializeField] private float pulseSpeed = 1f;

        private AnimationClip currentClip;
        private Coroutine animationCoroutine;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private float floatOffset;

        void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Configurar modo de filtrado para pixel art
            foreach (var animation in animations)
            {
                foreach (var sprite in animation.frames)
                {
                    if (sprite != null && sprite.texture != null)
                    {
                        sprite.texture.filterMode = filterMode;
                    }
                }
            }
        }

        void Start()
        {
            originalPosition = transform.localPosition;
            originalScale = transform.localScale;
            floatOffset = Random.Range(0f, 2f * Mathf.PI);

            if (playOnStart && !string.IsNullOrEmpty(defaultAnimation))
            {
                PlayAnimation(defaultAnimation);
            }
        }

        void Update()
        {
            // Aplicar efectos visuales
            if (enableFloatingEffect)
            {
                ApplyFloatingEffect();
            }

            if (enablePulseEffect)
            {
                ApplyPulseEffect();
            }
        }

        /// <summary>
        /// Reproduce una animación por nombre
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            AnimationClip clip = animations.Find(a => a.name == animationName);

            if (clip != null && clip.frames.Length > 0)
            {
                if (animationCoroutine != null)
                {
                    StopCoroutine(animationCoroutine);
                }

                currentClip = clip;
                animationCoroutine = StartCoroutine(PlayAnimationCoroutine(clip));
            }
            else
            {
                Debug.LogWarning($"[PixelArt] Animación '{animationName}' no encontrada o sin frames");
            }
        }

        /// <summary>
        /// Detiene la animación actual
        /// </summary>
        public void StopAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
        }

        /// <summary>
        /// Reproduce una animación una sola vez y luego vuelve a la default
        /// </summary>
        public void PlayOneShot(string animationName)
        {
            AnimationClip clip = animations.Find(a => a.name == animationName);

            if (clip != null)
            {
                if (animationCoroutine != null)
                {
                    StopCoroutine(animationCoroutine);
                }

                animationCoroutine = StartCoroutine(PlayOneShotCoroutine(clip));
            }
        }

        private IEnumerator PlayAnimationCoroutine(AnimationClip clip)
        {
            float frameDuration = 1f / clip.frameRate;
            int frameIndex = 0;
            int direction = 1;

            while (true)
            {
                // Mostrar frame actual
                if (frameIndex >= 0 && frameIndex < clip.frames.Length)
                {
                    spriteRenderer.sprite = clip.frames[frameIndex];
                }

                yield return new WaitForSeconds(frameDuration);

                // Avanzar al siguiente frame
                if (clip.pingPong)
                {
                    frameIndex += direction;

                    // Cambiar dirección en los extremos
                    if (frameIndex >= clip.frames.Length - 1 || frameIndex <= 0)
                    {
                        direction *= -1;
                    }
                }
                else
                {
                    frameIndex++;

                    // Reiniciar si es loop
                    if (frameIndex >= clip.frames.Length)
                    {
                        if (clip.loop)
                        {
                            frameIndex = 0;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        private IEnumerator PlayOneShotCoroutine(AnimationClip clip)
        {
            float frameDuration = 1f / clip.frameRate;

            // Reproducir todos los frames una vez
            for (int i = 0; i < clip.frames.Length; i++)
            {
                spriteRenderer.sprite = clip.frames[i];
                yield return new WaitForSeconds(frameDuration);
            }

            // Volver a la animación default
            if (!string.IsNullOrEmpty(defaultAnimation))
            {
                PlayAnimation(defaultAnimation);
            }
        }

        private void ApplyFloatingEffect()
        {
            float yOffset = Mathf.Sin(Time.time * floatSpeed + floatOffset) * floatAmplitude;
            transform.localPosition = originalPosition + Vector3.up * yOffset;
        }

        private void ApplyPulseEffect()
        {
            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            transform.localScale = originalScale * scale;
        }

        /// <summary>
        /// Añade una nueva animación en runtime
        /// </summary>
        public void AddAnimation(string name, Sprite[] frames, float frameRate = 12f, bool loop = true)
        {
            AnimationClip newClip = new AnimationClip
            {
                name = name,
                frames = frames,
                frameRate = frameRate,
                loop = loop
            };

            animations.Add(newClip);
        }

        /// <summary>
        /// Cambia el color del sprite
        /// </summary>
        public void SetSpriteColor(Color color)
        {
            spriteRenderer.color = color;
        }

        /// <summary>
        /// Aplica un efecto de fade
        /// </summary>
        public void FadeIn(float duration)
        {
            StartCoroutine(FadeCoroutine(0f, 1f, duration));
        }

        public void FadeOut(float duration)
        {
            StartCoroutine(FadeCoroutine(1f, 0f, duration));
        }

        private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration)
        {
            Color color = spriteRenderer.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                spriteRenderer.color = color;
                yield return null;
            }

            color.a = toAlpha;
            spriteRenderer.color = color;
        }

        /// <summary>
        /// Obtiene información de la animación actual
        /// </summary>
        public string GetCurrentAnimationName()
        {
            return currentClip?.name ?? "none";
        }

        public bool IsPlaying()
        {
            return animationCoroutine != null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Asegurar que los sprites usen Point filter para pixel art
            foreach (var animation in animations)
            {
                if (animation.frames != null)
                {
                    foreach (var sprite in animation.frames)
                    {
                        if (sprite != null && sprite.texture != null)
                        {
                            sprite.texture.filterMode = filterMode;
                        }
                    }
                }
            }
        }
#endif
    }
}