using Cysharp.Threading.Tasks;
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

    [Header("캐릭터 공격력(Vocal)")]
    [SerializeField] private TextMeshProUGUI attackText;
    [Header("캐릭터 공격속도")]
    [SerializeField] private TextMeshProUGUI attackSpeedText;
        [Header("캐릭터 체력")]
    [SerializeField] private TextMeshProUGUI healthText;
    [Header("캐릭터 공격범위")]
    [SerializeField] private TextMeshProUGUI attackRangeText;
    [Header("캐릭터 추가공격수")]
    [SerializeField] private TextMeshProUGUI additionalAttackText;

    [Header("캐릭터 치명타 확률")]
    [SerializeField] private TextMeshProUGUI critRateText;
    [Header("캐릭터 치명타 데미지")]
    [SerializeField] private TextMeshProUGUI critDmgText;

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

    [Header("의상 슬롯")]
    [SerializeField] private Button topCostumeSlot;      // 상의 슬롯 버튼
    [SerializeField] private Button pantsCostumeSlot;    // 하의 슬롯 버튼
    [SerializeField] private Button shoesCostumeSlot;    // 신발 슬롯 버튼
    [SerializeField] private Image topCostumeImage;      // 상의 썸네일
    [SerializeField] private Image pantsCostumeImage;    // 하의 썸네일
    [SerializeField] private Image shoesCostumeImage;    // 신발 썸네일

    [Header("의상 선택 팝업")]
    [SerializeField] private CostumeSelectPopup costumeSelectPopup;

    // 런타임에 만든 스프라이트 누수 방지용
    private Sprite _runtimeSprite;
    // 런타임에 만든 스킬 스프라이트 누수 방지용
    private Sprite[] _runtimeSkillSprites;
    // 현재 표시 중인 캐릭터 ID
    private int _currentCharacterId;

    [Header("종료 버튼")]
    [SerializeField] Button ExitButton;

    [Header("스탯 모드 전환 버튼")]
    [SerializeField] private Button statModeToggleButton;

    // 스탯 표시 모드 (false: 아이돌 모드, true: 전투 모드)
    private bool _isBattleMode = false;
    private CharacterCSVData _currentCharacterData;

    private void Awake()
    {
        if (statModeToggleButton != null)
        {
            statModeToggleButton.onClick.RemoveAllListeners();
            statModeToggleButton.onClick.AddListener(OnStatModeToggle);
        }

        // 의상 슬롯 버튼 리스너
        if (topCostumeSlot != null)
            topCostumeSlot.onClick.AddListener(() => OpenCostumePopup(CostumeType.Top));
        if (pantsCostumeSlot != null)
            pantsCostumeSlot.onClick.AddListener(() => OpenCostumePopup(CostumeType.Pants));
        if (shoesCostumeSlot != null)
            shoesCostumeSlot.onClick.AddListener(() => OpenCostumePopup(CostumeType.Shoes));

        // 팝업 콜백 등록
        if (costumeSelectPopup != null)
            costumeSelectPopup.OnCostumeChanged += RefreshCostumeSlots;
    }

    private void OnStatModeToggle()
    {
        _isBattleMode = !_isBattleMode;
        UpdateStatTexts();
    }

    private void UpdateStatTexts()
    {
        if (_currentCharacterData == null) return;

        if (_isBattleMode)
        {
            // 전투 모드 (실제 스탯)
            attackText.text = $"정화 강도 {_currentCharacterData.atk_dmg}";
            attackSpeedText.text = $"정화 속도 {_currentCharacterData.atk_speed}";
            healthText.text = $"체력 {_currentCharacterData.char_hp}";
            critRateText.text = $"강한정화 확률 {_currentCharacterData.crt_chance}%";
            // crt_dmg는 1.47 같은 배수로 저장됨 → 100 곱해서 147%로 표시
            int critDmgPercent = Mathf.RoundToInt(_currentCharacterData.crt_dmg * 100f);
            critDmgText.text = $"강한정화 강도 {critDmgPercent}%";
            additionalAttackText.text = $"추가 정화 확률 {_currentCharacterData.atk_addcount}%";
            attackRangeText.text = $"정화 도달 거리 {_currentCharacterData.atk_range}";
        }
        else
        {
            // 아이돌 모드 (변환된 스탯)
            attackText.text = $"보컬 {StatPower.GetVocalPower(_currentCharacterData.atk_dmg)}";
            attackSpeedText.text = $"랩 {StatPower.GetLabPower(_currentCharacterData.atk_speed)}";
            healthText.text = $"댄스 {StatPower.GetDancePower(_currentCharacterData.char_hp)}";
            critRateText.text = $"비주얼 {StatPower.GetVisualPower(_currentCharacterData.crt_chance)}%";
            critDmgText.text = $"섹시 {StatPower.GetSexyPower(_currentCharacterData.crt_dmg)}%";
            additionalAttackText.text = $"큐티 {StatPower.GetCutyPower(_currentCharacterData.atk_addcount)}%";
            attackRangeText.text = $"카리스마 {StatPower.GetCharismaPower(_currentCharacterData.atk_range)}";
        }
    }

    public void SetCharacter(CharacterCSVData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("[CharacterDetailPanel] characterData null");
            Clear();
            return;
        }
        _currentCharacterId = characterData.char_id;
        _currentCharacterData = characterData;
        _isBattleMode = false; // 새 캐릭터 선택 시 아이돌 모드로 리셋

        // 통합 텍스트: "하나 Lv.10 보컬"
        string typeName = GetAttributeName(characterData.char_type);
        nameText.text = $"{characterData.char_name} Lv.{characterData.char_lv} {typeName}";
        rankText.text = $"등급 {characterData.char_rank}";

        UpdateStatTexts();





        skillText.text = $"{SkillName(characterData.char_id)}";

        descriptionText.text = $"캐릭터 정보 {characterData.Info}";

        // 여기서 이미지 직접 로드/적용
        ApplyCharacterImage(characterData.card_imageName);

        // 스킬 이미지가 필요하면 여기서 ApplySkillImages(characterData.char_id); 같은 식으로 확장
        ApplySkillImages(characterData.char_id);

        // 레벨업 구현 Onclick 리스너 등은 여기서 추가 가능
        ApplyLevelUpText(characterData.char_id);

        // 랭크업 구현 Onclick 리스너 등은 여기서 추가 가능
        ApplyRankUpText(characterData.char_id);

        // 의상 슬롯 새로고침
        RefreshCostumeSlots();
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
        levelUpCostText.text = $"트레이닝 포인트 {currentPoint} / {lvdata.Lvup_ingrd_Itm_count}";

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
        rankUpCostText.text = $"{rankdata.Upgrade_ingrd_Itm1} 조각 {currentPoint} / {rankdata.Ingrd_Itm1_amount}";

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

        Debug.Log($"레벨업 완료 {startId} -> {finalId}");

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

            Debug.Log($"랭크업 완료 {startId} -> {finalId}");

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
        rankText.text = "등급 ";
        // levelText, typeText는 nameText에 통합됨 (필드 유지만)
        if (levelText != null) levelText.text = "";
        if (typeText != null) typeText.text = "";
        attackText.text = "공격력 ";
        attackSpeedText.text = "공격속도 ";
        attackRangeText.text = "공격범위 ";
        additionalAttackText.text = "추가공격수 ";
        healthText.text = "체력 ";
        critRateText.text = "치명타 확률 ";
        critDmgText.text = "치명타 데미지 ";
        skillText.text = "보유 스킬 ";
        descriptionText.text = "캐릭터 정보 ";
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
        levelUpCostText.text = "트레이닝 포인트 ";
        rankUpCostText.text = $"Name 조각 ";

        levelUpButton.interactable = false;
        rankUpButton.interactable = false;
    }

    public void ClosePanel()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);
        gameObject.SetActive(false);
    }
    public void OpenPanel() => gameObject.SetActive(true);

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

        return $"보유 중인 스킬 { string.Join(", ", skillnames)}";
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

        // 팝업 콜백 해제
        if (costumeSelectPopup != null)
            costumeSelectPopup.OnCostumeChanged -= RefreshCostumeSlots;
    }

    #region 유틸리티

    /// <summary>
    /// CharacterAttribute를 한글 이름으로 변환
    /// </summary>
    private string GetAttributeName(int charType)
    {
        return charType switch
        {
            1 => "보컬",
            2 => "랩",
            3 => "카리스마",
            4 => "큐티",
            5 => "댄스",
            6 => "비주얼",
            7 => "섹시",
            _ => ""
        };
    }

    #endregion

    #region 의상 시스템

    /// <summary>
    /// 의상 선택 팝업 열기
    /// </summary>
    private void OpenCostumePopup(CostumeType type)
    {
        if (_currentCharacterData == null)
            return;

        // 미보유 캐릭터는 의상 변경 불가
        if (!CharacterHelper.HasCharacter(_currentCharacterId))
        {
            ToastUI.Show("보유한 캐릭터만 의상을 변경할 수 있습니다.");
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (costumeSelectPopup != null)
        {
            costumeSelectPopup.Open(_currentCharacterData.char_name, type);
        }
    }

    /// <summary>
    /// 의상 슬롯 썸네일 새로고침
    /// </summary>
    private void RefreshCostumeSlots()
    {
        if (_currentCharacterData == null)
            return;

        RefreshCostumeSlotsAsync().Forget();
    }

    private async UniTaskVoid RefreshCostumeSlotsAsync()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || _currentCharacterData == null)
            return;

        string charName = _currentCharacterData.char_name;
        if (!saveData.equippedCostumeByChar.TryGetValue(charName, out var costume))
        {
            // 장착 의상 없음 - 슬롯 비우기
            ClearCostumeSlot(topCostumeImage);
            ClearCostumeSlot(pantsCostumeImage);
            ClearCostumeSlot(shoesCostumeImage);
            return;
        }

        // 각 슬롯 업데이트
        await UpdateCostumeSlot(topCostumeImage, CostumeType.Top, costume.topItemId);
        await UpdateCostumeSlot(pantsCostumeImage, CostumeType.Pants, costume.pantsItemId);
        await UpdateCostumeSlot(shoesCostumeImage, CostumeType.Shoes, costume.shoesItemId);
    }

    private async UniTask UpdateCostumeSlot(Image slotImage, CostumeType type, int itemId)
    {
        if (slotImage == null)
            return;

        if (itemId <= 0)
        {
            ClearCostumeSlot(slotImage);
            return;
        }

        // ItemTable에서 아이콘 가져오기 시도
        var itemData = DataTableManager.ItemTable?.Get(itemId);
        if (itemData != null && !string.IsNullOrEmpty(itemData.prefab))
        {
            var sprite = ResourceManager.Instance.GetSprite(itemData.prefab);
            if (sprite != null)
            {
                slotImage.sprite = sprite;
                slotImage.color = Color.white;
                return;
            }
        }

        // 없으면 첫 번째 의상 스프라이트 사용
        int spriteId = CostumeItemID.GetSpriteId(itemId);
        if (spriteId > 0)
        {
            string address = CostumeHelper.GetSpriteAddress(type, spriteId, 0);
            var sprite = await CostumeHelper.LoadSprite(address);
            if (sprite != null)
            {
                slotImage.sprite = sprite;
                slotImage.color = Color.white;
                return;
            }
        }

        ClearCostumeSlot(slotImage);
    }

    private void ClearCostumeSlot(Image slotImage)
    {
        if (slotImage == null)
            return;

        slotImage.sprite = null;
        slotImage.color = new Color(1f, 1f, 1f, 0.3f); // 빈 슬롯 표시
    }

    #endregion
}
