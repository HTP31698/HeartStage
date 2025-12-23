using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialNickNameScript : GenericWindow
{
    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    public bool IsOpen => gameObject.activeSelf;

    private System.Action onClosed; // 튜토리얼 닫힐 때 호출할 콜백


    protected override void Awake()
    {
        base.Awake();
        // 처음엔 창 꺼둔 상태에서 시작
        gameObject.SetActive(false);

        if (okButton != null)
        {
            okButton.onClick.AddListener(() => OnClickOk().Forget());

            // okButton 텍스트를 "확인"으로 설정
            var buttonText = okButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = "확인";
            }
        }

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
    }

    public override void Open()
    {
        base.Open();

        if (SaveLoadManager.Data is SaveDataV1 data && inputField != null)
        {
            inputField.text = data.nickname;
        }

        if (messageText != null)
            messageText.text = "사용할 닉네임을 입력해 주세요.";

        gameObject.SetActive(true);
        inputField?.ActivateInputField();
    }

    public override void Close()
    {
        base.Close();

        gameObject.SetActive(false);
        // "팝업 하나 닫혔음"을 ProfileWindow에 알려서 모달Panel 제어
        if (ProfileWindow.Instance != null && ProfileWindow.Instance.gameObject.activeSelf)
        {
            ProfileWindow.Instance.OnPopupClosed();
        }

        onClosed?.Invoke();
        onClosed = null; // 한 번만 실행
    }

    // 로딩에서 예열용
    public void Prewarm()
    {
        bool wasActive = gameObject.activeSelf;
        Open();
        gameObject.SetActive(wasActive);
    }

    private async UniTaskVoid OnClickOk()
    {
        if (inputField == null || messageText == null)
            return;

        string raw = inputField.text;
        messageText.text = "확인 중입니다...";

        // 튜토리얼용: 재화 소모 없이 직접 닉네임 변경
        if (!NicknameValidator.ValidateNickname(raw, out string error))
        {
            messageText.text = error;
            return;
        }

        if (SaveLoadManager.Data is SaveDataV1 data)
        {
            string trimmed = raw.Trim();
            data.nickname = trimmed;

            // 서버 저장
            await SaveLoadManager.SaveToServer();

            // UI 갱신
            ProfileWindow.Instance?.RefreshAll();
            LobbyManager.Instance?.MoneyUISet();

            messageText.text = "닉네임이 변경되었습니다.";
        }
        else
        {
            messageText.text = "세이브 데이터를 찾을 수 없습니다.";
            return;
        }

        Close();
    }

    public void SetOnClosedCallback(System.Action callback)
    {
        onClosed = callback;
    }
}