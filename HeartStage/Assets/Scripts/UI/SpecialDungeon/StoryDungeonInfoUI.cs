using UnityEngine;
using System.Collections.Generic;

public class StoryDungeonInfoUI : GenericWindow
{
    [Header("Story Stage Settings")]
    [SerializeField] private Transform content; 
    [SerializeField] private GameObject storyInfoPrefab; 

    private List<StoryInfoPrefab> createdStoryPrefabs = new List<StoryInfoPrefab>();

    public override void Open()
    {
        base.Open();
        CreateAllStoryStages();
    }

    public override void Close()
    {
        base.Close();
        ClearAllStoryStages();
    }

    /// 모든 스토리 스테이지 프리팹 생성
    private void CreateAllStoryStages()
    {
        // 기존 프리팹들 정리
        ClearAllStoryStages();

        // 스토리 스테이지 데이터 가져오기
        var orderedStoryStages = DataTableManager.StoryTable.GetOrderedStoryStages();

        foreach (var stageData in orderedStoryStages)
        {
            CreateStoryInfoPrefab(stageData);
        }

        Debug.Log($"스토리 던전 UI: 총 {createdStoryPrefabs.Count}개의 스테이지 프리팹 생성 완료");
    }

    /// 개별 StoryInfoPrefab 생성
    private void CreateStoryInfoPrefab(StoryStageCSVData stageData)
    {
        if (storyInfoPrefab == null || content == null)
        {
            return;
        }

        GameObject stageObject = Instantiate(storyInfoPrefab, content);
        StoryInfoPrefab storyInfo = stageObject.GetComponent<StoryInfoPrefab>();

        if (storyInfo != null)
        {
            // 스테이지 데이터 설정
            storyInfo.SetStageData(stageData, OnStoryStageSelected);

            // 생성된 프리팹 관리 목록에 추가
            createdStoryPrefabs.Add(storyInfo);

            // Transform 설정
            stageObject.transform.localScale = Vector3.one;
            if (stageObject.transform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition3D = Vector3.zero;
            }
        }

        else
        {
            Destroy(stageObject);
        }
    }

    /// 스토리 스테이지 선택 시 호출되는 콜백
    private void OnStoryStageSelected(int storyStageId)
    {
        var stageData = DataTableManager.StoryTable.GetStoryStage(storyStageId);
        if (stageData != null)
        {
            Debug.Log($"스토리 스테이지 선택됨: {stageData.story_stage_name} (ID: {storyStageId})");
        }
    }

    /// 생성된 모든 스토리 스테이지 프리팹 정리
    private void ClearAllStoryStages()
    {
        foreach (var storyPrefab in createdStoryPrefabs)
        {
            if (storyPrefab != null && storyPrefab.gameObject != null)
            {
                Destroy(storyPrefab.gameObject);
            }
        }
        createdStoryPrefabs.Clear();
    }
}