using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SeraStoryDungeonInfoUI : GenericWindow
{
    [Header("Story Stage Settings")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject seraStoryInfoPrefab;

    [Header("Animation")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease expandEase = Ease.OutCubic;
    [SerializeField] private Ease collapseEase = Ease.InCubic;

    private List<StoryInfoPrefab> createdStoryPrefabs = new List<StoryInfoPrefab>();
    private Tween _tween;

    public override void Open()
    {
        base.Open();

        AdjustSiblingIndex();

        CreateFilteredStoryStages();

        // Scale Y 애니메이션 (0 → 1)
        _tween?.Kill();
        transform.localScale = new Vector3(1, 0, 1);
        _tween = transform.DOScaleY(1f, duration)
            .SetEase(expandEase)
            .OnUpdate(RebuildLayout);
    }

    public override void Close()
    {
        // Scale Y 애니메이션 (1 → 0) 후 닫기
        _tween?.Kill();

        _tween = transform.DOScaleY(0f, duration)
            .SetEase(collapseEase)
            .OnUpdate(RebuildLayout)
            .OnComplete(() =>
            {
                ClearAllStoryStages();
                base.Close();
            });
    }

    private void RebuildLayout()
    {
        var parentRect = transform.parent as RectTransform;
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    private void OnDestroy()
    {
        _tween?.Kill();
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