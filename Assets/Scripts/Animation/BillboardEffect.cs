using UnityEngine;

namespace TCGARPrototype
{
    /// <summary>
    /// Hace que el objeto siempre mire hacia la cámara (efecto billboard)
    /// Perfecto para sprites 2D en entorno 3D/AR
    /// </summary>
    public class BillboardEffect : MonoBehaviour
    {
        public enum BillboardType
        {
            LookAtCamera,           // Rotación completa hacia la cámara
            CameraForwardFacing,     // Alineado con el forward de la cámara
            YAxisOnly               // Solo rota en el eje Y (vertical)
        }

        [Header("Billboard Settings")]
        [SerializeField] private BillboardType billboardType = BillboardType.LookAtCamera;
        [SerializeField] private bool reverseFace = false;
        [SerializeField] private bool lockXRotation = false;
        [SerializeField] private bool lockYRotation = false;
        [SerializeField] private bool lockZRotation = false;

        [Header("Performance")]
        [SerializeField] private bool useMainCamera = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float updateInterval = 0f; // 0 = cada frame

        private float lastUpdateTime;
        private Vector3 originalRotation;

        void Start()
        {
            // Guardar rotación original
            originalRotation = transform.rotation.eulerAngles;

            // Obtener cámara si no está asignada
            if (useMainCamera || targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindObjectOfType<Camera>();
                }
            }

            if (targetCamera == null)
            {
                Debug.LogError("[Billboard] No se encontró cámara en la escena!");
                enabled = false;
            }
        }

        void LateUpdate()
        {
            // Control de frecuencia de actualización para optimización
            if (updateInterval > 0 && Time.time - lastUpdateTime < updateInterval)
            {
                return;
            }

            if (targetCamera == null) return;

            ApplyBillboard();
            lastUpdateTime = Time.time;
        }

        private void ApplyBillboard()
        {
            switch (billboardType)
            {
                case BillboardType.LookAtCamera:
                    LookAtCameraRotation();
                    break;

                case BillboardType.CameraForwardFacing:
                    CameraForwardFacingRotation();
                    break;

                case BillboardType.YAxisOnly:
                    YAxisOnlyRotation();
                    break;
            }
        }

        private void LookAtCameraRotation()
        {
            // Calcular dirección hacia la cámara
            Vector3 targetDirection = targetCamera.transform.position - transform.position;

            if (reverseFace)
            {
                targetDirection = -targetDirection;
            }

            // Crear rotación mirando hacia la cámara
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // Aplicar bloqueos de ejes si es necesario
            ApplyAxisLocks(ref targetRotation);

            transform.rotation = targetRotation;
        }

        private void CameraForwardFacingRotation()
        {
            // Alinear con la dirección forward de la cámara
            Vector3 cameraForward = targetCamera.transform.forward;

            if (reverseFace)
            {
                cameraForward = -cameraForward;
            }

            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

            ApplyAxisLocks(ref targetRotation);

            transform.rotation = targetRotation;
        }

        private void YAxisOnlyRotation()
        {
            // Solo rotar en el eje Y (mantener verticalidad)
            Vector3 targetDirection = targetCamera.transform.position - transform.position;
            targetDirection.y = 0; // Eliminar componente vertical

            if (targetDirection != Vector3.zero)
            {
                if (reverseFace)
                {
                    targetDirection = -targetDirection;
                }

                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                // Mantener rotación X y Z originales
                Vector3 euler = targetRotation.eulerAngles;
                euler.x = originalRotation.x;
                euler.z = originalRotation.z;

                transform.rotation = Quaternion.Euler(euler);
            }
        }

        private void ApplyAxisLocks(ref Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;

            if (lockXRotation) euler.x = originalRotation.x;
            if (lockYRotation) euler.y = originalRotation.y;
            if (lockZRotation) euler.z = originalRotation.z;

            rotation = Quaternion.Euler(euler);
        }

        /// <summary>
        /// Establece la cámara objetivo manualmente
        /// </summary>
        public void SetCamera(Camera camera)
        {
            targetCamera = camera;
            useMainCamera = false;
        }

        /// <summary>
        /// Cambia el tipo de billboard en runtime
        /// </summary>
        public void SetBillboardType(BillboardType type)
        {
            billboardType = type;
        }

        /// <summary>
        /// Optimización: ajusta el intervalo de actualización
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0f, interval);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (targetCamera != null)
            {
                // Dibujar línea hacia la cámara para debug
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetCamera.transform.position);
            }
        }
#endif
    }
}