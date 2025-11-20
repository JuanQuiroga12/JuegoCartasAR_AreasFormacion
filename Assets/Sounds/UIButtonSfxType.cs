using UnityEngine;
using UnityEngine.UI;

public enum UIButtonSfxType
{
    ClickNormal,
    Fusionar,
    FusionButtonEnabled
}

[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour
{
    public UIButtonSfxType sfxType = UIButtonSfxType.ClickNormal;

    void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(PlaySfx);
    }

    private void PlaySfx()
    {
        if (SFXManager.Instance == null) return;

        switch (sfxType)
        {
            case UIButtonSfxType.ClickNormal:
                SFXManager.Instance.PlayClickBoton();
                break;

            case UIButtonSfxType.Fusionar:
                SFXManager.Instance.PlayFusionar();
                break;

            case UIButtonSfxType.FusionButtonEnabled:
                SFXManager.Instance.PlayFusionButtonEnabled();
                break;
        }
    }
}
