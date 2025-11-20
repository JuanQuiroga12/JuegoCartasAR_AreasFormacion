#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ValidateFusionSelector : EditorWindow
{
    [MenuItem("Tools/Validate Fusion Selector")]
    public static void Validate()
    {
        FusionResultSelector selector = Object.FindFirstObjectByType<FusionResultSelector>();

        if (selector == null)
        {
            Debug.LogError("[Validator] ❌ FusionResultSelector no encontrado en la escena!");
            EditorUtility.DisplayDialog("Error", "No se encontró FusionResultSelector en la escena", "OK");
            return;
        }

        Debug.Log("[Validator] ✅ FusionResultSelector encontrado");

        // Validar referencias usando reflection
        var selectorPanel = typeof(FusionResultSelector)
            .GetField("selectorPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(selector) as GameObject;

        if (selectorPanel == null)
        {
            Debug.LogError("[Validator] ❌ selectorPanel NO ESTÁ ASIGNADO!");
        }
        else
        {
            Debug.Log($"[Validator] ✅ selectorPanel: {selectorPanel.name}");
            Debug.Log($"[Validator]    - Activo en escena: {selectorPanel.activeInHierarchy}");
            Debug.Log($"[Validator]    - Activo self: {selectorPanel.activeSelf}");

            Canvas canvas = selectorPanel.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Validator] ❌ Panel no tiene Canvas padre!");
            }
            else
            {
                Debug.Log($"[Validator] ✅ Canvas padre: {canvas.name}");
            }
        }

        EditorUtility.DisplayDialog("Validación Completa", "Revisa la consola para ver los resultados", "OK");
    }
}
#endif