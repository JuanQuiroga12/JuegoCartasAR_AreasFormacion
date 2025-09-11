#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace TCGARPrototype
{
    /// <summary>
    /// Herramienta para testear el sistema AR en el Editor sin necesidad de build
    /// </summary>
    public class AREditorTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private GameObject pixelArtPrefab;
        [SerializeField] private Transform testCardPosition;
        [SerializeField] private bool simulateBillboard = true;

        [Header("Simulation")]
        [SerializeField] private KeyCode spawnKey = KeyCode.Space;
        [SerializeField] private KeyCode clearKey = KeyCode.C;
        [SerializeField] private float rotationSpeed = 30f;

        private GameObject currentAnimation;
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;

            // Crear posición de test si no existe
            if (testCardPosition == null)
            {
                GameObject testCard = GameObject.CreatePrimitive(PrimitiveType.Quad);
                testCard.name = "TestCard";
                testCard.transform.localScale = new Vector3(0.063f, 0.088f, 1f);
                testCardPosition = testCard.transform;

                // Añadir textura de la carta si existe
                MeshRenderer renderer = testCard.GetComponent<MeshRenderer>();
                renderer.material = new Material(Shader.Find("Unlit/Texture"));
            }
        }

        void Update()
        {
            // Spawn animación de prueba
            if (Input.GetKeyDown(spawnKey))
            {
                SpawnTestAnimation();
            }

            // Limpiar animaciones
            if (Input.GetKeyDown(clearKey))
            {
                ClearAnimations();
            }

            // Simular movimiento de cámara con mouse
            if (Input.GetMouseButton(1))
            {
                float h = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                float v = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

                mainCamera.transform.RotateAround(
                    testCardPosition.position,
                    Vector3.up,
                    h * 10f
                );

                mainCamera.transform.RotateAround(
                    testCardPosition.position,
                    mainCamera.transform.right,
                    -v * 10f
                );
            }

            // Zoom con scroll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                mainCamera.transform.position +=
                    mainCamera.transform.forward * scroll * 2f;
            }
        }

        private void SpawnTestAnimation()
        {
            if (pixelArtPrefab == null || testCardPosition == null) return;

            // Limpiar animación anterior
            if (currentAnimation != null)
            {
                DestroyImmediate(currentAnimation);
            }

            // Crear nueva animación
            currentAnimation = Instantiate(pixelArtPrefab);
            currentAnimation.transform.position =
                testCardPosition.position + Vector3.up * 0.05f;
            currentAnimation.transform.localScale = Vector3.one * 0.1f;

            // Añadir billboard si está habilitado
            if (simulateBillboard)
            {
                BillboardEffect billboard = currentAnimation.GetComponent<BillboardEffect>();
                if (billboard == null)
                {
                    billboard = currentAnimation.AddComponent<BillboardEffect>();
                }
                billboard.SetCamera(mainCamera);
            }

            // Iniciar animación
            PixelArtAnimator animator = currentAnimation.GetComponent<PixelArtAnimator>();
            if (animator != null)
            {
                animator.PlayAnimation("idle");
            }

            Debug.Log("[Test] Animación spawneada en posición de test");
        }

        private void ClearAnimations()
        {
            if (currentAnimation != null)
            {
                DestroyImmediate(currentAnimation);
                Debug.Log("[Test] Animaciones limpiadas");
            }
        }

        void OnDrawGizmos()
        {
            if (testCardPosition != null)
            {
                // Dibujar carta de prueba
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(
                    testCardPosition.position,
                    new Vector3(0.063f, 0.088f, 0.001f)
                );

                // Dibujar área de spawn
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(
                    testCardPosition.position + Vector3.up * 0.05f,
                    0.02f
                );
            }
        }
    }

    /// <summary>
    /// Editor personalizado para AREditorTester
    /// </summary>
    [CustomEditor(typeof(AREditorTester))]
    public class AREditorTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AREditorTester tester = (AREditorTester)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controles de Test", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Space: Spawn animación\n" +
                "• C: Limpiar animaciones\n" +
                "• Click derecho + arrastrar: Rotar cámara\n" +
                "• Scroll: Zoom",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Spawn Test Animation"))
            {
                tester.SendMessage("SpawnTestAnimation");
            }

            if (GUILayout.Button("Clear All"))
            {
                tester.SendMessage("ClearAnimations");
            }
        }
    }
}
#endif