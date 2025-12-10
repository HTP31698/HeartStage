using System.Collections.Generic;
using UnityEngine;

public static class PassivePatternUtil
{
    // 5x3 그리드 기준 (0~14)
    public const int Columns = 5;
    public const int Rows = 3;

    // SO 데이터 캐시
    private static PassivePatternData _patternData;

    // SO 경로 (Resources 폴더 기준)
    private const string PatternDataPath = "PassivePatterns";

    /// <summary>
    /// SO 데이터 로드 (캐싱)
    /// </summary>
    private static PassivePatternData GetPatternData()
    {
        if (_patternData == null)
        {
            _patternData = Resources.Load<PassivePatternData>(PatternDataPath);

            if (_patternData == null)
            {
                Debug.LogError($"[PassivePatternUtil] PassivePatternData를 찾을 수 없습니다. " +
                    $"Resources/{PatternDataPath}.asset 파일이 있는지 확인하세요.");
            }
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
