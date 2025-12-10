using System.Collections.Generic;
using UnityEngine;

public enum SynergyTarget
{
    None = 0,
    AlliesAll = 1,      // CSV에서 skill_target = 1
    GlobalOrEtc = 2,    // CSV에서 skill_target = 2 (함성 게이지, 몬스터 드랍, 적 속도 등)
    Stage = 4,
}

/// 배치된 캐릭터들의 CharacterType을 보고
/// - 어떤 시너지가 발동되는지 계산
/// - 발동된 시너지의 Effect를 아군(또는 시스템)에 적용
public static class SynergyManager
{
    public struct ActiveSynergy
    {
        public SynergyData data;
    }

    /// 현재 슬롯 상태(DraggableSlot 배열)를 보고 발동된 시너지 목록 계산
    public static List<ActiveSynergy> Evaluate(DraggableSlot[] slots)
    {
        var result = new List<ActiveSynergy>();
        if (slots == null || slots.Length == 0)
            return result;

        // 1) 현재 파티의 CharacterType 카운트
        var typeCounts = BuildTypeCounts(slots);

        // 2) 모든 시너지 SO 가져오기
        var allSynergyMap = DataTableManager.SynergyTable.GetAll(); // <int, SynergyData>:contentReference[oaicite:0]{index=0}
        foreach (var kvp in allSynergyMap)
        {
            var data = kvp.Value;
            if (data == null)
                continue;

            if (IsSatisfied(data, typeCounts))
            {
                result.Add(new ActiveSynergy { data = data });
            }
        }

        return result;
    }

    /// 발동된 시너지들을 실제 캐릭터/시스템에 적용
    public static void ApplySynergies(DraggableSlot[] slots, List<GameObject> allies)
    {
        if (slots == null || allies == null || allies.Count == 0)
            return;

        var actives = Evaluate(slots);
        if (actives == null || actives.Count == 0)
            return;

        foreach (var active in actives)
        {
            var data = active.data;
            if (data == null)
                continue;

            var target = (SynergyTarget)data.skill_target;

            ApplyOneSynergy(data, target, allies);
        }
    }

    // ----------------- 내부 Helper들 -----------------

    private static Dictionary<CharacterType, int> BuildTypeCounts(DraggableSlot[] slots)
    {
        var result = new Dictionary<CharacterType, int>();

        foreach (var slot in slots)
        {
            if (slot == null || slot.characterData == null)
                continue;

            var cd = slot.characterData;
            var type = (CharacterType)cd.char_type;  // CharacterData에 char_type 있다고 가정

            if (type == CharacterType.None)
                continue;

            if (!result.TryGetValue(type, out int count))
                count = 0;

            result[type] = count + 1;
        }

        return result;
    }

    /// 한 시너지가 현재 파티 타입 카운트로 충족되는지 체크
    /// (synergy_Unit1~3을 CharacterType 멀티셋으로 보고 비교)
    private static bool IsSatisfied(SynergyData data, Dictionary<CharacterType, int> have)
    {
        // SynergyTable.GetRequireUnit은 synegy_Unit1~3을 CharacterType 리스트로 돌려줌:contentReference[oaicite:1]{index=1}
        var reqList = DataTableManager.SynergyTable.GetRequireUnit(data.synergy_id);
        if (reqList == null || reqList.Count == 0)
            return false;

        // 멀티셋(타입별 요구 개수) 구성
        var req = new Dictionary<CharacterType, int>();
        foreach (var t in reqList)
        {
            if (!req.TryGetValue(t, out int c))
                c = 0;
            req[t] = c + 1;
        }

        // have가 req를 모두 만족하는지 확인
        foreach (var kvp in req)
        {
            if (!have.TryGetValue(kvp.Key, out int count))
                return false;

            if (count < kvp.Value)
                return false;
        }

        return true;
    }

    /// SynergyData 한 줄이 가진 effect_type1~3, effect_val1~3을 실제로 적용
    private static void ApplyOneSynergy(
        SynergyData data,
        SynergyTarget target,
        List<GameObject> allies)
    {
        // effect1~3 순회
        ApplyOneEffect(data.effect_type1, data.effect_val1, target, allies);
        ApplyOneEffect(data.effect_type2, data.effect_val2, target, allies);
        ApplyOneEffect(data.effect_type3, data.effect_val3, target, allies);
    }

    private static void ApplyOneEffect(
        int effectId,
        float value,
        SynergyTarget target,
        List<GameObject> allies)
    {
        if (effectId == 0)
            return;

        // 🔹 일단 예시는 전부 "아군 전체"에 붙이는 쪽으로 구현
        //  - 501~507: 아군 공격력/공속/사거리/체력 등
        switch (target)
        {
            case SynergyTarget.AlliesAll:
                foreach (var ally in allies)
                {
                    if (ally == null) continue;
                    EffectRegistry.Apply(ally, effectId, value, 99999f); // 패시브처럼 사실상 영구 버프로 처리:contentReference[oaicite:2]{index=2}
                }
                break;
            case SynergyTarget.GlobalOrEtc:
                //  - 510~514: 적 이동/공격 속도 감소 → 효과 3010이 알아서 적들에게 퍼지게 구현할 수도 있음
                break;
            case SynergyTarget.Stage:
                //  - 508~509: 함성 게이지 / 드랍량 배수 → 나중에 별도 시스템이 있으면 case 분기해서 처리
                EffectRegistry.Apply(StageManager.Instance.gameObject, effectId, value, 99999f);
                break;
            case SynergyTarget.None:
            default:
                // 필요하면 여기서 무시 or 로그
                break;
        }
    }

    //시너지 드래그 드롭시 효과 적용 확인
}
