using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Audio Source SFX")]
    public AudioSource sfxSource;

    [Header("Clips de efectos")]
    public AudioClip clickBoton;
    public AudioClip fusionar;
    public AudioClip fusionButtonEnabledClip;
    public AudioClip daño;
    public AudioClip muerte;
    public AudioClip victoria;
    public AudioClip error;


    [Header("Timer")]
    public AudioSource timerSource;      // Nuevo AudioSource
    public AudioClip timerLoopClip;      // Clip que se va a loopear

    void Awake()
    {
        // Singleton sencillo para acceder desde otros scripts
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // opcional, si quieres que siga entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayClickBoton()
    {
        if (clickBoton != null && sfxSource != null && sfxSource.enabled && sfxSource.gameObject.activeInHierarchy)
        {
            sfxSource.PlayOneShot(clickBoton);
        }
    }


    // Llamar cuando empiece un temporizador (preparación o turno)
    public void StartTimerLoop()
    {
        Debug.Log("[SFXManager] StartTimerLoop llamado"); // debug

        if (timerSource == null || timerLoopClip == null)
        {
            Debug.LogWarning("[SFXManager] Timer no configurado (timerSource o timerLoopClip es null)");
            return;
        }

        timerSource.clip = timerLoopClip;
        timerSource.loop = true;
        if (!timerSource.isPlaying)
            timerSource.Play();
    }


    // Llamar cuando termine el temporizador
    public void StopTimerLoop()
    {
        if (timerSource == null) return;

        timerSource.loop = false;
        timerSource.Stop();
    }

    public void PlayFusionar()
    {
        PlaySFX(fusionar);
    }

    public void PlayFusionButtonEnabled()
    {
        if (fusionButtonEnabledClip != null && sfxSource != null)
            sfxSource.PlayOneShot(fusionButtonEnabledClip);
    }

    public void PlayDaño()
    {
        PlaySFX(daño);
    }

    public void PlayMuerte()
    {
        PlaySFX(muerte);
    }

    public void PlayVictoria()
    {
        PlaySFX(victoria);
    }

    public void PlayError()
    {
        PlaySFX(error);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
