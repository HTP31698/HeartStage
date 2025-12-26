using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StoryDungeonInfoUI : GenericWindow
{
    [Header("Story Stage Settings")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject storyInfoPrefab;

    private List<StoryInfoPrefab> createdStoryPrefabs = new List<StoryInfoPrefab>();

    public override void Open()
    {
        base.Open();
        CreateFilteredStoryStages();
    }

    public override void Close()
    {
        base.Close();
        ClearAllStoryStages();
    }

    /// 필터된 스토리 스테이지 프리팹 생성
    private void CreateFilteredStoryStages()
    {
        // 기존 프리팹들 정리
        ClearAllStoryStages();

        // 스토리 스테이지 데이터 가져오기
        var orderedStoryStages = DataTableManager.StoryTable.GetOrderedStoryStages();

        // 현재 설정된 필터에 따라 스토리 필터링
        var filteredStages = FilterStoriesByCharacter(orderedStoryStages, StoryDungeonUI.currentStoryFilter);

        foreach (var stageData in filteredStages)
        {
            CreateStoryInfoPrefab(stageData);
        }
    }

    /// 캐릭터별로 스토리 필터링
    private List<StoryStageCSVData> FilterStoriesByCharacter(List<StoryStageCSVData> allStories, string characterFilter)
    {
        if (string.IsNullOrEmpty(characterFilter))
        {
            // 필터가 없으면 모든 스토리 반환
            return allStories;
        }

        // need_char 컬럼 기준으로 필터링
        return allStories.Where(story => IsStoryForCharacter(story, characterFilter)).ToList();
    }

    /// 특정 캐릭터의 스토리인지 판단 (need_char 컬럼 기준)
    private bool IsStoryForCharacter(StoryStageCSVData story, string characterFilter)
    {
        if (story == null || string.IsNullOrEmpty(characterFilter))
            return true;

        // CSV의 need_char 컬럼과 필터 문자열 비교
        return story.need_char == characterFilter;
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