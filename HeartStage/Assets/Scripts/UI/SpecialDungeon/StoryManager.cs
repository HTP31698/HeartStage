using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StoryManager : MonoBehaviour
{
    [SerializeField] private Button autoButton;
    [SerializeField] private Button skipButton;

    [SerializeField] private Image cutSceneImage;
    [SerializeField] private Image scriptBackGround;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scriptText;

    private float textSpeed = 0.1f;
    private float autoDelay = 2.5f;

    // 스크립트 데이터
    private List<StoryScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private int selectedStageId = 0;

    // 상태 관리
    private bool isInitialized = false;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool isAutoMode = false;
    public bool IsInitialized => isInitialized;

    private async void Start()
    {
        await Initialize();
    }

    private void Update()
    {
        if (!isPlaying) return;

        // 마우스 클릭 또는 터치 감지
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnScreenClicked();
        }
    }

    private async UniTask Initialize()
    {
        while (DataTableManager.StoryScriptTable == null)
        {
            await UniTask.Yield();
        }

        var gameData = SaveLoadManager.Data;
        selectedStageId = gameData.selectedStageID;

        LoadStageScript();
        SetupUI();

        isInitialized = true;
    }

    private void LoadStageScript()
    {
        currentScripts = DataTableManager.StoryScriptTable.GetStageScripts(selectedStageId);

        if (currentScripts == null || currentScripts.Count == 0)
        {
            GameSceneManager.ChangeScene(SceneType.LobbyScene);
            return;
        }
    }

    private void SetupUI()
    {
        if (autoButton != null)
        {
            autoButton.onClick.RemoveAllListeners();
            autoButton.onClick.AddListener(OnAutoButtonClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        if (nameText != null)
            nameText.text = "";
        if (scriptText != null)
            scriptText.text = "";

        SetCutsceneImage(selectedStageId);
    }

    private void SetCutsceneImage(int stageId)
    {
        if (currentScripts == null || currentScriptIndex < 0 || currentScriptIndex >= currentScripts.Count)
        {
            return;
        }

        var currentScript = currentScripts[currentScriptIndex];
        string cutImageName = currentScript.CutImage;

        if (!string.IsNullOrEmpty(cutImageName))
        {
            LoadAndSetCutsceneImage(cutImageName);
        }
    }

    private void LoadAndSetCutsceneImage(string imageName)
    {
        if (cutSceneImage == null) return;

        var texture = ResourceManager.Instance.Get<Texture2D>(imageName);
        if (texture != null)
        {
            cutSceneImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
    }

    public void StartCutscene()
    {
        if (!isInitialized || currentScripts == null || currentScripts.Count == 0)
        {
            OnCutsceneComplete();
            return;
        }

        isPlaying = true;
        currentScriptIndex = 0;

        ShowCurrentScript();
    }

    private void ShowCurrentScript()
    {
        if (currentScriptIndex >= currentScripts.Count)
        {
            OnCutsceneComplete();
            return;
        }

        var script = currentScripts[currentScriptIndex];

        // 이전 음성 즉시 정지 (다음 대사로 넘어갈 때마다 호출)
        SoundManager.Instance?.StopVoiceSFX();

        SetCutsceneImage(selectedStageId);

        if (nameText != null)
            nameText.text = script.Name;

        // 음성 재생 - Voice 필드에 값이 있는 경우에만
        PlayVoiceForCurrentScript(script);

        StartTypingEffect(script.Text).Forget();
    }

    /// 현재 스크립트의 음성 재생
    private void PlayVoiceForCurrentScript(StoryScriptCSVData script)
    {
        if (string.IsNullOrEmpty(script.Voice))
        {
            return;
        }

        SoundManager.Instance?.PlayVoiceSFX(script.Voice);
    }

    private async UniTask StartTypingEffect(string text)
    {
        if (scriptText == null) return;

        isTyping = true;
        scriptText.text = "";

        for (int i = 0; i <= text.Length; i++)
        {
            if (!isTyping || !isPlaying)
            {
                scriptText.text = text;
                break;
            }

            scriptText.text = text.Substring(0, i);
            await UniTask.Delay((int)(textSpeed * 1000), DelayType.UnscaledDeltaTime);
        }

        isTyping = false;

        // 타이핑 완료 후 자동 모드 체크
        CheckAutoProgress().Forget();
    }

    private async UniTaskVoid CheckAutoProgress()
    {
        if (!isAutoMode || !isPlaying || isTyping) return;

        await UniTask.Delay((int)(autoDelay * 1000), DelayType.UnscaledDeltaTime);

        if (isAutoMode && isPlaying && !isTyping)
        {
            NextScript();
        }
    }

    private void NextScript()
    {
        if (!isPlaying)
        {
            return;
        }

        if (isTyping)
        {
            // 타이핑 중이라면 즉시 완료
            isTyping = false;
        }
        else
        {
            // 다음 대사로 진행
            currentScriptIndex++;
            ShowCurrentScript();
        }
    }

    private void OnAutoButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        isAutoMode = !isAutoMode;

        UpdateAutoButtonVisual();

        // 자동 모드를 켰고, 현재 타이핑이 완료된 상태라면 자동 진행 시작
        if (isAutoMode && !isTyping && isPlaying)
        {
            CheckAutoProgress().Forget();
        }
    }

    private void OnSkipButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        OnCutsceneComplete();
    }

    private void UpdateAutoButtonVisual()
    {
        if (autoButton == null) return;
        // 이미지 교체 해야함
    }

    private void OnCutsceneComplete()
    {
        Debug.Log($"[StoryManager] 스테이지 {selectedStageId} 컷씬 완료");

        // 컷씬 종료 시 음성도 정지
        SoundManager.Instance?.StopVoiceSFX();

        isPlaying = false;
        isTyping = false;

        // 전투가 없는 스테이지인지 확인하여 분기 처리
        if (IsNonCombatStoryStage(selectedStageId))
        {
            Debug.Log($"[StoryManager] 전투 없음 - 바로 보상창 표시");
            // 전투 없이 바로 보상창 표시
            ShowStoryRewardDirectly(selectedStageId);
        }
        else
        {
            Debug.Log($"[StoryManager] 전투 있음 - 스테이지 씬으로 이동");
            // 전투가 있는 스테이지는 스테이지 씬으로 이동
            GameSceneManager.ChangeScene(SceneType.StageScene);
        }
    }

    private void OnScreenClicked()
    {
        if (IsClickOnButton())
        {
            return;
        }

        NextScript();
    }

    private bool IsClickOnButton()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Auto/Skip 버튼 위에서 클릭했는지만 체크
        foreach (var result in results)
        {
            if (result.gameObject == autoButton?.gameObject ||
                result.gameObject == skipButton?.gameObject)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsNonCombatStoryStage(int storyStageId)
    {
        // 66001(던전 1)과 66004(던전 4)는 전투가 없음
        bool isNonCombat = storyStageId == 66001 || storyStageId == 66004;
        Debug.Log($"[StoryManager] IsNonCombatStoryStage({storyStageId}) = {isNonCombat}");
        return isNonCombat;
    }

    /// 전투 없이 바로 스토리 보상창 표시
    private void ShowStoryRewardDirectly(int storyStageId)
    {
        Debug.Log($"[StoryManager] ShowStoryRewardDirectly 호출 - storyStageId: {storyStageId}");

        // 씬 전환 후 보상창 표시를 위한 플래그 설정
        var gameData = SaveLoadManager.Data;
        gameData.showStoryRewardAfterScene = true;
        Debug.Log($"[StoryManager] showStoryRewardAfterScene 플래그 설정: {gameData.showStoryRewardAfterScene}");

        SaveLoadManager.SaveToServer().Forget();
        Debug.Log($"[StoryManager] 데이터 저장 완료, 로비로 이동 시작");

        // 로비 씬으로 이동하면서 보상창 표시 예약
        GameSceneManager.ChangeScene(SceneType.LobbyScene);
    }

    private StageData ConvertStoryStageToStageData(StoryStageCSVData storyStage)
    {
        var stageData = ScriptableObject.CreateInstance<StageData>();

        stageData.stage_ID = storyStage.story_stage_id;
        stageData.stage_name = storyStage.story_stage_name;
        stageData.stage_step1 = storyStage.story_stage_id % 10;
        stageData.stage_step2 = 1;
        stageData.stage_type = storyStage.stage_type;
        stageData.member_count = storyStage.member_count;
        stageData.level_max = storyStage.level_max;
        stageData.Fever_Time_stack = storyStage.Fever_Time_stack;
        stageData.wave_time = storyStage.wave_time;
        stageData.wave1_id = storyStage.wave1_id;
        stageData.wave2_id = storyStage.wave2_id;
        stageData.wave3_id = storyStage.wave3_id;
        stageData.wave4_id = 0;
        stageData.dispatch_reward = 0;
        stageData.fail_stamina = 0;
        stageData.prefab = storyStage.prefab;
        stageData.stage_position = 2;

        return stageData;
    }
}