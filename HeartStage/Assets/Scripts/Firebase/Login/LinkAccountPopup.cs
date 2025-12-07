using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 익명 유저 계정 연동 유도 팝업
/// - 일정 기간 플레이 후 표시
/// - 연동하면 영구 보존, 안 하면 90일 후 삭제 경고
/// </summary>
public class LinkAccountPopup : MonoBehaviour
{
    [Header("팝업 루트")]
    [SerializeField] private GameObject popupRoot;

    [Header("경고 메시지")]
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("입력 필드")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("버튼")]
    [SerializeField] private Button linkButton;        // 연동하기
    [SerializeField] private Button laterButton;       // 나중에
    [SerializeField] private Button closeButton;       // X 버튼

    [Header("상태 텍스트")]
    [SerializeField] private TextMeshProUGUI statusText;

    private int _daysUntilDelete;

    private void Awake()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        linkButton?.onClick.AddListener(() => OnLinkButtonClicked().Forget());
        laterButton?.onClick.AddListener(OnLaterButtonClicked);
        closeButton?.onClick.AddListener(OnLaterButtonClicked);
    }

    /// <summary>
    /// 팝업 표시 (TitleSceneController에서 호출)
    /// </summary>
    public void Show(int daysPlayed, int daysUntilDelete)
    {
        _daysUntilDelete = daysUntilDelete;

        if (warningText != null)
        {
            if (daysUntilDelete <= 30)
            {
                // 긴급 경고
                warningText.text = $"<color=#FF6B6B>⚠️ 주의!</color>\n" +
                                   $"현재 게스트 계정으로 플레이 중입니다.\n" +
                                   $"<color=#FF6B6B>{daysUntilDelete}일 후</color> 데이터가 삭제됩니다.\n\n" +
                                   $"이메일을 연동하면 데이터가 영구 보존됩니다.";
            }
            else
            {
                // 일반 권유
                warningText.text = $"게스트 계정으로 {daysPlayed}일째 플레이 중입니다.\n\n" +
                                   $"이메일을 연동하면:\n" +
                                   $"• 데이터가 영구 보존됩니다\n" +
                                   $"• 다른 기기에서도 플레이 가능합니다\n" +
                                   $"• 기기 변경/앱 삭제 시에도 복구됩니다";
            }
        }

        if (statusText != null)
            statusText.text = "";

        if (emailInput != null)
            emailInput.text = "";

        if (passwordInput != null)
            passwordInput.text = "";

        SetButtonsInteractable(true);
        popupRoot?.SetActive(true);
    }

    /// <summary>
    /// 팝업 숨기기
    /// </summary>
    public void Hide()
    {
        popupRoot?.SetActive(false);
    }

    private async UniTaskVoid OnLinkButtonClicked()
    {
        string email = emailInput?.text?.Trim() ?? "";
        string password = passwordInput?.text ?? "";

        // 유효성 검사
        if (string.IsNullOrEmpty(email))
        {
            ShowStatus("이메일을 입력해주세요.", true);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowStatus("비밀번호를 입력해주세요.", true);
            return;
        }

        if (password.Length < 6)
        {
            ShowStatus("비밀번호는 6자 이상이어야 합니다.", true);
            return;
        }

        SetButtonsInteractable(false);
        ShowStatus("연동 중...", false);

        var (result, error) = await AuthManager.Instance.LinkEmailToAnonymousAsync(email, password);

        if (result == AuthManager.LinkResult.Success)
        {
            ShowStatus("연동 완료! 🎉", false);
            await UniTask.Delay(1000);
            Hide();
        }
        else
        {
            ShowStatus(GetLinkErrorMessage(result), true);
            SetButtonsInteractable(true);
        }
    }

    private void OnLaterButtonClicked()
    {
        // 오늘 봤다고 기록
        Hide();
    }

    private void SetButtonsInteractable(bool active)
    {
        if (linkButton != null) linkButton.interactable = active;
        if (laterButton != null) laterButton.interactable = active;
        if (closeButton != null) closeButton.interactable = active;
        if (emailInput != null) emailInput.interactable = active;
        if (passwordInput != null) passwordInput.interactable = active;
    }

    private void ShowStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? new Color(1f, 0.4f, 0.4f) : Color.white;
        }
    }

    private string GetLinkErrorMessage(AuthManager.LinkResult result)
    {
        return result switch
        {
            AuthManager.LinkResult.EmailAlreadyInUse => "이미 사용 중인 이메일입니다.\n다른 이메일을 입력해주세요.",
            AuthManager.LinkResult.InvalidEmail => "올바른 이메일 형식이 아닙니다.",
            AuthManager.LinkResult.WeakPassword => "비밀번호가 너무 약합니다. (6자 이상)",
            AuthManager.LinkResult.NetworkError => "네트워크 연결을 확인해주세요.",
            _ => "연동에 실패했습니다. 다시 시도해주세요."
        };
    }
}