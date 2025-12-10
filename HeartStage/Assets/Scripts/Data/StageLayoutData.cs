using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 레이아웃 데이터 (5x3 그리드)
/// PassivePatternData와 동일한 구조
/// </summary>
[CreateAssetMenu(fileName = "StageLayoutData", menuName = "HeartStage/Stage Layout Data")]
public class StageLayoutData : ScriptableObject
{
    public const int GridColumns = 5;
    public const int GridRows = 3;
    public const int SlotCount = 15;

    [System.Serializable]
    public class LayoutEntry
    {
        public int typeId;           // StageType enum 값
        public string description;   // 설명
        public int[] enabledSlots;   // 활성화된 슬롯 인덱스 배열

        public LayoutEntry(int id, string desc, int[] slots)
        {
            typeId = id;
            description = desc;
            enabledSlots = slots ?? new int[0];
        }
    }

    [SerializeField]
    private List<LayoutEntry> layouts = new List<LayoutEntry>();

    public List<LayoutEntry> GetAllLayouts() => layouts;

    public LayoutEntry GetLayout(int typeId)
    {
        return layouts.Find(l => l.typeId == typeId);
    }

    public void AddLayout(LayoutEntry entry)
    {
        // 중복 체크
        if (layouts.Exists(l => l.typeId == entry.typeId))
        {
            Debug.LogWarning($"[StageLayoutData] typeId {entry.typeId} already exists!");
            return;
        }
        layouts.Add(entry);
    }

    public void RemoveLayout(int typeId)
    {
        layouts.RemoveAll(l => l.typeId == typeId);
    }

    public void UpdateLayout(LayoutEntry entry)
    {
        int idx = layouts.FindIndex(l => l.typeId == entry.typeId);
        if (idx >= 0)
            layouts[idx] = entry;
        else
            layouts.Add(entry);
    }

    /// <summary>
    /// typeId에 해당하는 활성 슬롯 마스크 반환
    /// </summary>
    public bool[] BuildMask(int typeId)
    {
        bool[] mask = new bool[SlotCount];

        var entry = GetLayout(typeId);
        if (entry == null || entry.enabledSlots == null)
        {
            // 없으면 전체 활성화
            for (int i = 0; i < SlotCount; i++)
                mask[i] = true;
            return mask;
        }

        foreach (int idx in entry.enabledSlots)
        {
            if (idx >= 0 && idx < SlotCount)
                mask[idx] = true;
        }

        return mask;
    }
}
