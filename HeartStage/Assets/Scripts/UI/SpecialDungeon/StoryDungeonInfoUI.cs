using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StoryDungeonInfoUI : GenericWindow
{
    [Header("Story Stage Settings")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject storyInfoPrefab;

    private List<StoryInfoPrefab> createdStoryPrefabs = new List<StoryInfoPrefab>();
    private bool needsPositionFix = false; // 위치 수정이 필요한지 플래그

    public override void Open()
    {
        base.Open();

        needsPositionFix = true; // 위치 수정 플래그 설정

        // 하나 스토리 프리팹 바로 뒤에 위치하도록 Sibling Index 조정
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

        // 하나 프리팹 찾기
        int hanaIndex = -1;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == "HanaStoryPrefab")
            {
                hanaIndex = i;
                break;
            }
        }

        if (hanaIndex == -1) return;

        int targetIndex = hanaIndex + 1;
        int currentIndex = transform.GetSiblingIndex();

        // 목표 위치가 아니면 계속 수정
        if (currentIndex != targetIndex)
        {
            transform.SetSiblingIndex(targetIndex);

            // 레이아웃 강제 업데이트
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);

            Debug.Log($"[LateUpdate] 위치 강제 수정: {currentIndex} -> {targetIndex}");
        }
        else
        {
            // 3프레임 연속으로 올바른 위치에 있으면 체크 중단
            StartCoroutine(StopPositionCheckAfterDelay());
        }
    }

    private IEnumerator StopPositionCheckAfterDelay()
    {
        yield return null;
        yield return null;
        yield return null; // 3프레임 대기

        needsPositionFix = false;
        Debug.Log("위치 강제 수정 완료 - LateUpdate 체크 중단");
    }

    /// 하나 스토리 프리팩 바로 뒤에 위치하도록 Sibling Index 조정
    private void AdjustSiblingIndex()
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            Debug.LogError("부모 Transform이 null입니다!");
            return;
        }

        // 하나 스토리 프리팩을 찾기
        int hanaIndex = -1;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == "HanaStoryPrefab")
            {
                hanaIndex = i;
                break;
            }
        }

        if (hanaIndex == -1)
        {
            Debug.LogWarning("HanaStoryPrefab을 찾을 수 없습니다!");
            return;
        }

        int targetIndex = hanaIndex + 1;
        int currentIndex = transform.GetSiblingIndex();

        Debug.Log($"현재 StoryDungeonInfoUI 인덱스: {currentIndex}, 목표 인덱스: {targetIndex}");

        transform.SetSiblingIndex(targetIndex);

        // 즉시 레이아웃 업데이트
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);

        Debug.Log($"StoryDungeonInfoUI를 인덱스 {targetIndex}로 이동 완료");
    }

    // 나머지 코드는 동일...
    private void CreateFilteredStoryStages()
    {
        ClearAllStoryStages();
        var orderedStoryStages = DataTableManager.StoryTable.GetOrderedStoryStages();
        var filteredStages = FilterStoriesByCharacter(orderedStoryStages, StoryDungeonUI.currentStoryFilter);

        foreach (var stageData in filteredStages)
        {
            CreateStoryInfoPrefab(stageData);
        }
    }

    private List<StoryStageCSVData> FilterStoriesByCharacter(List<StoryStageCSVData> allStories, string characterFilter)
    {
        if (string.IsNullOrEmpty(characterFilter))
        {
            return allStories;
        }
        return allStories.Where(story => IsStoryForCharacter(story, characterFilter)).ToList();
    }

    private bool IsStoryForCharacter(StoryStageCSVData story, string characterFilter)
    {
        if (story == null || string.IsNullOrEmpty(characterFilter))
            return true;
        return story.need_char == characterFilter;
    }

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
            storyInfo.SetStageData(stageData, OnStoryStageSelected);
            createdStoryPrefabs.Add(storyInfo);
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

    private void OnStoryStageSelected(int storyStageId)
    {
        var stageData = DataTableManager.StoryTable.GetStoryStage(storyStageId);
        if (stageData != null)
        {
            Debug.Log($"스토리 스테이지 선택됨: {stageData.story_stage_name} (ID: {storyStageId})");
        }
    }

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