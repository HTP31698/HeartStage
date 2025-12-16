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

    private float textSpeed = 0.05f;
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
            Debug.Log("[StoryManager] 화면 클릭 감지!");
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
            Debug.LogWarning($"[StoryManager] 스테이지 {selectedStageId}의 스크립트를 찾을 수 없습니다!");
            GameSceneManager.ChangeScene(SceneType.LobbyScene);
            return;
        }

        Debug.Log($"[StoryManager] 스테이지 {selectedStageId} - {currentScripts.Count}개 스크립트 로드 완료");
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
        // 구현 예정
    }

    public void StartCutscene()
    {
        if (!isInitialized || currentScripts == null || currentScripts.Count == 0)
        {
            Debug.LogWarning("[StoryManager] 컷씬을 시작할 수 없습니다.");
            OnCutsceneComplete();
            return;
        }

        Debug.Log($"[StoryManager] 컷씬 시작 - 스테이지 {selectedStageId}");

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

        if (nameText != null)
            nameText.text = script.Name;

        StartTypingEffect(script.Text).Forget();
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

        if (isAutoMode && isPlaying)
        {
            await UniTask.Delay((int)(autoDelay * 1000), DelayType.UnscaledDeltaTime);
            if (isPlaying)
            {
                NextScript();
            }
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
            isTyping = false;
        }

        else
        {
            currentScriptIndex++;
            ShowCurrentScript();
        }
    }

    private void OnAutoButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        isAutoMode = !isAutoMode;
        Debug.Log($"[StoryManager] 자동 모드: {isAutoMode}");

        UpdateAutoButtonVisual();
    }

    private void OnSkipButtonClicked()
    {
        SoundManager.Instance.PlaySFX(SoundName.SFX_UI_Button_Click);

        OnCutsceneComplete();
    }

    private void UpdateAutoButtonVisual()
    {
        if (autoButton == null) return;

        var colors = autoButton.colors;
        colors.normalColor = isAutoMode ? Color.blue : Color.white;
        autoButton.colors = colors;
    }

    private void OnCutsceneComplete()
    {
        Debug.Log($"[StoryManager] 스테이지 {selectedStageId} 컷씬 완료 - 전투 스테이지로 이동");

        isPlaying = false;
        isTyping = false;

        GameSceneManager.ChangeScene(SceneType.StageScene);
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
}