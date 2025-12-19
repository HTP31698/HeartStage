using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리팹별 토큰 매핑 저장
/// 프리팹 GUID + 오브젝트 경로 → 토큰
/// </summary>
[CreateAssetMenu(fileName = "PrefabThemeMapping", menuName = "HeartStage/Prefab Theme Mapping", order = 2)]
public class PrefabThemeMapping : ScriptableObject
{
    [Serializable]
    public class MappingEntry
    {
        [Tooltip("프리팹 내 오브젝트 경로")]
        public string ObjectPath;

        [Tooltip("컴포넌트 타입 (Image, TMP_Text, Button 등)")]
        public string ComponentType;

        [Tooltip("적용할 토큰")]
        public ThemeColorToken Token;

        [Tooltip("알파값 유지")]
        public bool PreserveAlpha;

        [Tooltip("원본 색상 (마이그레이션 전)")]
        public Color OriginalColor;
    }

    [Header("프리팹 정보")]
    [Tooltip("원본 프리팹 참조")]
    public GameObject SourcePrefab;

    [Tooltip("프리팹 GUID")]
    public string PrefabGuid;

    [Tooltip("프리팹 경로")]
    public string PrefabPath;

    [Header("매핑 데이터")]
    public List<MappingEntry> Mappings = new List<MappingEntry>();

    [Header("메타 정보")]
    public string CreatedAt;
    public string LastModifiedAt;
    public int Version = 1;

    /// <summary>
    /// 매핑 추가
    /// </summary>
    public void AddMapping(string objectPath, string componentType, ThemeColorToken token, Color originalColor, bool preserveAlpha = false)
    {
        // 중복 체크
        var existing = Mappings.Find(m => m.ObjectPath == objectPath && m.ComponentType == componentType);
        if (existing != null)
        {
            existing.Token = token;
            existing.OriginalColor = originalColor;
            existing.PreserveAlpha = preserveAlpha;
        }
        else
        {
            Mappings.Add(new MappingEntry
            {
                ObjectPath = objectPath,
                ComponentType = componentType,
                Token = token,
                OriginalColor = originalColor,
                PreserveAlpha = preserveAlpha
            });
        }

        LastModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 매핑 가져오기
    /// </summary>
    public MappingEntry GetMapping(string objectPath, string componentType)
    {
        return Mappings.Find(m => m.ObjectPath == objectPath && m.ComponentType == componentType);
    }

    /// <summary>
    /// 매핑 존재 여부
    /// </summary>
    public bool HasMapping(string objectPath, string componentType)
    {
        return GetMapping(objectPath, componentType) != null;
    }

    /// <summary>
    /// 매핑 제거
    /// </summary>
    public bool RemoveMapping(string objectPath, string componentType)
    {
        var entry = GetMapping(objectPath, componentType);
        if (entry != null)
        {
            Mappings.Remove(entry);
            LastModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 전체 매핑 초기화
    /// </summary>
    public void ClearMappings()
    {
        Mappings.Clear();
        LastModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 매핑 개수
    /// </summary>
    public int MappingCount => Mappings.Count;

    /// <summary>
    /// 토큰 사용 통계
    /// </summary>
    public Dictionary<ThemeColorToken, int> GetTokenUsageStats()
    {
        var stats = new Dictionary<ThemeColorToken, int>();
        foreach (var mapping in Mappings)
        {
            if (stats.ContainsKey(mapping.Token))
                stats[mapping.Token]++;
            else
                stats[mapping.Token] = 1;
        }
        return stats;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 생성 시 초기화
    /// </summary>
    private void Reset()
    {
        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        LastModifiedAt = CreatedAt;
    }
#endif
}
