using UnityEngine;

namespace TCGARPrototype
{
    /// <summary>
    /// Hace que el objeto siempre mire hacia la c�mara (efecto billboard)
    /// Perfecto para sprites 2D en entorno 3D/AR
    /// </summary>
    public class BillboardEffect : MonoBehaviour
    {
        public enum BillboardType
        {
            LookAtCamera,           // Rotaci�n completa hacia la c�mara
            CameraForwardFacing,     // Alineado con el forward de la c�mara
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
            // Guardar rotaci�n original
            originalRotation = transform.rotation.eulerAngles;

            // Obtener c�mara si no est� asignada
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
                Debug.LogError("[Billboard] No se encontr� c�mara en la escena!");
                enabled = false;
            }
        }

        void LateUpdate()
        {
            // Control de frecuencia de actualizaci�n para optimizaci�n
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
            // Calcular direcci�n hacia la c�mara
            Vector3 targetDirection = targetCamera.transform.position - transform.position;

            if (reverseFace)
            {
                targetDirection = -targetDirection;
            }

            // Crear rotaci�n mirando hacia la c�mara
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // Aplicar bloqueos de ejes si es necesario
            ApplyAxisLocks(ref targetRotation);

            transform.rotation = targetRotation;
        }

        private void CameraForwardFacingRotation()
        {
            // Alinear con la direcci�n forward de la c�mara
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

                // Mantener rotaci�n X y Z originales
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
        /// Establece la c�mara objetivo manualmente
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
        /// Optimizaci�n: ajusta el intervalo de actualizaci�n
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
                // Dibujar l�nea hacia la c�mara para debug
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetCamera.transform.position);
            }
        }
#endif
    }
}