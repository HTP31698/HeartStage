using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Daily / Weekly / Achievement가 같이 쓰는
/// 공통 퀘스트 아이템 UI 베이스.
/// </summary>
public class QuestItemUIBase : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] protected TextMeshProUGUI InfoText;
    [SerializeField] protected Image iconImage;
    [SerializeField] protected Button completeButton;

    [Header("진행도 표시")]
    [SerializeField] protected Slider progressSlider;      // 진행도 슬라이더 (없으면 비워둬도 됨)
    [SerializeField] protected TextMeshProUGUI progressText; // "3/10" 형태 텍스트 (없으면 비워둬도 됨)

    [Header("완료 상태 텍스트 & 색상")]
    [SerializeField] protected TextMeshProUGUI stateText;  // 버튼 상태 텍스트 ("미완료" / "받기" / "완료")
    [SerializeField] protected Color claimableButtonColor = Color.white;                     // 받기 가능 상태 색상
    [SerializeField] protected Color notClearedButtonColor = new Color(0.6f, 0.6f, 0.6f, 1f); // 미완료 상태 색상
    [SerializeField] protected Color completedButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);  // 완료 상태 색상

    [Header("아이콘 Addressables 키를 QuestData.Icon_image에서 읽음")]
    [SerializeField] protected bool useIconAddressable = true;

    public int QuestId => questData != null ? questData.Quest_ID : 0;

    /// <summary>
    /// 받기 버튼의 Transform (애니메이션 시작점용)
    /// </summary>
    public Transform ClaimButtonTransform => completeButton != null ? completeButton.transform : transform;

    protected IQuestItemOwner owner;
    protected QuestData questData;

    protected bool isCleared;   // 조건 충족 여부
    protected bool isCompleted; // 보상 수령 여부
    protected int currentProgress; // 현재 진행도
    protected int requiredProgress; // 필요 진행도

    protected virtual void Awake()
    {
        if (completeButton != null)
        {
            completeButton.onClick.RemoveAllListeners();
            completeButton.onClick.AddListener(OnClickCompleteInternal);
        }
    }

    /// <summary>
    /// 각 탭(Daily/Weekly/Archivement)에서 생성 직후 한 번 호출
    /// </summary>
    public virtual void Init(IQuestItemOwner owner, QuestData data, bool cleared, bool completed)
    {
        Init(owner, data, cleared, completed, 0);
    }

    /// <summary>
    /// 진행도 포함 초기화 (슬라이더 표시용)
    /// </summary>
    public virtual void Init(IQuestItemOwner owner, QuestData data, bool cleared, bool completed, int progress)
    {
        Init(owner, data, cleared, completed, progress, data.Quest_required);
    }

    /// <summary>
    /// 진행도 + 필요량 지정 초기화 (1회성 퀘스트 등 특수 케이스용)
    /// </summary>
    public virtual void Init(IQuestItemOwner owner, QuestData data, bool cleared, bool completed, int progress, int required)
    {
        this.owner = owner;
        this.questData = data;
        this.isCleared = cleared;
        this.isCompleted = completed;
        this.currentProgress = progress;
        this.requiredProgress = required;

        // 텍스트 ({Quest_required}, {Target_name} 치환)
        if (InfoText != null)
            InfoText.text = FormatQuestInfo(data);

        // 아이콘
        if (useIconAddressable && iconImage != null && !string.IsNullOrEmpty(data.Icon_image))
        {
            LoadIconAsync(data.Icon_image);
        }

        ApplyVisualState();
        UpdateProgressUI();
    }

    /// <summary>
    /// 진행도만 업데이트 (실시간 갱신용)
    /// </summary>
    public virtual void SetProgress(int progress)
    {
        currentProgress = progress;
        UpdateProgressUI();
    }

    /// <summary>
    /// 슬라이더 및 진행도 텍스트 업데이트
    /// </summary>
    protected virtual void UpdateProgressUI()
    {
        // 완료된 퀘스트는 최대치로 표시
        int displayProgress = isCompleted ? requiredProgress : Mathf.Min(currentProgress, requiredProgress);

        if (progressSlider != null)
        {
            progressSlider.maxValue = requiredProgress;
            progressSlider.value = displayProgress;
        }

        if (progressText != null)
        {
            progressText.text = $"{displayProgress}/{requiredProgress}";
        }
    }

    /// <summary>
    /// SaveData 또는 외부 이벤트 기반으로 상태 갱신
    /// </summary>
    public virtual void SetState(bool cleared, bool completed)
    {
        isCleared = cleared;
        isCompleted = completed;
        ApplyVisualState();
        UpdateProgressUI();
    }

    /// <summary>
    /// 완료만 true로 바꾸고 싶을 때(예: ApplyCompletedStateToItems)
    /// </summary>
    public virtual void SetCompleted(bool completed)
    {
        isCompleted = completed;
        ApplyVisualState();
        UpdateProgressUI();
    }

    protected virtual void ApplyVisualState()
    {
        // 버튼 인터랙션 & 색상
        if (completeButton != null)
        {
            // Unity의 자동 색상 전환 완전히 비활성화 (타이밍 이슈 방지)
            completeButton.transition = Selectable.Transition.None;

            // 버튼 인터랙션 설정
            bool canInteract = isCleared && !isCompleted;
            completeButton.interactable = canInteract;

            // 상태에 따른 색상 결정
            Color targetColor;
            if (isCompleted)
            {
                targetColor = completedButtonColor;
            }
            else if (isCleared)
            {
                targetColor = claimableButtonColor;
            }
            else
            {
                targetColor = notClearedButtonColor;
            }

            // 항상 불투명하게 설정
            targetColor.a = 1f;

            // targetGraphic 색상 적용
            var targetGraphic = completeButton.targetGraphic as Image;
            if (targetGraphic != null)
            {
                targetGraphic.color = targetColor;
            }
        }

        // 상태 텍스트 (버튼 내부 또는 외부)
        if (stateText != null)
        {
            if (isCompleted)
            {
                stateText.text = "완료";
            }
            else if (isCleared)
            {
                stateText.text = "받기";
            }
            else
            {
                stateText.text = "미완료";
            }
        }
    }

    protected void LoadIconAsync(string key)
    {
        var handle = Addressables.LoadAssetAsync<Sprite>(key);
        handle.Completed += OnIconLoaded;
    }

    private void OnIconLoaded(AsyncOperationHandle<Sprite> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded && iconImage != null)
        {
            iconImage.sprite = handle.Result;
        }
    }

    /// <summary>
    /// Quest_info 텍스트에서 플레이스홀더 치환
    /// - {Quest_required}: 필요 횟수
    /// - {Target_name}: 대상 이름 (몬스터/스테이지)
    /// </summary>
    private string FormatQuestInfo(QuestData data)
    {
        if (data == null || string.IsNullOrEmpty(data.Quest_info))
            return data?.Quest_info ?? "";

        string result = data.Quest_info;

        // {Quest_required} 치환
        result = result.Replace("{Quest_required}", data.Quest_required.ToString());

        // {Target_name} 치환
        if (result.Contains("{Target_name}") && data.Target_ID > 0)
        {
            string targetName = GetTargetName(data.Event_type, data.Target_ID);
            result = result.Replace("{Target_name}", targetName);
        }

        return result;
    }

    /// <summary>
    /// Event_type과 Target_ID로 대상 이름 조회
    /// </summary>
    private string GetTargetName(QuestEventType eventType, int targetId)
    {
        switch (eventType)
        {
            case QuestEventType.BossKill:
            case QuestEventType.MonsterKill:
                var monsterTable = DataTableManager.Get<MonsterTable>(DataTableIds.Monster);
                if (monsterTable != null)
                {
                    var monster = monsterTable.Get(targetId);
                    if (monster != null)
                        return monster.mon_name;
                }
                return $"몬스터({targetId})";

            case QuestEventType.ClearStage:
                var stageTable = DataTableManager.Get<StageTable>(DataTableIds.Stage);
                if (stageTable != null)
                {
                    var stage = stageTable.GetStage(targetId);
                    if (stage != null)
                        return stage.stage_name;
                }
                return $"스테이지({targetId})";

            default:
                return targetId.ToString();
        }
    }

    private void OnClickCompleteInternal()
    {
        if (owner == null || questData == null)
            return;

        if (isCompleted)
        {
            Debug.Log("[QuestItemUIBase] 이미 완료된 퀘스트입니다.");
            return;
        }

        // ★ 클리어 여부는 탭 쪽에서 관리
        //  → 이건 SaveData + 외부 이벤트로 SetState(cleared, ...)에서 들어온 값만 믿는다.
        if (!isCleared)
        {
            Debug.Log("[QuestItemUIBase] 아직 클리어 조건을 만족하지 않은 퀘스트입니다.");
            return;
        }

        // 여기까지 왔으면:
        // - 조건은 이미 충족(cleared == true)
        // - 아직 보상은 안 받음(completed == false)
        owner.OnQuestItemClickedComplete(questData, this);
    }
}

/// <summary>
/// 퀘스트 카드의 주인 (DailyQuests / WeeklyQuests / ArchivementQuests)가
/// 반드시 구현해야 하는 인터페이스
/// </summary>
public interface IQuestItemOwner
{
    void OnQuestItemClickedComplete(QuestData questData, QuestItemUIBase itemUI);
}
