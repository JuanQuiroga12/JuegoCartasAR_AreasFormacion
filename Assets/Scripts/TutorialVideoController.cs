using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialVideoController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelPrincipal;
    public GameObject panelTutorial;

    [Header("Video UI")]
    public RawImage videoImage;
    public VideoPlayer videoPlayer;
    public Button skipButton;

    void Start()
    {
        // Estado inicial: se ve el panel principal
        panelPrincipal.SetActive(true);
        panelTutorial.SetActive(false);

        // Evento fin de video
        videoPlayer.loopPointReached += OnVideoFinished;

        // Evento botón Skip
        skipButton.onClick.AddListener(SkipVideo);
    }

    // Asigna este método al botón "Tutorial" en el OnClick del inspector
    public void OpenTutorial()
    {
        panelPrincipal.SetActive(false);
        panelTutorial.SetActive(true);

        videoImage.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(true);

        videoPlayer.Stop();
        videoPlayer.Play();
    }

    void SkipVideo()
    {
        videoPlayer.Stop();
        CloseTutorial();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        CloseTutorial();
    }

    void CloseTutorial()
    {
        panelTutorial.SetActive(false);
        panelPrincipal.SetActive(true);
    }
}
