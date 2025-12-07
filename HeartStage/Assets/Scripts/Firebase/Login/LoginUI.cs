using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private GameObject loginPanel;

    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwdInput;

    [SerializeField] private Button loginButton;
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button anonymouslyLoginButton;

    [SerializeField] private TextMeshProUGUI errorText;

    private void OnEnable()
    {
        errorText.text = string.Empty;
    }

    private async UniTaskVoid Start()
    {
        SetButtonsInteractable(false);

        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);

        loginButton.onClick.AddListener(() => OnLoginButtonClicked().Forget());
        signUpButton.onClick.AddListener(() => OnSignUpButtonClicked().Forget());
        anonymouslyLoginButton.onClick.AddListener(() => OnAnonymouslyLoginButtonClicked().Forget());

        SetButtonsInteractable(true);
        UpdateUI();
    }

    private async UniTaskVoid OnLoginButtonClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwdInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("이메일과 비밀번호를 입력해주세요.");
            return;
        }

        SetButtonsInteractable(false);
        ShowError(""); // 에러 메시지 초기화

        var (result, error) = await AuthManager.Instance.SignInWithEmailAsync(email, password);

        if (result == AuthManager.LoginResult.Success)
        {
            // 성공 - TitleSceneController가 알아서 다음 단계 진행
        }
        else
        {
            ShowError(GetLoginErrorMessage(result, error));
            SetButtonsInteractable(true);
        }

        UpdateUI();
    }

    private async UniTaskVoid OnSignUpButtonClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwdInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("이메일과 비밀번호를 입력해주세요.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("비밀번호는 6자 이상이어야 합니다.");
            return;
        }

        SetButtonsInteractable(false);
        ShowError("");

        var (result, error) = await AuthManager.Instance.CreateUserWithEmailAsync(email, password);

        if (result == AuthManager.LoginResult.Success)
        {
            // 성공
        }
        else
        {
            ShowError(GetLoginErrorMessage(result, error));
            SetButtonsInteractable(true);
        }

        UpdateUI();
    }

    private async UniTaskVoid OnAnonymouslyLoginButtonClicked()
    {
        SetButtonsInteractable(false);
        ShowError("");

        var (result, error) = await AuthManager.Instance.SignInAnonymouslyAsync();

        if (result == AuthManager.LoginResult.Success)
        {
            // 성공
        }
        else
        {
            ShowError(GetLoginErrorMessage(result, error));
            SetButtonsInteractable(true);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsInitialized)
            return;

        bool isLoggedIn = AuthManager.Instance.IsLoggedIn;
        loginPanel.SetActive(!isLoggedIn);
    }

    private void SetButtonsInteractable(bool active)
    {
        loginButton.interactable = active;
        signUpButton.interactable = active;
        anonymouslyLoginButton.interactable = active;
    }

    private void ShowError(string message)
    {
        errorText.text = message;
    }

    private string GetLoginErrorMessage(AuthManager.LoginResult result, string rawError)
    {
        return result switch
        {
            AuthManager.LoginResult.NetworkError => "네트워크 연결을 확인해주세요.",
            AuthManager.LoginResult.InvalidCredentials => "이메일 또는 비밀번호가 올바르지 않습니다.",
            AuthManager.LoginResult.TooManyRequests => "너무 많은 시도가 있었습니다. 잠시 후 다시 시도해주세요.",
            _ => "로그인에 실패했습니다. 다시 시도해주세요."
        };
    }
}