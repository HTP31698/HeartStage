using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SeraStoryDungeonInfoUI : GenericWindow
{
    [Header("Story Stage Settings")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject seraStoryInfoPrefab;

    private List<StoryInfoPrefab> createdStoryPrefabs = new List<StoryInfoPrefab>();
    private bool needsPositionFix = false; // 위치 수정이 필요한지 플래그

    public override void Open()
    {
        base.Open();

        needsPositionFix = true; // 위치 수정 플래그 설정

        AdjustSiblingIndex();

        CreateFilteredStoryStages();
    }

    public override void Close()
    {
        base.Close();
        needsPositionFix = false; // 플래그 리셋
        ClearAllStoryStages();
    }

    private void LateUpdate()
    {
        // 위치 수정이 필요한 경우에만 계속 체크
        if (needsPositionFix)
        {
            ForceCorrectPosition();
        }
    }

    /// 강제로 올바른 위치에 배치
    private void ForceCorrectPosition()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        // 세라 프리팹 찾기
        int seraIndex = -1;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == "SeraStoryPrefab")
            {
                seraIndex = i;
                break;
            }
        }

        if (seraIndex == -1) return;

        int targetIndex = seraIndex + 1;
        int currentIndex = transform.GetSiblingIndex();

        // 목표 위치가 아니면 계속 수정
        if (currentIndex != targetIndex)
        {
            transform.SetSiblingIndex(targetIndex);

            // 레이아웃 강제 업데이트
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);

            Debug.Log($"[SeraStoryDungeonInfoUI] 위치 강제 수정: {currentIndex} -> {targetIndex}");
        }
    }

    /// 세라 스토리 프리팹 바로 뒤에 위치하도록
    private void AdjustSiblingIndex()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        // 세라 스토리 프리팹을 찾아서 그 뒤에 위치
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == "SeraStoryPrefab")
            {
                // 세라 프리팹 바로 뒤 인덱스로 이동
                transform.SetSiblingIndex(i + 1);
                Debug.Log($"SeraStoryDungeonInfoUI를 SeraStoryPrefab 뒤로 이동: 인덱스 {i + 1}");
                break;
            }
        }
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
        if (seraStoryInfoPrefab == null || content == null)
        {
            return;
        }

        GameObject stageObject = Instantiate(seraStoryInfoPrefab, content);
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