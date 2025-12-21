using UnityEngine;
using UnityEngine.UI;

public class LastStageNoticeUI : GenericWindow
{
    [SerializeField] private Button lobbyButton;

    protected override void Awake()
    {
        base.Awake();
        lobbyButton.onClick.AddListener(OnLobbyButtonClicked);

        // Canvas Sorting Order 강제 설정
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999; // 가장 앞에 표시
        }
    }

    public override void Open()
    {
        base.Open();

        // 열 때마다 Sorting Order 재설정 (안전장치)
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 9999;
        }
    }

    public override void Close()
    {
        base.Close();
    }

    private void OnLobbyButtonClicked()
    {
        Close();
        WindowManager.currentWindow = WindowType.LobbyHome;
        GameSceneManager.ChangeScene(SceneType.LobbyScene);
    }
}
