using UnityEngine;
using UnityEngine.UI;

public class StoryDungeonUI : GenericWindow
{
    [SerializeField] private Button stroyButton;
    [SerializeField] private Button seraStoryButton;

    // 현재 필터된 스토리 타입을 저장
    public static string currentStoryFilter = "";

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출

        // 하나 스토리 버튼 설정
        stroyButton.onClick.RemoveAllListeners();
        stroyButton.onClick.AddListener(OnHanaStoryButtonClicked);

        // 세라 스토리 버튼 설정
        seraStoryButton.onClick.RemoveAllListeners();
        seraStoryButton.onClick.AddListener(OnSeraStoryButtonClicked);
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }

    // 하나 스토리 버튼 클릭 시
    private void OnHanaStoryButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        // 토글 방식으로 처리
        if (IsStoryDungeonInfoActive() && currentStoryFilter == "하나")
        {
            // 같은 필터가 이미 열려있으면 닫기만
            WindowManager.Instance.CloseOverlay(WindowType.StoryDungeonInfo);
        }
        else
        {
            // 닫혀있거나 다른 필터면 하나 스토리로 열기
            currentStoryFilter = "하나";

            if (IsStoryDungeonInfoActive())
            {
                // 다른 필터로 열려있으면 먼저 닫고 다시 열기
                WindowManager.Instance.CloseOverlay(WindowType.StoryDungeonInfo);
            }

            WindowManager.Instance.OpenOverlayNoDim(WindowType.StoryDungeonInfo);
        }
    }

    // 세라 스토리 버튼 클릭 시
    private void OnSeraStoryButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        // 토글 방식으로 처리
        if (IsStoryDungeonInfoActive() && currentStoryFilter == "세라")
        {
            // 같은 필터가 이미 열려있으면 닫기만
            WindowManager.Instance.CloseOverlay(WindowType.StoryDungeonInfo);
        }
        else
        {
            // 닫혀있거나 다른 필터면 세라 스토리로 열기
            currentStoryFilter = "세라";

            if (IsStoryDungeonInfoActive())
            {
                // 다른 필터로 열려있으면 먼저 닫고 다시 열기
                WindowManager.Instance.CloseOverlay(WindowType.StoryDungeonInfo);
            }

            WindowManager.Instance.OpenOverlayNoDim(WindowType.StoryDungeonInfo);
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