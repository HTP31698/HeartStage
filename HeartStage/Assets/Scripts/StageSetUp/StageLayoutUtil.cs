using System.Collections.Generic;
using UnityEngine;

public static class StageLayoutUtil
{
    public const int SlotCount = 15; // 5x3 고정 공간

    // 캐싱된 SO 참조
    private static StageLayoutData _cachedData;

    // SO 로드 (Resources 폴더에서)
    private static StageLayoutData LayoutData
    {
        get
        {
            if (_cachedData == null)
            {
                _cachedData = Resources.Load<StageLayoutData>("StageLayouts");
            }
            return _cachedData;
        }
    }

    // 폴백용 하드코딩 (SO가 없을 때)
    private static readonly Dictionary<StageType, int[]> FallbackIndices
        = new Dictionary<StageType, int[]>
    {
        // 전체 오픈
        { StageType.Full, new [] {
            0,1,2,3,4,
            5,6,7,8,9,
            10,11,12,13,14
        }},
        // 중앙 3x3 : 1 2 3 / 6 7 8 / 11 12 13
        { StageType.Stage1, new [] {
            1,2,3,
            6,7,8,
            11,12,13
        }},
        //Stage2 : 1 2 3 / 6 7 8 / 10 11 12 13 14
        { StageType.Stage2, new []{
            1,2,3,
            6,7,8,
            10,11,12,13,14
        }},
    };

    public static bool[] BuildMask(int stageTypeInt)
    {
        bool[] mask = new bool[SlotCount];

        // ★ SO 우선 사용
        if (LayoutData != null)
        {
            return LayoutData.BuildMask(stageTypeInt);
        }

        // SO가 없으면 폴백
        var type = (StageType)stageTypeInt;

        if (!FallbackIndices.TryGetValue(type, out var indices))
        {
            // 정의 안 된 타입이면 안전하게 전체 오픈
            indices = FallbackIndices[StageType.Full];
        }

        foreach (var i in indices)
            if (i >= 0 && i < SlotCount) mask[i] = true;

        return mask;
    }

    /// <summary>
    /// 캐시 클리어 (에디터에서 SO 변경 후 호출)
    /// </summary>
    public static void ClearCache()
    {
        _cachedData = null;
    }
}

