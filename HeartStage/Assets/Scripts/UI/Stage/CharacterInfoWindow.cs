using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoWindow : GenericWindow
{
    public static CharacterInfoWindow Instance;

    public Image characterImage;
    public TextMeshProUGUI characterName;
    public Image attributeIcon;
    public Image activeSkillIcon;
    public List<Image> rankImages;
    public TextMeshProUGUI passiveDescText;
    public TextMeshProUGUI activeDescText;
    // 스탯 Value
    public TextMeshProUGUI vocal;
    public TextMeshProUGUI lab;
    public TextMeshProUGUI dance;
    public TextMeshProUGUI visual;
    public TextMeshProUGUI sexy;
    public TextMeshProUGUI cuty;
    public TextMeshProUGUI charisma;

    [Header("Passive Range Grid")]
    public List<Image> cells;
    public Color originCellColor;
    public Color skillRangeColor;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public void Init(CharacterData data)
    {
        // 포토카드 교체 반영
        LoadPhotocardAsync(data).Forget();
        characterName.text = data.char_name;
        // 캐릭터 속성 아이콘 변경
        CharacterAttributeIcon.ChangeIcon(attributeIcon, data.char_type);
        // 랭크 세팅
        for (int i = 0; i < rankImages.Count; i++)
        {
            if (i < data.char_rank - 1)
                rankImages[i].enabled = true;
            else
                rankImages[i].enabled = false;
        }
        // 패시브 스킬 정보 세팅하기
        var skillIds = data.GetSkillIds();
        var passiveSkills = skillIds.Where(x => x > 32000).ToList();
        // 1. 원래 색으로 초기화
        foreach (var cell in cells)
        {
            cell.color = originCellColor;
        }

        if (passiveSkills.Count > 0)
        {
            var skillData = DataTableManager.SkillTable.Get(passiveSkills[0]);
            passiveDescText.text = skillData.GetFormattedInfo();
            // 2. 실제 스킬 범위 색칠
            var skillRangeIndexes = PassivePatternUtil.GetPatternTiles(7, skillData.passive_type, 15);
            foreach (var index in skillRangeIndexes)
            {
                cells[index].color = skillRangeColor;
            }
        }
        else
        {
            passiveDescText.text = string.Empty;
        }
        // 스탯 표시
        vocal.text = $"{StatPower.GetVocalPower(data.atk_dmg)}";
        lab.text = $"{StatPower.GetLabPower(data.atk_speed)}";
        dance.text = $"{StatPower.GetDancePower(data.char_hp)}";
        visual.text = $"{StatPower.GetVisualPower(data.crt_chance)}";
        sexy.text = $"{StatPower.GetSexyPower(data.crt_dmg)}";
        cuty.text = $"{StatPower.GetCutyPower(data.atk_addcount)}";
        charisma.text = $"{StatPower.GetCharismaPower(data.atk_range)}";
        // 액티브 스킬 아이콘 표시하기
        var activeSkills = skillIds.Where(x => x > 31000 && x < 32000).ToList();
        if (activeSkills.Count > 0)
        {
            var skillData = DataTableManager.SkillTable.Get(activeSkills[0]);
            activeDescText.text = skillData.GetFormattedInfo();
            activeSkillIcon.sprite = ResourceManager.Instance.GetSprite(skillData.icon_prefab);
            activeSkillIcon.enabled = true;
        }
        else
        {
            activeDescText.text = string.Empty;
            activeSkillIcon.enabled = false;
        }
    }

    public void OpenForTutorial(CharacterData data)
    {
        Init(data);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        // 튜토리얼 모드에서는 WindowManager와의 연동을 비활성화
        var tempWindowType = windowType;
        var tempIsOverlay = isOverlayWindow;

        // 임시로 오버레이가 아닌 것으로 설정 (Close시 WindowManager 알림 방지)
        windowType = WindowType.None;
        isOverlayWindow = false;

        // 딤 배경 수동 표시
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.ShowDimManual();
        }

        Open();

        // 원래 설정 복원
        windowType = tempWindowType;
        isOverlayWindow = tempIsOverlay;
    }

    public void CloseForTutorial()
    {
        gameObject.SetActive(false);

        // 딤 배경 해제
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.HideDimManual();
        }
    }

    private async UniTaskVoid LoadPhotocardAsync(CharacterData data)
    {
        string charCode = PhotocardHelper.ExtractCharCode(data.char_id);
        var sprite = await PhotocardHelper.LoadDisplaySprite(charCode);

        if (sprite != null)
        {
            characterImage.sprite = sprite;
        }
        else
        {
            // fallback: 기존 방식
            characterImage.sprite = ResourceManager.Instance.GetSprite(data.card_imageName);
        }
    }
}