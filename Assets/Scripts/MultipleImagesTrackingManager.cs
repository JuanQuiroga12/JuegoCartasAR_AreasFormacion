using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MultipleImagesTrackingManager : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();

    // 🔥 NUEVO: Diccionario para mapear imágenes a prefabs manualmente
    [Header("Prefab Mapping")]
    [SerializeField] private List<ImagePrefabPair> imagePrefabMapping = new List<ImagePrefabPair>();
    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    // 🔥 NUEVO: Eventos para notificar detecciones
    public System.Action<string, ARTrackedImage> OnImageDetected;
    public System.Action<string> OnImageLost;

    void Awake()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        // 🔥 NUEVO: Inicializar diccionario de prefabs
        InitializePrefabDictionary();
    }

    // 🔥 NUEVO: Método para inicializar el diccionario de prefabs
    private void InitializePrefabDictionary()
    {
        prefabDictionary.Clear();
        foreach (var pair in imagePrefabMapping)
        {
            if (!string.IsNullOrEmpty(pair.imageName) && pair.prefab != null)
            {
                prefabDictionary[pair.imageName] = pair.prefab;
                Debug.Log($"[MultipleImagesTracking] Prefab mapeado: {pair.imageName} → {pair.prefab.name}");
            }
        }
    }

    void OnEnable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        }
    }

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // Imágenes recién añadidas
        foreach (var trackedImage in eventArgs.added)
        {
            UpdateTrackedImage(trackedImage);
        }

        // Imágenes actualizadas
        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateTrackedImage(trackedImage);
        }

        // 🔥 FIX: Imágenes removidas - usar .Value para acceder al ARTrackedImage
        foreach (var trackedImagePair in eventArgs.removed)
        {
            RemoveTrackedImage(trackedImagePair.Value);
        }
    }

    private void UpdateTrackedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // Si la imagen está siendo rastreada
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            // 🔥 NUEVO: Notificar que la imagen fue detectada
            OnImageDetected?.Invoke(imageName, trackedImage);

            // Si no existe el prefab, crearlo
            if (!spawnedPrefabs.ContainsKey(imageName))
            {
                // 🔥 CORREGIDO: Buscar prefab en el diccionario manual
                if (prefabDictionary.TryGetValue(imageName, out GameObject prefab))
                {
                    GameObject spawnedObject = Instantiate(prefab, trackedImage.transform);
                    spawnedPrefabs[imageName] = spawnedObject;
                    Debug.Log($"[MultipleImagesTracking] ✅ Prefab instanciado para: {imageName}");
                }
                else
                {
                    Debug.LogWarning($"[MultipleImagesTracking] ⚠️ No hay prefab asignado para: {imageName}");
                }
            }
            else
            {
                // Actualizar posición del prefab existente
                spawnedPrefabs[imageName].transform.position = trackedImage.transform.position;
                spawnedPrefabs[imageName].transform.rotation = trackedImage.transform.rotation;
                spawnedPrefabs[imageName].SetActive(true);
            }
        }
        else if (trackedImage.trackingState == TrackingState.Limited ||
                 trackedImage.trackingState == TrackingState.None)
        {
            // 🔥 NUEVO: Notificar que se perdió el tracking
            OnImageLost?.Invoke(imageName);

            // Ocultar el prefab si el tracking es limitado o se perdió
            if (spawnedPrefabs.ContainsKey(imageName))
            {
                spawnedPrefabs[imageName].SetActive(false);
            }
        }
    }

    private void RemoveTrackedImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // 🔥 NUEVO: Notificar que la imagen se removió
        OnImageLost?.Invoke(imageName);

        if (spawnedPrefabs.ContainsKey(imageName))
        {
            Destroy(spawnedPrefabs[imageName]);
            spawnedPrefabs.Remove(imageName);
            Debug.Log($"[MultipleImagesTracking] 🗑️ Prefab destruido para: {imageName}");
        }
    }
}

// 🔥 NUEVO: Clase auxiliar para mapear imágenes a prefabs en el Inspector
[System.Serializable]
public class ImagePrefabPair
{
    [Tooltip("Nombre de la imagen en la Reference Image Library")]
    public string imageName;

    [Tooltip("Prefab a instanciar cuando se detecte esta imagen")]
    public GameObject prefab;
}