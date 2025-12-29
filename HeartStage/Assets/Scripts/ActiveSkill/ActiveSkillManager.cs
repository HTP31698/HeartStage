using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActiveSkillManager : MonoBehaviour
{
    public static ActiveSkillManager Instance;

    private Dictionary<int, SkillData> skillDB = new Dictionary<int, SkillData>();
    private Dictionary<GameObject, Dictionary<int, ISkillBehavior>> skillBehaviors = new Dictionary<GameObject, Dictionary<int, ISkillBehavior>>();
    private List<ActiveSkillTimer> activeTimers = new List<ActiveSkillTimer>();

    [SerializeField] private Slider cooldownSliderPrefab;
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Vector3 uiWorldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private ActiveSkillDesc activeSkillDesc;

    public static System.Action OnAnySkillReady; // 스킬 준비 알림용 이벤트

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        skillDB = DataTableManager.SkillTable.GetAll();
    }

    private void Update()
    {
        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            var timer = activeTimers[i];

            if (timer == null || timer.Caster == null)
            {
                if (timer != null) timer.Dispose();
                activeTimers.RemoveAt(i);
                continue;
            }

            // 스킬 준비 상태 변화 감지
            bool wasReady = timer.WasReadyLastFrame;
            timer.UpdateTimer(Time.deltaTime);

            // 스킬이 새로 준비되었을 때 이벤트 발생
            if (!wasReady && timer.IsReady)
            {
                OnAnySkillReady?.Invoke();
            }
        }
    }

    // 수동 스킬 발동 시 호출
    public void TryUseSkill(GameObject caster, int skillId)
    {
        var timer = activeTimers.Find(t => t.Caster == caster && t.SkillData.skill_id == skillId);
        if (timer == null)
            return;

        if (timer.IsReady)
        {
            UseSkill(timer);      // 스킬 실행
            timer.StartCooldown(); // 쿨타임 UI 다시 보이고 재시작
        }
    }

    private void UseSkill(ActiveSkillTimer timer)
    {
        var skillId = timer.SkillData.skill_id;

        if (skillBehaviors.TryGetValue(timer.Caster, out var skillDict) &&
            skillDict.TryGetValue(skillId, out var behavior))
        {
            behavior.Execute();
        }
    }

    public void RegisterSkill(GameObject caster, int skillId)
    {
        if (skillDB.TryGetValue(skillId, out var data))
        {
            var existing = activeTimers.Find(t => t.Caster == caster && t.SkillData.skill_id == skillId);
            if (existing != null)
            {
                existing.Reset();
                return;
            }

            var timer = new ActiveSkillTimer(caster, data, cooldownSliderPrefab, mainCanvas, uiWorldOffset);
            activeTimers.Add(timer);
        }
    }

    public void UnRegisterSkill(GameObject caster, int skillId)
    {
        var timer = activeTimers.Find(t => t.Caster == caster && t.SkillData.skill_id == skillId);
        if (timer != null)
        {
            timer.Dispose();
            activeTimers.Remove(timer);
        }

        if (skillBehaviors.TryGetValue(caster, out var skillDict))
        {
            skillDict.Remove(skillId);
        }
    }

    public void RegisterSkillBehavior(GameObject caster, int skillId, ISkillBehavior behavior)
    {
        if (!skillBehaviors.TryGetValue(caster, out var dict))
        {
            dict = new Dictionary<int, ISkillBehavior>();
            skillBehaviors.Add(caster, dict);
        }

        if (!dict.ContainsKey(skillId))
        {
            dict.Add(skillId, behavior);
        }
    }

    // 스킬 설명창 띄우기
    public void ShowDesc(int skillId)
    {
        activeSkillDesc.ShowDesc(skillId);
    }

    // 스킬 설명창 닫기
    public void CloseDesc()
    {
        activeSkillDesc.gameObject.SetActive(false);
    }

    // 특정 캐릭터의 스킬이 준비되었는지 확인
    public bool IsSkillReady(GameObject caster, int skillId)
    {
        var timer = activeTimers.Find(t => t.Caster == caster && t.SkillData.skill_id == skillId);
        return timer?.IsReady ?? false;
    }

    // 스킬이 준비된 모든 캐릭터 목록 반환
    public List<GameObject> GetAllReadySkillCasters()
    {
        var readyCasters = new List<GameObject>();
        foreach (var timer in activeTimers)
        {
            if (timer.IsReady)
            {
                readyCasters.Add(timer.Caster);
            }
        }
        return readyCasters;
    }

    // 특정 캐릭터가 등록한 스킬 ID 반환
    public int GetCharacterSkillId(GameObject caster)
    {
        var timer = activeTimers.Find(t => t.Caster == caster);
        return timer?.SkillData.skill_id ?? -1;
    }
}

// 타이머 클래스
public class ActiveSkillTimer
{
    private float currentTime;
    private Vector3 offset;
    private bool wasReadyLastFrame; // 이전 프레임의 준비 상태를 추적

    public SkillData SkillData { get; private set; }
    public GameObject Caster { get; private set; }
    public CooldownUIHandler UI { get; private set; }

    public bool IsReady => currentTime <= 0f;
    public bool WasReadyLastFrame => wasReadyLastFrame; // 이전 프레임 준비 상태 반환

    public ActiveSkillTimer(GameObject caster, SkillData data, Slider prefab, Canvas canvas, Vector3 uiOffset)
    {
        Caster = caster;
        SkillData = data;
        currentTime = SkillData.skill_cool;
        wasReadyLastFrame = false; // 초기에는 준비되지 않은 상태
        offset = uiOffset;

        UI = new CooldownUIHandler(caster, prefab, canvas, offset);
        UI.InitMaxValue(data.skill_cool);
        UI.Show();
        UI.UpdateUI(currentTime);
    }

    public void StartCooldown()
    {
        currentTime = SkillData.skill_cool;
        wasReadyLastFrame = false; // 쿨다운 시작 시 준비되지 않은 상태로 설정
        UI.Show();
        UI.ResetSlider();
    }

    public void UpdateTimer(float delta)
    {
        bool wasReadyBefore = IsReady;  // 업데이트 전 상태 저장

        if (!IsReady)
        {
            float speedFactor = 1f;
            // 피버타임 체크
            if (StageManager.Instance.isFever)
            {
                float denom = 1f - Mathf.Clamp(StageManager.Instance.feverValue, 0f, 0.99f);
                speedFactor = 1f / denom;
            }
            currentTime -= delta * speedFactor;
            UI.UpdateUI(currentTime);

            if (IsReady)
            {
                currentTime = 0;
                UI.Hide();  // 다 차면 숨김

                var skillController = Caster.GetComponent<CharacterSkillController>();
                skillController?.SkillReady();
            }
        }

        wasReadyLastFrame = wasReadyBefore;  // 이전 상태를 다음 프레임을 위해 저장
    }

    public void Reset()
    {
        currentTime = 0;
        wasReadyLastFrame = true; // 리셋 시 준비 완료 상태로 설정
        UI.Hide();
    }

    public void Dispose()
    {
        UI.Dispose();
    }
}