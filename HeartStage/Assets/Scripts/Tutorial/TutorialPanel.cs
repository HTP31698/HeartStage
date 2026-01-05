using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialPanel : GenericWindow
{
    [SerializeField] private GameObject tutorialScriptPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Image arrow;
    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private Button skipButton;

    [Header("Reference")]
    [SerializeField] private TutorialNickNameScript nicknameWindow;

    [Header("Reference")]
    [SerializeField] private Button battleButton;
    [SerializeField] private Button stageStartButton;

    [Header("Tutorial UI Control")]
    [SerializeField] private GameObject backgroundPanel; // BackGroundPanel GameObject
    [SerializeField] private GameObject tutorialSelectWindowPanel; // 스테이지선택용 패널
    [SerializeField] private Button itemChestButton; // 아이템 보관함 버튼
    [SerializeField] private Button iconButton; // 아이콘 버튼
    [SerializeField] private Button boardButton; // 게시판 버튼
    [SerializeField] private Button optionButton; // 옵션 버튼

    private TutorialScriptPrefab currentScriptUI;
    private List<TutorialScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool isWaitingForBattleButton = false; // 배틀 버튼 대기 상태

    private bool isProcessingClick = false; // 클릭 처리 중인지 여부
    private float clickCooldown = 0.3f; // 클릭 쿨다운 시간
    private float lastClickTime = 0f; // 마지막 클릭 시간

    protected override void Awake()
    {
        base.Awake();

        // TutorialPanel은 WindowManager의 관리에서 제외
        windowType = WindowType.None;
        isOverlayWindow = true;
    }

    public override void Open()
    {
        if (SaveLoadManager.Data != null)
        {
            var saveData = SaveLoadManager.Data as SaveDataV1;
            if (saveData != null && saveData.isTutorialCompleted)
            {
                Close();
                return;
            }
        }

        base.Open();

        // 튜토리얼 중 로비 캐릭터 터치 비활성화
        if (DragZoomPanManager.Instance != null)
        {
            DragZoomPanManager.Instance.LockForTutorial();
        }

        // 아이템 보관함 버튼 비활성화
        if (itemChestButton != null)
        {
            itemChestButton.interactable = false;
        }

        // 아이콘 버튼 비활성화
        if (iconButton != null)
        {
            iconButton.interactable = false;
        }

        // 게시판 버튼 비활성화
        if (boardButton != null)
        {
            boardButton.interactable = false;
        }

        // 옵션 버튼 비활성화
        if (optionButton != null)
        {
            optionButton.interactable = false;
        }

        // 스킵 버튼 이벤트 등록
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        // Location 1부터 시작
        StartLocationScript(1);
    }

    public override void Close()
    {
        SoundManager.Instance?.StopVoiceSFX();

        // 튜토리얼 종료 시 로비 캐릭터 터치 다시 활성화
        if (DragZoomPanManager.Instance != null)
        {
            DragZoomPanManager.Instance.UnlockForTutorial();
        }

        // 아이템 보관함 버튼 다시 활성화
        if (itemChestButton != null)
        {
            itemChestButton.interactable = true;
        }

        // 아이콘 버튼 다시 활성화
        if (iconButton != null)
        {
            iconButton.interactable = true;
        }

        // 게시판 버튼 다시 활성화
        if (boardButton != null)
        {
            boardButton.interactable = true;
        }

        // 옵션 버튼 다시 활성화
        if (optionButton != null)
        {
            optionButton.interactable = true;
        }

        base.Close();

        // 정리
        isPlaying = false;
        isTyping = false;
        isWaitingForBattleButton = false;
        isProcessingClick = false; // 추가
        lastClickTime = 0f; // 추가

        // 백그라운드 패널 복원
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }

        // 화살표 숨기기
        HideAllArrows();

        if (currentScriptUI != null)
        {
            Destroy(currentScriptUI.gameObject);
            currentScriptUI = null;
        }

        // 모든 버튼 이벤트 해제
        RemoveAllButtonListeners();

        // 스킵 버튼 이벤트 해제
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        // 배틀 버튼 대기 중이면 화면 클릭 무시
        if (isWaitingForBattleButton) return;

        if (isProcessingClick) return;

        if (Time.unscaledTime - lastClickTime < clickCooldown)
            return;

        // 마우스 클릭 또는 터치 감지 (TutorialManager와 동일)
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnScreenClicked();
        }
    }

    public void StartLocationScript(int locationId)
    {
        Debug.Log($"[TutorialPanel] Location {locationId} 시작");

        // 스크립트 로드
        currentScripts = DataTableManager.TutorialScriptTable.GetLocationScripts(locationId);

        if (currentScripts == null || currentScripts.Count == 0)
        {
            Debug.LogWarning($"[TutorialPanel] Location {locationId} 스크립트가 없음");
            CompleteTutorial();
            return;
        }

        currentScriptIndex = 0;

        // TutorialScriptPrefab 생성
        CreateScriptUI();

        // 진행 시작
        isPlaying = true;
        ShowCurrentScript();
    }

    private void CreateScriptUI()
    {
        if (tutorialScriptPrefab != null && contentParent != null)
        {
            var prefabObj = Instantiate(tutorialScriptPrefab, contentParent);
            currentScriptUI = prefabObj.GetComponent<TutorialScriptPrefab>();
        }
    }

    private void ShowCurrentScript()
    {
        if (currentScriptIndex >= currentScripts.Count)
        {
            // 현재 Location 완료 - 다음 Location 확인
            CheckNextLocation();
            return;
        }

        var script = currentScripts[currentScriptIndex];

        SoundManager.Instance?.StopVoiceSFX();

        PlayVoiceForCurrentScript(script);

        // 타이핑 효과로 텍스트 표시
        StartTypingEffect(script.Text).Forget();
    }

    private async UniTask StartTypingEffect(string text)
    {
        if (currentScriptUI == null) return;

        isTyping = true;
        currentScriptUI.SetTutorialText("");

        float textSpeed = 0.05f;

        for (int i = 0; i <= text.Length; i++)
        {
            if (!isTyping || !isPlaying || currentScriptUI == null)
            {
                if (currentScriptUI != null)
                    currentScriptUI.SetTutorialText(text);
                break;
            }

            currentScriptUI.SetTutorialText(text.Substring(0, i));
            await UniTask.Delay((int)(textSpeed * 1000), DelayType.UnscaledDeltaTime);
        }

        isTyping = false;

        if (currentScriptUI == null || currentScripts == null || currentScriptIndex >= currentScripts.Count)
            return;

        ExecuteScriptAction(currentScripts[currentScriptIndex]);
    }

    private void OnScreenClicked()
    {
        if (nicknameWindow != null && nicknameWindow.IsOpen)
        {
            return;
        }

        isProcessingClick = true;
        lastClickTime = Time.unscaledTime;

        try
        {
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
        finally
        {
            // 다음 프레임에서 클릭 처리 플래그 해제
            ResetClickProcessingFlag().Forget();
        }
    }

    private async UniTaskVoid ResetClickProcessingFlag()
    {
        await UniTask.Yield();
        isProcessingClick = false;
    }

    private void NextScript()
    {
        currentScriptIndex++;
        ShowCurrentScript();
    }

    private void CheckNextLocation()
    {
        // Location 1 → 2 → 3
        int nextLocationId = GetNextLocationId();

        if (nextLocationId > 0)
        {
            Debug.Log($"[TutorialPanel] 다음 Location {nextLocationId}로 이동");
            StartLocationScript(nextLocationId);
        }
        else
        {
            Debug.Log("[TutorialPanel] 모든 튜토리얼 완료");
            CompleteTutorial();
        }
    }

    private int GetNextLocationId()
    {
        // 현재 스크립트의 location에서 다음 location 찾기
        if (currentScripts != null && currentScripts.Count > 0)
        {
            int currentLocationId = currentScripts[0].location;
            int nextLocationId = currentLocationId + 1;

            // 다음 location에 스크립트가 있는지 확인
            if (DataTableManager.TutorialScriptTable.HasScripts(nextLocationId))
            {
                return nextLocationId;
            }
        }

        return -1; // 더 이상 없음
    }

    private void CompleteTutorial()
    {
        // 화살표 숨기기
        HideAllArrows();

        // 튜토리얼 완료 플래그 설정
        if (SaveLoadManager.Data != null)
        {
            SaveLoadManager.Data.isTutorialCompleted = true;
            SaveLoadManager.SaveToServer().Forget();
        }

        // 패널 닫기
        Close();
    }

    private void ExecuteScriptAction(TutorialScriptCSVData script)
    {
        if (string.IsNullOrEmpty(script.Action)) return;

        switch (script.Action)
        {
            case "NickName":
                OpenNicknameWindow();
                break;
            case "BattleArrow":
                ActionBattleArrow();
                break;
            case "TutorialStageArrow":
                ShowTutorialStageArrow();
                break;
            case "StageStartArrow": 
                ShowStageStartArrow();
                break;
        }
    }
    private void ActionBattleArrow()
    {
        // battleButton 외의 모든 버튼 비활성화
        DisableOtherButtons(battleButton);

        // 화살표 표시
        ShowArrowOnButton(battleButton, OnBattleButtonClicked);
    }

    private void DisableOtherButtons(Button exceptButton)
    {
        // TutorialPanel 내부 버튼들
        if (stageStartButton != null && stageStartButton != exceptButton)
        {
            stageStartButton.interactable = false;
        }

        // LobbyUI의 버튼들도 찾아서 비활성화
        if (lobbyUI != null)
        {
            // Reflection을 사용해서 LobbyUI의 버튼들 비활성화
            var lobbyUIType = typeof(LobbyUI);

            // stageUiButton 비활성화
            var stageUiButtonField = lobbyUIType.GetField("stageUiButton",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (stageUiButtonField != null)
            {
                var stageUiButton = stageUiButtonField.GetValue(lobbyUI) as Button;
                if (stageUiButton != null && stageUiButton != exceptButton)
                {
                    stageUiButton.interactable = false;
                }
            }

            // homeUiButton 비활성화
            var homeUiButtonField = lobbyUIType.GetField("homeUiButton",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (homeUiButtonField != null)
            {
                var homeUiButton = homeUiButtonField.GetValue(lobbyUI) as Button;
                if (homeUiButton != null && homeUiButton != exceptButton)
                {
                    homeUiButton.interactable = false;
                }
            }

            // 나머지 버튼들도 동일하게...
            DisableLobbyButton(lobbyUI, "gachaButton", exceptButton);
            DisableLobbyButton(lobbyUI, "storeButton", exceptButton);
            DisableLobbyButton(lobbyUI, "characterDictButton", exceptButton);
            DisableLobbyButton(lobbyUI, "QuestButton", exceptButton);
            DisableLobbyButton(lobbyUI, "specialDungeonButton", exceptButton);
        }
    }

    private void DisableLobbyButton(LobbyUI lobbyUI, string buttonFieldName, Button exceptButton)
    {
        var lobbyUIType = typeof(LobbyUI);
        var buttonField = lobbyUIType.GetField(buttonFieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (buttonField != null)
        {
            var button = buttonField.GetValue(lobbyUI) as Button;
            if (button != null && button != exceptButton)
            {
                button.interactable = false;
            }
        }
    }

    private void EnableAllButtons()
    {
        // TutorialPanel 내부 버튼들
        if (stageStartButton != null)
        {
            stageStartButton.interactable = true;
        }

        // LobbyUI 버튼들도 다시 활성화
        if (lobbyUI != null)
        {
            EnableLobbyButton(lobbyUI, "stageUiButton");
            EnableLobbyButton(lobbyUI, "homeUiButton");
            EnableLobbyButton(lobbyUI, "gachaButton");
            EnableLobbyButton(lobbyUI, "storeButton");
            EnableLobbyButton(lobbyUI, "characterDictButton");
            EnableLobbyButton(lobbyUI, "QuestButton");
            EnableLobbyButton(lobbyUI, "specialDungeonButton");
        }
    }

    //  LobbyUI 버튼 활성화 헬퍼 메서드
    private void EnableLobbyButton(LobbyUI lobbyUI, string buttonFieldName)
    {
        var lobbyUIType = typeof(LobbyUI);
        var buttonField = lobbyUIType.GetField(buttonFieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (buttonField != null)
        {
            var button = buttonField.GetValue(lobbyUI) as Button;
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }
    private void OpenNicknameWindow()
    {
        if (nicknameWindow != null)
        {
            nicknameWindow.Open();
            nicknameWindow.transform.SetAsLastSibling(); // 맨 앞으로
        }
    }

    // 통합된 화살표 표시 메서드
    private void ShowArrowOnButton(Button targetButton, UnityEngine.Events.UnityAction clickAction)
    {
        if (arrow == null || targetButton == null)
        {
            return;
        }

        // 버튼 클릭 이벤트 등록
        targetButton.onClick.AddListener(clickAction);

        // 버튼 대기 상태로 설정 (화면 클릭 무시)
        isWaitingForBattleButton = true;

        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(false);
        }

        // TutorialPanel의 레이캐스트 차단 해제
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
        }

        // 화살표 활성화
        arrow.gameObject.SetActive(true);

        // 버튼의 위치에 화살표 배치
        PositionArrowOverButton(targetButton);

        StartArrowAnimation().Forget();
    }

    // 튜토리얼 스테이지 화살표 표시 
    private void ShowTutorialStageArrow()
    {
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(false);
        }

        if (tutorialSelectWindowPanel != null)
        {
            tutorialSelectWindowPanel.SetActive(false);
        }

        DisableOtherButtons(null);

        // WindowManager에서 StageWindow 찾기
        if (WindowManager.Instance != null)
        {
            var windowsField = WindowManager.Instance.GetType().GetField("windows",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (windowsField != null)
            {
                var windowsDict = windowsField.GetValue(WindowManager.Instance) as Dictionary<WindowType, GenericWindow>;

                if (windowsDict != null && windowsDict.ContainsKey(WindowType.StageSelect))
                {
                    var stageWindow = windowsDict[WindowType.StageSelect];

                    if (stageWindow != null)
                    {
                        // StageWindow에서 튜토리얼 스테이지 버튼 찾기
                        Button tutorialButton = FindTutorialStageButton(stageWindow.transform);

                        if (tutorialButton != null)
                        {
                            // 1. 모든 버튼 비활성화, 튜토리얼 버튼만 활성화
                            DisableAllStageButtonsExcept(tutorialButton, stageWindow.transform);

                            // 2. 스크롤 막기
                            DisableAllScrollRects(stageWindow.transform);

                            ShowArrowOnButton(tutorialButton, () => OnTutorialStageButtonClicked(tutorialButton));
                        }
                    }
                }
            }
        }
    }

    // 모든 StageWindow 내 버튼을 비활성화하고, exceptButton만 활성화
    private void DisableAllStageButtonsExcept(Button exceptButton, Transform root)
    {
        var buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            btn.interactable = (btn == exceptButton);
        }
    }

    // 모든 ScrollRect 비활성화
    private void DisableAllScrollRects(Transform root)
    {
        var scrollRects = root.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true);
        foreach (var scroll in scrollRects)
        {
            scroll.enabled = false;
        }
    }

    // 튜토리얼 스테이지 버튼을 찾는 메서드
    private Button FindTutorialStageButton(Transform stageWindowTransform)
    {
        // StageWindow의 contentParent에서 모든 자식 오브젝트 검사
        Transform contentParent = stageWindowTransform.Find("ContentParent");

        if (contentParent == null)
        {
            // contentParent를 찾기 위해 재귀적으로 검색
            contentParent = FindContentParent(stageWindowTransform);
        }

        if (contentParent != null)
        {
            for (int i = 0; i < contentParent.childCount; i++)
            {
                Transform child = contentParent.GetChild(i);

                // StageChoosePrefab 컴포넌트 확인
                var stageChoose = child.GetComponent<StageChoosePrefab>();

                if (stageChoose != null)
                {
                    // 텍스트 컴포넌트에서 "튜토리얼"인지 확인
                    TextMeshProUGUI textComponent = child.GetComponentInChildren<TextMeshProUGUI>();

                    if (textComponent != null && textComponent.text.Contains("튜토리얼"))
                    {
                        Button button = child.GetComponent<Button>();
                        if (button != null)
                        {
                            return button;
                        }
                    }
                }
            }
        }

        return null;
    }

    // contentParent를 재귀적으로 찾는 메서드
    private Transform FindContentParent(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name.ToLower().Contains("content"))
            {
                return child;
            }

            // 재귀적으로 검색
            Transform found = FindContentParent(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    // 버튼 위에 화살표 배치하는 공통 메서드
    private void PositionArrowOverButton(Button targetButton)
    {
        RectTransform buttonRect = targetButton.GetComponent<RectTransform>();
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();

        if (buttonRect != null && arrowRect != null)
        {
            // 버튼의 중심점을 월드 좌표로 변환
            Vector3 buttonWorldPos = buttonRect.TransformPoint(buttonRect.rect.center);
            Vector3 arrowLocalPos = arrowRect.parent.InverseTransformPoint(buttonWorldPos);

            // 화살표를 버튼 위쪽으로 더 높게 오프셋 적용
            arrowLocalPos.y += buttonRect.rect.height * 0.5f + 90f;

            arrowRect.localPosition = arrowLocalPos;
        }
    }

    private void OnBattleButtonClicked()
    {
        // 클릭 처리 중이면 무시
        if (isProcessingClick) return;

        isProcessingClick = true;
        lastClickTime = Time.unscaledTime;

        if (tutorialSelectWindowPanel != null)
        {
            tutorialSelectWindowPanel.SetActive(true);
        }

        OnButtonClickedCommon(battleButton);
        ResetClickProcessingFlag().Forget();
    }

    // 튜토리얼 스테이지 버튼 클릭
    private void OnTutorialStageButtonClicked(Button tutorialButton)
    {
        // 클릭 처리 중이면 무시
        if (isProcessingClick) return;

        isProcessingClick = true;
        lastClickTime = Time.unscaledTime;

        // 버튼 이벤트 해제
        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveAllListeners();
        }

        // 화살표 숨기기
        HideAllArrows();

        // TutorialPanel의 레이캐스트 차단 복원
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // 대기 상태 해제
        isWaitingForBattleButton = false;

        // 다음 스크립트로 넘어가지 말고 바로 StageStartArrow 실행
        ShowStageStartArrow();

        ResetClickProcessingFlag().Forget();
    }

    // 버튼 클릭 시 공통 처리
    private void OnButtonClickedCommon(Button clickedButton)
    {
        // 버튼 이벤트 해제
        if (clickedButton != null)
        {
            clickedButton.onClick.RemoveAllListeners();
        }

        // 화살표 숨기기
        HideAllArrows();

        EnableAllButtons();

        // TutorialPanel의 레이캐스트 차단 복원
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        // 백그라운드 패널 복원
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }

        // 대기 상태 해제
        isWaitingForBattleButton = false;

        // 다음 스크립트로 진행
        NextScript();
    }

    // 모든 화살표 숨기기
    private void HideAllArrows()
    {
        if (arrow != null)
        {
            arrow.gameObject.SetActive(false);
        }
    }

    // 모든 버튼 이벤트 제거
    private void RemoveAllButtonListeners()
    {
        if (battleButton != null)
        {
            battleButton.onClick.RemoveListener(OnBattleButtonClicked);
        }
    }

    private async UniTaskVoid StartArrowAnimation()
    {
        if (arrow == null) return;

        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        Vector3 originalPos = arrowRect.localPosition;
        float animationTime = 0f;

        while (arrow != null && arrow.gameObject.activeSelf)
        {
            animationTime += Time.unscaledDeltaTime;

            // 위아래로 부드럽게 움직이는 애니메이션
            float yOffset = Mathf.Sin(animationTime * 2f) * 10f;
            arrowRect.localPosition = originalPos + new Vector3(0, yOffset, 0);

            await UniTask.Yield();

            // 오브젝트가 파괴되었는지 체크
            if (this == null) return;
        }
    }

    private void OnDisable()
    {
        // 튜토리얼이 진행 중이면 무조건 막기
        if (isPlaying)
        {
            // 다음 프레임에 강제로 다시 켜기 (WindowManager가 꺼버리는 것을 방지)
            ForceReactivate().Forget();
        }
    }

    // 재활성화
    private async UniTaskVoid ForceReactivate()
    {
        // 한 프레임 기다림
        await UniTask.Yield();

        // 오브젝트가 파괴되었는지 체크
        if (this == null) return;

        if (!gameObject.activeSelf && isPlaying)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
    }

    private void ShowStageStartArrow()
    {
        // stageStartButton만 활성화, 나머지 버튼은 모두 비활성화
        DisableOtherButtons(stageStartButton);

        // StageInfoWindow 내 버튼 제어
        EnableOnlyStageStartButtonInStageInfo();

        ShowArrowOnButton(stageStartButton, OnStageStartButtonClicked);
    }

    private void OnStageStartButtonClicked()
    {
        // 클릭 처리 중이면 무시
        if (isProcessingClick) return;

        isProcessingClick = true;
        lastClickTime = Time.unscaledTime;

        OnButtonClickedCommon(stageStartButton);
        CompleteTutorial();

        ResetClickProcessingFlag().Forget();
    }

    // StageInfoWindow 내에서 stageStartButton만 활성화, 나머지 버튼은 비활성화
    private void EnableOnlyStageStartButtonInStageInfo()
    {
        if (lobbyUI != null)
        {
            var stageInfoWindowField = typeof(LobbyUI).GetField("stageInfoWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var stageInfoWindow = stageInfoWindowField?.GetValue(lobbyUI) as StageInfoWindow;
            if (stageInfoWindow != null)
            {
                var type = typeof(StageInfoWindow);

                // stageStartButton만 활성화
                var startBtnField = type.GetField("stageStartButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var monitoringBtnField = type.GetField("monitoringButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var closeBtnField = type.GetField("closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                var startBtn = startBtnField?.GetValue(stageInfoWindow) as Button;
                var monitoringBtn = monitoringBtnField?.GetValue(stageInfoWindow) as Button;
                var closeBtn = closeBtnField?.GetValue(stageInfoWindow) as Button;

                if (startBtn != null) startBtn.interactable = true;
                if (monitoringBtn != null) monitoringBtn.interactable = false;
                if (closeBtn != null) closeBtn.interactable = false;
            }
        }
    }

    private void PlayVoiceForCurrentScript(TutorialScriptCSVData script)
    {
        if (string.IsNullOrEmpty(script.Voice))
        {
            return;
        }

        SoundManager.Instance?.PlayVoiceSFX(script.Voice);
    }

    private void OnSkipButtonClicked()
    {
        Debug.Log("[TutorialPanel] 스킵 버튼 클릭 - 튜토리얼 완료 처리");
        CompleteTutorial();
    }
}