using UnityEngine;
using UnityEngine.UI;

public class StoryDungeonUI : GenericWindow
{
    [SerializeField] private Button stroyButton;

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        stroyButton.onClick.RemoveAllListeners();
        stroyButton.onClick.AddListener(OnStoryButtonClicked);
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

        if (IsStoryDungeonInfoActive())
        {
            // 활성화되어 있으면 닫기
            WindowManager.Instance.CloseOverlay(WindowType.StoryDungeonInfo);
        }
        else
        {
            // 비활성화되어 있으면 열기
            WindowManager.Instance.OpenOverlay(WindowType.StoryDungeonInfo);
        }
    }

    private bool IsStoryDungeonInfoActive()
    {
        var windows = WindowManager.Instance.windowList;

        foreach (var windowPair in windows)
        {
            if (windowPair.windowType == WindowType.StoryDungeonInfo)
            {
                return windowPair.window != null && windowPair.window.gameObject.activeSelf;
            }
        }

        return false;
    }
}
