using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MonitoringRewardUI : GenericWindow
{
    [SerializeField] private Button rewardButton;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform rewardTransform;

    // 생성된 아이템 프리팹들을 관리하기 위한 리스트
    private List<GameObject> spawnedRewardItems = new List<GameObject>();

    private void Awake()
    {
        rewardButton.onClick.AddListener(OnRewardButtonClicked);
    }

    public override void Open()
    {
        base.Open();

        // 열릴 때 자동으로 현재 선택된 스테이지의 보상 표시
        var gameData = SaveLoadManager.Data;
        int currentStageID = gameData.selectedStageID;

        if (currentStageID != -1)
        {
            var stageData = DataTableManager.StageTable?.GetStageData(currentStageID);
            if (stageData != null)
            {
                int rewardId = stageData.dispatch_reward;
                ShowRewards(rewardId);
            }
        }
    }

    public override void Close()
    {
        base.Close();
        ClearRewardItems();
    }


    /// 파견 보상 데이터를 받아서 UI에 표시
    public void ShowRewards(int stageId)
    {
        ClearRewardItems();

        // RewardTable에서 해당 스테이지의 보상 데이터 가져오기
        var rewardData = DataTableManager.RewardTable.Get(stageId);
        if (rewardData == null)
        {
            Debug.LogWarning($"보상 데이터를 찾을 수 없습니다: Stage {stageId}");
            return;
        }

        // 보상 아이템들 생성 (일반 클리어 보상 기준)
        CreateRewardItem(rewardData.normal_clear1, rewardData.normal_clear1_a);
        CreateRewardItem(rewardData.normal_clear2, rewardData.normal_clear2_a);
        CreateRewardItem(rewardData.normal_clear3, rewardData.normal_clear3_a);
    }


    /// 개별 보상 아이템 프리팹 생성
    private void CreateRewardItem(int itemId, int count)
    {
        // 유효하지 않은 아이템이면 스킵
        if (itemId <= 0 || count <= 0)
            return;

        if (itemPrefab == null || rewardTransform == null)
        {
            Debug.LogWarning("itemPrefab 또는 rewardTransform이 설정되지 않았습니다.");
            return;
        }

        // 프리팹 생성
        GameObject rewardItem = Instantiate(itemPrefab, rewardTransform);
        spawnedRewardItems.Add(rewardItem);

        // MonitoringItemPrefab 컴포넌트 설정 - 변수명 변경
        MonitoringItemPrefab itemComponent = rewardItem.GetComponent<MonitoringItemPrefab>();
        if (itemComponent != null)
        {
            itemComponent.SetItemData(itemId, count);
        }

        // Transform 설정
        if (rewardItem.transform is RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition3D = Vector3.zero;
        }
    }


    /// 생성된 보상 아이템들 정리
    private void ClearRewardItems()
    {
        foreach (var item in spawnedRewardItems)
        {
            if (item != null)
                Destroy(item);
        }
        spawnedRewardItems.Clear();
    }

    private void OnRewardButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        base.Close();
    }
}