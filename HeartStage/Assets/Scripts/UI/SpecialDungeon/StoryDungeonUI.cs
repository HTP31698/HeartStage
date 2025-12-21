using UnityEngine;
using UnityEngine.UI;

public class StoryDungeonUI : GenericWindow
{
    [SerializeField] private Button stroyButton;

    protected override void Awake()
    {
        base.Awake();
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
        WindowManager.Instance.OpenOverlayNoDim(WindowType.StoryDungeonInfo);
    }
}
