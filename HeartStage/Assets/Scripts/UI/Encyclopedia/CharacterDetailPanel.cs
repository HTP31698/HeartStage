using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterDetailPanel : MonoBehaviour
{
    [Header("캐릭터 이름 (이름 + 레벨 + 속성 통합)")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("캐릭터 등급 (마이크 4개)")]
    [SerializeField] private Image[] rankMicImages;  // 4개의 마이크 이미지
    [SerializeField] private Sprite rankMicOnSprite;   // 불 들어온 마이크 스프라이트
    [SerializeField] private Sprite rankMicOffSprite;  // 꺼진 마이크 스프라이트

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

    [Header("캐릭터 렌더링")]
    [SerializeField] private RawImage characterRawImage;  // 캐릭터 표시용 RawImage
    [SerializeField] private Camera characterCamera;  // 캐릭터 전용 카메라
    [SerializeField] private float spawnDistance = 10f;  // 카메라 앞 거리
    [SerializeField] private float characterPrefabScale = 1f;
    [SerializeField] private RuntimeAnimatorController lobbyAnimController;
    private GameObject _currentCharacterPrefab;
    private RenderTexture _characterRenderTexture;

    [Header("트레이닝 포인트 (슬라이더 영역)")]
    [SerializeField] private Slider trainingPointSlider;             // 트레이닝 포인트 게이지
    [SerializeField] private TextMeshProUGUI trainingPointText;      // 55/960

    [Header("레벨업 버튼 (라이트스틱)")]
    [SerializeField] private Button levelUpButton;
    [SerializeField] private TextMeshProUGUI lightStickActiveCostText;   // Active 상태 필요 라이트스틱 수량
    [SerializeField] private TextMeshProUGUI lightStickFalseCostText;    // False 상태 필요 라이트스틱 수량
    [SerializeField] private GameObject lightStickActiveBackground;  // 활성 상태 배경
    [SerializeField] private GameObject lightStickFalseBackground;   // 비활성 상태 배경
    [SerializeField] private GameObject lightStickActiveContent;     // 활성 상태 컨텐츠
    [SerializeField] private GameObject lightStickFalseContent;      // 비활성 상태 컨텐츠
    [SerializeField] private GameObject lightStickFalseIcon;         // False 컨텐츠 내 아이콘 (최대레벨 시 숨김)
    [SerializeField] private GameObject lightStickFalseCostObject;   // False 컨텐츠 내 텍스트 오브젝트 (최대레벨 시 숨김)

    [Header("랭크업 버튼")]
    [SerializeField] private Button rankUpButton;
    [SerializeField] private GameObject rankUpUpIcon;  // 강화 가능 시 표시되는 아이콘
    [SerializeField] private TextMeshProUGUI rankUpButtonText;  // 버튼에 등급 이름 표시

    [Header("의상 슬롯")]
    [SerializeField] private Button topCostumeSlot;      // 상의 슬롯 버튼
    [SerializeField] private Button pantsCostumeSlot;    // 하의 슬롯 버튼
    [SerializeField] private Button shoesCostumeSlot;    // 신발 슬롯 버튼
    [SerializeField] private Image topCostumeImage;      // 상의 썸네일
    [SerializeField] private Image pantsCostumeImage;    // 하의 썸네일
    [SerializeField] private Image shoesCostumeImage;    // 신발 썸네일

    [Header("의상 선택 팝업")]
    [SerializeField] private CostumeSelectPopup costumeSelectPopup;

    [Header("등급 강화 확인 팝업")]
    [SerializeField] private RankUpConfirmPopup rankUpConfirmPopup;

    [Header("하단 탭 시스템")]
    [SerializeField] private Button charInfoTabButton;      // 캐릭터 정보 탭 버튼
    [SerializeField] private Button performanceTabButton;   // 퍼포먼스 탭 버튼
    [SerializeField] private Button positionTabButton;      // 포지션 탭 버튼
    [SerializeField] private GameObject charInfoContent;    // 캐릭터 정보 컨텐츠
    [SerializeField] private GameObject performanceContent; // 퍼포먼스 컨텐츠
    [SerializeField] private GameObject positionContent;    // 포지션 컨텐츠

    [Header("캐릭터 정보 탭 (캐릭터 설명)")]
    [SerializeField] private TextMeshProUGUI charInfoDescText;    // 캐릭터 설명 (키, 나이, 성격, 스토리)

    [Header("퍼포먼스 탭 (액티브 스킬)")]
    [SerializeField] private Image performanceSkillIcon;        // 액티브 스킬 아이콘
    [SerializeField] private TextMeshProUGUI performanceSkillNameText;  // 스킬 이름
    [SerializeField] private TextMeshProUGUI performanceCooldownText;   // 대기시간: n초
    [SerializeField] private TextMeshProUGUI performanceDescText;       // 스킬 설명

    [Header("포지션 탭 (패시브 스킬)")]
    [SerializeField] private PassiveTileDisplay positionTileDisplay;    // 패시브 범위 이미지
    [SerializeField] private TextMeshProUGUI positionDescText;          // 패시브 스킬 설명

    // 탭 상태
    private enum TabType { CharInfo, Performance, Position }
    private TabType _currentTab = TabType.CharInfo;

    // 현재 표시 중인 캐릭터 ID
    private int _currentCharacterId;
    // 랭크업 아이콘 애니메이션
    private Tweener _rankUpIconTween;

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

        // 등급 강화 팝업 콜백 등록
        if (rankUpConfirmPopup != null)
            rankUpConfirmPopup.OnRankUpCompleted += OnRankUpCompleted;

        // 탭 버튼 리스너
        if (charInfoTabButton != null)
            charInfoTabButton.onClick.AddListener(() => SwitchTab(TabType.CharInfo));
        if (performanceTabButton != null)
            performanceTabButton.onClick.AddListener(() => SwitchTab(TabType.Performance));
        if (positionTabButton != null)
            positionTabButton.onClick.AddListener(() => SwitchTab(TabType.Position));
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
        SetCharacterAsync(characterData).Forget();
    }

    private async UniTaskVoid SetCharacterAsync(CharacterCSVData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("[CharacterDetailPanel] characterData null");
            Clear();
            NoteLoadingUI.Hide();
            return;
        }

        // 로딩 표시
        NoteLoadingUI.Show();

        _currentCharacterId = characterData.char_id;
        _currentCharacterData = characterData;
        _isBattleMode = false; // 새 캐릭터 선택 시 아이돌 모드로 리셋

        // 통합 텍스트: "하나 Lv.10 보컬"
        string typeName = GetAttributeName(characterData.char_type);
        nameText.text = $"{characterData.char_name} Lv.{characterData.char_lv} {typeName}";
        UpdateRankMics(characterData.char_rank);

        UpdateStatTexts();

        // 캐릭터 정보 탭 설명 업데이트
        if (charInfoDescText != null)
            charInfoDescText.text = characterData.Info;

        // RawImage 숨기기 (렌더링 준비 전)
        if (characterRawImage != null)
        {
            characterRawImage.gameObject.SetActive(false);
        }

        // 캐릭터 프리팹 생성 (Animator 포함된 UI 프리팹)
        SpawnCharacterPrefab(characterData.image_PrefabName);

        // 2프레임 대기 (RenderTexture 렌더링 완료 대기)
        await UniTask.DelayFrame(2);

        // RawImage 켜고 페이드인 + 스케일업
        if (characterRawImage != null)
        {
            characterRawImage.color = new Color(1f, 1f, 1f, 0f);
            characterRawImage.transform.localScale = Vector3.one * 0.9f;
            characterRawImage.gameObject.SetActive(true);
            characterRawImage.DOFade(1f, 0.25f).SetEase(Ease.OutQuad);
            characterRawImage.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

        // 로딩 숨기기
        NoteLoadingUI.Hide();

        // 레벨업 구현 Onclick 리스너 등은 여기서 추가 가능
        ApplyLevelUpText(characterData.char_id);

        // 랭크업 구현 Onclick 리스너 등은 여기서 추가 가능
        ApplyRankUpText(characterData.char_id);

        // 의상 슬롯 새로고침
        RefreshCostumeSlots();

        // 탭 초기화 및 데이터 업데이트
        ResetTabToDefault();
        UpdatePerformanceTab();
        UpdatePositionTab();
    }


    public void ApplyLevelUpText(int charId)
    {
        if (levelUpButton == null)
            return;

        if (!CharacterHelper.HasCharacter(charId))
        {
            if (trainingPointText != null) trainingPointText.text = "-";
            SetLightStickCostText("-");
            SetTrainingPointSlider(0, 1);
            levelUpButton.interactable = false;
            levelUpButton.onClick.RemoveAllListeners();
            SetLightStickButtonVisual(false);
            return;
        }

        var lvdata = DataTableManager.LevelUpTable.Get(charId);
        if (lvdata == null)
        {
            // 최대 레벨
            if (trainingPointText != null) trainingPointText.text = "최대 레벨";
            SetLightStickCostText("-");
            SetTrainingPointSlider(1, 1);  // 게이지 꽉 참
            levelUpButton.interactable = false;
            levelUpButton.onClick.RemoveAllListeners();
            SetLightStickButtonVisual(false, isMaxLevel: true);
            return;
        }

        // 트레이닝 포인트 (Itm1) - 슬라이더 + 텍스트
        int currentPoint = ItemInvenHelper.GetAmount(lvdata.Lvup_ingrd_Itm1);
        int requiredPoint = lvdata.Lvup_ingrd_Itm1_count;
        if (trainingPointText != null)
            trainingPointText.text = $"{currentPoint}/{requiredPoint}";
        SetTrainingPointSlider(currentPoint, requiredPoint);

        // 라이트스틱 (Itm2) - 버튼 안에 필요 수량 표시
        int currentLightStick = ItemInvenHelper.GetAmount(lvdata.Lvup_ingrd_Itm2);
        int requiredLightStick = lvdata.Lvup_ingrd_Itm2_count;
        SetLightStickCostText($"{requiredLightStick}");

        levelUpButton.onClick.RemoveAllListeners();

        // 둘 다 충족해야 레벨업 가능
        bool hasEnoughPoint = currentPoint >= requiredPoint;
        bool hasEnoughLightStick = currentLightStick >= requiredLightStick;
        bool canLevelUp = hasEnoughPoint && hasEnoughLightStick;

        levelUpButton.interactable = canLevelUp;
        SetLightStickButtonVisual(canLevelUp);

        if (canLevelUp)
        {
            levelUpButton.onClick.AddListener(() => OnLevelUpButtonClick(charId));
        }
    }

    /// <summary>
    /// 라이트스틱 버튼 비주얼 상태 전환 (Active/False 배경 및 컨텐츠)
    /// </summary>
    /// <param name="canLevelUp">레벨업 가능 여부</param>
    /// <param name="isMaxLevel">최대 레벨 여부 (FalseContent 내 아이콘/텍스트 숨김)</param>
    private void SetLightStickButtonVisual(bool canLevelUp, bool isMaxLevel = false)
    {
        if (lightStickActiveBackground != null)
            lightStickActiveBackground.SetActive(canLevelUp);
        if (lightStickFalseBackground != null)
            lightStickFalseBackground.SetActive(!canLevelUp);
        if (lightStickActiveContent != null)
            lightStickActiveContent.SetActive(canLevelUp);
        if (lightStickFalseContent != null)
            lightStickFalseContent.SetActive(!canLevelUp);

        // 최대 레벨일 때 FalseContent 내 아이콘/텍스트 숨김
        if (lightStickFalseIcon != null)
            lightStickFalseIcon.SetActive(!isMaxLevel);
        if (lightStickFalseCostObject != null)
            lightStickFalseCostObject.SetActive(!isMaxLevel);
    }

    /// <summary>
    /// 라이트스틱 필요 수량 텍스트 설정 (Active/False 둘 다)
    /// </summary>
    private void SetLightStickCostText(string text)
    {
        if (lightStickActiveCostText != null)
            lightStickActiveCostText.text = text;
        if (lightStickFalseCostText != null)
            lightStickFalseCostText.text = text;
    }

    /// <summary>
    /// 트레이닝 포인트 슬라이더 값 설정
    /// </summary>
    private void SetTrainingPointSlider(int current, int required)
    {
        if (trainingPointSlider == null)
            return;

        trainingPointSlider.maxValue = required;
        trainingPointSlider.value = Mathf.Min(current, required);
    }

    public void ApplyRankUpText(int charId)
    {
        if (rankUpButton == null)
            return;

        // 현재 캐릭터 랭크 가져오기
        var charData = DataTableManager.CharacterTable.Get(charId);
        int currentRank = charData?.char_rank ?? 1;

        // 버튼에 등급 이름 표시
        if (rankUpButtonText != null)
            rankUpButtonText.text = GetRankName(currentRank);

        if (!CharacterHelper.HasCharacter(charId))
        {
            rankUpButton.interactable = false;
            rankUpButton.onClick.RemoveAllListeners();
            SetRankUpIconActive(false);
            return;
        }

        var rankdata = DataTableManager.RankUpTable.Get(charId);
        if (rankdata == null)
        {
            // 최대 랭크 - 버튼 비활성화
            rankUpButton.interactable = false;
            rankUpButton.onClick.RemoveAllListeners();
            SetRankUpIconActive(false);
            return;
        }

        int currentPoint = ItemInvenHelper.GetAmount(rankdata.Upgrade_ingrd_Itm1);
        bool canRankUp = currentPoint >= rankdata.Ingrd_Itm1_amount;

        rankUpButton.onClick.RemoveAllListeners();
        rankUpButton.interactable = true;  // 팝업에서 재료 확인하므로 버튼은 항상 활성화
        SetRankUpIconActive(canRankUp);

        rankUpButton.onClick.AddListener(() => OnRankUpButtonClick(charId));
    }

    /// <summary>
    /// 랭크업 아이콘 활성화/비활성화 + 위아래 흔들림 애니메이션
    /// </summary>
    private void SetRankUpIconActive(bool active)
    {
        if (rankUpUpIcon == null) return;

        // 기존 애니메이션 정지
        _rankUpIconTween?.Kill();

        if (active)
        {
            rankUpUpIcon.SetActive(true);
            // 위아래로 살짝 흔들리는 애니메이션 (5px, 0.5초 주기, 무한 반복)
            _rankUpIconTween = rankUpUpIcon.transform
                .DOLocalMoveY(rankUpUpIcon.transform.localPosition.y + 5f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            rankUpUpIcon.SetActive(false);
        }
    }

    public void OnLevelUpButtonClick(int charId)
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        var lvdata = DataTableManager.LevelUpTable.Get(charId);
        if (lvdata == null)
        {
            if (trainingPointText != null)
                trainingPointText.text = "최대 레벨";

            if (levelUpButton != null)
            {
                levelUpButton.interactable = false;
                levelUpButton.onClick.RemoveAllListeners();
            }
            SetLightStickButtonVisual(false);
            return;
        }

        // 트레이닝 포인트 소모
        if (!ItemInvenHelper.TryConsumeItem(lvdata.Lvup_ingrd_Itm1, lvdata.Lvup_ingrd_Itm1_count))
        {
            ApplyLevelUpText(charId);
            return;
        }

        // 라이트스틱 소모
        if (lvdata.Lvup_ingrd_Itm2_count > 0 &&
            !ItemInvenHelper.TryConsumeItem(lvdata.Lvup_ingrd_Itm2, lvdata.Lvup_ingrd_Itm2_count))
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

        // 등급 강화 확인 팝업 열기
        if (rankUpConfirmPopup != null)
        {
            rankUpConfirmPopup.Open(charId);
        }
    }

    /// <summary>
    /// 등급 강화 완료 시 호출되는 콜백
    /// </summary>
    private void OnRankUpCompleted()
    {
        // 업그레이드된 캐릭터 정보 다시 로드
        var rankdata = DataTableManager.RankUpTable.Get(_currentCharacterId);
        if (rankdata != null)
        {
            int finalId = rankdata.Upgrade_char;
            var nextRankData = DataTableManager.CharacterTable.Get(finalId);

            if (nextRankData != null)
            {
                rankUpButton.onClick.RemoveAllListeners();
                SetCharacter(nextRankData);

                // 새 랭크 기준으로 텍스트/버튼 다시 계산
                ApplyRankUpText(nextRankData.char_id);
                ApplyLevelUpText(nextRankData.char_id);
            }
        }
    }


    public void Clear()
    {
        nameText.text = "";
        UpdateRankMics(0);  // 마이크 모두 끄기
        attackText.text = "공격력 ";
        attackSpeedText.text = "공격속도 ";
        attackRangeText.text = "공격범위 ";
        additionalAttackText.text = "추가공격수 ";
        healthText.text = "체력 ";
        critRateText.text = "치명타 확률 ";
        critDmgText.text = "치명타 데미지 ";
        if (charInfoDescText != null)
            charInfoDescText.text = "";

        if (trainingPointText != null) trainingPointText.text = "";
        SetLightStickCostText("");
        SetTrainingPointSlider(0, 1);

        levelUpButton.interactable = false;
        rankUpButton.interactable = false;
        SetLightStickButtonVisual(false);

        // 캐릭터 프리팹 정리
        ClearCharacterPrefab();
    }

    public void ClosePanel()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        // RawImage 숨기기 (다음 열 때 이전 캐릭터 안 보이게)
        if (characterRawImage != null)
            characterRawImage.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }
    public void OpenPanel()
    {
        // RawImage 미리 숨기기 (뿅하고 나타나는 거 방지)
        if (characterRawImage != null)
            characterRawImage.gameObject.SetActive(false);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();  // 제일 위에 표시
        // 애니메이션은 WindowAnimator가 OnEnable에서 자동 재생
    }

    private void OnEnable()
    {
        if (ExitButton != null)
        {
            ExitButton.onClick.RemoveAllListeners();
            ExitButton.onClick.AddListener(ClosePanel);
        }
    }

    private void OnDisable()
    {
        // RawImage 숨기기 (다음 열 때 이전 캐릭터 안 보이게)
        if (characterRawImage != null)
            characterRawImage.gameObject.SetActive(false);
    }

    #region 탭 시스템

    /// <summary>
    /// 탭 전환
    /// </summary>
    private void SwitchTab(TabType tab)
    {
        if (_currentTab == tab)
            return;

        _currentTab = tab;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        // 컨텐츠 활성화/비활성화
        if (charInfoContent != null)
            charInfoContent.SetActive(tab == TabType.CharInfo);
        if (performanceContent != null)
            performanceContent.SetActive(tab == TabType.Performance);
        if (positionContent != null)
            positionContent.SetActive(tab == TabType.Position);

        // 버튼 상태 업데이트 (선택된 탭은 비활성화)
        if (charInfoTabButton != null)
            charInfoTabButton.interactable = tab != TabType.CharInfo;
        if (performanceTabButton != null)
            performanceTabButton.interactable = tab != TabType.Performance;
        if (positionTabButton != null)
            positionTabButton.interactable = tab != TabType.Position;
    }

    /// <summary>
    /// 탭 초기화 (캐릭터 정보 탭으로 리셋)
    /// </summary>
    private void ResetTabToDefault()
    {
        _currentTab = TabType.CharInfo;

        if (charInfoContent != null)
            charInfoContent.SetActive(true);
        if (performanceContent != null)
            performanceContent.SetActive(false);
        if (positionContent != null)
            positionContent.SetActive(false);

        if (charInfoTabButton != null)
            charInfoTabButton.interactable = false;
        if (performanceTabButton != null)
            performanceTabButton.interactable = true;
        if (positionTabButton != null)
            positionTabButton.interactable = true;
    }

    /// <summary>
    /// 퍼포먼스 탭 데이터 설정 (액티브 스킬)
    /// </summary>
    private void UpdatePerformanceTab()
    {
        if (_currentCharacterData == null)
            return;

        // 액티브 스킬 찾기 (skill_id2가 액티브 스킬)
        var skillIds = DataTableManager.CharacterTable.GetSkillIds(_currentCharacterData.char_id);
        if (skillIds == null || skillIds.Count < 2)
        {
            ClearPerformanceTab();
            return;
        }

        int activeSkillId = skillIds[1];  // 두 번째 스킬이 액티브
        var skillData = DataTableManager.SkillTable.Get(activeSkillId);
        if (skillData == null)
        {
            ClearPerformanceTab();
            return;
        }

        // 스킬 이름
        if (performanceSkillNameText != null)
            performanceSkillNameText.text = skillData.skill_name;

        // 대기시간
        if (performanceCooldownText != null)
            performanceCooldownText.text = $"대기시간: {skillData.skill_cool}초";

        // 스킬 설명
        if (performanceDescText != null)
            performanceDescText.text = skillData.GetFormattedInfo();

        // 스킬 아이콘
        if (performanceSkillIcon != null && !string.IsNullOrEmpty(skillData.icon_prefab))
        {
            var sprite = ResourceManager.Instance.GetSprite(skillData.icon_prefab);
            if (sprite != null)
                performanceSkillIcon.sprite = sprite;
        }
    }

    private void ClearPerformanceTab()
    {
        if (performanceSkillNameText != null)
            performanceSkillNameText.text = "";
        if (performanceCooldownText != null)
            performanceCooldownText.text = "";
        if (performanceDescText != null)
            performanceDescText.text = "";
        if (performanceSkillIcon != null)
            performanceSkillIcon.sprite = null;
    }

    /// <summary>
    /// 포지션 탭 데이터 설정 (패시브 스킬)
    /// </summary>
    private void UpdatePositionTab()
    {
        if (_currentCharacterData == null)
            return;

        // 패시브 스킬 찾기 (skill_id1이 패시브 스킬)
        var skillIds = DataTableManager.CharacterTable.GetSkillIds(_currentCharacterData.char_id);
        if (skillIds == null || skillIds.Count < 1)
        {
            ClearPositionTab();
            return;
        }

        int passiveSkillId = skillIds[0];  // 첫 번째 스킬이 패시브
        var skillData = DataTableManager.SkillTable.Get(passiveSkillId);
        if (skillData == null)
        {
            ClearPositionTab();
            return;
        }

        // 패시브 타일 표시 (캐릭터 타입에 따른 색상 적용)
        if (positionTileDisplay != null)
            positionTileDisplay.SetPattern(skillData.passive_type, _currentCharacterData.char_type);

        // 패시브 스킬 설명
        if (positionDescText != null)
            positionDescText.text = skillData.GetFormattedInfo();
    }

    private void ClearPositionTab()
    {
        if (positionTileDisplay != null)
            positionTileDisplay.Clear();
        if (positionDescText != null)
            positionDescText.text = "";
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 랭크에 따라 마이크 이미지 스프라이트 업데이트 (1~4 랭크)
    /// 1=병아리연습생, 2=에이스연습생, 3=신인아이돌, 4=인기아이돌
    /// </summary>
    private void UpdateRankMics(int rank)
    {
        if (rankMicImages == null || rankMicImages.Length == 0)
            return;

        for (int i = 0; i < rankMicImages.Length; i++)
        {
            if (rankMicImages[i] == null) continue;

            // rank가 i+1 이상이면 불 켜짐 (rank 1 = 마이크 1개, rank 4 = 마이크 4개)
            bool isOn = i < rank;
            rankMicImages[i].sprite = isOn ? rankMicOnSprite : rankMicOffSprite;
        }
    }

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

    /// <summary>
    /// 랭크를 등급 이름으로 변환
    /// </summary>
    private string GetRankName(int rank)
    {
        return rank switch
        {
            1 => "병아리 연습생",
            2 => "에이스 연습생",
            3 => "신인 아이돌",
            4 => "인기 아이돌",
            _ => ""
        };
    }

    /// <summary>
    /// 캐릭터 프리팹 생성 및 RenderTexture로 렌더링
    /// </summary>
    private void SpawnCharacterPrefab(string prefabKey)
    {
        // 이전 프리팹 제거
        ClearCharacterPrefab();

        if (string.IsNullOrEmpty(prefabKey))
        {
            Debug.LogWarning("[CharacterDetailPanel] prefabKey 없음");
            return;
        }

        // 프리팹 로드
        var prefab = ResourceManager.Instance.Get<GameObject>(prefabKey);
        if (prefab == null)
        {
            Debug.LogWarning($"[CharacterDetailPanel] 캐릭터 프리팹 로드 실패: {prefabKey}");
            return;
        }

        // RenderTexture 생성 (RawImage 크기에 맞춤)
        SetupRenderTexture();

        // 프리팹 생성 (카메라 앞에)
        Vector3 spawnPos = characterCamera.transform.position + characterCamera.transform.forward * spawnDistance;
        _currentCharacterPrefab = Instantiate(prefab);
        _currentCharacterPrefab.transform.position = spawnPos;
        _currentCharacterPrefab.transform.rotation = Quaternion.identity;
        _currentCharacterPrefab.transform.localScale = Vector3.one * characterPrefabScale;

        // 애니메이션 컨트롤러 설정 + 즉시 Idle 상태로 전환
        var animator = _currentCharacterPrefab.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            // Animator 끄고 위치 고정
            animator.enabled = false;

            if (lobbyAnimController != null)
            {
                animator.runtimeAnimatorController = lobbyAnimController;
            }

            // Animator 켜고 Idle 상태로
            animator.enabled = true;
            animator.Play("Idle_1", 0, 0f);
            animator.Update(0f);  // 즉시 적용
        }

        // CostumeController 초기화
        var costumeController = _currentCharacterPrefab.GetComponent<CostumeController>();
        if (costumeController != null && _currentCharacterData != null)
        {
            costumeController.Initialize(_currentCharacterData.char_name);
        }
    }

    /// <summary>
    /// RenderTexture 설정
    /// </summary>
    private void SetupRenderTexture()
    {
        if (characterRawImage == null || characterCamera == null)
            return;

        // 기존 RenderTexture 해제
        if (_characterRenderTexture != null)
        {
            characterCamera.targetTexture = null;
            _characterRenderTexture.Release();
            Destroy(_characterRenderTexture);
        }

        // RawImage 크기에 맞는 RenderTexture 생성
        RectTransform rt = characterRawImage.rectTransform;
        int width = Mathf.Max(1, (int)rt.rect.width);
        int height = Mathf.Max(1, (int)rt.rect.height);

        // 해상도 스케일 (선명도 조절)
        width = Mathf.Max(256, width * 2);
        height = Mathf.Max(256, height * 2);

        _characterRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        _characterRenderTexture.antiAliasing = 2;
        _characterRenderTexture.Create();

        // 카메라에 RenderTexture 연결
        characterCamera.targetTexture = _characterRenderTexture;

        // RawImage에 RenderTexture 연결
        characterRawImage.texture = _characterRenderTexture;
    }

    /// <summary>
    /// 캐릭터 프리팹 및 RenderTexture 정리
    /// </summary>
    private void ClearCharacterPrefab()
    {
        if (_currentCharacterPrefab != null)
        {
            Destroy(_currentCharacterPrefab);
            _currentCharacterPrefab = null;
        }
    }

    private void OnDestroy()
    {
        ClearCharacterPrefab();

        // RenderTexture 정리
        if (_characterRenderTexture != null)
        {
            if (characterCamera != null)
                characterCamera.targetTexture = null;
            _characterRenderTexture.Release();
            Destroy(_characterRenderTexture);
            _characterRenderTexture = null;
        }

        // 랭크업 아이콘 애니메이션 정리
        _rankUpIconTween?.Kill();

        // 팝업 콜백 해제
        if (costumeSelectPopup != null)
            costumeSelectPopup.OnCostumeChanged -= RefreshCostumeSlots;

        if (rankUpConfirmPopup != null)
            rankUpConfirmPopup.OnRankUpCompleted -= OnRankUpCompleted;
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

        // 이미 팝업이 열려있으면 무시 (더블 터치 방지)
        if (costumeSelectPopup != null && costumeSelectPopup.IsOpen)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (costumeSelectPopup == null)
            return;

        // 현재 캐릭터 프리팹의 CostumeController 전달
        CostumeController costumeController = null;
        if (_currentCharacterPrefab != null)
        {
            costumeController = _currentCharacterPrefab.GetComponent<CostumeController>();
        }

        // 팝업 열기 (내부에서 NoteLoadingUI 표시 및 데이터 로드)
        costumeSelectPopup.Open(_currentCharacterData.char_name, type, costumeController, _characterRenderTexture);
    }

    /// <summary>
    /// 의상 슬롯 썸네일 새로고침 + 프리팹 의상 업데이트
    /// </summary>
    private void RefreshCostumeSlots()
    {
        if (_currentCharacterData == null)
            return;

        // 프리팹의 CostumeController 의상도 다시 로드
        if (_currentCharacterPrefab != null)
        {
            var costumeController = _currentCharacterPrefab.GetComponent<CostumeController>();
            if (costumeController != null)
            {
                costumeController.LoadEquippedCostume();
            }
        }

        RefreshCostumeSlotsAsync().Forget();
    }

    private async UniTaskVoid RefreshCostumeSlotsAsync()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || _currentCharacterData == null)
            return;

        // 로딩 표시
        NoteLoadingUI.Show();

        string charName = _currentCharacterData.char_name;
        if (!saveData.equippedCostumeByChar.TryGetValue(charName, out var costume))
        {
            // 장착 의상 없음 - 슬롯 비우기
            ClearCostumeSlot(topCostumeImage);
            ClearCostumeSlot(pantsCostumeImage);
            ClearCostumeSlot(shoesCostumeImage);
            NoteLoadingUI.Hide();
            return;
        }

        // 각 슬롯 업데이트
        await UpdateCostumeSlot(topCostumeImage, CostumeType.Top, costume.topItemId);
        await UpdateCostumeSlot(pantsCostumeImage, CostumeType.Pants, costume.pantsItemId);
        await UpdateCostumeSlot(shoesCostumeImage, CostumeType.Shoes, costume.shoesItemId);

        // 로딩 숨기기
        NoteLoadingUI.Hide();
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
                ApplySpriteToSlot(slotImage, sprite);
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
                ApplySpriteToSlot(slotImage, sprite);
                return;
            }
        }

        ClearCostumeSlot(slotImage);
    }

    private void ApplySpriteToSlot(Image slotImage, Sprite sprite)
    {
        slotImage.sprite = sprite;
        slotImage.color = Color.white;
        slotImage.type = Image.Type.Simple;
        slotImage.preserveAspect = false;  // 슬롯 크기에 맞게 늘림
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
