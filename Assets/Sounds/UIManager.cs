using UnityEngine;

public class UIOptionsPanel : MonoBehaviour
{
    [Header("Canvas de ajustes")]
    public GameObject canvasAjustesAudio;

    public void AbrirAjustesAudio()
    {
        canvasAjustesAudio.SetActive(true);
    }

    public void CerrarAjustesAudio()
    {
        canvasAjustesAudio.SetActive(false);
    }

    public void ToggleAjustesAudio()
    {
        bool activo = canvasAjustesAudio.activeSelf;
        canvasAjustesAudio.SetActive(!activo);
    }
}
