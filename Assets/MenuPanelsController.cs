using UnityEngine;

public class MenuPanelsController : MonoBehaviour
{
    public GameObject panelMenuPrincipal;
    public GameObject panelReglas;

    public void AbrirReglas()
    {
        panelMenuPrincipal.SetActive(false);
        panelReglas.SetActive(true);
    }

    public void VolverAlMenu()
    {
        panelReglas.SetActive(false);
        panelMenuPrincipal.SetActive(true);
    }
}
