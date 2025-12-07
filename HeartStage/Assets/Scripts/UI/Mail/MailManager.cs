using Cysharp.Threading.Tasks;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MailManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static MailManager Instance;

    // Firebase 데이터베이스 참조
    private DatabaseReference db;

    // 메일 관련 이벤트
    public event Action<List<MailData>> OnMailsLoaded;    // 메일 목록 로드 완료 시
    public event Action<MailData> OnMailReceived;         // 새 메일 수신 시
    public event Action OnMailReadStatusChanged;         // 메일 읽음 상태 변경 시

    [SerializeField] private Button mailTextButton;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        mailTextButton.onClick.AddListener(OnTestButtonClicked);
    }

    /// Firebase 초기화 및 글로벌 메일 리스너 등록
    private async void Start()
    {
        await FirebaseInitializer.Instance.WaitForInitilazationAsync();
        db = FirebaseDatabase.DefaultInstance.RootReference;

        // 글로벌 메일 실시간 감지 시작
        FireBaseMailChanged();

        // 게임 시작 시 미처리 글로벌 메일 확인
        await CheckGlobalMailGameExit();

        // 게임 재접속시 개인 메일함 확인 (오프라인 중 받은 메일 처리)
        await CheckPersonalMailsOnLogin();
    }

    // 
    private async UniTask CheckGlobalMailGameExit()
    {
        try
        {
            var snapshot = await db.Child("globalMail").GetValueAsync();
            if (snapshot.Exists && snapshot.Value != null)
            {
                var mailData = snapshot.Value as Dictionary<string, object>;
                if (mailData != null && IsValidGlobalMail(mailData))
                {
                    Debug.Log("미처리 글로벌 메일 발견 - 처리 시작");

                    var items = ParseItems(mailData);
                    string title = mailData["title"].ToString();
                    string content = mailData["content"].ToString();

                    await CreateGlobalMailForCurrentUser(title, content, items);
                    await SendGlobalMailToAllUsers(title, content, items);
                    await DisableGlobalMail();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"미처리 글로벌 메일 확인 실패: {ex.Message}");
        }
    }

    /// 게임 재접속시 개인 메일함 확인
    private async UniTask CheckPersonalMailsOnLogin()
    {
        try
        {
            if (!IsAuthManagerValid()) return;

            string userId = AuthManager.Instance.UserId;
            Debug.Log($"[메일 확인] 유저 ID: {userId}");

            var mails = await GetUserMailsAsync(userId);
            Debug.Log($"[메일 확인] 총 메일 수: {mails.Count}");

            // 읽지 않은 메일이 있으면 알림
            var unreadMails = mails.Where(m => !m.isRead).ToList();
            if (unreadMails.Count > 0)
            {
                Debug.Log($"오프라인 중 받은 메일 {unreadMails.Count}개 발견");
                foreach (var mail in unreadMails)
                {
                    Debug.Log($"- 메일: {mail.title} (발송자: {mail.senderName})");
                }
            }
        }
        catch 
        {
        }
    }

    /// Firebase의 globalMail 노드 변경사항을 실시간으로 감지
    private void FireBaseMailChanged()
    {
        if (db != null)
        {
            db.Child("globalMail").ValueChanged += OnGlobalMailChanged;
        }
    }

    /// 글로벌 메일 데이터 변경 시 호출
    private async void OnGlobalMailChanged(object sender, ValueChangedEventArgs args)
    {
        // 에러 체크 및 null 검사
        if (args.DatabaseError != null || args.Snapshot.Value == null) return;

        // Firebase 데이터를 Dictionary로 변환
        var mailData = args.Snapshot.Value as Dictionary<string, object>;
        if (mailData == null) return;

        // 유효한 글로벌 메일인지 확인
        if (IsValidGlobalMail(mailData))
        {
            var items = ParseItems(mailData);
            string title = mailData["title"].ToString();
            string content = mailData["content"].ToString();

            // 현재 유저에게 즉시 메일 생성 (실시간 알림)
            await CreateGlobalMailForCurrentUser(title, content, items);

            // 모든 유저에게 메일 발송 (접속하지 않은 유저 포함)
            await SendGlobalMailToAllUsers(title, content, items);

            // 글로벌 메일 처리 완료 후 비활성화 
            await DisableGlobalMail();
        }
    }

    // 전체 유저 메일 발송 (미접속 포함)
    private async UniTask SendGlobalMailToAllUsers(string title, string content, List<ItemAttachment> items)
    {
        try
        {
            var allUserIds = await GetAllUserIdsAsync();
            if (allUserIds.Count == 0) return;

            string globalMailId = $"mail_{title.GetHashCode():X8}";
            int successCount = 0;
            int skippedCount = 0;

            foreach (string userId in allUserIds)
            {
                try
                {
                    // 이미 받은 메일인지 중복 체크
                    var existingSnapshot = await db.Child("mails").Child(userId).Child(globalMailId).GetValueAsync();
                    if (existingSnapshot.Exists)
                    {
                        skippedCount++;
                        continue;
                    }

                    var globalMail = new MailData(
                        mailId: globalMailId,
                        senderId: "system",
                        senderName: "운영팀",
                        receiverId: userId,
                        title: title,
                        content: content,
                        itemList: items
                    )
                    {
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    // 현재 유저가 아닌 경우만 OnMailReceived 이벤트 발생 안 함
                    string json = JsonUtility.ToJson(globalMail);
                    await db.Child("mails").Child(userId).Child(globalMailId).SetRawJsonValueAsync(json);
                    successCount++;
                }
                catch 
                { 
                }
            }

            Debug.Log($"글로벌 메일 발송 완료: 성공 {successCount}명, 중복 스킵 {skippedCount}명");
        }
        catch
        {
        }
    }

    /// 글로벌 메일 데이터가 유효한지 검증
    private bool IsValidGlobalMail(Dictionary<string, object> mailData)
    {
        return mailData.ContainsKey("active") &&
               (bool)mailData["active"] &&
               mailData.ContainsKey("title") &&
               mailData.ContainsKey("content");
    }


    // 메일 데이터에서 아이템 목록을 파싱
    private List<ItemAttachment> ParseItems(Dictionary<string, object> mailData)
    {
        var items = new List<ItemAttachment>();

        // items 키가 존재하고 List<object> 타입인지 확인
        if (mailData.ContainsKey("items") && mailData["items"] is List<object> itemsData)
        {
            foreach (var item in itemsData)
            {
                // 각 아이템이 Dictionary이고 필수 키들을 포함하는지 확인
                if (item is Dictionary<string, object> itemDict &&
                    itemDict.ContainsKey("itemId") && itemDict.ContainsKey("count"))
                {
                    items.Add(new ItemAttachment(
                        itemDict["itemId"].ToString(),
                        Convert.ToInt32(itemDict["count"])
                    ));
                }
            }
        }

        return items;
    }

    // 현재 로그인한 유저에게 글로벌 메일 생성
    private async UniTask CreateGlobalMailForCurrentUser(string title, string content, List<ItemAttachment> items)
    {
        // AuthManager 유효성 검사
        if (!IsAuthManagerValid()) return;

        string userId = AuthManager.Instance.UserId;
        // 고유한 글로벌 메일 ID 생성
        string globalMailId = $"mail_{title.GetHashCode():X8}";

        // 이미 받은 메일인지 중복 체크
        var existingSnapshot = await db.Child("mails").Child(userId).Child(globalMailId).GetValueAsync();
        if (existingSnapshot.Exists) return;

        // 글로벌 메일 데이터 생성
        var globalMail = new MailData(
            mailId: globalMailId,
            senderId: "system",
            senderName: "운영팀",
            receiverId: userId,
            title: title,
            content: content,
            itemList: items
        )
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()  // 현재 시간을 밀리초로 설정
        };

        // 메일 전송
        await SendMailAsync(globalMail);
    }

    // AuthManager 인스턴스와 UserId 유효성 검사
    private bool IsAuthManagerValid()
    {
        return AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.UserId);
    }

    // 글로벌 메일을 Firebase에 설정 (모든 유저가 받을 메일)
    public async UniTask SetGlobalMail(string title, string content, List<ItemAttachment> items = null)
    {
        try
        {
            var globalMailData = CreateGlobalMailData(title, content, items);
            await db.Child("globalMail").SetValueAsync(globalMailData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"글로벌 메일 설정 실패: {ex.Message}");
        }
    }


    // Firebase에 저장할 글로벌 메일 데이터 구조 생성
    private Dictionary<string, object> CreateGlobalMailData(string title, string content, List<ItemAttachment> items)
    {
        var itemsData = new List<Dictionary<string, object>>();

        // 아이템이 있으면 Firebase 형식으로 변환
        if (items != null)
        {
            foreach (var item in items)
            {
                itemsData.Add(new Dictionary<string, object>
                {
                    {"itemId", item.itemId},
                    {"count", item.count}
                });
            }
        }

        return new Dictionary<string, object>
        {
            {"active", true},        // 글로벌 메일 활성화 상태
            {"title", title},
            {"content", content},
            {"items", itemsData}
        };
    }

    /// 특정 유저의 모든 메일을 Firebase에서 가져오기
    public async UniTask<List<MailData>> GetUserMailsAsync(string userId)
    {
        try
        {
            var snapshot = await db.Child("mails").Child(userId).GetValueAsync();
            var mails = new List<MailData>();

            if (snapshot.Exists)
            {
                // Firebase의 각 자식 노드를 MailData로 변환
                foreach (var child in snapshot.Children)
                {
                    string json = child.GetRawJsonValue();
                    MailData mail = JsonUtility.FromJson<MailData>(json);
                    mails.Add(mail);
                }

                // 타임스탬프 기준으로 최신순 정렬
                mails = mails.OrderByDescending(m => m.timestamp).ToList();
            }

            // 메일 로드 완료 이벤트 발생
            OnMailsLoaded?.Invoke(mails);
            return mails;
        }
        catch (Exception ex)
        {
            Debug.LogError($"메일 로드 실패: {ex.Message}");
            return new List<MailData>();
        }
    }

    // 메일을 Firebase에 전송/저장
    public async UniTask<bool> SendMailAsync(MailData mailData)
    {
        try
        {
            // MailData를 JSON으로 변환하여 Firebase에 저장
            string json = JsonUtility.ToJson(mailData);
            await db.Child("mails").Child(mailData.receiverId).Child(mailData.mailId).SetRawJsonValueAsync(json);

            // 새 메일 수신 이벤트 발생
            OnMailReceived?.Invoke(mailData);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"메일 전송 실패: {ex.Message}");
            return false;
        }
    }

    // 메일을 읽음 상태로 표시
    public async UniTask MarkAsReadAsync(string userId, string mailId)
    {
        try
        {
            await db.Child("mails").Child(userId).Child(mailId).Child("isRead").SetValueAsync(true);
            
            // 읽음 상태 변경 이벤트 발생
            OnMailReadStatusChanged?.Invoke();
        }
        catch { }
    }

    // Firebase에서 메일 삭제
    public async UniTask DeleteMailAsync(string userId, string mailId)
    {
        try
        {
            await db.Child("mails").Child(userId).Child(mailId).RemoveValueAsync();
        }
        catch { }
    }

    // 단일 메일의 보상 수령 상태 업데이트
    public async UniTask UpdateRewardStatusAsync(string userId, string mailId, bool isRewarded)
    {
        try
        {
            await db.Child("mails").Child(userId).Child(mailId).Child("isRewarded").SetValueAsync(isRewarded);
        }
        catch 
        {
        }
    }

    // 여러 메일의 보상 수령 상태를 한 번에 업데이트 
    public async UniTask UpdateMultipleRewardStatusAsync(string userId, List<string> mailIds)
    {
        try
        {
            // 배치 업데이트를 위한 Dictionary 구성
            var updates = new Dictionary<string, object>();
            foreach (string mailId in mailIds)
            {
                updates[$"mails/{userId}/{mailId}/isRewarded"] = true;
            }

            // 한 번의 호출로 모든 메일 상태 업데이트
            await db.UpdateChildrenAsync(updates);
        }
        catch
        {
        }
    }

    // 게임에 등록된 모든 유저 ID 목록 가져오기
    public async UniTask<List<string>> GetAllUserIdsAsync()
    {
        try
        {
            // users 노드에서 모든 유저 정보 조회
            var snapshot = await db.Child("users").GetValueAsync();
            var userIds = new List<string>();

            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    userIds.Add(child.Key);  // 각 자식 노드의 키가 유저 ID
                }
            }

            return userIds;
        }
        catch 
        {
            return new List<string>();
        }
    }

    /// 모든 유저에게 동일한 메일을 일괄 발송 (코드로만 호출)
    public async UniTask SendMailToAllUsers(string title, string content, List<ItemAttachment> items = null)
    {
        try
        {
            // 모든 유저 ID 가져오기
            var allUserIds = await GetAllUserIdsAsync();

            if (allUserIds.Count == 0)
            {
                return;
            }

            // null 체크 및 기본값 설정
            items ??= new List<ItemAttachment>();

            foreach (string userId in allUserIds)
            {
                try
                {
                    var broadcastMail = new MailData(
                        mailId: Guid.NewGuid().ToString(),  // 유니크한 메일 ID 생성
                        senderId: "admin",
                        senderName: "운영팀",
                        receiverId: userId,
                        title: title,
                        content: content,
                        itemList: items
                    )
                    {
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    await SendMailAsync(broadcastMail);
                }
                catch 
                {
                }
            }

        }
        catch
        {
        }
    }

    // 우편 테스트 버튼
    private async UniTask SendAdminMail()
    {
        // AuthManager 체크
        if (AuthManager.Instance == null)
        {
            return;
        }

        if (!AuthManager.Instance.IsInitialized)
        {
            return;
        }

        if (!AuthManager.Instance.IsLoggedIn)
        {
            return;
        }

        // Firebase 데이터베이스 체크
        if (db == null)
        {
            return;
        }

        try
        {
            var items = new List<ItemAttachment>
        {
            new ItemAttachment("7101", 10000),
            new ItemAttachment("7102", 10000)
        };

            await MailManager.Instance.SendMailToAllUsers
            (
                "환영 합니다!",
                "접속 보상 선물입니다.",
                items
            );
        }
        catch 
        {
        }
    }

    private void OnTestButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        SendAdminMail().Forget();
    }

    // 글로벌 메일 비활성화 
    public async UniTask DisableGlobalMail()
    {
        try
        {
            await db.Child("globalMail").Child("active").SetValueAsync(false);
        }
        catch
        {
        }
    }

    // 파이어베이스 에서 개인에게 줄때 개인 아이디 mails에 노드 추가해서 사용 키에 파이어베이스 키
//  {
//  "mailId": "unique_mail_id_123", // 키와 동일해야함 
//  "senderId": "admin",
//  "senderName": "운영팀",
//  "receiverId": "VlN8aEQSCNS9UP3WqUwm5oJliUZ2", // 유저 아이디와 동일해야함
//  "title": "개인 보상",
//  "content": "특별 이벤트 참여 보상입니다.",
//  "timestamp": 1733184000000,
//  "isRead": false,
//  "isRewarded": false,
//  "itemList": [
//    {
//      "itemId": "7101",
//      "count": 1000
//    },
//    {
//    "itemId": "7102", 
//      "count": 100
//    }
//  ]
//  }
}