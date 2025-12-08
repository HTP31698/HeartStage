using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageWindow : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("버튼 - 1개 (가운데)")]
    [SerializeField] private Button singleButton;
    [SerializeField] private TextMeshProUGUI singleButtonLabel;
    [SerializeField] private Image singleButtonImage;
    [SerializeField] private Image singleButtonFrame;

    [Header("버튼 - 2개 (양옆)")]
    [SerializeField] private GameObject twoButtonContainer;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI confirmButtonLabel;
    [SerializeField] private TextMeshProUGUI cancelButtonLabel;
    [SerializeField] private Image confirmButtonImage;
    [SerializeField] private Image cancelButtonImage;
    [SerializeField] private Image confirmButtonFrame;
    [SerializeField] private Image cancelButtonFrame;

    [Header("윈도우 프레임")]
    [SerializeField] private Image windowFrame;

    [Header("색상 설정")]
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private Color neutralColor = Color.yellow;
    [SerializeField] private Color confirmColor = Color.green;
    [SerializeField] private Color cancelColor = Color.red;

    [SerializeField, Range(0f, 1f)]
    private float frameDarkenFactor = 0.75f;

    private Action _onConfirm;
    private Action _onCancel;
    private Action _onSingleClose;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (singleButton != null)
        {
            singleButton.onClick.RemoveAllListeners();
            singleButton.onClick.AddListener(OnClickSingleButton);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnClickCancel);
        }

        if (singleButtonImage == null && singleButton != null)
            singleButtonImage = singleButton.targetGraphic as Image;

        if (confirmButtonImage == null && confirmButton != null)
            confirmButtonImage = confirmButton.targetGraphic as Image;

        if (cancelButtonImage == null && cancelButton != null)
            cancelButtonImage = cancelButton.targetGraphic as Image;
    }

    #region 1개 버튼

    public void Open(string title, string message)
    {
        SetTitle(title);
        SetMessage(message);
        SetSingleButtonMode(neutralColor);

        if (root != null)
            root.SetActive(true);
    }

    public void Open(string title, string message, bool isSuccess, Action onClose = null)
    {
        SetTitle(title);
        SetMessage(message);
        SetSingleButtonMode(isSuccess ? successColor : failColor, onClose);

        if (root != null)
            root.SetActive(true);
    }

    public void OpenFail(string title, string message, Action onClose = null)
    {
        Open(title, message, false, onClose);
    }

    public void OpenSuccess(string title, string message, Action onClose = null)
    {
        Open(title, message, true, onClose);
    }

    #endregion

    #region 2개 버튼

    public void OpenTwoButton(
        string title,
        string message,
        string confirmText,
        string cancelText,
        Action onConfirm,
        Action onCancel = null)
    {
        SetTitle(title);
        SetMessage(message);
        SetTwoButtonMode(confirmText, cancelText);

        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (root != null)
            root.SetActive(true);
    }

    public void OpenAcceptDecline(string title, string message, Action onAccept, Action onDecline = null)
    {
        OpenTwoButton(title, message, "수락", "거절", onAccept, onDecline);
    }

    public void OpenConfirmCancel(string title, string message, Action onConfirm, Action onCancel = null)
    {
        OpenTwoButton(title, message, "확인", "취소", onConfirm, onCancel);
    }

    public void OpenDeleteConfirm(string title, string message, Action onDelete, Action onCancel = null)
    {
        OpenTwoButton(title, message, "삭제", "취소", onDelete, onCancel);
    }

    #endregion

    #region 버튼 모드 설정

    private void SetSingleButtonMode(Color buttonColor, Action onClose = null)
    {
        if (singleButton != null)
            singleButton.gameObject.SetActive(true);

        if (twoButtonContainer != null)
            twoButtonContainer.SetActive(false);

        ApplyButtonColor(singleButtonImage, singleButtonFrame, singleButtonLabel, buttonColor);
        ApplyWindowFrameColor(buttonColor);

        _onConfirm = null;
        _onCancel = null;
        _onSingleClose = onClose;
    }

    private void SetTwoButtonMode(string confirmText, string cancelText)
    {
        if (singleButton != null)
            singleButton.gameObject.SetActive(false);

        if (twoButtonContainer != null)
            twoButtonContainer.SetActive(true);

        if (confirmButtonLabel != null)
            confirmButtonLabel.text = confirmText;

        if (cancelButtonLabel != null)
            cancelButtonLabel.text = cancelText;

        ApplyButtonColor(confirmButtonImage, confirmButtonFrame, confirmButtonLabel, confirmColor);
        ApplyButtonColor(cancelButtonImage, cancelButtonFrame, cancelButtonLabel, cancelColor);
        ApplyWindowFrameColor(neutralColor);
    }

    private void ApplyButtonColor(Image buttonImage, Image buttonFrame, TextMeshProUGUI label, Color baseColor)
    {
        if (buttonImage != null)
            buttonImage.color = baseColor;

        if (buttonFrame != null)
        {
            buttonFrame.color = new Color(
                baseColor.r * frameDarkenFactor,
                baseColor.g * frameDarkenFactor,
                baseColor.b * frameDarkenFactor,
                buttonFrame.color.a
            );
        }

        if (label != null)
            label.color = Color.white;
    }

    private void ApplyWindowFrameColor(Color baseColor)
    {
        if (windowFrame != null)
        {
            windowFrame.color = new Color(
                baseColor.r * frameDarkenFactor,
                baseColor.g * frameDarkenFactor,
                baseColor.b * frameDarkenFactor,
                windowFrame.color.a
            );
        }
    }

    #endregion

    #region 버튼 클릭

    private void OnClickConfirm()
    {
        Close();
        _onConfirm?.Invoke();
    }

    private void OnClickCancel()
    {
        Close();
        _onCancel?.Invoke();
    }

    private void OnClickSingleButton()
    {
        Close();
        _onSingleClose?.Invoke();
    }

    #endregion

    public void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }

    public void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }
}