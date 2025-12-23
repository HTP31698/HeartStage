using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknameWindow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;

    public bool IsOpen => gameObject.activeSelf;

    private System.Action onClosed; // 튜토리얼 닫힐 때 호출할 콜백


    private void Awake()
    {
        // 처음엔 창 꺼둔 상태에서 시작
        gameObject.SetActive(false);

        if (okButton != null)
            okButton.onClick.AddListener(() => OnClickOk().Forget());

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (SaveLoadManager.Data is SaveDataV1 data && inputField != null)
        {
            inputField.text = data.nickname;
        }

        if (messageText != null)
            messageText.text = "사용할 닉네임을 입력해 주세요.";

        gameObject.SetActive(true);
        inputField?.ActivateInputField();
    }

    public void Close()
    {
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

        var (ok, error) = await NicknameService.TryChangeNicknameAsync(raw);

        if (!ok)
        {
            messageText.text = error;
            return;
        }

        messageText.text = "닉네임이 변경되었습니다.";

        // 프로필 UI / 로비 재화 UI 갱신
        ProfileWindow.Instance?.RefreshAll();
        LobbyManager.Instance?.MoneyUISet();

        Close();
    }

    public void SetOnClosedCallback(System.Action callback)
    {
        onClosed = callback;
    }
}
