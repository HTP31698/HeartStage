using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterDetailPanel : MonoBehaviour
{
    [Header("캐릭터 이름")]
    [SerializeField] private TextMeshProUGUI nameText;
    [Header("캐릭터 등급")]
    [SerializeField] private TextMeshProUGUI rankText;
    [Header("캐릭터 레벨")]
    [SerializeField] private TextMeshProUGUI levelText;
    [Header("캐릭터 속성")]
    [SerializeField] private TextMeshProUGUI typeText;

    [Header("캐릭터 공격력")]
    [SerializeField] private TextMeshProUGUI attackText;
    [Header("캐릭터 공격속도")]
    [SerializeField] private TextMeshProUGUI attackSpeedText;
    [Header("캐릭터 공격범위")]
    [SerializeField] private TextMeshProUGUI attackRangeText;
    [Header("캐릭터 추가공격수")]
    [SerializeField] private TextMeshProUGUI additionalAttackText;
    [Header("캐릭터 체력")]
    [SerializeField] private TextMeshProUGUI healthText;
    [Header("캐릭터 치명타 확률")]
    [SerializeField] private TextMeshProUGUI critRateText;
    [Header("캐릭터 치명타 데미지")]
    [SerializeField] private TextMeshProUGUI critDmgText;
    [Header("캐릭터 공격 투사체수")]
    [SerializeField] private TextMeshProUGUI projectileCountText;

    [Header("캐릭터 설명")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [Header("캐릭터 보유 스킬")]
    [SerializeField] private TextMeshProUGUI skillText;

    [Header("캐릭터 이미지")]
    [SerializeField] private Image characterImage;
    [Header("스킬 이미지")]
    [SerializeField] private Image[] skillImages;

    [Header("레벨업 필요 재화")]
    [SerializeField] private TextMeshProUGUI levelUpCostText;
    [Header("랭크업 필요 재화")]
    [SerializeField] private TextMeshProUGUI rankUpCostText;

    [Header("레벨업 버튼")]
    [SerializeField] private Button levelUpButton;
    [Header("랭크업 버튼")]
    [SerializeField] private Button rankUpButton;


    // 런타임에 만든 스프라이트 누수 방지용
    private Sprite _runtimeSprite;
    // 런타임에 만든 스킬 스프라이트 누수 방지용
    private Sprite[] _runtimeSkillSprites;
    // 현재 표시 중인 캐릭터 ID
    private int _currentCharacterId;

    [Header("종료 버튼")]
    [SerializeField] Button ExitButton;

    public void SetCharacter(CharacterCSVData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("[CharacterDetailPanel] characterData null");
            Clear();
            return;
        }
        _currentCharacterId = characterData.char_id;

        nameText.text = characterData.char_name;
        rankText.text = $"등급: {characterData.char_rank}";
        levelText.text = $"레벨: {characterData.char_lv}";
        typeText.text = $"속성: {(CharacterType)characterData.char_type}";

        attackText.text = $"보컬: {StatPower.GetVocalPower(characterData.atk_dmg)}";
        attackSpeedText.text = $"랩: {StatPower.GetLabPower(characterData.atk_speed)}";
        attackRangeText.text = $"카리스마: {StatPower.GetCharismaPower(characterData.atk_range)}";
        additionalAttackText.text = $"큐티: {StatPower.GetCutyPower(characterData.atk_addcount)}";
        healthText.text = $"댄스: {StatPower.GetDancePower(characterData.char_hp)}";
        critRateText.text = $"비주얼: {StatPower.GetVisualPower(characterData.crt_chance)}";
        critDmgText.text = $"섹시: {StatPower.GetSexyPower(characterData.crt_dmg)}";

        projectileCountText.text = $"투사체수: {characterData.bullet_count}";
        skillText.text = $"{SkillName(characterData.char_id)}";

        descriptionText.text = $"캐릭터 정보: {characterData.Info}";

        // 여기서 이미지 직접 로드/적용
        ApplyCharacterImage(characterData.card_imageName);

        // 스킬 이미지가 필요하면 여기서 ApplySkillImages(characterData.char_id); 같은 식으로 확장
        ApplySkillImages(characterData.char_id);

        // 레벨업 구현 Onclick 리스너 등은 여기서 추가 가능
        ApplyLevelUpText(characterData.char_id);

        // 랭크업 구현 Onclick 리스너 등은 여기서 추가 가능
        ApplyRankUpText(characterData.char_id);

        // 필요 재화 정보도 여기서 설정 가능

    }

    private void ApplyCharacterImage(string imageKey)
    {
        if (characterImage == null)
            return;

        // 이전 스프라이트 정리
        if (_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }

        if (string.IsNullOrEmpty(imageKey))
        {
            characterImage.sprite = null;
            return;
        }

        var tex = ResourceManager.Instance.Get<Texture2D>(imageKey);
        if (tex == null)
        {
            Debug.LogWarning($"[CharacterDetailPanel] Texture 로드 실패: {imageKey}");
            characterImage.sprite = null;
            return;
        }

        _runtimeSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        characterImage.sprite = _runtimeSprite;
    }

    private void ApplySkillImages(int charId)
    {
        // 슬롯이 없으면 그냥 종료
        if (skillImages == null || skillImages.Length == 0)
            return;

        // 1) 이전에 만든 스킬 스프라이트 정리
        if (_runtimeSkillSprites != null)
        {
            for (int i = 0; i < _runtimeSkillSprites.Length; i++)
            {
                if (_runtimeSkillSprites[i] != null)
                    Destroy(_runtimeSkillSprites[i]);
            }
        }
        _runtimeSkillSprites = new Sprite[skillImages.Length];

        // 2) 캐릭터 → 스킬 ID 리스트 얻기
        var skillIds = DataTableManager.CharacterTable.GetSkillIds(charId);
        if (skillIds == null || skillIds.Count == 0)
        {
            // 스킬이 없으면 모든 슬롯 비우고 끄기
            for (int i = 0; i < skillImages.Length; i++)
            {
                skillImages[i].sprite = null;
                skillImages[i].gameObject.SetActive(false);
            }
            return;
        }

        // 3) 스킬 슬롯 채우기
        int count = Mathf.Min(skillImages.Length, skillIds.Count);

        for (int i = 0; i < count; i++)
        {
            int skillId = skillIds[i];
            var skillData = DataTableManager.SkillTable.Get(skillId);
            if (skillData == null)
            {
                skillImages[i].sprite = null;
                skillImages[i].gameObject.SetActive(false);
                continue;
            }

            // 🔹 네 스킬 아이콘 키 필드명
            string iconKey = skillData.icon_prefab; // 여기 필드명 맞게 유지

            if (string.IsNullOrEmpty(iconKey))
            {
                skillImages[i].sprite = null;
                skillImages[i].gameObject.SetActive(false);
                continue;
            }

            var tex = ResourceManager.Instance.Get<Texture2D>(iconKey);
            if (tex == null)
            {
                Debug.LogWarning($"[CharacterDetailPanel] Skill Texture 로드 실패: {iconKey}");
                skillImages[i].sprite = null;
                skillImages[i].gameObject.SetActive(false);
                continue;
            }

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            _runtimeSkillSprites[i] = sprite;
            skillImages[i].sprite = sprite;

            // ✅ 랭크업으로 새 스킬 생길 때 다시 켜주기
            skillImages[i].gameObject.SetActive(true);
        }

        // 4) 남는 슬롯은 비우고 끄기
        for (int i = count; i < skillImages.Length; i++)
        {
            skillImages[i].sprite = null;
            skillImages[i].gameObject.SetActive(false);
        }
    }

    public void ApplyLevelUpText(int charId)
    {
        if (levelUpButton == null || levelUpCostText == null)
            return;

        if (!CharacterHelper.HasCharacter(charId))
        {
            levelUpCostText.text = "-미보유 캐릭터-";
            levelUpButton.interactable = false;
            levelUpButton.onClick.RemoveAllListeners();
            return;
        }

        var lvdata = DataTableManager.LevelUpTable.Get(charId);
        if (lvdata == null)
        {
            levelUpCostText.text = "-최대 레벨-";
            levelUpButton.interactable = false;
            levelUpButton.onClick.RemoveAllListeners();
            return;
        }

        int currentPoint = ItemInvenHelper.GetAmount(lvdata.Lvup_ingrd_Itm);
        levelUpCostText.text = $"트레이닝 포인트: {currentPoint} / {lvdata.Lvup_ingrd_Itm_count}";

        levelUpButton.onClick.RemoveAllListeners();

        if (currentPoint >= lvdata.Lvup_ingrd_Itm_count)
        {
            levelUpButton.interactable = true;
            levelUpButton.onClick.AddListener(() => OnLevelUpButtonClick(charId));
        }
        else
        {
            levelUpButton.interactable = false;
        }
    }

    public void ApplyRankUpText(int charId)
    {
        if (rankUpButton == null || rankUpCostText == null)
            return;

        if (!CharacterHelper.HasCharacter(charId))
        {
            rankUpCostText.text = "-미보유 캐릭터-";
            rankUpButton.interactable = false;
            rankUpButton.onClick.RemoveAllListeners();
            return;
        }

        var rankdata = DataTableManager.RankUpTable.Get(charId);
        if (rankdata == null)
        {
            rankUpCostText.text = "-최대 랭크-";
            rankUpButton.interactable = false;
            rankUpButton.onClick.RemoveAllListeners();
            return;
        }

        int currentPoint = ItemInvenHelper.GetAmount(rankdata.Upgrade_ingrd_Itm1);
        rankUpCostText.text = $"{rankdata.Upgrade_ingrd_Itm1} 조각: {currentPoint} / {rankdata.Ingrd_Itm1_amount}";

        rankUpButton.onClick.RemoveAllListeners();

        if (currentPoint >= rankdata.Ingrd_Itm1_amount)
        {
            rankUpButton.interactable = true;
            rankUpButton.onClick.AddListener(() => OnRankUpButtonClick(charId));
        }
        else
        {
            rankUpButton.interactable = false;
        }
    }

    public void OnLevelUpButtonClick(int charId)
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        var lvdata = DataTableManager.LevelUpTable.Get(charId);
        if (lvdata == null)
        {
            if (levelUpCostText != null)
                levelUpCostText.text = "-최대 레벨-";

            if (levelUpButton != null)
            {
                levelUpButton.interactable = false;
                levelUpButton.onClick.RemoveAllListeners();
            }
            return;
        }

        if (!ItemInvenHelper.TryConsumeItem(lvdata.Lvup_ingrd_Itm, lvdata.Lvup_ingrd_Itm_count))
        {
            ApplyLevelUpText(charId);
            return;
        }

        int startId = _currentCharacterId;
        int finalId = lvdata.Lvup_char;

        CharacterHelper.CommitUpgradeResult(startId, finalId);

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Enhance);

        Debug.Log($"레벨업 완료: {startId} -> {finalId}");

        var nextLevelData = DataTableManager.CharacterTable.Get(finalId);
        if (nextLevelData != null)
        {
            SetCharacter(nextLevelData);
        }
        ApplyLevelUpText(finalId);
        ApplyRankUpText(finalId);
    }

    public void OnRankUpButtonClick(int charId)
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        var rankdata = DataTableManager.RankUpTable.Get(charId);
        if (rankdata == null) return;

        if (ItemInvenHelper.TryConsumeItem(rankdata.Upgrade_ingrd_Itm1, rankdata.Ingrd_Itm1_amount))
        {
            int startId = _currentCharacterId;
            int finalId = rankdata.Upgrade_char;

            CharacterHelper.CommitUpgradeResult(startId, finalId);

            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Enhance);

            Debug.Log($"랭크업 완료: {startId} -> {finalId}");

            var nextRankData = DataTableManager.CharacterTable.Get(finalId);
            rankUpButton.onClick.RemoveAllListeners();
            SetCharacter(nextRankData);

            // 새 랭크 기준으로 텍스트/버튼 다시 계산
            ApplyRankUpText(nextRankData.char_id);
            ApplyLevelUpText(nextRankData.char_id); // 레벨업도 같이 갱신해두면 편함
        }
    }


    public void Clear()
    {
        nameText.text = "";
        rankText.text = "등급: ";
        levelText.text = "레벨: ";
        typeText.text = "속성: ";
        attackText.text = "공격력: ";
        attackSpeedText.text = "공격속도: ";
        attackRangeText.text = "공격범위: ";
        additionalAttackText.text = "추가공격수: ";
        projectileCountText.text = "투사체수: ";
        healthText.text = "체력: ";
        critRateText.text = "치명타 확률: ";
        critDmgText.text = "치명타 데미지: ";
        skillText.text = "보유 스킬: ";
        descriptionText.text = "캐릭터 정보: ";
        characterImage.sprite = null;

        if (_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }
        if( _runtimeSkillSprites != null)
        {
            for (int i = 0; i < _runtimeSkillSprites.Length; i++)
            {
                if (_runtimeSkillSprites[i] != null)
                    Destroy(_runtimeSkillSprites[i]);
            }
            _runtimeSkillSprites = null;
        }
        levelUpCostText.text = "트레이닝 포인트: ";
        rankUpCostText.text = $"Name 조각: ";

        levelUpButton.interactable = false;
        rankUpButton.interactable = false;
    }

    public void ClosePanel()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        gameObject.SetActive(false);
    }
    public void OpenPanel()
    {
        transform.SetAsLastSibling(); // 캔버스 최상단으로
        gameObject.SetActive(true);
    }

    public string SkillName(int characterid)
    {
        var skillids = DataTableManager.CharacterTable.GetSkillIds(characterid);
        if (skillids == null) return "";

        var skillnames = new List<string>();
        foreach (var skillid in skillids)
        {
            var skillData = DataTableManager.SkillTable.Get(skillid);
            if (skillData != null)
                skillnames.Add(skillData.skill_name);
        }

        return $"보유 중인 스킬: { string.Join(", ", skillnames)}";
    }

    private void OnEnable()
    {
        if (ExitButton != null)
        {
            ExitButton.onClick.RemoveAllListeners();
            ExitButton.onClick.AddListener(ClosePanel);
        }
    }

    private void OnDestroy()
    {
        if (_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }
    }
}
