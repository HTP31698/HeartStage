using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특수 던전 UI (로비에서 진입)
/// - 특별 스테이지 버튼 → SpecialStageUI 오버레이
/// - 스토리 버튼 → StoryDungeonUI 오버레이
/// </summary>
public class SpecialDungeonUI : GenericWindow
{
    [SerializeField] private Button specialStageButton;
    [SerializeField] private Button stroyButton; // 기존 프리팹 필드명 유지

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출
        if (specialStageButton != null)
        {
            specialStageButton.onClick.RemoveAllListeners();
            specialStageButton.onClick.AddListener(OnSpecialStageButtonClicked);
        }

        if (stroyButton != null)
        {
            stroyButton.onClick.RemoveAllListeners();
            stroyButton.onClick.AddListener(OnStoryButtonClicked);
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

    private void OnSpecialStageButtonClicked()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        // 스토리 오버레이가 열려있으면 먼저 닫기
        WindowManager.Instance.CloseAllOverlays();
        WindowManager.Instance.OpenOverlayNoDim(WindowType.SpecialStage);
    }

    private void OnStoryButtonClicked()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        // 특별 스테이지 오버레이가 열려있으면 먼저 닫기
        WindowManager.Instance.CloseOverlay(WindowType.SpecialStage);
        WindowManager.Instance.OpenOverlayNoDim(WindowType.StoryDungeon);
    }
}
