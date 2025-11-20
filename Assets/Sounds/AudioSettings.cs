using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [Header("Mixer")]
    public AudioMixer mainMixer;

    [Header("Sliders UI")]
    public Slider musicSlider;
    public Slider sfxSlider;

    // Nombres EXACTOS de los parámetros expuestos en el AudioMixer
    private const string MUSIC_PARAM = "MusicVol";
    private const string SFX_PARAM = "SFXVol";

    // Claves para PlayerPrefs
    private const string MUSIC_PREF = "MusicVolume";
    private const string SFX_PREF = "SFXVolume";

    void Start()
    {
        // Cargar valores guardados o usar 1 por defecto
        float musicValue = PlayerPrefs.GetFloat(MUSIC_PREF, 1f);
        float sfxValue = PlayerPrefs.GetFloat(SFX_PREF, 1f);

        // Asignar a sliders SIN disparar eventos adicionales
        musicSlider.value = musicValue;
        sfxSlider.value = sfxValue;

        // Aplicar al mixer
        SetMusicVolume(musicValue);
        SetSFXVolume(sfxValue);

        // Suscribirse a los cambios de los sliders
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
    }

    private void OnDestroy()
    {
        // Buen hábito: quitar listeners
        musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
    }

    public void OnMusicSliderChanged(float value)
    {
        SetMusicVolume(value);
        PlayerPrefs.SetFloat(MUSIC_PREF, value);
        PlayerPrefs.Save();
    }

    public void OnSFXSliderChanged(float value)
    {
        SetSFXVolume(value);
        PlayerPrefs.SetFloat(SFX_PREF, value);
        PlayerPrefs.Save();
    }

    private void SetMusicVolume(float value)
    {
        // Convertir valor lineal (0–1) a dB
        float dB = Mathf.Log10(value) * 20f;
        mainMixer.SetFloat(MUSIC_PARAM, dB);
    }

    private void SetSFXVolume(float value)
    {
        float dB = Mathf.Log10(value) * 20f;
        mainMixer.SetFloat(SFX_PARAM, dB);
    }
}
