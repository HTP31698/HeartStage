using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageSetupWindow : MonoBehaviour
{
    public static System.Action OnStageStarted; // 이벤트

    //드래그 슬롯들
    public DraggableSlot[] DraggableSlots;
    //스테이지 자리 : (인덱스, 캐릭터ID)
    public Dictionary<int, int> StageIndexs;
    //스폰 자리
    public GameObject[] SpawnPos;
    public Button StartButton;
    [Header("돌아가기 버튼")]
    public Button BackButton;
    [Header("전체 초기화 버튼")]
    public Button ResetAllButton;
    public GameObject basePrefab;

    // 캐릭터 펜스 (싱글톤으로 사용)
    public CharacterFence fence;

    //패시브 타입 보여줄 이미지 바닥 색변경
    public Image[] PassiveImages;

    // 이번 배치에서 "패시브 바닥으로 판정된 타일들"
    private bool[] _passiveTiles;

    // 🔹 타일별 패시브 중첩 개수 (1,2,3...)
    private int[] _passiveStackCounts;

    //미리보기 오버레이
    private bool[] _previewPassiveTiles;

    //이 타일에 어떤 효과가 있는지
    private struct PassiveEffectData
    {
        public int effectId;
        public float value;

        public PassiveEffectData(int id, float v)
        {
            effectId = id;
            value = v;
        }
    }
    // 타일 인덱스 → 그 타일에 쌓인 모든 패시브 효과 리스트
    private Dictionary<int, List<PassiveEffectData>> PassiveIndexs;

    // 스테이지 데이터 적용
    private bool[] _enabledMask;

    //최대 배치 유닛 수
    private int _maxDeployUnits;
    [SerializeField] private TMPro.TextMeshProUGUI deployCountText; // 있으면 연결
    [SerializeField] private Color deployOkColor = Color.white;
    [SerializeField] private Color deployFullColor = Color.red;

    [Header("Passive Tile Colors")]
    [SerializeField] private Color passiveTileColor = new Color(1f, 165f / 255f, 0f); // 주황 느낌
    [SerializeField] private Color normalTileColor = Color.white;

    // 🔹 중첩 개수에 따른 색
    [SerializeField] private Color stack2Color = Color.green;       // 2중첩: 초록
    [SerializeField] private Color stack3Color = Color.blue;        // 3중첩: 파랑
    [SerializeField] private Color stack4Color = Color.yellow;      // 4중첩: 노랑
    [SerializeField] private Color stack5Color = Color.red;         // 5이상: 빨강

    [SerializeField] private Color previewColor = Color.cyan;       // 미리보기 색

    [SerializeField] private SynergyPanel synergyPanel;

    //스폰 캐릭터 리스트 
    private readonly List<GameObject> _spawnedAllies = new();

    // 준비 완료 플래그
    public bool IsReady { get; private set; }

    // 🔹 이 컴포넌트가 담당할 전역 프로그레스 구간 (85% ~ 99%)
    private const float GlobalStart = 0.85f;
    private const float GlobalEnd = 0.99f;

    private void ReportStageProgress(float local01)
    {
        float clamped = Mathf.Clamp01(local01);
        float global = Mathf.Lerp(GlobalStart, GlobalEnd, clamped);
        SceneLoader.SetProgressExternal(global);
    }

    private async void Start()
    {
        IsReady = false;
        ReportStageProgress(0.0f);

        StageIndexs = new Dictionary<int, int>();
        PassiveIndexs = new Dictionary<int, List<PassiveEffectData>>();

        if (DraggableSlots != null)
        {
            int len = DraggableSlots.Length;
            _passiveTiles = new bool[len];
            _passiveStackCounts = new int[len];

            for (int i = 0; i < len; i++)
                if (DraggableSlots[i] != null)
                    DraggableSlots[i].slotIndex = i;
        }

        ReportStageProgress(0.1f);

        //Time.timeScale = 0f;
        StartButton.onClick.AddListener(StartButtonClick);
        if (BackButton != null)
            BackButton.onClick.AddListener(BackButtonClick);
        if (ResetAllButton != null)
            ResetAllButton.onClick.AddListener(ResetAllButtonClick);

        // 데이터 준비 + 스테이지 적용
        await WaitAndApplyStage();

        ReportStageProgress(0.6f);

        if (synergyPanel != null)
            synergyPanel.BuildAllButtons();

        ReportStageProgress(0.8f);

        DraggableSlot.OnAnySlotChanged += HandleSlotChanged;

        // 초기 시너지/배치 카운트도 한 번 갱신
        UpdateSynergyUI();
        UpdateDeployCountUI();

        ReportStageProgress(0.9f);
        IsReady = true;
    }

    private async UniTask WaitAndApplyStage()
    {
        ReportStageProgress(0.2f);

        // StageManager & currentStageCSVData 준비될 때까지
        while (StageManager.Instance == null || StageManager.Instance.GetCurrentStageData() == null)
        {
            await UniTask.Delay(10, DelayType.UnscaledDeltaTime);
        }

        ReportStageProgress(0.4f);

        var stageCsv = StageManager.Instance.GetCurrentStageData();
        ApplyStage(stageCsv);

        // 혹시라도 색/카운트 바로 보이게 강제 갱신
        RebuildPassiveTiles();
        UpdateDeployCountUI();
    }

    private void OnDisable()
    {
        StartButton.onClick.RemoveListener(StartButtonClick);
        if (BackButton != null)
            BackButton.onClick.RemoveListener(BackButtonClick);
        if (ResetAllButton != null)
            ResetAllButton.onClick.RemoveListener(ResetAllButtonClick);
        DraggableSlot.OnAnySlotChanged -= HandleSlotChanged;
    }

    private void BackButtonClick()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Exit_Button_Click);

        // 돌아가기 플래그 설정 (로비에서 StageInfoWindow 자동 오픈용)
        SaveLoadManager.Data.returnToStageInfo = true;

        // 타임스케일 복원 후 로비로 이동
        Time.timeScale = 1f;
        LoadSceneManager.Instance.GoLobby();
    }

    /// <summary>
    /// 배치된 모든 캐릭터를 슬롯에서 내림
    /// </summary>
    private void ResetAllButtonClick()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        if (DraggableSlots == null) return;

        for (int i = 0; i < DraggableSlots.Length; i++)
        {
            var slot = DraggableSlots[i];
            if (slot == null || slot.characterData == null)
                continue;

            // 슬롯 비우고 DragMe 잠금 해제
            slot.ClearSlotAndUnlockSource(slot.characterData);
        }

        // UI 갱신
        RebuildPassiveTiles();
        UpdateSynergyUI();
        UpdateDeployCountUI();
    }

    private Dictionary<int, int> GetStagePos()
    {
        StageIndexs.Clear();
        PassiveIndexs.Clear();

        int slotCount = DraggableSlots.Length;

        for (int i = 0; i < slotCount; i++)
        {
            if (_enabledMask != null && !_enabledMask[i])
                continue;

            var slot = DraggableSlots[i];
            if (slot == null || slot.characterData == null)
                continue;

            var cd = slot.characterData;
            StageIndexs[i] = cd.char_id;

            var data = DataTableManager.SkillTable.Get(cd.skill_id1);
            PassiveType passiveType = (PassiveType)data.passive_type;

            if (passiveType == PassiveType.None)
                continue;

            // 🔹 이 캐릭터가 영향을 미치는 모든 타일에 대해
            foreach (int tileIndex in PassivePatternUtil.GetPatternTiles(i, passiveType, slotCount))
            {
                if (_enabledMask != null && !_enabledMask[tileIndex])
                    continue;
                if (!PassiveIndexs.TryGetValue(tileIndex, out var list))
                {
                    list = new List<PassiveEffectData>();
                    PassiveIndexs[tileIndex] = list;
                }

                // skill_eff1 ~ 3이 0이 아니면 각각 효과로 추가
                if (data.skill_eff1 != 0)
                    list.Add(new PassiveEffectData(data.skill_eff1, data.skill_eff1_val));

                if (data.skill_eff2 != 0)
                    list.Add(new PassiveEffectData(data.skill_eff2, data.skill_eff2_val));

                if (data.skill_eff3 != 0)
                    list.Add(new PassiveEffectData(data.skill_eff3, data.skill_eff3_val));
            }
        }

        return StageIndexs;
    }

    private void StartButtonClick()
    {
        if (_maxDeployUnits > 0 && GetCurrentDeployCount() > _maxDeployUnits)
        {
            Debug.LogWarning($"[StageSetupWindow] Deploy limit exceeded! cur={GetCurrentDeployCount()} max={_maxDeployUnits}");
            return;
        }
        if (GetCurrentDeployCount() == 0)
        {
            Debug.LogWarning("[StageSetupWindow] No units deployed!");
            return;
        }

        RebuildPassiveTiles();

        GetStagePos();

        var allies = PlaceAll();

        SynergyManager.ApplySynergies(DraggableSlots, allies);

        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);
        StageManager.Instance.SetTimeScale(1f);

        OnStageStarted?.Invoke(); // 이벤트 발생

        gameObject.SetActive(false);
    }

    private List<GameObject> PlaceAll()
    {
        DespawnAllAllies();

        var allies = new List<GameObject>();

        foreach (var kvp in StageIndexs)
        {
            int slotIndex = kvp.Key;
            int characterId = kvp.Value;

            Vector3 spawnPosition = SpawnPos[slotIndex].transform.position;
            var obj = PlaceCharacter(characterId, spawnPosition, slotIndex);
            allies.Add(obj);
        }

        return allies;
    }

    private GameObject PlaceCharacter(int characterId, Vector3 worldPos, int slotIndex)
    {
        GameObject obj = Instantiate(basePrefab, worldPos, Quaternion.identity);
        var attack = obj.GetComponent<CharacterAttack>();

        _spawnedAllies.Add(obj);   // 스폰 리스트에 등록

        AddPassiveEffects(obj, slotIndex);

        attack.id = characterId;

        CharacterFence.Instance.Init();

        return obj;
    }

    private void AddPassiveEffects(GameObject obj, int slotIndex)
    {
        // 이 타일에 쌓인 패시브가 없으면 끝
        if (!PassiveIndexs.TryGetValue(slotIndex, out var effects) || effects.Count == 0)
        {
            Debug.Log($"[AddPassiveEffects] slot {slotIndex} 적용할 패시브 없음");
            return;
        }

        foreach (var e in effects)
        {
            EffectRegistry.Apply(obj, e.effectId, e.value, 99999f);
        }
    }

    private void ResetPassiveTiles()
    {
        if (DraggableSlots == null) return;

        int len = DraggableSlots.Length;

        if (_passiveTiles == null || _passiveTiles.Length != len)
            _passiveTiles = new bool[len];
        if (_passiveStackCounts == null || _passiveStackCounts.Length != len)
            _passiveStackCounts = new int[len];

        Array.Clear(_passiveTiles, 0, len);
        Array.Clear(_passiveStackCounts, 0, len);

        // 색은 ApplyTileColors에서 처리
    }

    /// 현재 DraggableSlots 상태 + 각 캐릭터의 PassiveType을 기준으로
    /// 바닥 패시브 타일(_passiveTiles) 계산 + 색칠
    private void RebuildPassiveTiles()
    {
        ResetPassiveTiles();
        if (DraggableSlots == null) return;

        int slotCount = DraggableSlots.Length;

        // 0~14 슬롯 돌면서
        for (int i = 0; i < slotCount; i++)
        {
            if (_enabledMask != null && !_enabledMask[i])
                continue;

            var slot = DraggableSlots[i];
            if (slot == null || slot.characterData == null)
                continue;

            var cd = slot.characterData;
            var skill = DataTableManager.SkillTable.Get(cd.skill_id1);

            PassiveType passiveType = (PassiveType)skill.passive_type; // skill.passive_type이 int라고 가정

            if (passiveType == PassiveType.None)
                continue;

            // 기준칸 = i, 패턴 오프셋 적용
            foreach (int idx in PassivePatternUtil.GetPatternTiles(i, passiveType, slotCount))
            {
                if (_enabledMask != null && !_enabledMask[idx])
                    continue;
                _passiveTiles[idx] = true;
                _passiveStackCounts[idx]++;   // 🔹 중첩 개수 누적
            }
        }

        ApplyTileColors();
    }

    // 🔹 중첩 개수 → 색 변환
    private Color GetColorByStackCount(int stack)
    {
        if (stack <= 0) return normalTileColor;

        switch (stack)
        {
            case 1: return passiveTileColor; // 주황
            case 2: return stack2Color;      // 초록
            case 3: return stack3Color;      // 파랑
            case 4: return stack4Color;      // 노랑
            default: return stack5Color;     // 5 이상 빨강
        }
    }

    // 🔹 실제 바닥 타일 색을 중첩 개수에 맞게 반영
    private void ApplyTileColors()
    {
        if (PassiveImages == null || _passiveStackCounts == null) return;

        int len = Mathf.Min(PassiveImages.Length, _passiveStackCounts.Length);
        for (int i = 0; i < len; i++)
        {
            var img = PassiveImages[i];
            if (img == null) continue;

            int stack = _passiveStackCounts[i];
            img.color = GetColorByStackCount(stack);
        }
    }

    public void ShowPassivePreview(int slotIndex, CharacterData cd)
    {
        if (cd == null) return;
        if (DraggableSlots == null || PassiveImages == null) return;

        int slotCount = DraggableSlots.Length;

        if (_previewPassiveTiles == null || _previewPassiveTiles.Length != slotCount)
            _previewPassiveTiles = new bool[slotCount];

        Array.Clear(_previewPassiveTiles, 0, _previewPassiveTiles.Length);

        var skill = DataTableManager.SkillTable.Get(cd.skill_id1);
        PassiveType type = (PassiveType)skill.passive_type;

        if (type == PassiveType.None) return;

        foreach (int idx in PassivePatternUtil.GetPatternTiles(slotIndex, type, slotCount))
        {
            if (_enabledMask != null && !_enabledMask[idx])
                continue;
            if (idx >= 0 && idx < _previewPassiveTiles.Length)
                _previewPassiveTiles[idx] = true;
        }

        // 미리보기 색 적용 (겹치면 preview가 우선)
        int len2 = Mathf.Min(PassiveImages.Length, _passiveStackCounts != null ? _passiveStackCounts.Length : PassiveImages.Length);
        for (int i = 0; i < len2; i++)
        {
            var img = PassiveImages[i];
            if (img == null) continue;

            bool isPreview = _previewPassiveTiles[i];

            if (isPreview)
                img.color = previewColor; // 미리보기 색
            else
                img.color = GetColorByStackCount(
                    (_passiveStackCounts != null && i < _passiveStackCounts.Length)
                        ? _passiveStackCounts[i] : 0);
        }
    }

    public void ClearPassivePreview()
    {
        _previewPassiveTiles = null;
        ApplyTileColors();
    }

    // 테스트 함수 (바로 시작 버튼) 고치기
    public void TestStart()
    {
        DraggableSlots[1].characterData = ResourceManager.Instance.Get<CharacterData>("hina21");
        DraggableSlots[2].characterData = ResourceManager.Instance.Get<CharacterData>("jian21");
        DraggableSlots[3].characterData = ResourceManager.Instance.Get<CharacterData>("sera21");
        DraggableSlots[6].characterData = ResourceManager.Instance.Get<CharacterData>("lia21");
        StartButtonClick();
    }

    private void HandleSlotChanged()
    {
        // 1) 패시브 타일 다시 계산 + 색칠
        RebuildPassiveTiles();

        // 2) 시너지 UI 갱신
        UpdateSynergyUI();

        // 3) 배치 수 UI 갱신
        UpdateDeployCountUI();
    }

    private void UpdateSynergyUI()
    {
        if (synergyPanel == null) return;
        var actives = SynergyManager.Evaluate(DraggableSlots);
        synergyPanel.UpdateActiveSynergies(actives);
    }

    // 스테이지 타일 관련
    public void ApplyStage(StageData stage)
    {
        // 1) stage_type -> mask
        _enabledMask = StageLayoutUtil.BuildMask(stage.stage_type);

        // 2) max deploy units (dispatch_member 우선)
        _maxDeployUnits = stage.dispatch_member > 0 ? stage.dispatch_member : stage.member_count;

        // 3) 슬롯/바닥 UI 비활성화
        for (int i = 0; i < DraggableSlots.Length; i++)
        {
            bool enabled = _enabledMask[i];
            var slot = DraggableSlots[i];
            if (slot == null) continue;

            // 비활성 타일은 데이터 제거(숨은 버프 소스 방지)
            if (!enabled)
                slot.characterData = null;

            // 슬롯 자체를 꺼서 드롭/클릭 막기
            slot.gameObject.SetActive(enabled);

            // 바닥 이미지도 동일
            if (PassiveImages != null && i < PassiveImages.Length && PassiveImages[i] != null)
                PassiveImages[i].gameObject.SetActive(enabled);
        }

        // 4) 마스크 반영 후 패시브/시너지 계산
        RebuildPassiveTiles();
        UpdateSynergyUI();
    }

    public void ApplyStage(StageCSVData stage)
    {
        if (stage == null)
        {
            Debug.LogWarning("[StageSetupWindow] ApplyStage called with null StageCSVData");
            return;
        }

        // stage_type -> mask
        _enabledMask = StageLayoutUtil.BuildMask(stage.stage_type);

        // 배치 가능 명수 (dispatch_member 우선)
        _maxDeployUnits = stage.dispatch_member > 0
            ? stage.dispatch_member
            : stage.member_count;

        // 비활성 타일 처리(SetActive false 방식)
        for (int i = 0; i < DraggableSlots.Length; i++)
        {
            bool enabled = _enabledMask[i];
            var slot = DraggableSlots[i];
            if (slot == null) continue;

            if (!enabled)
                slot.characterData = null; // 숨은 소스 방지

            slot.gameObject.SetActive(enabled);

            if (PassiveImages != null && i < PassiveImages.Length && PassiveImages[i] != null)
                PassiveImages[i].gameObject.SetActive(enabled);
        }

        RebuildPassiveTiles();
        UpdateSynergyUI();
        UpdateDeployCountUI();
    }

    public int GetCurrentDeployCount()
    {
        if (DraggableSlots == null) return 0;

        int count = 0;
        for (int i = 0; i < DraggableSlots.Length; i++)
        {
            if (_enabledMask != null && !_enabledMask[i]) continue;

            var slot = DraggableSlots[i];
            if (slot != null && slot.characterData != null)
                count++;
        }
        return count;
    }

    public bool IsDeployLimitReached()
    {
        if (_maxDeployUnits <= 0)
            return false; // 0이면 제한 없음으로 처리

        return GetCurrentDeployCount() >= _maxDeployUnits;
    }

    private void UpdateDeployCountUI()
    {
        if (deployCountText == null) return;

        int cur = GetCurrentDeployCount();
        int max = _maxDeployUnits;

        deployCountText.text = $"{cur} / {max}";
        deployCountText.color = (max > 0 && cur >= max) ? deployFullColor : deployOkColor;
    }

    public void DespawnAllAllies()
    {
        for (int i = _spawnedAllies.Count - 1; i >= 0; i--)
        {
            var go = _spawnedAllies[i];

            if (go != null && !go.Equals(null))
                Destroy(go);

            _spawnedAllies.RemoveAt(i);
        }
    }
}
