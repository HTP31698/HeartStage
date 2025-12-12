using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public static class PassivePatternUtil
{
    // 5x3 그리드 기준 (0~14)
    public const int Columns = 5;
    public const int Rows = 3;

    // SO 데이터 캐시
    private static PassivePatternData _patternData;

    // Addressable 주소 (StageAssets 라벨)
    private const string AddressableKey = "Assets/ScriptableObject/Tool/PassivePatterns.asset";

    // 초기화 완료 여부
    public static bool IsInitialized => _patternData != null;

    /// <summary>
    /// Addressable로 SO 비동기 로드 (앱 시작 시 호출)
    /// </summary>
    public static async UniTask InitializeAsync()
    {
        if (_patternData != null) return;

        try
        {
            var handle = Addressables.LoadAssetAsync<PassivePatternData>(AddressableKey);
            _patternData = await handle.ToUniTask();

            if (_patternData == null)
            {
                Debug.LogError($"[PassivePatternUtil] PassivePatternData를 찾을 수 없습니다. " +
                    $"Addressable '{AddressableKey}'가 등록되어 있는지 확인하세요.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PassivePatternUtil] Addressable 로드 실패: {e.Message}");
        }
    }

    /// <summary>
    /// SO 데이터 반환 (캐싱됨)
    /// 에디터에서는 AssetDatabase로 자동 로드, 런타임에서는 InitializeAsync() 필요
    /// </summary>
    private static PassivePatternData GetPatternData()
    {
        if (_patternData == null)
        {
#if UNITY_EDITOR
            // 에디터에서는 AssetDatabase로 자동 로드
            _patternData = UnityEditor.AssetDatabase.LoadAssetAtPath<PassivePatternData>(AddressableKey);
            if (_patternData == null)
            {
                // 경로로 못 찾으면 타입으로 검색
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PassivePatternData");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    _patternData = UnityEditor.AssetDatabase.LoadAssetAtPath<PassivePatternData>(path);
                }
            }
#else
            Debug.LogWarning("[PassivePatternUtil] 데이터가 초기화되지 않았습니다. InitializeAsync()를 먼저 호출하세요.");
#endif
        }
        return _patternData;
    }

    /// <summary>
    /// 캐시 초기화 (에디터에서 SO 수정 후 갱신용)
    /// </summary>
    public static void ClearCache()
    {
        _patternData = null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: AssetDatabase로 직접 로드
    /// </summary>
    public static PassivePatternData GetPatternDataEditor()
    {
        if (_patternData == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PassivePatternData");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                _patternData = UnityEditor.AssetDatabase.LoadAssetAtPath<PassivePatternData>(path);
            }
        }
        return _patternData;
    }
#endif

    /// <summary>
    /// 중심 index와 PassiveType(enum)을 기준으로, 버프가 들어가는 타일 인덱스들 리턴
    /// (기존 호환용 오버로드)
    /// </summary>
    public static IEnumerable<int> GetPatternTiles(int centerIndex, PassiveType type, int slotCount)
    {
        return GetPatternTiles(centerIndex, (int)type, slotCount);
    }

    /// <summary>
    /// 중심 index와 typeId(int)를 기준으로, 버프가 들어가는 타일 인덱스들 리턴
    /// </summary>
    public static IEnumerable<int> GetPatternTiles(int centerIndex, int typeId, int slotCount)
    {
        // typeId가 0(None)이면 빈 결과
        if (typeId <= 0)
            yield break;

        var data = GetPatternData();
        if (data == null)
            yield break;

        var offsets = data.GetPattern(typeId);
        if (offsets == null || offsets.Length == 0)
            yield break;

        int total = slotCount;
        int centerRow = centerIndex / Columns;
        int centerCol = centerIndex % Columns;

        foreach (var offset in offsets)
        {
            int r = centerRow + offset.x;
            int c = centerCol + offset.y;

            if (r < 0 || r >= Rows || c < 0 || c >= Columns)
                continue;

            int idx = r * Columns + c;
            if (idx < 0 || idx >= total)
                continue;

            yield return idx;
        }
    }
}
