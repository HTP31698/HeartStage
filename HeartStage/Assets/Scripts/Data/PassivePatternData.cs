using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 패시브 타일 패턴 데이터 (ScriptableObject)
/// 에디터 툴에서 편집, 런타임에서 읽기 전용
/// </summary>
[CreateAssetMenu(fileName = "PassivePatterns", menuName = "Data/PassivePatternData")]
public class PassivePatternData : ScriptableObject
{
    [SerializeField]
    private List<PatternEntry> patterns = new List<PatternEntry>();

    /// <summary>
    /// typeId로 패턴 오프셋 배열 반환
    /// </summary>
    public Vector2Int[] GetPattern(int typeId)
    {
        var entry = patterns.Find(p => p.typeId == typeId);
        return entry != null ? entry.offsets : null;
    }

    /// <summary>
    /// 모든 패턴 엔트리 반환 (에디터용)
    /// </summary>
    public List<PatternEntry> GetAllPatterns() => patterns;

    /// <summary>
    /// 패턴 추가 (에디터용)
    /// </summary>
    public void AddPattern(PatternEntry entry)
    {
        patterns.Add(entry);
    }

    /// <summary>
    /// 패턴 제거 (에디터용)
    /// </summary>
    public void RemovePattern(int typeId)
    {
        patterns.RemoveAll(p => p.typeId == typeId);
    }

    /// <summary>
    /// 다음 사용 가능한 typeId 반환
    /// </summary>
    public int GetNextTypeId()
    {
        int maxId = 0;
        foreach (var p in patterns)
        {
            if (p.typeId > maxId)
                maxId = p.typeId;
        }
        return maxId + 1;
    }

    /// <summary>
    /// typeId 존재 여부 확인
    /// </summary>
    public bool HasPattern(int typeId)
    {
        return patterns.Exists(p => p.typeId == typeId);
    }

    [System.Serializable]
    public class PatternEntry
    {
        public int typeId;              // 1, 2, 3... (0은 None으로 예약)
        public string description;      // "자기 + 아래" 등 설명
        public Vector2Int[] offsets;    // 패턴 오프셋 배열 (중심 기준)

        public PatternEntry()
        {
            typeId = 0;
            description = "";
            offsets = new Vector2Int[] { new Vector2Int(0, 0) };
        }

        public PatternEntry(int id, string desc, Vector2Int[] off)
        {
            typeId = id;
            description = desc;
            offsets = off;
        }
    }
}
