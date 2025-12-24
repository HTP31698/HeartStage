using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TutorialManager : MonoBehaviour
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
    private List<TutorialScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private int selectedLocationId = 0;

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
        while (DataTableManager.TutorialScriptTable == null)
        {
            await UniTask.Yield();
        }

        // 튜토리얼은 기본적으로 location 0부터 시작
        selectedLocationId = 0;

        LoadLocationScript();
        SetupUI();

        isInitialized = true;
    }

    private void LoadLocationScript()
    {
        currentScripts = DataTableManager.TutorialScriptTable.GetLocationScripts(selectedLocationId);

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

        SetCutsceneImage(0);
    }

    private void SetCutsceneImage(int scriptIndex)
    {
        if (currentScripts == null || scriptIndex < 0 || scriptIndex >= currentScripts.Count)
        {
            return;
        }

        var currentScript = currentScripts[scriptIndex];
        string cutImageName = currentScript.CutImage;

        if (!string.IsNullOrEmpty(cutImageName))
        {
            LoadAndSetCutsceneImage(cutImageName);
        }
    }

    private void LoadAndSetCutsceneImage(string imageName)
    {
        if (cutSceneImage == null) return;

        var sprite = ResourceManager.Instance.GetSprite(imageName);
        if (sprite != null)
        {
            cutSceneImage.sprite = sprite;
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
        //SoundManager.Instance?.StopVoiceSFX();

        SetCutsceneImage(currentScriptIndex);

        if (nameText != null)
            nameText.text = script.Name;

        // 음성 재생 - Voice 필드에 값이 있는 경우에만
        //PlayVoiceForCurrentScript(script);

        StartTypingEffect(script.Text).Forget();
    }

    /// 현재 스크립트의 음성 재생
    private void PlayVoiceForCurrentScript(TutorialScriptCSVData script)
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

        // 자동 모드일 경우 잠시 대기 후 다음 대사로
        if (isAutoMode)
        {
            await UniTask.Delay((int)(autoDelay * 1000), DelayType.UnscaledDeltaTime);
            if (isAutoMode && isPlaying) // 대기 중에 상태가 바뀌었을 수 있으므로 재확인
            {
                NextScript();
            }
        }
    }

    private void OnScreenClicked()
    {
        if (IsClickOnButton())
        {
            return;
        }

        if (isTyping)
        {
            // 타이핑 중이면 즉시 완료
            isTyping = false;
        }
        else
        {
            // 타이핑이 끝났으면 다음 대사로
            NextScript();
        }
    }

    private void NextScript()
    {
        currentScriptIndex++;
        ShowCurrentScript();
    }

    private void OnAutoButtonClicked()
    {
        isAutoMode = !isAutoMode;
        Debug.Log($"[TutorialManager] 자동 모드: {isAutoMode}");
    }

    private void OnSkipButtonClicked()
    {
        OnCutsceneComplete();
    }

    private void OnCutsceneComplete()
    {
        isPlaying = false;
        isAutoMode = false;

        // 음성 정지
        //SoundManager.Instance?.StopVoiceSFX();

        Debug.Log("[TutorialManager] 튜토리얼 컷씬 완료");

        if (SaveLoadManager.Data != null)
        {
            SaveLoadManager.Data.isTutorialCutsceneCompleted = true;
            SaveLoadManager.SaveToServer().Forget();
        }

        // 다음 씬으로 이동 (로비 또는 튜토리얼 플레이 씬)
        GameSceneManager.ChangeScene(SceneType.LobbyScene);
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