using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace TCGARPrototype
{
    /// <summary>
    /// Gestiona el tracking de cartas y spawn de animaciones pixel art
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class ARCardTracker : MonoBehaviour
    {
        [Header("AR Configuration")]
        [SerializeField] private ARTrackedImageManager trackedImageManager;
        [SerializeField] private GameObject pixelArtPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float animationHeight = 0.05f; // Altura sobre la carta
        [SerializeField] private float animationScale = 0.1f;
        [SerializeField] private bool enableBillboard = true;

        // Dictionary para gestionar múltiples cartas
        private Dictionary<string, GameObject> spawnedAnimations = new Dictionary<string, GameObject>();
        private Camera arCamera;

        void Awake()
        {
            trackedImageManager = GetComponent<ARTrackedImageManager>();
            arCamera = Camera.main;

            if (arCamera == null)
            {
                arCamera = FindObjectOfType<Camera>();
            }
        }

        void OnEnable()
        {
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        void OnDisable()
        {
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            // Procesar nuevas cartas detectadas
            foreach (var trackedImage in eventArgs.added)
            {
                SpawnAnimationOnCard(trackedImage);
            }

            // Actualizar cartas existentes
            foreach (var trackedImage in eventArgs.updated)
            {
                UpdateAnimationOnCard(trackedImage);
            }

            // Ocultar animaciones de cartas perdidas
            foreach (var trackedImage in eventArgs.removed)
            {
                HideAnimation(trackedImage);
            }
        }

        private void SpawnAnimationOnCard(ARTrackedImage trackedImage)
        {
            string imageName = trackedImage.referenceImage.name;

            // Verificar si ya existe una animación para esta carta
            if (!spawnedAnimations.ContainsKey(imageName))
            {
                // Instanciar el prefab de animación pixel art
                GameObject animationObject = Instantiate(pixelArtPrefab);
                animationObject.name = $"PixelArt_{imageName}";

                // Configurar posición y escala inicial
                animationObject.transform.position = trackedImage.transform.position + Vector3.up * animationHeight;
                animationObject.transform.localScale = Vector3.one * animationScale;

                // Añadir componente Billboard si está habilitado
                if (enableBillboard)
                {
                    BillboardEffect billboard = animationObject.GetComponent<BillboardEffect>();
                    if (billboard == null)
                    {
                        billboard = animationObject.AddComponent<BillboardEffect>();
                    }
                    billboard.SetCamera(arCamera);
                }

                // Guardar referencia
                spawnedAnimations[imageName] = animationObject;

                Debug.Log($"[AR] Carta detectada: {imageName}");
            }

            UpdateAnimationOnCard(trackedImage);
        }

        private void UpdateAnimationOnCard(ARTrackedImage trackedImage)
        {
            string imageName = trackedImage.referenceImage.name;

            if (spawnedAnimations.TryGetValue(imageName, out GameObject animationObject))
            {
                // Actualizar visibilidad según el estado de tracking
                switch (trackedImage.trackingState)
                {
                    case TrackingState.Tracking:
                        animationObject.SetActive(true);
                        // Actualizar posición con offset vertical
                        animationObject.transform.position =
                            trackedImage.transform.position + Vector3.up * animationHeight;
                        break;

                    case TrackingState.Limited:
                    case TrackingState.None:
                        animationObject.SetActive(false);
                        break;
                }
            }
        }

        private void HideAnimation(ARTrackedImage trackedImage)
        {
            string imageName = trackedImage.referenceImage.name;

            if (spawnedAnimations.TryGetValue(imageName, out GameObject animationObject))
            {
                animationObject.SetActive(false);
                Debug.Log($"[AR] Carta perdida: {imageName}");
            }
        }

        /// <summary>
        /// Método para cambiar dinámicamente la animación de una carta
        /// </summary>
        public void ChangeCardAnimation(string cardName, GameObject newAnimationPrefab)
        {
            if (spawnedAnimations.TryGetValue(cardName, out GameObject currentAnimation))
            {
                Vector3 position = currentAnimation.transform.position;
                Destroy(currentAnimation);

                GameObject newAnimation = Instantiate(newAnimationPrefab, position, Quaternion.identity);
                spawnedAnimations[cardName] = newAnimation;

                if (enableBillboard)
                {
                    BillboardEffect billboard = newAnimation.AddComponent<BillboardEffect>();
                    billboard.SetCamera(arCamera);
                }
            }
        }

        /// <summary>
        /// Limpia todas las animaciones spawneadas
        /// </summary>
        public void ClearAllAnimations()
        {
            foreach (var kvp in spawnedAnimations)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            spawnedAnimations.Clear();
        }
    }
}