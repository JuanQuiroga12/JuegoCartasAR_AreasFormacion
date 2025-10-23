using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public GameObject[] tutorialPanels;
    private int currentIndex = 0;

    private void Start()
    {
        // Activar solo el primer panel
        for (int i = 0; i < tutorialPanels.Length; i++)
        {
            tutorialPanels[i].SetActive(i == 0);
        }
    }

    public void NextPanel()
    {
        tutorialPanels[currentIndex].SetActive(false);
        currentIndex = (currentIndex + 1) % tutorialPanels.Length;
        tutorialPanels[currentIndex].SetActive(true);
    }

    public void CloseTutorial()
    {
        // Desactivar todos los paneles del tutorial
        for (int i = 0; i < tutorialPanels.Length; i++)
        {
            tutorialPanels[i].SetActive(false);
        }
        // Activar el primer panel (menú principal)
        tutorialPanels[0].SetActive(true);
        currentIndex = 0;
    }

}
