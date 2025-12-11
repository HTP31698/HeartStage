using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public static class StageLayoutUtil
{
    public const int SlotCount = 15; // 5x3 고정 공간

    // 캐싱된 SO 참조
    private static StageLayoutData _cachedData;

    // Addressable 주소 (StageAssets 라벨)
    private const string AddressableKey = "Assets/ScriptableObject/Tool/StageLayouts.asset";

    // 초기화 완료 여부
    public static bool IsInitialized => _cachedData != null;

    /// <summary>
    /// Addressable로 SO 비동기 로드 (앱 시작 시 호출)
    /// </summary>
    public static async UniTask InitializeAsync()
    {
        if (_cachedData != null) return;

        try
        {
            var handle = Addressables.LoadAssetAsync<StageLayoutData>(AddressableKey);
            _cachedData = await handle.ToUniTask();

            if (_cachedData == null)
            {
                Debug.LogError($"[StageLayoutUtil] StageLayoutData를 찾을 수 없습니다. " +
                    $"Addressable '{AddressableKey}'가 등록되어 있는지 확인하세요.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StageLayoutUtil] Addressable 로드 실패: {e.Message}");
        }
    }

    // SO 데이터 (캐싱됨)
    // 에디터에서는 AssetDatabase로 자동 로드, 런타임에서는 InitializeAsync() 필요
    private static StageLayoutData LayoutData
    {
        get
        {
            if (_cachedData == null)
            {
#if UNITY_EDITOR
                // 에디터에서는 AssetDatabase로 자동 로드
                _cachedData = UnityEditor.AssetDatabase.LoadAssetAtPath<StageLayoutData>(AddressableKey);
                if (_cachedData == null)
                {
                    // 경로로 못 찾으면 타입으로 검색
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StageLayoutData");
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        _cachedData = UnityEditor.AssetDatabase.LoadAssetAtPath<StageLayoutData>(path);
                    }
                }
#else
                Debug.LogWarning("[StageLayoutUtil] 데이터가 초기화되지 않았습니다. InitializeAsync()를 먼저 호출하세요.");
#endif
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

