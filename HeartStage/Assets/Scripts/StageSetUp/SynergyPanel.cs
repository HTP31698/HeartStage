using System.Collections.Generic;
using UnityEngine;

public class SynergyPanel : MonoBehaviour
{
    [SerializeField] private SynergyButton[] slots;          // 미리 만들어둔 버튼 3개
    [SerializeField] private SynergyDetailPanel detailPanel; // 클릭 시 열릴 상세창

    // 현재 어떤 시너지 id가 어떤 슬롯 버튼을 쓰는지
    private readonly Dictionary<int, SynergyButton> buttonsById = new Dictionary<int, SynergyButton>();

    private void Awake()
    {
        if (slots == null)
            return;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            // 항상 버튼은 켜두고
            slot.gameObject.SetActive(true);

            // 내용은 빈 슬롯으로 초기화
            slot.InitEmpty();

            // 클릭 이벤트 연결
            slot.onClick = OnButtonClicked;
        }

        if (detailPanel != null)
            detailPanel.gameObject.SetActive(false);
    }

    /// 패널 초기화: 슬롯들 전부 비우기 (항상 3칸 보이게 유지)
    public void BuildAllButtons()
    {
        buttonsById.Clear();

        if (slots == null)
            return;

        foreach (var slot in slots)
        {
            if (slot == null) continue;
            slot.gameObject.SetActive(true); // 항상 보이게
            slot.InitEmpty();                // 내용만 비우기
            slot.onClick = OnButtonClicked;
        }
    }

    /// 현재 "발동 중"인 시너지 목록을 받아서,
    /// 앞에서부터 3개까지만 슬롯에 꽂아준다.
    public void UpdateActiveSynergies(List<SynergyManager.ActiveSynergy> actives)
    {
        buttonsById.Clear();

        if (slots == null)
        {
            Debug.LogWarning("[SynergyPanel] slots == null");
            return;
        }

        // 1) 모든 슬롯을 "빈칸"으로 초기화
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            slot.InitEmpty();
        }

        // 2) 발동된 시너지가 없으면 → 빈칸 3개 유지
        if (actives == null || actives.Count == 0)
        {
            return;
        }

        var table = DataTableManager.SynergyTable;

        int slotIndex = 0;
        foreach (var active in actives)
        {
            if (slotIndex >= slots.Length)
                break;

            var data = active.data;
            if (data == null)
            {
                Debug.LogWarning("[SynergyPanel] ActiveSynergy.data == null");
                continue;
            }

            int id = data.synergy_id;
            var csv = table.Get(id);
            if (csv == null)
            {
                Debug.LogWarning($"[SynergyPanel] CSV에 synergy_id={id} 없음");
                continue;
            }

            var slot = slots[slotIndex];
            if (slot == null)
            {
                Debug.LogWarning($"[SynergyPanel] slots[{slotIndex}] == null");
                continue;
            }

            Debug.Log($"[SynergyPanel] 슬롯 {slotIndex}에 시너지 id={id}, name={csv.synergy_name} 표시");

            // GameObject는 이미 켜져 있으니까 내용만 채우면 됨
            slot.Init(csv, active: true);

            buttonsById[id] = slot;
            slotIndex++;
        }
    }

    private void OnButtonClicked(SynergyButton btn)
    {
        var data = btn.GetData();
        if (data == null || detailPanel == null)
            return;

        detailPanel.Show(data, btn.IsActive);
    }
}
