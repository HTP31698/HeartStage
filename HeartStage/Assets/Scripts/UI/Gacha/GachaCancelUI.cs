using UnityEngine;
using UnityEngine.UI;

public class GachaCancelUI : GenericWindow
{
    [SerializeField] private Button exitButton;

    protected override void Awake()
    {
        base.Awake();
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }
    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }
    private void OnExitButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }
}
