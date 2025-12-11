using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class SkillTestManager : MonoBehaviour
{
    [Header("라벨 설정 (Addressables)")]
    [SerializeField] private string projectileEffectLabel = "SFX"; // 발사 이펙트 라벨
    [SerializeField] private string hitEffectLabel = "Hit";        // 히트 이펙트 라벨

    [Header("Skill 선택")]
    [SerializeField] private TMP_Dropdown skillDropdown;           // 스킬 선택 드롭다운
    [SerializeField] private TMP_InputField skillIdInput;          // skill_id 숫자로 검색용 InputField

    // ==========================
    // Page2 - 기본 정보 / 스탯 (실제론 Panel2에 배치)
    // ==========================
    [Header("Page2 - 기본 정보 / 스탯 (Panel2)")]
    [SerializeField] private TMP_InputField skillNameInput;
    [SerializeField] private TMP_InputField skillAutoInput;
    [SerializeField] private TMP_InputField skillTypeInput;
    [SerializeField] private TMP_Dropdown passiveTypeDropdown;
    [SerializeField] private TMP_InputField skillTargetInput;
    [SerializeField] private Toggle skillPierceToggle;
    [SerializeField] private TMP_InputField skillDmgInput;
    [SerializeField] private TMP_InputField skillCoolInput;
    [SerializeField] private TMP_InputField skillSpeedInput;
    [SerializeField] private TMP_InputField skillCrtInput;

    [SerializeField] private TMP_InputField rangeInput;    // skill_range 입력
    [SerializeField] private Slider rangeSlider;           // skill_range 슬라이더

    [SerializeField] private TMP_InputField skillDurationInput;

    // ==========================
    // Page3 - 소환 / 이펙트 / Info / Prefab
    // ==========================
    [Header("Page3 - 소환 / 이펙트 / Info / Prefab (Panel3)")]
    [SerializeField] private TMP_InputField summonMinInput;
    [SerializeField] private TMP_InputField summonMaxInput;
    [SerializeField] private TMP_InputField summonTypeInput;

    [SerializeField] private TMP_InputField skillEff1Input;
    [SerializeField] private TMP_InputField skillEff1ValInput;
    [SerializeField] private TMP_InputField skillEff1DurationInput;

    [SerializeField] private TMP_InputField skillEff2Input;
    [SerializeField] private TMP_InputField skillEff2ValInput;
    [SerializeField] private TMP_InputField skillEff2DurationInput;

    [SerializeField] private TMP_InputField skillEff3Input;
    [SerializeField] private TMP_InputField skillEff3ValInput;
    [SerializeField] private TMP_InputField skillEff3DurationInput;

    [SerializeField] private TMP_InputField infoInput;
    [SerializeField] private TMP_InputField iconPrefabInput;
    [SerializeField] private TMP_InputField skillPrefabInput;

    // ==========================
    // Projectile / Hit UI
    // ==========================
    [Header("Projectile / Hit UI")]
    [SerializeField] private TMP_InputField projectileKeyInput;    // projectile 키 입력
    [SerializeField] private TMP_Dropdown projectileKeyDropdown;   // projectile 키 목록
    [SerializeField] private TMP_InputField hitKeyInput;           // hit 키 입력
    [SerializeField] private TMP_Dropdown hitKeyDropdown;          // hit 키 목록
    [SerializeField] private Button hitPreviewButton;              // hit 프리뷰 버튼
    [SerializeField] private Button hitApplyButton;                // hit 적용 버튼

    [Header("프리뷰 위치")]
    [SerializeField] private Transform previewPoint;

    [Header("Projectile 버튼")]
    [SerializeField] private Button projectilePreviewButton;       // projectile 프리뷰
    [SerializeField] private Button projectileApplyButton;         // projectile 적용

    [Header("슬라이더 기본값")]
    [SerializeField] private float defaultSliderMin = 0f;
    [SerializeField] private float defaultSliderMax = 10f;

    [Header("페이지 전환 (Panel1/2/3)")]
    [SerializeField] private GameObject page1Root;   // Panel1: 테스트/기타
    [SerializeField] private GameObject page2Root;   // Panel2: 기본 정보 / 스탯 편집
    [SerializeField] private GameObject page3Root;   // Panel3: 소환 / 이펙트 / Info / Prefab 편집

    [SerializeField] private Button page1Button;     // "1 / 3" 버튼 (테스트)
    [SerializeField] private Button page2Button;     // "2 / 3" 버튼 (기본 정보)
    [SerializeField] private Button page3Button;     // "3 / 3" 버튼 (이펙트/소환)

    // 내부 데이터
    private Dictionary<int, SkillData> skillDB;
    private List<SkillData> skillList;
    private List<string> projectileKeys;
    private List<string> hitKeys;

    private SkillData currentSkill;
    private string currentProjectileKey;
    private string currentHitKey;

    // 프리뷰 관리
    private GameObject currentPreviewInstance;
    private AsyncOperationHandle<GameObject> currentHandle;
    private bool hasHandle = false;

    private int currentPage = 1;

    // ==========================
    // PassiveType 패턴 미리보기 (5 x 3 고정)
    // ==========================
    [Header("PassiveType 패턴 미리보기")]
    [SerializeField] private GameObject passivePatternRoot;   // 전체 패턴 패널 (비/활성)
    [SerializeField] private Image[] passiveTiles;            // 0~14까지 15칸 직접 할당

    [SerializeField] private Color passiveDefaultColor = new Color(1f, 1f, 1f, 0.15f); // 기본 색
    [SerializeField] private Color passiveActiveColor = Color.yellow;                  // 버프 타일
    [SerializeField] private Color passiveCenterColor = Color.red;                     // 중앙 타일

    private const int PassiveTileCount = 15; // 5 x 3
    private const int PassiveCenterIndex = 7; // 가운데(1행 2열) = 인덱스 7

    private async void Start()
    {
        // 1) SkillTable 준비될 때까지 대기
        while (DataTableManager.SkillTable == null)
            await UniTask.Delay(10, DelayType.UnscaledDeltaTime);

        // 2) SkillData 전부 가져오기 (id -> SkillData)
        skillDB = DataTableManager.SkillTable.GetAll();
        skillList = new List<SkillData>(skillDB.Values);
        skillList.Sort((a, b) => a.skill_id.CompareTo(b.skill_id));

        // 3) 드롭다운, 슬라이더, 패시브 타입 드롭다운 구성
        BuildSkillDropdown();
        SetupRangeSliderIfNeeded();
        BuildPassiveTypeDropdown();

        // 4) Addressables 라벨 기반 projectile/hit 이펙트 키 목록 구성
        StartCoroutine(BuildProjectileKeyDropdownFromLabel(projectileEffectLabel));
        StartCoroutine(BuildHitKeyDropdownFromLabel(hitEffectLabel));

        // 5) 이벤트 리스너 등록

        // 스킬 선택 쪽
        if (skillDropdown != null)
            skillDropdown.onValueChanged.AddListener(OnSkillChanged);
        if (skillIdInput != null)
            skillIdInput.onEndEdit.AddListener(OnSkillIdInputChanged);

        // Page2 - 기본 정보 / 스탯
        if (skillNameInput != null) skillNameInput.onEndEdit.AddListener(OnSkillNameChanged);
        // 데미지계산로직변경해야함
        // if (skillAutoInput != null) skillAutoInput.onEndEdit.AddListener(OnSkillAutoChanged);
        if (skillTypeInput != null) skillTypeInput.onEndEdit.AddListener(OnSkillTypeChanged);
        if (passiveTypeDropdown != null) passiveTypeDropdown.onValueChanged.AddListener(OnPassiveTypeDropdownChanged);
        if (skillTargetInput != null) skillTargetInput.onEndEdit.AddListener(OnSkillTargetChanged);
        if (skillPierceToggle != null) skillPierceToggle.onValueChanged.AddListener(OnSkillPierceToggleChanged);
        // 데미지계산로직변경해야함
        // if (skillDmgInput != null) skillDmgInput.onEndEdit.AddListener(OnSkillDmgChanged);
        if (skillCoolInput != null) skillCoolInput.onEndEdit.AddListener(OnSkillCoolChanged);
        if (skillSpeedInput != null) skillSpeedInput.onEndEdit.AddListener(OnSkillSpeedChanged);
        // 데미지계산로직변경해야함
        // if (skillCrtInput != null) skillCrtInput.onEndEdit.AddListener(OnSkillCrtChanged);
        if (rangeInput != null) rangeInput.onEndEdit.AddListener(OnRangeInputChanged);
        if (rangeSlider != null) rangeSlider.onValueChanged.AddListener(OnRangeSliderChanged);
        if (skillDurationInput != null) skillDurationInput.onEndEdit.AddListener(OnSkillDurationChanged);

        // Page3 - 소환 / 이펙트 / Info / Prefab
        if (summonMinInput != null) summonMinInput.onEndEdit.AddListener(OnSummonMinChanged);
        if (summonMaxInput != null) summonMaxInput.onEndEdit.AddListener(OnSummonMaxChanged);
        if (summonTypeInput != null) summonTypeInput.onEndEdit.AddListener(OnSummonTypeChanged);

        if (skillEff1Input != null) skillEff1Input.onEndEdit.AddListener(OnSkillEff1Changed);
        if (skillEff1ValInput != null) skillEff1ValInput.onEndEdit.AddListener(OnSkillEff1ValChanged);
        if (skillEff1DurationInput != null) skillEff1DurationInput.onEndEdit.AddListener(OnSkillEff1DurationChanged);

        if (skillEff2Input != null) skillEff2Input.onEndEdit.AddListener(OnSkillEff2Changed);
        if (skillEff2ValInput != null) skillEff2ValInput.onEndEdit.AddListener(OnSkillEff2ValChanged);
        if (skillEff2DurationInput != null) skillEff2DurationInput.onEndEdit.AddListener(OnSkillEff2DurationChanged);

        if (skillEff3Input != null) skillEff3Input.onEndEdit.AddListener(OnSkillEff3Changed);
        if (skillEff3ValInput != null) skillEff3ValInput.onEndEdit.AddListener(OnSkillEff3ValChanged);
        if (skillEff3DurationInput != null) skillEff3DurationInput.onEndEdit.AddListener(OnSkillEff3DurationChanged);

        if (infoInput != null) infoInput.onEndEdit.AddListener(OnInfoChanged);
        if (iconPrefabInput != null) iconPrefabInput.onEndEdit.AddListener(OnIconPrefabChanged);
        if (skillPrefabInput != null) skillPrefabInput.onEndEdit.AddListener(OnSkillPrefabChanged);

        // Projectile / Hit
        if (projectileKeyDropdown != null)
            projectileKeyDropdown.onValueChanged.AddListener(OnProjectileKeyDropdownChanged);
        if (hitKeyDropdown != null)
            hitKeyDropdown.onValueChanged.AddListener(OnHitKeyDropdownChanged);
        if (projectileKeyInput != null)
            projectileKeyInput.onEndEdit.AddListener(OnProjectileKeyInputChanged);
        if (hitKeyInput != null)
            hitKeyInput.onEndEdit.AddListener(OnHitKeyInputChanged);

        if (projectilePreviewButton != null)
            projectilePreviewButton.onClick.AddListener(OnProjectilePreviewClicked);
        if (projectileApplyButton != null)
            projectileApplyButton.onClick.AddListener(OnProjectileApplyClicked);
        if (hitPreviewButton != null)
            hitPreviewButton.onClick.AddListener(OnHitPreviewClicked);
        if (hitApplyButton != null)
            hitApplyButton.onClick.AddListener(OnHitApplyClicked);

        // 페이지 버튼 (1: 테스트, 2: 기본, 3: 이펙트/소환)
        if (page1Button != null) page1Button.onClick.AddListener(() => ShowPage(1));
        if (page2Button != null) page2Button.onClick.AddListener(() => ShowPage(2));
        if (page3Button != null) page3Button.onClick.AddListener(() => ShowPage(3));

        // 기본은 1페이지(테스트 패널) 보여주기
        ShowPage(1);

        // 초기 스킬 선택
        if (skillList.Count > 0)
            OnSkillChanged(0);

        // 패시브 패턴 초기 그리기
        InitPassivePatternPreview();
    }

    private void OnDestroy()
    {
        ReleasePreview();
    }

    // ==========================
    // UI 빌드
    // ==========================

    private void BuildSkillDropdown()
    {
        if (skillDropdown == null) return;

        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var s in skillList)
        {
            string label = $"{s.skill_id} - {s.skill_name}";
            options.Add(new TMP_Dropdown.OptionData(label));
        }

        skillDropdown.ClearOptions();
        skillDropdown.AddOptions(options);
    }

    private void SetupRangeSliderIfNeeded()
    {
        if (rangeSlider == null) return;

        if (Mathf.Approximately(rangeSlider.minValue, 0f) &&
            Mathf.Approximately(rangeSlider.maxValue, 1f))
        {
            rangeSlider.minValue = defaultSliderMin;
            rangeSlider.maxValue = defaultSliderMax;
        }
    }

    private void BuildPassiveTypeDropdown()
    {
        if (passiveTypeDropdown == null) return;

        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var name in System.Enum.GetNames(typeof(PassiveType)))
        {
            options.Add(new TMP_Dropdown.OptionData(name));
        }

        passiveTypeDropdown.ClearOptions();
        passiveTypeDropdown.AddOptions(options);
    }

    private IEnumerator BuildProjectileKeyDropdownFromLabel(string label)
    {
        if (projectileKeyDropdown == null)
            yield break;

        var handle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[SkillTest] 라벨 '{label}' 로 projectile 이펙트 목록 로드 실패");
            Addressables.Release(handle);
            yield break;
        }

        projectileKeys = new List<string>();
        foreach (var loc in handle.Result)
        {
            projectileKeys.Add(loc.PrimaryKey);
        }

        projectileKeys.Sort();

        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var key in projectileKeys)
            options.Add(new TMP_Dropdown.OptionData(key));

        projectileKeyDropdown.ClearOptions();
        projectileKeyDropdown.AddOptions(options);

        Addressables.Release(handle);
    }

    private IEnumerator BuildHitKeyDropdownFromLabel(string label)
    {
        if (hitKeyDropdown == null)
            yield break;

        var handle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[SkillTest] 라벨 '{label}' 로 hit 이펙트 목록 로드 실패");
            Addressables.Release(handle);
            yield break;
        }

        hitKeys = new List<string>();
        foreach (var loc in handle.Result)
        {
            hitKeys.Add(loc.PrimaryKey);
        }

        hitKeys.Sort();

        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var key in hitKeys)
            options.Add(new TMP_Dropdown.OptionData(key));

        hitKeyDropdown.ClearOptions();
        hitKeyDropdown.AddOptions(options);

        Addressables.Release(handle);
    }

    // ==========================
    // 스킬 선택 / UI 동기화
    // ==========================

    private void OnSkillChanged(int index)
    {
        if (index < 0 || index >= skillList.Count)
            return;

        currentSkill = skillList[index];

        currentProjectileKey = currentSkill.skillprojectile_prefab;
        currentHitKey = currentSkill.skillhit_prefab;

        Debug.Log($"[SkillTest] 스킬 선택: {currentSkill.skill_id} - {currentSkill.skill_name}");

        // ID InputField에도 현재 skill_id 반영
        if (skillIdInput != null)
            skillIdInput.SetTextWithoutNotify(currentSkill.skill_id.ToString());

        SyncSkillToUI();
    }

    /// <summary>
    /// skillIdInput 에 숫자를 입력해서 스킬 선택하는 기능.
    /// - int 파싱 실패 or 해당 id 없음 → 아무 변화 없이 현재 스킬 id로 되돌림.
    /// </summary>
    private void OnSkillIdInputChanged(string newText)
    {
        if (skillList == null || skillList.Count == 0)
            return;

        int currentId = (currentSkill != null) ? currentSkill.skill_id : 0;

        int id = ParseInt(newText, currentId);

        if (id == currentId)
        {
            if (currentSkill != null && skillIdInput != null)
                skillIdInput.SetTextWithoutNotify(currentSkill.skill_id.ToString());
            return;
        }

        int idx = skillList.FindIndex(s => s.skill_id == id);
        if (idx < 0)
        {
            Debug.LogWarning($"[SkillTest] skill_id {id} 를 찾을 수 없습니다.");
            if (currentSkill != null && skillIdInput != null)
                skillIdInput.SetTextWithoutNotify(currentSkill.skill_id.ToString());
            return;
        }

        if (skillDropdown != null)
            skillDropdown.SetValueWithoutNotify(idx);

        OnSkillChanged(idx);
    }

    private void SyncSkillToUI()
    {
        if (currentSkill == null) return;

        // ----- Page2 : 기본 정보 / 스탯 -----
        if (skillNameInput != null)
            skillNameInput.SetTextWithoutNotify(currentSkill.skill_name ?? "");

        // 데미지계산로직변경해야함
        // if (skillAutoInput != null)
        //     skillAutoInput.SetTextWithoutNotify(currentSkill.skill_auto.ToString());

        if (skillTypeInput != null)
            skillTypeInput.SetTextWithoutNotify(currentSkill.skill_type.ToString());

        if (passiveTypeDropdown != null)
        {
            int val = Mathf.Clamp((int)currentSkill.passive_type, 0, passiveTypeDropdown.options.Count - 1);
            passiveTypeDropdown.SetValueWithoutNotify(val);
        }

        // 패시브 패턴 미리보기 갱신
        UpdatePassivePatternPreview(currentSkill.passive_type);

        if (skillTargetInput != null)
            skillTargetInput.SetTextWithoutNotify(currentSkill.skill_target.ToString());

        if (skillPierceToggle != null)
            skillPierceToggle.SetIsOnWithoutNotify(currentSkill.skill_pierce);

        // 데미지계산로직변경해야함
        // if (skillDmgInput != null)
        //     skillDmgInput.SetTextWithoutNotify(currentSkill.skill_dmg.ToString());

        if (skillCoolInput != null)
            skillCoolInput.SetTextWithoutNotify(currentSkill.skill_cool.ToString("0.00", CultureInfo.InvariantCulture));

        if (skillSpeedInput != null)
            skillSpeedInput.SetTextWithoutNotify(currentSkill.skill_speed.ToString("0.00", CultureInfo.InvariantCulture));

        // 데미지계산로직변경해야함
        // if (skillCrtInput != null)
        //     skillCrtInput.SetTextWithoutNotify(currentSkill.skill_crt.ToString("0.00", CultureInfo.InvariantCulture));

        if (rangeInput != null)
            rangeInput.SetTextWithoutNotify(currentSkill.skill_range.ToString("0.00", CultureInfo.InvariantCulture));

        if (rangeSlider != null)
        {
            float clamped = Mathf.Clamp(currentSkill.skill_range, rangeSlider.minValue, rangeSlider.maxValue);
            rangeSlider.SetValueWithoutNotify(clamped);
        }

        if (skillDurationInput != null)
            skillDurationInput.SetTextWithoutNotify(currentSkill.skill_duration.ToString("0.00", CultureInfo.InvariantCulture));

        // ----- Page3 : 소환 / 이펙트 / Info / Prefab -----
        if (summonMinInput != null)
            summonMinInput.SetTextWithoutNotify(currentSkill.summon_min.ToString());
        if (summonMaxInput != null)
            summonMaxInput.SetTextWithoutNotify(currentSkill.summon_max.ToString());
        if (summonTypeInput != null)
            summonTypeInput.SetTextWithoutNotify(currentSkill.summon_type.ToString());

        if (skillEff1Input != null)
            skillEff1Input.SetTextWithoutNotify(currentSkill.skill_eff1.ToString());
        if (skillEff1ValInput != null)
            skillEff1ValInput.SetTextWithoutNotify(currentSkill.skill_eff1_val.ToString("0.00", CultureInfo.InvariantCulture));
        if (skillEff1DurationInput != null)
            skillEff1DurationInput.SetTextWithoutNotify(currentSkill.skill_eff1_duration.ToString("0.00", CultureInfo.InvariantCulture));

        if (skillEff2Input != null)
            skillEff2Input.SetTextWithoutNotify(currentSkill.skill_eff2.ToString());
        if (skillEff2ValInput != null)
            skillEff2ValInput.SetTextWithoutNotify(currentSkill.skill_eff2_val.ToString("0.00", CultureInfo.InvariantCulture));
        if (skillEff2DurationInput != null)
            skillEff2DurationInput.SetTextWithoutNotify(currentSkill.skill_eff2_duration.ToString("0.00", CultureInfo.InvariantCulture));

        if (skillEff3Input != null)
            skillEff3Input.SetTextWithoutNotify(currentSkill.skill_eff3.ToString());
        if (skillEff3ValInput != null)
            skillEff3ValInput.SetTextWithoutNotify(currentSkill.skill_eff3_val.ToString("0.00", CultureInfo.InvariantCulture));
        if (skillEff3DurationInput != null)
            skillEff3DurationInput.SetTextWithoutNotify(currentSkill.skill_eff3_duration.ToString("0.00", CultureInfo.InvariantCulture));

        if (infoInput != null)
            infoInput.SetTextWithoutNotify(currentSkill.info ?? "");

        if (iconPrefabInput != null)
            iconPrefabInput.SetTextWithoutNotify(currentSkill.icon_prefab ?? "");

        if (skillPrefabInput != null)
            skillPrefabInput.SetTextWithoutNotify(currentSkill.skill_prefab ?? "");

        // projectile / hit
        if (projectileKeyInput != null)
            projectileKeyInput.SetTextWithoutNotify(currentProjectileKey ?? "");
        if (projectileKeyDropdown != null && projectileKeys != null)
        {
            int idx = projectileKeys.IndexOf(currentProjectileKey);
            if (idx >= 0)
                projectileKeyDropdown.SetValueWithoutNotify(idx);
        }

        if (hitKeyInput != null)
            hitKeyInput.SetTextWithoutNotify(currentHitKey ?? "");
        if (hitKeyDropdown != null && hitKeys != null)
        {
            int idx = hitKeys.IndexOf(currentHitKey);
            if (idx >= 0)
                hitKeyDropdown.SetValueWithoutNotify(idx);
        }

        ApplyRangeToPreview();
    }

    // ==========================
    // 공통 파서
    // ==========================

    private int ParseInt(string text, int fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        if (int.TryParse(text, out int v)) return v;
        return fallback;
    }

    private float ParseFloat(string text, float fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        text = text.Replace(',', '.');
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            return v;
        return fallback;
    }

    // ==========================
    // Page2 변경 핸들러 (기본 정보 / 스탯)
    // ==========================

    private void OnSkillNameChanged(string newText)
    {
        if (currentSkill == null) return;
        currentSkill.skill_name = newText;
        MarkCurrentSkillDirty();
    }

    // 데미지계산로직변경해야함
    // private void OnSkillAutoChanged(string newText)
    // {
    //     if (currentSkill == null) return;
    //     int val = ParseInt(newText, currentSkill.skill_auto);
    //     currentSkill.skill_auto = val;
    //     if (skillAutoInput != null)
    //         skillAutoInput.SetTextWithoutNotify(val.ToString());
    //     MarkCurrentSkillDirty();
    // }

    private void OnSkillTypeChanged(string newText)
    {
        if (currentSkill == null) return;
        int val = ParseInt(newText, currentSkill.skill_type);
        currentSkill.skill_type = val;
        if (skillTypeInput != null)
            skillTypeInput.SetTextWithoutNotify(val.ToString());
        MarkCurrentSkillDirty();
    }

    private void OnPassiveTypeDropdownChanged(int index)
    {
        if (currentSkill == null) return;
        currentSkill.passive_type = (PassiveType)index;

        // 패턴 미리보기 갱신
        UpdatePassivePatternPreview(currentSkill.passive_type);

        MarkCurrentSkillDirty();
    }

    private void OnSkillTargetChanged(string newText)
    {
        if (currentSkill == null) return;
        int val = ParseInt(newText, currentSkill.skill_target);
        currentSkill.skill_target = val;
        if (skillTargetInput != null)
            skillTargetInput.SetTextWithoutNotify(val.ToString());
        MarkCurrentSkillDirty();
    }

    private void OnSkillPierceToggleChanged(bool isOn)
    {
        if (currentSkill == null) return;
        currentSkill.skill_pierce = isOn;
        MarkCurrentSkillDirty();
    }

    // 데미지계산로직변경해야함
    // private void OnSkillDmgChanged(string newText)
    // {
    //     if (currentSkill == null) return;
    //     int val = ParseInt(newText, currentSkill.skill_dmg);
    //     currentSkill.skill_dmg = val;
    //     if (skillDmgInput != null)
    //         skillDmgInput.SetTextWithoutNotify(val.ToString());
    //     MarkCurrentSkillDirty();
    // }

    private void OnSkillCoolChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_cool);
        currentSkill.skill_cool = val;
        if (skillCoolInput != null)
            skillCoolInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    private void OnSkillSpeedChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_speed);
        currentSkill.skill_speed = val;
        if (skillSpeedInput != null)
            skillSpeedInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    // 데미지계산로직변경해야함
    // private void OnSkillCrtChanged(string newText)
    // {
    //     if (currentSkill == null) return;
    //     float val = ParseFloat(newText, currentSkill.skill_crt);
    //     currentSkill.skill_crt = val;
    //     if (skillCrtInput != null)
    //         skillCrtInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
    //     MarkCurrentSkillDirty();
    // }

    private void OnRangeInputChanged(string newText)
    {
        if (currentSkill == null) return;

        float val = ParseFloat(newText, currentSkill.skill_range);
        if (val < 0f) val = 0f;

        currentSkill.skill_range = val;

        if (rangeInput != null)
            rangeInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));

        if (rangeSlider != null)
        {
            float clamped = Mathf.Clamp(val, rangeSlider.minValue, rangeSlider.maxValue);
            rangeSlider.SetValueWithoutNotify(clamped);
        }

        Debug.Log($"[SkillTest] range 변경(입력): {currentSkill.skill_name} -> {val}");

        ApplyRangeToPreview();
        MarkCurrentSkillDirty();
    }

    private void OnRangeSliderChanged(float value)
    {
        if (currentSkill == null) return;

        currentSkill.skill_range = value;

        if (rangeInput != null)
            rangeInput.SetTextWithoutNotify(value.ToString("0.00", CultureInfo.InvariantCulture));

        Debug.Log($"[SkillTest] range 변경(슬라이더): {currentSkill.skill_name} -> {value}");

        ApplyRangeToPreview();
        MarkCurrentSkillDirty();
    }

    private void OnSkillDurationChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_duration);
        currentSkill.skill_duration = val;
        if (skillDurationInput != null)
            skillDurationInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    private void ApplyRangeToPreview()
    {
        if (currentPreviewInstance == null || currentSkill == null)
            return;

        float r = Mathf.Max(0.01f, currentSkill.skill_range);

        var circle = currentPreviewInstance.GetComponentInChildren<CircleCollider2D>();
        if (circle != null)
        {
            circle.radius = r;
        }

        currentPreviewInstance.transform.localScale = Vector3.one * r;
    }

    // ==========================
    // Page3 변경 핸들러 (소환 / 이펙트 / Info / Prefab)
    // ==========================

    private void OnSummonMinChanged(string newText)
    {
        if (currentSkill == null) return;
        int val = ParseInt(newText, currentSkill.summon_min);
        currentSkill.summon_min = val;
        if (summonMinInput != null)
            summonMinInput.SetTextWithoutNotify(val.ToString());
        MarkCurrentSkillDirty();
    }

    private void OnSummonMaxChanged(string newText)
    {
        if (currentSkill == null) return;
        int val = ParseInt(newText, currentSkill.summon_max);
        currentSkill.summon_max = val;
        if (summonMaxInput != null)
            summonMaxInput.SetTextWithoutNotify(val.ToString());
        MarkCurrentSkillDirty();
    }

    private void OnSummonTypeChanged(string newText)
    {
        if (currentSkill == null) return;
        int val = ParseInt(newText, currentSkill.summon_type);
        currentSkill.summon_type = val;
        if (summonTypeInput != null)
            summonTypeInput.SetTextWithoutNotify(val.ToString());
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff1Changed(string newText)
    {
        if (currentSkill == null) return;
        int val = ParseInt(newText, currentSkill.skill_eff1);
        currentSkill.skill_eff1 = val;
        if (skillEff1Input != null)
            skillEff1Input.SetTextWithoutNotify(val.ToString());
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff1ValChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_eff1_val);
        currentSkill.skill_eff1_val = val;
        if (skillEff1ValInput != null)
            skillEff1ValInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff1DurationChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_eff1_duration);
        currentSkill.skill_eff1_duration = val;
        if (skillEff1DurationInput != null)
            skillEff1DurationInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff2Changed(string newText)
    {
        if (currentSkill == null) return;
        int val = ParseInt(newText, currentSkill.skill_eff2);
        currentSkill.skill_eff2 = val;
        if (skillEff2Input != null)
            skillEff2Input.SetTextWithoutNotify(val.ToString());
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff2ValChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_eff2_val);
        currentSkill.skill_eff2_val = val;
        if (skillEff2ValInput != null)
            skillEff2ValInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff2DurationChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_eff2_duration);
        currentSkill.skill_eff2_duration = val;
        if (skillEff2DurationInput != null)
            skillEff2DurationInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff3Changed(string newText)
    {
        if (currentSkill == null) return;
        int val = ParseInt(newText, currentSkill.skill_eff3);
        currentSkill.skill_eff3 = val;
        if (skillEff3Input != null)
            skillEff3Input.SetTextWithoutNotify(val.ToString());
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff3ValChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_eff3_val);
        currentSkill.skill_eff3_val = val;
        if (skillEff3ValInput != null)
            skillEff3ValInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    private void OnSkillEff3DurationChanged(string newText)
    {
        if (currentSkill == null) return;
        float val = ParseFloat(newText, currentSkill.skill_eff3_duration);
        currentSkill.skill_eff3_duration = val;
        if (skillEff3DurationInput != null)
            skillEff3DurationInput.SetTextWithoutNotify(val.ToString("0.00", CultureInfo.InvariantCulture));
        MarkCurrentSkillDirty();
    }

    private void OnInfoChanged(string newText)
    {
        if (currentSkill == null) return;
        currentSkill.info = newText;
        MarkCurrentSkillDirty();
    }

    private void OnIconPrefabChanged(string newText)
    {
        if (currentSkill == null) return;
        currentSkill.icon_prefab = newText;
        MarkCurrentSkillDirty();
    }

    private void OnSkillPrefabChanged(string newText)
    {
        if (currentSkill == null) return;
        currentSkill.skill_prefab = newText;
        MarkCurrentSkillDirty();
    }

    // ==========================
    // Projectile / Hit 프리뷰 & 적용
    // ==========================

    private void OnProjectileKeyDropdownChanged(int index)
    {
        if (projectileKeys == null ||
            index < 0 || index >= projectileKeys.Count)
            return;

        currentProjectileKey = projectileKeys[index];

        if (projectileKeyInput != null)
            projectileKeyInput.SetTextWithoutNotify(currentProjectileKey);

        Debug.Log($"[SkillTest] projectile 키 선택: {currentProjectileKey}");
    }

    private void OnProjectileKeyInputChanged(string newText)
    {
        currentProjectileKey = newText;
        Debug.Log($"[SkillTest] projectile 키 입력 변경: {currentProjectileKey}");
    }

    private void OnHitKeyDropdownChanged(int index)
    {
        if (hitKeys == null ||
            index < 0 || index >= hitKeys.Count)
            return;

        currentHitKey = hitKeys[index];

        if (hitKeyInput != null)
            hitKeyInput.SetTextWithoutNotify(currentHitKey);

        Debug.Log($"[SkillTest] hit 키 선택: {currentHitKey}");
    }

    private void OnHitKeyInputChanged(string newText)
    {
        currentHitKey = newText;
        Debug.Log($"[SkillTest] hit 키 입력 변경: {currentHitKey}");
    }

    private void OnProjectilePreviewClicked()
    {
        currentProjectileKey = (currentProjectileKey ?? "").Trim();

        if (string.IsNullOrEmpty(currentProjectileKey))
        {
            Debug.LogWarning("[SkillTest] projectile 키가 비어있습니다.");
            return;
        }

        if (previewPoint == null)
        {
            Debug.LogWarning("[SkillTest] previewPoint 가 설정되어 있지 않습니다.");
            return;
        }

        ReleasePreview();

        Debug.Log($"[SkillTest] projectile 프리뷰 시도: {currentProjectileKey}");

        var handle = Addressables.InstantiateAsync(
            currentProjectileKey,
            previewPoint.position,
            Quaternion.identity
        );

        hasHandle = true;
        currentHandle = handle;

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                currentPreviewInstance = op.Result;
                ApplyUnscaled(currentPreviewInstance);
                Debug.Log($"[SkillTest] projectile 프리뷰 생성 성공: {currentProjectileKey}");
                ApplyRangeToPreview();
            }
            else
            {
                Debug.LogError($"[SkillTest] projectile 프리뷰 생성 실패: {currentProjectileKey}");
                if (op.OperationException != null)
                    Debug.LogError(op.OperationException);
            }
        };
    }

    private void OnHitPreviewClicked()
    {
        currentHitKey = (currentHitKey ?? "").Trim();

        if (string.IsNullOrEmpty(currentHitKey))
        {
            Debug.LogWarning("[SkillTest] hit 키가 비어있습니다.");
            return;
        }

        if (previewPoint == null)
        {
            Debug.LogWarning("[SkillTest] previewPoint 가 설정되어 있지 않습니다.");
            return;
        }

        ReleasePreview();

        Debug.Log($"[SkillTest] hit 프리뷰 시도: {currentHitKey}");

        var handle = Addressables.InstantiateAsync(
            currentHitKey,
            previewPoint.position,
            Quaternion.identity
        );

        hasHandle = true;
        currentHandle = handle;

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                currentPreviewInstance = op.Result;
                Debug.Log($"[SkillTest] hit 프리뷰 생성 성공: {currentHitKey}");
                ApplyRangeToPreview();
            }
            else
            {
                Debug.LogError($"[SkillTest] hit 프리뷰 생성 실패: {currentHitKey}");
                if (op.OperationException != null)
                    Debug.LogError(op.OperationException);
            }
        };
    }

    private void OnProjectileApplyClicked()
    {
        if (currentSkill == null)
        {
            Debug.LogWarning("[SkillTest] 선택된 스킬이 없습니다.");
            return;
        }

        currentSkill.skillprojectile_prefab = currentProjectileKey;
        Debug.Log($"[SkillTest] {currentSkill.skill_name} 의 skillprojectile_prefab = '{currentProjectileKey}'");

        MarkCurrentSkillDirty();
    }

    private void OnHitApplyClicked()
    {
        if (currentSkill == null)
        {
            Debug.LogWarning("[SkillTest] 선택된 스킬이 없습니다.");
            return;
        }

        currentSkill.skillhit_prefab = currentHitKey;
        Debug.Log($"[SkillTest] {currentSkill.skill_name} 의 skillhit_prefab = '{currentHitKey}'");

        MarkCurrentSkillDirty();
    }

    private void ReleasePreview()
    {
        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
        }

        if (hasHandle)
        {
            Addressables.Release(currentHandle);
            hasHandle = false;
        }
    }

    private void ApplyUnscaled(GameObject go)
    {
        if (go == null) return;

        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.useUnscaledTime = true;
        }

        foreach (var anim in go.GetComponentsInChildren<Animator>(true))
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    // ==========================
    // 페이지 전환 (Panel 1 / 2 / 3)
    // ==========================

    private void ShowPage(int page)
    {
        currentPage = page;

        if (page1Root != null)
            page1Root.SetActive(page == 1);
        if (page2Root != null)
            page2Root.SetActive(page == 2);
        if (page3Root != null)
            page3Root.SetActive(page == 3);

        if (page1Button != null)
            page1Button.interactable = page != 1;
        if (page2Button != null)
            page2Button.interactable = page != 2;
        if (page3Button != null)
            page3Button.interactable = page != 3;
    }

    // ==========================
    // PassiveType 패턴 미리보기
    // ==========================

    private void InitPassivePatternPreview()
    {
        if (passiveTiles == null || passiveTiles.Length == 0)
            return;

        PassiveType type = PassiveType.None;
        if (currentSkill != null)
            type = currentSkill.passive_type;

        UpdatePassivePatternPreview(type);
    }

    private void UpdatePassivePatternPreview(PassiveType type)
    {
        // Type.None이면 패널 숨김
        if (passivePatternRoot != null)
            passivePatternRoot.SetActive(type != PassiveType.None);

        if (type == PassiveType.None)
            return;

        if (passiveTiles == null || passiveTiles.Length == 0)
            return;

        // 1) 전부 기본 색 + 중앙 칸 centerColor
        for (int i = 0; i < passiveTiles.Length; i++)
        {
            if (passiveTiles[i] == null) continue;

            if (i == PassiveCenterIndex)
                passiveTiles[i].color = passiveCenterColor;
            else
                passiveTiles[i].color = passiveDefaultColor;
        }

        // 2) PassivePatternUtil로 실제 버프 들어가는 타일들 색칠
        foreach (int idx in PassivePatternUtil.GetPatternTiles(PassiveCenterIndex, type, PassiveTileCount))
        {
            if (idx < 0 || idx >= passiveTiles.Length)
                continue;

            var img = passiveTiles[idx];
            if (img == null) continue;

            if (idx == PassiveCenterIndex)
                img.color = passiveCenterColor;
            else
                img.color = passiveActiveColor;
        }
    }

    // ==========================
    // 공통 (에디터용 Dirty 표시)
    // ==========================

    private void MarkCurrentSkillDirty()
    {
#if UNITY_EDITOR
        if (currentSkill != null)
            UnityEditor.EditorUtility.SetDirty(currentSkill);
#endif
    }
}
