using UnityEngine;
using UnityEngine.UI;

public class SpecialDungeonUI : GenericWindow
{
    [SerializeField] private Button stroyButton;
    [SerializeField] private Button infiniteButton;

    [Header("Infinite Stage Settings")]
    [SerializeField] private int infiniteStageId = 90001; // 기본 무한 스테이지 ID

    private void Awake()
    {
        stroyButton.onClick.RemoveAllListeners();
        stroyButton.onClick.AddListener(OnStoryButtonClicked);

        if (infiniteButton != null)
        {
            infiniteButton.onClick.RemoveAllListeners();
            infiniteButton.onClick.AddListener(OnInfiniteButtonClicked);
        }
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }

    private void OnStoryButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        WindowManager.Instance.OpenOverlay(WindowType.StoryDungeon);
    }

    private void OnInfiniteButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (LoadSceneManager.Instance == null)
        {
            Debug.LogError("[SpecialDungeonUI] LoadSceneManager.Instance가 null입니다!");
            return;
        }

        LoadSceneManager.Instance.GoInfiniteStage(infiniteStageId);
    }
}
