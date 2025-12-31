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
    private bool isReturnedFromBattle = false; // 전투 복귀 플래그
    private bool hasCompletedBattle = false; // 전투 완료 후 스토리 진행 중인지 여부
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

        // BGM 정지 (로비 음악 포함)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
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

        // 전투 후 복귀인 경우 저장된 인덱스부터 시작
        var saveData = SaveLoadManager.Data as SaveDataV1;
        
        if (saveData != null && saveData.storyScriptResumeIndex >= 0)
        {
            currentScriptIndex = saveData.storyScriptResumeIndex;
            isReturnedFromBattle = true; // 전투 복귀 플래그 설정
            hasCompletedBattle = true; // 전투 완료 후 스토리 진행 중
        }
        else
        {
            currentScriptIndex = 0; // 처음부터 시작
            isReturnedFromBattle = false;
            hasCompletedBattle = false;
        }

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

        // 전투 후 복귀 컷씬 BGM 처리 (2, 3스테이지 공통)
        if (isReturnedFromBattle)
        {
            PlayBattleAfterBGM(selectedStageId);
        }
        else
        {
            PlayStoryBGM(selectedStageId);
        }

        // 전투 복귀가 아닌 경우에만 처음부터 시작
        if (!isReturnedFromBattle)
        {
            currentScriptIndex = 0;
        }

        // OnCutsceneComplete()에서 스토리가 완전히 끝난 후 리셋됨       

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
            // 전투 시작 라인인지 체크
            if (ShouldStartBattle())
            {
                StartBattleFromStory();
                return;
            }

            // 다음 대사로 진행
            currentScriptIndex++;
            ShowCurrentScript();
        }
    }

    /// 현재 라인에서 전투를 시작해야 하는지 체크
    private bool ShouldStartBattle()
    {
        // 전투 복귀 후에는 전투 시작 조건 스킵 (무한 루프 방지)
        if (isReturnedFromBattle)
        {
            Debug.Log("[StoryManager] 전투 복귀 상태 - 전투 시작 조건 스킵");
            isReturnedFromBattle = false; // 플래그 리셋
            return false;
        }

        if (currentScripts == null || currentScriptIndex >= currentScripts.Count)
            return false;

        var currentScript = currentScripts[currentScriptIndex];
        
        // 66006 스테이지(세라 스토리 2)의 44번 라인 이후에 전투 시작
        if (selectedStageId == 66006 && currentScript.Line == 44)
        {
            return true;
        }

        // 66007 스테이지(세라 스토리 3)의 69번 라인 이후에 전투 시작
        if (selectedStageId == 66007 && currentScript.Line == 69)
        {
            return true;
        }

        return false;
    }

    /// 스토리 중간에 전투 시작
    private async void StartBattleFromStory()
    {
        // 다음 라인 인덱스 저장 (전투 클리어 후 이어서 진행)
        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData != null)
        {
            saveData.storyScriptResumeIndex = currentScriptIndex + 1;
            Debug.Log($"[StoryManager] 전투 시작 - storyScriptResumeIndex 저장: {saveData.storyScriptResumeIndex}");
            await SaveLoadManager.SaveToServer(); // 저장 완료까지 대기
        }

        // 컷씬 종료 시 음성 정지
        SoundManager.Instance?.StopVoiceSFX();

        isPlaying = false;
        isTyping = false;

        // 전투 스테이지로 이동
        GameSceneManager.ChangeScene(SceneType.StageScene);
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

        // 전투 복귀 후에는 남은 스토리만 스킵 (보상창으로 이동)
        if (hasCompletedBattle)
        {
            OnCutsceneComplete();
            return;
        }

        // 전투 시작 라인 찾기
        int battleStartIndex = FindBattleStartIndex();
        
        if (battleStartIndex >= 0)
        {
            // 전투 시작 라인까지 스킵하고 전투 시작
            currentScriptIndex = battleStartIndex;
            StartBattleFromStory();
        }
        else
        {
            // 전투가 없는 스테이지는 전체 스킵
            OnCutsceneComplete();
        }
    }

    /// 전투 시작 라인의 인덱스 찾기
    private int FindBattleStartIndex()
    {
        if (currentScripts == null || currentScripts.Count == 0)
            return -1;

        for (int i = 0; i < currentScripts.Count; i++)
        {
            var script = currentScripts[i];
            
            // 66006 스테이지(세라 스토리 2)의 44번 라인
            if (selectedStageId == 66006 && script.Line == 44)
            {
                return i;
            }

            // 66007 스테이지(세라 스토리 3)의 69번 라인
            if (selectedStageId == 66007 && script.Line == 69)
            {
                return i;
            }
        }

        return -1; // 전투 시작 라인 없음
    }

    private void UpdateAutoButtonVisual()
    {
        if (autoButton == null) return;
    }

    private void OnCutsceneComplete()
    {
        // 컷씬 종료 시 음성도 정지
        SoundManager.Instance?.StopVoiceSFX();

        isPlaying = false;
        isTyping = false;

        // 스토리 완료 시 storyScriptResumeIndex 리셋
        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData != null)
        {
            saveData.storyScriptResumeIndex = -1;
            Debug.Log("[StoryManager] OnCutsceneComplete에서 storyScriptResumeIndex 리셋");
        }

        // 전투가 없는 스테이지인지 확인하여 분기 처리
        if (IsNonCombatStoryStage(selectedStageId))
        {
            // 전투 없이 바로 보상창 표시
            ShowStoryRewardDirectly(selectedStageId);
        }
        else if (hasCompletedBattle)
        {
            // 전투 완료 후 스토리가 끝난 경우 보상창 표시
            Debug.Log("[StoryManager] 전투 완료 후 스토리 종료 - 보상창 표시");
            ShowStoryRewardDirectly(selectedStageId);
        }
        else
        {
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
        bool isNonCombat = storyStageId == 66001 || storyStageId == 66004 || storyStageId == 66005 || storyStageId == 66008; ;
        return isNonCombat;
    }

    /// 전투 없이 바로 스토리 보상창 표시
    private void ShowStoryRewardDirectly(int storyStageId)
    {
        // 스토리 스테이지 완료 상태 저장 추가
        var saveData = SaveLoadManager.Data as SaveDataV1;
        if (saveData != null)
        {
            // 완료 목록에 추가 (UI 표시용)
            if (!saveData.completedStoryStages.Contains(storyStageId))
            {
                saveData.completedStoryStages.Add(storyStageId);
            }

            // 클리어 목록에도 추가 (보상 중복 방지용)
            if (!saveData.clearedStoryStages.Contains(storyStageId))
            {
                saveData.clearedStoryStages.Add(storyStageId);
            }
        }

        // 씬 전환 후 보상창 표시를 위한 플래그 설정
        var gameData = SaveLoadManager.Data;
        gameData.showStoryRewardAfterScene = true;
        gameData.StoryAfterLobby = true; // 스토리 던전 UI 복원용 플래그

        // 던전 윈도우로 돌아가도록 설정
        WindowManager.currentWindow = WindowType.SpecialDungeon;

        SaveLoadManager.SaveToServer().Forget();

        // 로비 씬으로 이동하면서 보상창 표시 예약
        GameSceneManager.ChangeScene(SceneType.LobbyScene);
    }

    private void PlayStoryBGM(int stageId)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.StopBGM();

        string bgmName = "BGM_DefaultStory"; 

        switch (stageId)
        {
            case 66001: // 하나 스토리1 컷씬
            case 66005: // 세라 스토리1 컷씬
                bgmName = SoundName.BGM_StoryScript_1;
                break;

            case 66002: // 하나 스토리2 컷씬
                bgmName = SoundName.BGM_HanaStoryScript_2;
                break;

            case 66006: // 세라 스토리2 컷씬
                bgmName = SoundName.BGM_SeraStoryScript_2;
                break;

            case 66003: // 하나 스토리3 컷씬
            case 66007: // 세라 스토리3 컷씬
                bgmName = SoundName.BGM_StoryScript_3;
                break;

            case 66004: // 하나 스토리4 컷씬
            case 66008: // 세라 스토리4 컷씬
                bgmName = SoundName.BGM_StoryScript_4;
                break;
        }

        SoundManager.Instance.PlayBGM(bgmName);
    }

    private void PlayBattleAfterBGM(int stageId)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.StopBGM();

        string bgmName = null;
        switch (stageId)
        {
            case 66006: // 세라 스토리2 전투 후
            case 66002: // 하나 스토리2 전투 후
                bgmName = SoundName.BGM_StoryScript_2_BattleAfter;
                break;

            case 66007: // 세라 스토리3 전투 후
            case 66003: // 하나 스토리3 전투 후
                bgmName = SoundName.BGM_StoryScript_3_BattleAfter;
                break;
        }

        if (!string.IsNullOrEmpty(bgmName))
            SoundManager.Instance.PlayBGM(bgmName);
    }
}