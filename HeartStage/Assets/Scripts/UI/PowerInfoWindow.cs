using TMPro;
using UnityEngine;

public class PowerInfoWindow : MonoBehaviour
{
    [Header("Total")]
    public TextMeshProUGUI totalPowerName;
    public TextMeshProUGUI totalPowerAmount;
    [Header("Vocal")]
    public TextMeshProUGUI atkName;
    public TextMeshProUGUI atkTotalValue;
    public TextMeshProUGUI atkBuffValue;
    [Header("Lab")]
    public TextMeshProUGUI atkSpeedName;
    public TextMeshProUGUI atkSpeedTotalValue;
    public TextMeshProUGUI atkSpeedBuffValue;
    [Header("Dance")]
    public TextMeshProUGUI hpName;
    public TextMeshProUGUI hpTotalValue;
    public TextMeshProUGUI hpBuffValue;
    [Header("Visual")]
    public TextMeshProUGUI crtChanceName;
    public TextMeshProUGUI crtChanceTotalValue;
    public TextMeshProUGUI crtChanceBuffValue;
    [Header("Sexy")]
    public TextMeshProUGUI crtDamageName;
    public TextMeshProUGUI crtDamageTotalValue;
    public TextMeshProUGUI crtDamageBuffValue;
    [Header("Cuty")]
    public TextMeshProUGUI addAtkChanceName;
    public TextMeshProUGUI addAtkChanceTotalValue;
    public TextMeshProUGUI addAtkChanceBuffValue;
    [Header("Charisma")]
    public TextMeshProUGUI rangeName;
    public TextMeshProUGUI rangeTotalValue;
    public TextMeshProUGUI rangeBuffValue;

    private void OnEnable()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        float baseAtkSum = 0f;
        float baseAtkSpeedSum = 0f;
        float baseHpSum = 0f;
        float baseCritChanceSum = 0f;
        float baseCritDamageSum = 0f;
        float baseExtraAtkChanceSum = 0f;
        float baseRangeSum = 0f;

        float finalAtkSum = 0f;
        float finalAtkSpeedSum = 0f;
        float finalHpSum = 0f;
        float finalCritChanceSum = 0f;
        float finalCritDamageSum = 0f;
        float finalExtraAtkChanceSum = 0f;
        float finalRangeSum = 0f;

        var characters = GameObject.FindGameObjectsWithTag(Tag.Tower);
        foreach (var character in characters)
        {
            var characterAttack = character.GetComponent<CharacterAttack>();
            var baseData = DataTableManager.CharacterTable.Get(characterAttack.id);

            // 1) Atk → 보컬
            {
                float baseStat = baseData.atk_dmg;
                float finalStat = StatCalc.GetFinalStat(character, StatType.Attack, baseStat);

                baseAtkSum += StatPower.GetVocalPower(baseStat);
                finalAtkSum += StatPower.GetVocalPower(finalStat);
            }
            // 2) AtkSpeed → 랩
            {
                float baseStat = baseData.atk_speed;
                float finalStat = StatCalc.GetFinalStat(character, StatType.AttackSpeed, baseStat);

                baseAtkSpeedSum += StatPower.GetLabPower(baseStat);
                finalAtkSpeedSum += StatPower.GetLabPower(finalStat);
            }
            // 3) HP → 댄스
            {
                float baseStat = baseData.char_hp;
                float finalStat = StatCalc.GetFinalStat(character, StatType.MaxHp, baseStat);

                baseHpSum += StatPower.GetDancePower(baseStat);
                finalHpSum += StatPower.GetDancePower(finalStat);
            }
            // 4) CritChance → 비주얼
            {
                float baseStat = baseData.crt_chance;
                float finalStat = StatCalc.GetFinalStat(character, StatType.CritChance, baseStat);

                baseCritChanceSum += StatPower.GetVisualPower(baseStat);
                finalCritChanceSum += StatPower.GetVisualPower(finalStat);
            }
            // 5) CritDamage → 섹시
            {
                float baseStat = baseData.crt_dmg;
                float finalStat = StatCalc.GetFinalStat(character, StatType.CritDamage, baseStat);

                baseCritDamageSum += StatPower.GetSexyPower(baseStat);
                finalCritDamageSum += StatPower.GetSexyPower(finalStat);
            }
            // 6) ExtraAtkChance → 큐티
            {
                float baseStat = baseData.atk_addcount;
                float finalStat = StatCalc.GetFinalStat(character, StatType.ExtraAttackChance, baseStat);

                baseExtraAtkChanceSum += StatPower.GetCutyPower(baseStat);
                finalExtraAtkChanceSum += StatPower.GetCutyPower(finalStat);
            }
            // 7) Range → 카리스마
            {
                float baseStat = baseData.atk_range;
                float finalStat = StatCalc.GetFinalStat(character, StatType.AttackRange, baseStat);

                baseRangeSum += StatPower.GetCharismaPower(baseStat);
                finalRangeSum += StatPower.GetCharismaPower(finalStat);
            }
        }

        // 개별 스탯 UI 업데이트
        UpdateStatUI(atkName, atkTotalValue, atkBuffValue, baseAtkSum, finalAtkSum);
        UpdateStatUI(atkSpeedName, atkSpeedTotalValue, atkSpeedBuffValue, baseAtkSpeedSum, finalAtkSpeedSum);
        UpdateStatUI(hpName, hpTotalValue, hpBuffValue, baseHpSum, finalHpSum);
        UpdateStatUI(crtChanceName, crtChanceTotalValue, crtChanceBuffValue, baseCritChanceSum, finalCritChanceSum);
        UpdateStatUI(crtDamageName, crtDamageTotalValue, crtDamageBuffValue, baseCritDamageSum, finalCritDamageSum);
        UpdateStatUI(addAtkChanceName, addAtkChanceTotalValue, addAtkChanceBuffValue, baseExtraAtkChanceSum, finalExtraAtkChanceSum);
        UpdateStatUI(rangeName, rangeTotalValue, rangeBuffValue, baseRangeSum, finalRangeSum);

        // TOTAL 계산
        float finalTotal = finalAtkSum + finalAtkSpeedSum + finalHpSum + finalCritChanceSum + finalCritDamageSum + finalExtraAtkChanceSum + finalRangeSum;
        totalPowerAmount.text = $"{Mathf.FloorToInt(finalTotal)}";
    }

    private void UpdateStatUI(TextMeshProUGUI nameUI, TextMeshProUGUI totalUI, TextMeshProUGUI buffUI,
        float baseValue, float finalValue)
    {
        totalUI.text = $"{Mathf.FloorToInt(finalValue)}";
        int diff = Mathf.FloorToInt(finalValue - baseValue);

        if (diff == 0)
        {
            nameUI.color = totalUI.color = buffUI.color = Color.white;
            buffUI.text = "+ 000";
        }
        else if (diff > 0)
        {
            nameUI.color = totalUI.color = buffUI.color = Color.green;
            buffUI.text = $"+ {diff}";
        }
        else
        {
            nameUI.color = totalUI.color = buffUI.color = Color.red;
            buffUI.text = $"- {-diff}";
        }
    }

    public void ApplyNameDisplayMode(bool isRealStatDisplay)
    {
        if (isRealStatDisplay)
        {
            // 실제 스탯 표기
            atkName.text = "공격력";
            atkSpeedName.text = "공격속도";
            hpName.text = "체력";
            crtChanceName.text = "치명타 확률";
            crtDamageName.text = "치명타 피해";
            addAtkChanceName.text = "추가 공격 확률";
            rangeName.text = "사거리";

            totalPowerName.text = "전체스탯";
        }
        else
        {
            // 아이돌 능력 표기 (보컬/랩/댄스 등)
            atkName.text = "보컬";
            atkSpeedName.text = "랩";
            hpName.text = "댄스";
            crtChanceName.text = "비주얼";
            crtDamageName.text = "섹시";
            addAtkChanceName.text = "큐티";
            rangeName.text = "카리스마";

            totalPowerName.text = "아이돌력";
        }
    }
}
