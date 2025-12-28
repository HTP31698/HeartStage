using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배치 단계에서 예상 스탯을 보여주는 패널
/// StageSetupWindow에 연결하여 시너지 + 패시브 타일 버프 포함 계산
/// </summary>
public class PlacementPowerInfoPanel : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private StageSetupWindow stageSetupWindow;
    [SerializeField] private Button closeButton;      // X 버튼
    [SerializeField] private Button toggleModeButton; // 표시 모드 토글 버튼 (패널 자체 or 별도)

    [Header("Total")]
    public TextMeshProUGUI totalPowerName;
    public TextMeshProUGUI totalPowerAmount;

    [Header("Vocal (공격력)")]
    public TextMeshProUGUI atkName;
    public TextMeshProUGUI atkTotalValue;
    public TextMeshProUGUI atkBuffValue;

    [Header("Lab (공격속도)")]
    public TextMeshProUGUI atkSpeedName;
    public TextMeshProUGUI atkSpeedTotalValue;
    public TextMeshProUGUI atkSpeedBuffValue;

    [Header("Dance (체력)")]
    public TextMeshProUGUI hpName;
    public TextMeshProUGUI hpTotalValue;
    public TextMeshProUGUI hpBuffValue;

    [Header("Visual (치명타 확률)")]
    public TextMeshProUGUI crtChanceName;
    public TextMeshProUGUI crtChanceTotalValue;
    public TextMeshProUGUI crtChanceBuffValue;

    [Header("Sexy (치명타 피해)")]
    public TextMeshProUGUI crtDamageName;
    public TextMeshProUGUI crtDamageTotalValue;
    public TextMeshProUGUI crtDamageBuffValue;

    [Header("Cuty (추가 공격 확률)")]
    public TextMeshProUGUI addAtkChanceName;
    public TextMeshProUGUI addAtkChanceTotalValue;
    public TextMeshProUGUI addAtkChanceBuffValue;

    [Header("Charisma (사거리)")]
    public TextMeshProUGUI rangeName;
    public TextMeshProUGUI rangeTotalValue;
    public TextMeshProUGUI rangeBuffValue;

    [Header("표시 모드")]
    [SerializeField] private bool isRealStatDisplay = false;

    private void OnEnable()
    {
        // StageSetupWindow 자동 연결
        if (stageSetupWindow == null)
            stageSetupWindow = FindObjectOfType<StageSetupWindow>();

        // 슬롯 변경 이벤트 구독
        DraggableSlot.OnAnySlotChanged += UpdateUI;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (toggleModeButton != null)
            toggleModeButton.onClick.AddListener(ToggleDisplayMode);

        UpdateUI();
        ApplyNameDisplayMode(isRealStatDisplay);
    }

    private void OnDisable()
    {
        DraggableSlot.OnAnySlotChanged -= UpdateUI;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (toggleModeButton != null)
            toggleModeButton.onClick.RemoveListener(ToggleDisplayMode);
    }

    private void ToggleDisplayMode()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        isRealStatDisplay = !isRealStatDisplay;
        ApplyNameDisplayMode(isRealStatDisplay);
    }

    private void Close()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        gameObject.SetActive(false);
    }

    public void SetRealStatDisplay(bool value)
    {
        isRealStatDisplay = value;
        ApplyNameDisplayMode(value);
    }

    private void UpdateUI()
    {
        if (stageSetupWindow == null)
        {
            Debug.LogWarning("[PlacementPowerInfoPanel] StageSetupWindow not connected!");
            return;
        }

        // 기본 스탯 합계
        float baseAtkSum = 0f;
        float baseAtkSpeedSum = 0f;
        float baseHpSum = 0f;
        float baseCritChanceSum = 0f;
        float baseCritDamageSum = 0f;
        float baseExtraAtkChanceSum = 0f;
        float baseRangeSum = 0f;

        // 버프 적용 후 스탯 합계
        float finalAtkSum = 0f;
        float finalAtkSpeedSum = 0f;
        float finalHpSum = 0f;
        float finalCritChanceSum = 0f;
        float finalCritDamageSum = 0f;
        float finalExtraAtkChanceSum = 0f;
        float finalRangeSum = 0f;

        // 배치된 캐릭터들
        var placedCharacters = stageSetupWindow.GetPlacedCharacters();

        foreach (var (slotIndex, cd) in placedCharacters)
        {
            // CharacterTable에서 기본 스탯 가져오기
            var baseData = DataTableManager.CharacterTable.Get(cd.char_id);
            if (baseData == null) continue;

            // 해당 슬롯의 버프 계산 (시너지 + 패시브 타일)
            var buffs = stageSetupWindow.CalculateTotalBuffsForSlot(slotIndex);

            // 1) Atk → 보컬
            {
                float baseStat = baseData.atk_dmg;
                float buffRatio = GetBuffRatio(buffs, StatType.Attack);
                float finalStat = baseStat * (1f + buffRatio);

                baseAtkSum += StatPower.GetVocalPower(baseStat);
                finalAtkSum += StatPower.GetVocalPower(finalStat);
            }

            // 2) AtkSpeed → 랩
            {
                float baseStat = baseData.atk_speed;
                float buffRatio = GetBuffRatio(buffs, StatType.AttackSpeed);
                float finalStat = baseStat * (1f + buffRatio);

                baseAtkSpeedSum += StatPower.GetLabPower(baseStat);
                finalAtkSpeedSum += StatPower.GetLabPower(finalStat);
            }

            // 3) HP → 댄스
            {
                float baseStat = baseData.char_hp;
                float buffRatio = GetBuffRatio(buffs, StatType.MaxHp);
                float finalStat = baseStat * (1f + buffRatio);

                baseHpSum += StatPower.GetDancePower(baseStat);
                finalHpSum += StatPower.GetDancePower(finalStat);
            }

            // 4) CritChance → 비주얼
            {
                float baseStat = baseData.crt_chance;
                float buffRatio = GetBuffRatio(buffs, StatType.CritChance);
                float finalStat = baseStat * (1f + buffRatio);

                baseCritChanceSum += StatPower.GetVisualPower(baseStat);
                finalCritChanceSum += StatPower.GetVisualPower(finalStat);
            }

            // 5) CritDamage → 섹시
            {
                float baseStat = baseData.crt_dmg;
                float buffRatio = GetBuffRatio(buffs, StatType.CritDamage);
                float finalStat = baseStat * (1f + buffRatio);

                baseCritDamageSum += StatPower.GetSexyPower(baseStat);
                finalCritDamageSum += StatPower.GetSexyPower(finalStat);
            }

            // 6) ExtraAtkChance → 큐티
            {
                float baseStat = baseData.atk_addcount;
                float buffRatio = GetBuffRatio(buffs, StatType.ExtraAttackChance);
                float finalStat = baseStat * (1f + buffRatio);

                baseExtraAtkChanceSum += StatPower.GetCutyPower(baseStat);
                finalExtraAtkChanceSum += StatPower.GetCutyPower(finalStat);
            }

            // 7) Range → 카리스마
            {
                float baseStat = baseData.atk_range;
                float buffRatio = GetBuffRatio(buffs, StatType.AttackRange);
                float finalStat = baseStat * (1f + buffRatio);

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
        if (totalPowerAmount != null)
            totalPowerAmount.text = $"{Mathf.FloorToInt(finalTotal)}";
    }

    private float GetBuffRatio(System.Collections.Generic.Dictionary<StatType, float> buffs, StatType type)
    {
        if (buffs == null) return 0f;
        return buffs.TryGetValue(type, out float val) ? val : 0f;
    }

    private void UpdateStatUI(TextMeshProUGUI nameUI, TextMeshProUGUI totalUI, TextMeshProUGUI buffUI,
        float baseValue, float finalValue)
    {
        if (totalUI != null)
            totalUI.text = $"{Mathf.FloorToInt(finalValue)}";

        int diff = Mathf.FloorToInt(finalValue - baseValue);

        if (diff == 0)
        {
            if (nameUI != null) nameUI.color = Color.white;
            if (totalUI != null) totalUI.color = Color.white;
            if (buffUI != null)
            {
                buffUI.color = Color.white;
                buffUI.text = "+ 000";
            }
        }
        else if (diff > 0)
        {
            if (nameUI != null) nameUI.color = Color.green;
            if (totalUI != null) totalUI.color = Color.green;
            if (buffUI != null)
            {
                buffUI.color = Color.green;
                buffUI.text = $"+ {diff}";
            }
        }
        else
        {
            if (nameUI != null) nameUI.color = Color.red;
            if (totalUI != null) totalUI.color = Color.red;
            if (buffUI != null)
            {
                buffUI.color = Color.red;
                buffUI.text = $"- {-diff}";
            }
        }
    }

    public void ApplyNameDisplayMode(bool realStatDisplay)
    {
        if (realStatDisplay)
        {
            // 실제 스탯 표기
            if (atkName != null) atkName.text = "공격력";
            if (atkSpeedName != null) atkSpeedName.text = "공격속도";
            if (hpName != null) hpName.text = "체력";
            if (crtChanceName != null) crtChanceName.text = "치명타 확률";
            if (crtDamageName != null) crtDamageName.text = "치명타 피해";
            if (addAtkChanceName != null) addAtkChanceName.text = "추가 공격 확률";
            if (rangeName != null) rangeName.text = "사거리";

            if (totalPowerName != null) totalPowerName.text = "전체스탯";
        }
        else
        {
            // 아이돌 능력 표기 (보컬/랩/댄스 등)
            if (atkName != null) atkName.text = "보컬";
            if (atkSpeedName != null) atkSpeedName.text = "랩";
            if (hpName != null) hpName.text = "댄스";
            if (crtChanceName != null) crtChanceName.text = "비주얼";
            if (crtDamageName != null) crtDamageName.text = "섹시";
            if (addAtkChanceName != null) addAtkChanceName.text = "큐티";
            if (rangeName != null) rangeName.text = "카리스마";

            if (totalPowerName != null) totalPowerName.text = "아이돌력";
        }
    }

    /// <summary>
    /// 강제 갱신 (외부에서 호출용)
    /// </summary>
    public void Refresh()
    {
        UpdateUI();
    }
}
