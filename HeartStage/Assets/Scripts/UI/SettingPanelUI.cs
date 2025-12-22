using UnityEngine;
using UnityEngine.UI;

public class SettingPanelUI : GenericWindow
{
    [Header("볼륨")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;

    [Header("프레임")]
    [SerializeField] private Toggle highFrmeToggle;
    [SerializeField] private Toggle lowFrmeToggle;

    [Header("버튼")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button copyUidButton;
    [SerializeField] private Button logoutButton;

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        highFrmeToggle.onValueChanged.AddListener(OnToggle60Changed);
        lowFrmeToggle.onValueChanged.AddListener(OnToggle30Changed);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);

        if (copyUidButton != null)
            copyUidButton.onClick.AddListener(OnClickCopyUid);

        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnClickLogout);
    }

    public override void Open()
    {
        base.Open();
        LoadCurrentVolumeSettings();
    }
    public override void Close()
    {
        base.Close();
    }

    private void OnSFXVolumeChanged(float value)
    {
       SoundManager.Instance.SetSFXVolumeByMixer(value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        SoundManager.Instance.SetBGMVolumeByMixer(value);
    }

    private void LoadCurrentVolumeSettings()
    {
        if(sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = SaveLoadManager.Data.sfxVolume;
        }

        if(bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = SaveLoadManager.Data.bgmVolume;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 리스너 해제
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        }
    }

    private void OnClickClose()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }

    private void OnClickCopyUid()
    {
        string uid = AuthManager.Instance?.UserId;
        if (string.IsNullOrEmpty(uid))
        {
            ToastUI.Error("UID를 가져올 수 없습니다");
            return;
        }

        GUIUtility.systemCopyBuffer = uid;
        ToastUI.Success("UID가 복사되었습니다");
    }

    private void OnClickLogout()
    {
        ConfirmDialog.ShowLogout(
            onConfirm: () =>
            {
                // 다이얼로그 즉시 숨기기 (씬 전환 전에 완전히 사라지도록)
                if (ConfirmDialog.Instance != null)
                    ConfirmDialog.Instance.gameObject.SetActive(false);

                Close();
                AuthManager.Instance?.SignOut();
            }
        );
    }

    private void OnToggle30Changed(bool isOn)
    {
        if (isOn)
            SetFPS(30);
    }

    private void OnToggle60Changed(bool isOn)
    {
        if (isOn)
            SetFPS(60);
    }

    private void SetFPS(int fps)
    {
        Application.targetFrameRate = fps;
    }
}