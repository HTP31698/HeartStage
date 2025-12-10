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
    [SerializeField] protected Color normalButtonColor = Color.white;          // 기본 색
    [SerializeField] protected Color completedButtonColor = new Color(0.7f, 0.7f, 0.7f); // 완료 후 약간 어두운 색

    [Header("아이콘 Addressables 키를 QuestData.Icon_image에서 읽음")]
    [SerializeField] protected bool useIconAddressable = true;

    public int QuestId => questData != null ? questData.Quest_ID : 0;

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

        // 텍스트 ({Quest_required} 치환)
        if (InfoText != null)
            InfoText.text = FormatQuestInfo(data.Quest_info, data.Quest_required);

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
            if (!isCleared)
            {
                completeButton.interactable = false;
            }
            else
            {
                // 조건만 충족되면, 아직 완료 안 되었을 때 한 번은 누를 수 있음
                completeButton.interactable = !isCompleted;
            }

            var targetGraphic = completeButton.targetGraphic as Image;
            if (targetGraphic != null)
            {
                targetGraphic.color = isCompleted ? completedButtonColor : normalButtonColor;
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
    /// Quest_info 텍스트에서 {Quest_required}를 실제 값으로 치환
    /// </summary>
    private string FormatQuestInfo(string questInfo, int questRequired)
    {
        if (string.IsNullOrEmpty(questInfo))
            return questInfo;

        return questInfo.Replace("{Quest_required}", questRequired.ToString());
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
