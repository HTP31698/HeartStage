using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class MailInfoUI : GenericWindow
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button receiveRewardButton;
    [SerializeField] private Image mailIcon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Transform rewardItemParent;
    [SerializeField] private GameObject rewardItemPrefab;
    [SerializeField] private MailUI mailUI;

    private MailData currentMailData;
    private Sprite currentMailSprite;
    private TextMeshProUGUI buttonText; 

    // UserId 캐싱
    private string UserId => AuthManager.Instance?.UserId;

    protected override void Awake()
    {
        base.Awake();
        closeButton.onClick.AddListener(OnCloseButtonClicked);
        receiveRewardButton.onClick.AddListener(() => OnReceiveRewardClickedAsync().Forget());

        buttonText = receiveRewardButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetMailData(MailData mailData)
    {
        currentMailData = mailData;

        titleText.text = mailData.title;
        contentText.text = mailData.content;

        if (!mailData.isRead)
        {
            mailData.isRead = true;
            MarkMailAsReadAsync().Forget();

            // MailUI에 읽음 상태 업데이트 알림
            mailUI?.UpdateMailReadStatus(mailData.mailId, true);
        }

        SetMailIconState();
        SetupRewardItems();
        InitializeRewardButton();
    }

    // 메일 아이콘을 항상 열림 상태로 설정
    private void SetMailIconState()
    {
        if (currentMailSprite != null)
        {
            DestroyImmediate(currentMailSprite);
            currentMailSprite = null;
        }

        var texture = ResourceManager.Instance.Get<Texture2D>("Mail-Open-100");
        if (texture != null)
        {
            currentMailSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            mailIcon.sprite = currentMailSprite;
        }
        else
        {
            Debug.LogWarning("메일 아이콘 이미지를 로드할 수 없습니다: Mail-Open-100");
        }
    }

    private void SetupRewardItems()
    {
        // 기존 아이템 제거
        foreach (Transform child in rewardItemParent)
        {
            Destroy(child.gameObject);
        }

        // 새 아이템 생성
        if (currentMailData.itemList?.Count > 0)
        {
            foreach (var item in currentMailData.itemList)
            {
                GameObject itemObj = Instantiate(rewardItemPrefab, rewardItemParent);
                itemObj.GetComponent<MailItemPrefab>()?.Setup(item);
            }
        }
    }

    private void InitializeRewardButton()
    {
        bool hasItems = currentMailData.itemList?.Count > 0;

        if (hasItems)
        {
            receiveRewardButton.interactable = !currentMailData.isRewarded;
            if (buttonText != null)
                buttonText.text = currentMailData.isRewarded ? "수령 완료" : "보상 수령";
        }
        else
        {
            receiveRewardButton.interactable = false;
        }
    }

    private async UniTaskVoid OnReceiveRewardClickedAsync()
    {
        if (currentMailData.isRewarded || string.IsNullOrEmpty(UserId)) return;

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        // 아이템 지급 처리
        GrantItemRewards();

        currentMailData.isRewarded = true;

        await MailManager.Instance.UpdateRewardStatusAsync(UserId, currentMailData.mailId, true);

        mailUI?.UpdateMailRewardStatus(currentMailData.mailId, true);
        InitializeRewardButton();
    }

    // 아이템 지급 로직을 별도 메서드로 분리
    private void GrantItemRewards()
    {
        if (currentMailData.itemList == null) return;

        foreach (var item in currentMailData.itemList)
        {
            if (int.TryParse(item.itemId, out int itemId))
            {
                ItemInvenHelper.AddItem(itemId, item.count);
            }
        }
    }

    private async UniTaskVoid MarkMailAsReadAsync()
    {
        if (string.IsNullOrEmpty(UserId)) return;
        await MailManager.Instance.MarkAsReadAsync(UserId, currentMailData.mailId);
    }

    public override void Open()
    {
        base.Open();
    }
    public override void Close()
    {
        base.Close();
    }

    private void OnCloseButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }

    private void OnDestroy()
    {
        if (currentMailSprite != null)
        {
            DestroyImmediate(currentMailSprite);
            currentMailSprite = null;
        }
    }
}