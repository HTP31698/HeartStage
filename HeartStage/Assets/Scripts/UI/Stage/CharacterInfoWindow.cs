using System.Collections.Generic;
using System.Linq;
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

    private void Awake()
    {
        Instance = this;
    }

    public void Init(CharacterData data)
    {
        var texture = ResourceManager.Instance.Get<Texture2D>(data.card_imageName);
        characterImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
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
            var texture2d = ResourceManager.Instance.Get<Texture2D>(skillData.icon_prefab);
            activeSkillIcon.sprite = Sprite.Create(texture2d, new Rect(0, 0, texture2d.width, texture2d.height), new Vector2(0.5f, 0.5f));
            activeSkillIcon.enabled = true;
        }
        else
        {
            activeDescText.text = string.Empty;
            activeSkillIcon.enabled = false;
        }
    }
}