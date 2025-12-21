using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class MailUI : GenericWindow
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button receiveAllButton;
    [SerializeField] private GameObject mailPrefab;
    [SerializeField] private Transform mailContentParent;
    [SerializeField] private MailInfoUI mailInfoUI;

    private List<MailData> currentMails = new List<MailData>();

    // UserId 캐싱용 프로퍼티
    private string UserId => AuthManager.Instance?.UserId;

    protected override void Awake()
    {
        base.Awake();
        closeButton.onClick.AddListener(OnExitButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteReadMailsClicked);
        receiveAllButton.onClick.AddListener(() => OnReceiveAllItemsClickedAsync().Forget());
    }

    private void OnEnable()
    {
        // 실시간 메일 수신 이벤트 구독
        if (MailManager.Instance != null)
        {
            MailManager.Instance.OnMailReceived += OnNewMailReceived;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (MailManager.Instance != null)
        {
            MailManager.Instance.OnMailReceived -= OnNewMailReceived;
        }
    }

    public override void Open()
    {
        base.Open();
        LoadUserMails().Forget();
    }

    public override void Close()
    {
        base.Close();
    }

    // 유저의 모든 메일 로드
    public async UniTaskVoid LoadUserMails()
    {
        if (string.IsNullOrEmpty(UserId)) return;

        currentMails = await MailManager.Instance.GetUserMailsAsync(UserId);
        RefreshMailList();
    }

    // 메일 목록 UI 새로고침
    public void RefreshMailList()
    {
        // 기존 프리팹 제거
        foreach (Transform child in mailContentParent)
        {
            Destroy(child.gameObject);
        }

        // 새로운 메일 프리팹 생성
        foreach (var mailData in currentMails)
        {
            CreateMailPrefab(mailData);
        }
    }

    // 개별 메일 프리팹 생성
    private void CreateMailPrefab(MailData mailData)
    {
        GameObject mailObj = Instantiate(mailPrefab, mailContentParent);
        MailPrefab mailPrefabScript = mailObj.GetComponent<MailPrefab>();

        if (mailPrefabScript != null)
        {
            mailPrefabScript.Setup(mailData);

            // 이벤트 중복 등록 방지
            mailPrefabScript.OnMailClicked -= OnMailPrefabClicked;
            mailPrefabScript.OnMailClicked += OnMailPrefabClicked;
        }
    }

    // 메일 클릭 시 상세 정보 표시
    private void OnMailPrefabClicked(MailData mailData)
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (mailInfoUI != null)
        {
            mailInfoUI.SetMailData(mailData);
        }

        WindowManager.Instance.OpenOverlay(WindowType.MailInfoUI);
    }

    // 읽은 메일 삭제
    private async void OnDeleteReadMailsClicked()
    {
        if (string.IsNullOrEmpty(UserId)) return;

        for (int i = currentMails.Count - 1; i >= 0; i--)
        {
            var mail = currentMails[i];
            if (CanDeleteMail(mail))
            {
                await MailManager.Instance.DeleteMailAsync(UserId, mail.mailId);
                currentMails.RemoveAt(i);
            }
        }

        RefreshMailList();
    }

    // 메일 삭제 가능 여부 확인
    private bool CanDeleteMail(MailData mail)
    {
        if (!mail.isRead) return false;
        bool hasRewards = mail.itemList?.Count > 0;
        return !hasRewards || mail.isRewarded;
    }

    // 모든 메일의 보상 일괄 수령 (wrapper 메서드 제거)
    private async UniTaskVoid OnReceiveAllItemsClickedAsync()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (string.IsNullOrEmpty(UserId)) return;

        List<string> rewardedMailIds = new List<string>();
        List<string> readMailIds = new List<string>();

        foreach (var mail in currentMails)
        {
            if (mail.isRewarded) continue;

            if (mail.itemList?.Count > 0)
            {
                foreach (var item in mail.itemList)
                {
                    if (int.TryParse(item.itemId, out int itemId))
                    {
                        ItemInvenHelper.AddItem(itemId, item.count);
                    }
                }

                mail.isRewarded = true;
                rewardedMailIds.Add(mail.mailId);

                if (!mail.isRead)
                {
                    mail.isRead = true;
                    readMailIds.Add(mail.mailId);
                }
            }
        }

        // 보상 수령 상태 업데이트
        if (rewardedMailIds.Count > 0)
        {
            await MailManager.Instance.UpdateMultipleRewardStatusAsync(UserId, rewardedMailIds);            
        }

        if (readMailIds.Count > 0)
        {
            foreach (string mailId in readMailIds)
            {
                await MailManager.Instance.MarkAsReadAsync(UserId, mailId);
            }
        }

        RefreshMailList();
    }

    // 메일 보상 수령 상태 업데이트 (RefreshMailList 호출 제거 - MailInfoUI에서 이미 호출됨)
    public void UpdateMailRewardStatus(string mailId, bool isRewarded)
    {
        var mail = currentMails.Find(m => m.mailId == mailId);
        if (mail != null)
        {
            mail.isRewarded = isRewarded;
        }
    }

    // 새로운 메일이 실시간으로 수신되었을 때 호출
    private void OnNewMailReceived(MailData newMail)
    {
        if (UserId != newMail.receiverId) return;

        // 중복 메일 체크
        bool alreadyExists = currentMails.Exists(m => m.mailId == newMail.mailId);
        if (!alreadyExists)
        {
            // 새 메일을 목록 맨 앞에 추가 (최신순)
            currentMails.Insert(0, newMail);

            // UI가 활성화되어 있을 때만 즉시 새로고침
            if (gameObject.activeInHierarchy)
            {
                RefreshMailList();
            }
        }
    }

    private void OnExitButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        Close();
    }

    // 메일 읽음 상태 업데이트
    public void UpdateMailReadStatus(string mailId, bool isRead)
    {
        var mail = currentMails.Find(m => m.mailId == mailId);
        if (mail != null)
        {
            mail.isRead = isRead;

            // 해당 메일 프리팹만 업데이트 (전체 새로고침 대신 효율적 업데이트)
            foreach (Transform child in mailContentParent)
            {
                var mailPrefab = child.GetComponent<MailPrefab>();
                if (mailPrefab != null && mailPrefab.GetMailData()?.mailId == mailId)
                {
                    mailPrefab.Setup(mail);
                    break;
                }
            }
        }
    }
}