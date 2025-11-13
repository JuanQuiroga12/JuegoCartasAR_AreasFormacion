using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    public RawImage videoImage;
    public VideoPlayer videoPlayer;
    public Button skipButton;
    public GameObject sceneContent; // Panel u objetos a mostrar tras el video

    void Start()
    {
        videoImage.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(true);
        sceneContent.SetActive(false);
        videoPlayer.Play();
        videoPlayer.loopPointReached += OnVideoFinished;
        skipButton.onClick.AddListener(SkipVideo);
    }

    public GameObject videoGroup; // asigna el GameObject padre que contiene videoImage y skipButton

    void SkipVideo()
    {
        videoPlayer.Stop();
        videoGroup.SetActive(false);
        sceneContent.SetActive(true);
    }


    void OnVideoFinished(VideoPlayer vp)
    {
        EndVideo();
    }

    void EndVideo()
    {
        videoImage.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
        sceneContent.SetActive(true);
    }
}
