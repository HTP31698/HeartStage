using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;

public class StageWindow : GenericWindow
{
    [Header("UI References")]
    public RectTransform contentParent;
    public GameObject stagePrefab; // 피벗 (0.5,0), 앵커 (0.5,0)    
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private StageInfoWindow stageInfoUI;

    [Header("Button")]
    //[SerializeField] private Button closeButton;
    [SerializeField] private Button stageInfoButton;

    private float verticalSpacing = 500f; // 세로 간격
    private float horizontalOffset = 350f; // 좌우 번갈이 거리
    private float verticalPadding = 100f; // 화면 상단 패딩

    [Header("Field")]
    private StageTable stageTable;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;

    // 스테이지 ID → 인덱스 맵핑 (스크롤 위치 계산용)
    private Dictionary<int, int> _stageIdToIndex = new();

    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출 (WindowAnimator 초기화)
        isOverlayWindow = true; // 오버레이 창으로 설정
    }

    // 자식 오브젝트 삭제
    [ContextMenu("DeleteChildren")]
    public void DeleteChildren()
    {
        for (int i = contentParent.childCount - 1; i >= 1; i--)
        {            
            DestroyImmediate(contentParent.GetChild(i).gameObject);
        }
    }

    // 스테이지 이미지 간격에 맞게 생성
    [ContextMenu("GenerateStages()")]
    public void GenerateStages()
    {
        DeleteChildren();

        if (stageTable == null)
        {
            stageTable = DataTableManager.StageTable;
        }

        var allStages = stageTable.GetOrderedStagesSO();

        allStages.Sort((x, y) =>
        {
            int result = x.stage_step1.CompareTo(y.stage_step1);
            if (result == 0)
            {
                result = x.stage_step2.CompareTo(y.stage_step2);
            }
            return result;
        });

        int index = 0;

        for (int i = 0; i < allStages.Count; i++)
        {
            var stageData = allStages[i];
            var sb = new StringBuilder();
            sb.Clear();

            if (stageData.stage_step1 == 0 && stageData.stage_step2 == 1)
            {
                sb.Append("튜토리얼");
            }
            else
            {
                sb.Append($"{stageData.stage_step1} - {stageData.stage_step2}");
            }

            GameObject stageObj = Instantiate(stagePrefab, contentParent);

            RectTransform rect = stageObj.GetComponent<RectTransform>();

            float y = index * verticalSpacing + verticalPadding;
            float x = (index % 2 == 0) ? -horizontalOffset : horizontalOffset;

            rect.anchoredPosition = new Vector2(x, y);

            var text = stageObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = sb.ToString();

            // StageChoosePrefab 컴포넌트 초기화 
            var stageChoosePrefab = stageObj.GetComponent<StageChoosePrefab>();
            if (stageChoosePrefab != null)
            {
                stageChoosePrefab.Initialize(stageData);
            }

            // 스테이지 잠금 여부 확인 (이전 스테이지 클리어 필요)
            bool isUnlocked = IsStageUnlocked(allStages, i);

            var button = stageObj.GetComponent<Button>();
            if (button != null)
            {
                // 잠금 상태면 버튼 비활성화
                button.interactable = isUnlocked;

                // 지역 변수에 복사하여 각각의 값을 캡처
                var capturedStageData = stageData; // 클로저 문제 해결
                var capturedButton = button; // 버튼 참조도 캡처
                button.onClick.AddListener(() => OnStageInfoButtonClicked(capturedStageData, capturedButton));
            }

            // StageChoosePrefab 잠금 UI 설정
            if (stageChoosePrefab != null)
            {
                stageChoosePrefab.SetLocked(!isUnlocked);
            }

            // 스테이지 ID → 인덱스 맵핑 저장 (스크롤 위치 계산용)
            _stageIdToIndex[stageData.stage_ID] = index;

            index++;
        }

        // Content의 높이 자동 조정
        float contentHeight = (allStages.Count - 1) * verticalSpacing + 500f;
        Vector2 size = contentParent.sizeDelta;
        size.y = contentHeight;
        contentParent.sizeDelta = size;
    }

    /// <summary>
    /// 스테이지가 잠금 해제 상태인지 확인
    /// 첫 번째 스테이지(튜토리얼)는 항상 열림
    /// 이후 스테이지는 이전 스테이지 클리어 시 열림
    /// </summary>
    private bool IsStageUnlocked(List<StageData> allStages, int currentIndex)
    {
        // 첫 번째 스테이지(튜토리얼)는 항상 열림
        if (currentIndex == 0)
            return true;

        // 이전 스테이지 데이터 가져오기
        var previousStage = allStages[currentIndex - 1];

        // 이전 스테이지가 클리어되었는지 확인 (웨이브 기반)
        return IsStageCleared(previousStage);
    }

    /// <summary>
    /// 스테이지의 모든 웨이브가 클리어되었는지 확인
    /// </summary>
    private bool IsStageCleared(StageData stageData)
    {
        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData == null || saveData.clearWaveList == null)
        {
            return false;
        }

        // 웨이브 ID
        int[] waveIds = {
            stageData.wave1_id,
            stageData.wave2_id,
            stageData.wave3_id,
            stageData.wave4_id
        };

        foreach (int waveId in waveIds)
        {
            if (waveId > 0)
            {
                if (!saveData.clearWaveList.Contains(waveId))
                {
                    return false; // 하나라도 클리어되지 않았으면 false
                }
            }
        }

        return true; // 모든 웨이브가 클리어되었으면 true
    }

    private void OnStageInfoButtonClicked(StageData stageData, Button fromButton)
    {
        Debug.Log($"[StageWindow] OnStageInfoButtonClicked: {stageData?.stage_ID}, windowManager={windowManager != null}");

        if (windowManager == null)
        {
            Debug.LogError("[StageWindow] windowManager is null!");
            return;
        }

        if (stageInfoUI != null)
        {
            stageInfoUI.SetStageData(stageData);
        }

        // 버튼 위치에서 시작하는 애니메이션 설정
        if (fromButton != null)
        {
            WindowAnimator.SetNextFromButton(fromButton.GetComponent<RectTransform>());
        }

        windowManager.OpenOverlayNoDim(WindowType.StageInfo);
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
    }

    public override void Open()
    {
        base.Open();
        GenerateStages();

        // 돌아가기로 로비에 온 경우 선택했던 스테이지로 스크롤
        int selectedId = SaveLoadManager.Data?.selectedStageID ?? -1;
        if (selectedId > 0)
        {
            ScrollToStage(selectedId);
        }
    }

    /// <summary>
    /// 특정 스테이지 ID 위치로 스크롤
    /// </summary>
    public void ScrollToStage(int stageId)
    {
        if (scrollRect == null || !_stageIdToIndex.TryGetValue(stageId, out int index))
            return;

        // 해당 스테이지의 Y 위치 계산
        float targetY = index * verticalSpacing + verticalPadding;

        // Content 전체 높이
        float contentHeight = contentParent.sizeDelta.y;
        // Viewport 높이
        float viewportHeight = scrollRect.viewport.rect.height;

        // 스크롤 가능한 범위
        float scrollableHeight = contentHeight - viewportHeight;
        if (scrollableHeight <= 0)
            return;

        // normalizedPosition (0 = 맨 아래, 1 = 맨 위)
        // targetY를 중앙에 두려면 약간 오프셋
        float targetScroll = (targetY - viewportHeight * 0.5f) / scrollableHeight;
        targetScroll = Mathf.Clamp01(targetScroll);

        scrollRect.verticalNormalizedPosition = targetScroll;
    }

    public override void Close()
    {
        base.Close();
    }
}
