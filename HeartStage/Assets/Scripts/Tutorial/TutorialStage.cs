using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialStage : MonoBehaviour
{
    [SerializeField] private GameObject tutorialScriptPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Image arrow;
    [SerializeField] private OwnedCharacterSetup ownedCharacterSetup;
    [SerializeField] private Image panelBackgroundImage; // 패널 

    [SerializeField] private Button characterInfoCloseButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject fence;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private BossAlertUI bossAlertUI;
    [SerializeField] private Button feverButton;
    [SerializeField] private Button selectWindowBackButton;

    [SerializeField] private DraggableSlot[] draggableSlots;

    private GameObject stageBorderParent;
    [SerializeField] private Image stageBorderImage;

    private TutorialScriptPrefab currentScriptUI;
    private List<TutorialScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool isWaitingForCharacterClick = false; // 캐릭터 클릭 대기 상태
    private bool isWaitingForStartButton = false; // 스타트 버튼 대기 상태 추가
    private bool closeCharacterInfoOnce = false;
    private bool isWaitingForSkillReady = false;


    private bool isProcessingInput = false; // 입력 처리 중 플래그 추가
    private float lastClickTime = 0f; // 마지막 클릭 시간
    private const float clickCool = 0.3f;

    private Transform waitingCharacterSlot; // 대기 중인 캐릭터 슬롯

    private bool isWaitingForCharacterDrag = false; // 캐릭터 드래그 대기 상태
    private bool blockScreenClick = false; // 화면 클릭 차단 플래그 추가

    private int characterPlaceCount = 0; // 배치된 캐릭터 수 추적
    private int requiredCharacterCount = 3; // 필요한 캐릭터 수

    [SerializeField] private GameObject characterStage; // 캐릭터가 배치되는 스테이지
    [SerializeField] private Button infoButton;

    private void Awake()
    {
        // 캐릭터 배치 이벤트 구독
        DraggableSlot.OnAnySlotChanged += OnCharacterPlaced;
        characterInfoCloseButton.onClick.AddListener(OnCharacterInfoCloseButtonClicked);

        // 스킬 준비 이벤트 구독 추가
        ActiveSkillManager.OnAnySkillReady += OnSkillReady;
    }

    private void OnEnable()
    {
        // 스테이지 튜토리얼이 이미 완료되었는지 확인
        if (SaveLoadManager.Data != null)
        {
            var saveData = SaveLoadManager.Data as SaveDataV1;
            if (saveData != null && saveData.isStageTutorialCompleted)
            {
                Debug.Log("[TutorialStage] 스테이지 튜토리얼이 이미 완료되었습니다.");
                this.gameObject.SetActive(false);
                return;
            }
        }
    }
    private void OnDestroy()
    {
        // 이벤트 해제
        DraggableSlot.OnAnySlotChanged -= OnCharacterPlaced;
        ActiveSkillManager.OnAnySkillReady -= OnSkillReady;
    }

    public void Close()
    {
        SoundManager.Instance?.StopVoiceSFX();

        isPlaying = false;
        isTyping = false;
        isWaitingForCharacterClick = false;
        isWaitingForCharacterDrag = false;
        isWaitingForStartButton = false;
        waitingCharacterSlot = null;
        isProcessingInput = false;
        isWaitingForSkillReady = false;
        lastClickTime = 0f;
        characterPlaceCount = 0;

        Time.timeScale = 1f;

        HideAllArrows();
        RestorePanel();

        // 모든 버튼과 캐릭터 상호작용 다시 활성화
        EnableOtherButtons();
        EnableCharacterInteraction();
        EnableCharacterSlotDragging();

        if (selectWindowBackButton != null)
            selectWindowBackButton.interactable = true;

        if (currentScriptUI != null)
        {
            Destroy(currentScriptUI.gameObject);
            currentScriptUI = null;
        }

        // UI 테두리 정리
        if (stageBorderParent != null)
        {
            Destroy(stageBorderParent);
            stageBorderParent = null;
        }

        // 스타트 버튼 이벤트 해제
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        this.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (isWaitingForCharacterClick) return;
        if (isWaitingForCharacterDrag) return;
        if (isWaitingForStartButton) return;
        if (isWaitingForSkillReady) return; 
        if (isProcessingInput) return;
        if (blockScreenClick) return;       

        if (Time.unscaledTime - lastClickTime < clickCool)
            return;
        
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnScreenClicked();
        }
    }

    private void OnScreenClicked()
    {
        if (isProcessingInput) return;

        if (Time.unscaledTime - lastClickTime < clickCool) return;

        lastClickTime = Time.unscaledTime;
        isProcessingInput = true;

        try
        {
            if (isTyping)
            {
                isTyping = false;
            }
            else
            {
                NextScript();
            }
        }
        finally
        {
            ResetInputProcessingFlag().Forget();
        }
    }

    // 입력 처리 플래그를 다음 프레임에서 해제
    private async UniTaskVoid ResetInputProcessingFlag()
    {
        await UniTask.Yield();
        isProcessingInput = false;
    }

    public void StartLocationScript(int locationId)
    {
        currentScripts = DataTableManager.TutorialScriptTable.GetLocationScripts(locationId);

        if (currentScripts == null || currentScripts.Count == 0)
        {
            CompleteTutorial();
            return;
        }

        currentScriptIndex = 0;
        characterPlaceCount = 0; // 새 로케이션 시작시 초기화
        CreateScriptUI();
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
            CheckNextLocation();
            return;
        }

        var script = currentScripts[currentScriptIndex];

        SoundManager.Instance?.StopVoiceSFX();
        PlayVoiceForCurrentScript(script);
        StartTypingEffect(script.Text).Forget();
    }

    private async UniTask StartTypingEffect(string text, bool executeAction = true)
    {
        if (currentScriptUI == null) return;

        isTyping = true;
        currentScriptUI.SetTutorialText("");

        for (int i = 0; i <= text.Length; i++)
        {
            if (!isTyping || !isPlaying)
            {
                currentScriptUI.SetTutorialText(text);
                break;
            }

            currentScriptUI.SetTutorialText(text.Substring(0, i));
            await UniTask.Delay(25, DelayType.UnscaledDeltaTime);
        }

        isTyping = false;

        // 약간의 딜레이 후에 액션 실행 바로 눌리는거 방지
        await UniTask.Delay(100, DelayType.UnscaledDeltaTime);

        if (executeAction)
        {
            ExecuteScriptAction(currentScripts[currentScriptIndex]);
        }
    }

    private void NextScript()
    {
        if (isProcessingInput && currentScriptIndex >= currentScripts.Count - 1)
            return;

        currentScriptIndex++;
        ShowCurrentScript();
    }

    private void CheckNextLocation()
    {
        int nextLocationId = GetNextLocationId();

        if (nextLocationId > 0)
        {
            StartLocationScript(nextLocationId);
        }
        else
        {
            CompleteTutorial();
        }
    }

    private int GetNextLocationId()
    {
        if (currentScripts != null && currentScripts.Count > 0)
        {
            int currentLocationId = currentScripts[0].location;
            int nextLocationId = currentLocationId + 1;

            if (DataTableManager.TutorialScriptTable.HasScripts(nextLocationId))
            {
                return nextLocationId;
            }
        }
        return -1;
    }

    private void CompleteTutorial()
    {
        SoundManager.Instance?.StopVoiceSFX();

        ResetUIState();
        HideStageAreaBorder();

        if (SaveLoadManager.Data != null)
        {
            var saveData = SaveLoadManager.Data as SaveDataV1;
            if (saveData != null)
            {
                saveData.isStageTutorialCompleted = true;
                SaveLoadManager.SaveToServer().Forget();
            }
        }

        Close();
    }

    private void ExecuteScriptAction(TutorialScriptCSVData script)
    {
        if (string.IsNullOrEmpty(script.Action)) return;

        switch (script.Action)
        {
            case "IdolArrow":
                ActionIdolArrow(); // 아이돌 정보창 열기
                break;
            case "DragArrow":
                ActionDragArrow(); // 캐릭터 드래그 배치
                break;
            case "BuffStageLine":
                ActionBuffStageLine(); // 스테이지 라인 강조
                break;
            case "InfoArrow":
                ActionInfoArrow(); // 버프 정보창 
                break;
            case "ReturnArrow":
                ActionReturnArrow(); // 리턴 버튼
                break;
            case "StartArrow":
                ActionStartArrow(); // 스타트 
                break;
            case "StopGame":
                ActionStopGame();
                break;
            case "StopLineArrow":
                ActionStopLineArrow();
                break;
            case "ResumeBattle":
                ActionResumeBattle();
                break;
            case "SkillGageArrow":
                ActionSkillGageArrow();
                break;
            case "SkillUse":
                ActionSkillUse();
                break;
            case "BossAlert":
                ActionBossAlert();
                break;
            case "FeverArrow":
                ActionFeverArrow();
                break;
            case "BossFight":
                ActionBossFight();
                break;
            case "StageClear":
                break;
        }
    }

    #region Idol Arrow Actions

    private void ActionIdolArrow()
    {
        ActionIdolArrowAsync().Forget();
    }

    private async UniTaskVoid ActionIdolArrowAsync()
    {
        closeCharacterInfoOnce = false;

        // OwnedCharacterSetup이 준비될 때까지 대기
        if (ownedCharacterSetup != null)
        {
            await UniTask.WaitUntil(() => ownedCharacterSetup.IsReady);
        }

        // 첫 번째 캐릭터 슬롯 찾기
        Transform firstCharacterSlot = FindFirstCharacterSlot();

        if (firstCharacterSlot != null)
        {
            // 패널을 투명하게 설정
            SetPanelTransparent();

            if (selectWindowBackButton != null)
                selectWindowBackButton.interactable = false;

            // characterInfoCloseButton을 제외한 다른 버튼들만 비활성화
            DisableOtherButtonsExceptInfo();

            // 캐릭터 드래그 완전히 차단 (정보창 열기만 허용)
            DisableCharacterDragForInfoOnly(firstCharacterSlot);

            // 캐릭터 클릭 대기 상태로 설정
            isWaitingForCharacterClick = true;
            waitingCharacterSlot = firstCharacterSlot;

            // 화살표를 대상 위에 표시
            ShowArrowOnTarget(firstCharacterSlot);
        }
    }

    // IdolArrow에서 사용: 캐릭터 드래그는 차단하고 정보창 열기만 허용
    private void DisableCharacterDragForInfoOnly(Transform targetCharacter)
    {
        if (ownedCharacterSetup?.content != null)
        {
            DragMe[] dragMeComponents = ownedCharacterSetup.content.GetComponentsInChildren<DragMe>();
            foreach (var dragMe in dragMeComponents)
            {
                if (dragMe.transform == targetCharacter)
                {
                    // 타겟 캐릭터는 정보창 열기만 가능하도록 설정
                    dragMe.enabled = false; // 드래그 기능 완전 비활성화

                    // 클릭 이벤트만 허용하기 위해 Button 컴포넌트 활성화
                    Button characterButton = dragMe.GetComponent<Button>();
                    if (characterButton == null)
                    {
                        characterButton = dragMe.gameObject.AddComponent<Button>();
                    }

                    // 정보창 열기 이벤트 등록
                    characterButton.onClick.RemoveAllListeners();
                    characterButton.onClick.AddListener(() => {
                        // 캐릭터 정보창 열기 로직 (기존 DragMe의 정보창 열기 로직과 동일)
                        var characterData = dragMe.GetComponent<DragMe>()?.characterData;
                        if (characterData != null)
                        {
                            WindowManager.Instance.Open(WindowType.CharacterInfo);
                            // CharacterInfoWindow에 데이터 설정하는 로직 추가 필요
                        }
                    });
                }
                else
                {
                    // 다른 캐릭터들은 완전히 비활성화
                    dragMe.enabled = false;
                    Image dragMeImage = dragMe.GetComponent<Image>();
                    if (dragMeImage != null)
                    {
                        dragMeImage.raycastTarget = false;
                    }
                }
            }
        }
    }

    // characterInfoCloseButton을 제외한 다른 버튼들만 비활성화
    private void DisableOtherButtonsExceptInfo()
    {
        if (returnButton != null)
            returnButton.interactable = false;

        if (infoButton != null)
            infoButton.interactable = false;

        // characterInfoCloseButton은 활성화 상태로 유지
    }

    private void OnCharacterInfoCloseButtonClicked()
    {
        // 입력 처리 중이면 무시
        if (isProcessingInput) return;

        // 클릭 쿨다운 체크
        if (Time.unscaledTime - lastClickTime < clickCool) return;

        // 클릭 시간 업데이트 및 입력 처리 플래그 설정
        lastClickTime = Time.unscaledTime;
        isProcessingInput = true;

        try
        {
            // 캐릭터 클릭 대기 중인 경우 상태 복원
            if (isWaitingForCharacterClick && !closeCharacterInfoOnce)
            {
                closeCharacterInfoOnce = true;

                // 화살표 숨기기 및 패널 복원
                ResetUIState();

                // 모든 버튼과 캐릭터 상호작용 다시 활성화
                EnableOtherButtons();
                EnableCharacterInteraction();

                // 캐릭터 클릭 대기 상태 해제
                isWaitingForCharacterClick = false;
                waitingCharacterSlot = null;

                NextScript();
            }

            HideAllArrows();
        }
        finally
        {
            ResetInputProcessingFlag().Forget();
        }
    }

    #endregion

    #region Drag Arrow Actions

    private void ActionDragArrow()
    {
        // 즉시 화면 클릭 차단 및 드래그 대기 상태 설정
        blockScreenClick = true;
        isWaitingForCharacterDrag = true;
        ActionDragArrowAsync().Forget();
    }

    private async UniTaskVoid ActionDragArrowAsync()
    {
        // OwnedCharacterSetup이 준비될 때까지 대기
        if (ownedCharacterSetup != null)
        {
            await UniTask.WaitUntil(() => ownedCharacterSetup.IsReady);
        }

        // 다음 사용 가능한 캐릭터 슬롯 찾기 (배치 횟수에 따라)
        Transform nextCharacterSlot = FindNextAvailableCharacterSlot();

        if (nextCharacterSlot != null)
        {
            // 패널을 투명하게 설정
            SetPanelTransparent();

            // 캐릭터 드래그 활성화 (정상적인 드래그 허용)
            EnableCharacterDragForPlacement();

            // 화살표를 대상 위에 표시하고 드래그 애니메이션 시작
            ShowDragArrowOnTarget(nextCharacterSlot);
        }
    }

    // DragArrow에서 사용: 캐릭터 드래그를 정상적으로 허용
    private void EnableCharacterDragForPlacement()
    {
        if (ownedCharacterSetup?.content != null)
        {
            DragMe[] dragMeComponents = ownedCharacterSetup.content.GetComponentsInChildren<DragMe>();
            foreach (var dragMe in dragMeComponents)
            {
                // 모든 DragMe 컴포넌트 활성화
                dragMe.enabled = true;

                // 임시로 추가된 Button 컴포넌트 제거 (IdolArrow에서 추가된 것)
                Button tempButton = dragMe.GetComponent<Button>();
                if (tempButton != null)
                {
                    DestroyImmediate(tempButton);
                }

                // raycastTarget 활성화
                Image dragMeImage = dragMe.GetComponent<Image>();
                if (dragMeImage != null)
                {
                    dragMeImage.raycastTarget = true;
                }
            }
        }
    }

    private void ShowDragArrowOnTarget(Transform target)
    {
        if (arrow == null)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        arrow.gameObject.SetActive(true);

        // 화살표를 최상위로 이동
        arrow.transform.SetAsLastSibling();

        PositionArrowOverTarget(target);

        StartDragArrowAnimation().Forget();
    }

    private async UniTaskVoid StartDragArrowAnimation()
    {
        if (arrow == null) return;
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        Vector3 originalPos = arrowRect.localPosition;
        Vector3 targetPos = originalPos + new Vector3(0f, 150f, 0f);

        float animationTime = 0f;
        float duration = 1f; // 속도도 조절 가능 (작을수록 빠름)

        while (arrow.gameObject.activeSelf && isPlaying)
        {
            animationTime += Time.unscaledDeltaTime;

            // 진행률
            float t = (animationTime % duration) / duration;

            // 일직선 이동
            Vector3 currentPos = Vector3.Lerp(originalPos, targetPos, t);
            arrowRect.localPosition = currentPos;

            // 목적지에 도달하면 다시 시작점으로 리셋
            if (t >= 1f)
            {
                animationTime = 0f;
            }

            await UniTask.Yield();
        }
    }

    // 캐릭터 배치 이벤트 핸들러 - 3캐릭터 순차 배치 처리
    private void OnCharacterPlaced()
    {
        // 드래그 대기 중이 아니면 무시
        if (!isWaitingForCharacterDrag)
            return;

        // 실제로 배치된 캐릭터 수를 직접 확인
        int actualPlacedCount = GetActualPlacedCharacterCount();

        // 실제 배치된 캐릭터 수가 이전보다 증가했는지 확인
        if (actualPlacedCount <= characterPlaceCount)
        {
            Debug.Log($"[TutorialStage] 실제 배치 수 변화 없음 - 현재: {actualPlacedCount}, 기존: {characterPlaceCount}");
            return; // 실제 배치가 없으면 무시
        }

        // 배치 횟수 업데이트
        characterPlaceCount = actualPlacedCount;

        Debug.Log($"[TutorialStage] {characterPlaceCount}번째 캐릭터 배치 완료!");

        // 화살표 숨기기 및 패널 복원
        ResetUIState();

        // 드래그 대기 상태 해제
        isWaitingForCharacterDrag = false;
        isWaitingForCharacterClick = false;
        waitingCharacterSlot = null;

        // 아직 배치해야 할 캐릭터가 남아있는지 확인
        if (characterPlaceCount < requiredCharacterCount)
        {
            // 다음 캐릭터 배치를 위해 현재 스크립트 다시 실행
            Debug.Log($"[TutorialStage] {requiredCharacterCount - characterPlaceCount}명 더 배치 필요");

            // 잠시 대기 후 다시 드래그 안내 시작
            DelayedDragArrowAsync().Forget();
        }
        else
        {
            // 모든 캐릭터 배치가 완료되었을 때만 화면 클릭 차단 해제
            blockScreenClick = false;
            NextScript();
        }
    }

    // 실제로 배치된 캐릭터 수를 확인하는 메서드
    private int GetActualPlacedCharacterCount()
    {
        int count = 0;

        if (draggableSlots != null)
        {
            foreach (var slot in draggableSlots)
            {
                // 슬롯에 캐릭터가 배치되어 있는지 확인
                if (slot != null && slot.characterData != null)
                {
                    count++;
                }
            }
        }

        Debug.Log($"[TutorialStage] 실제 배치된 캐릭터 수: {count}");
        return count;
    }

    private async UniTaskVoid DelayedDragArrowAsync()
    {
        await UniTask.Delay(500, DelayType.UnscaledDeltaTime);

        if (isPlaying)
        {
            // 다음 캐릭터 배치를 위해 상태 설정
            blockScreenClick = true;
            isWaitingForCharacterDrag = true;
            ActionDragArrowAsync().Forget();
        }
    }

    #endregion

    #region Skill Actions

    private void ActionSkillGageArrow()
    {
        ActionSkillGageArrowAsync().Forget();
    }

    private async UniTaskVoid ActionSkillGageArrowAsync()
    {
        // 이미 스킬 대기 중이면 무시
        if (isWaitingForSkillReady)
        {
            return; 
        }

        // 이미 준비된 스킬이 있는지 확인
        Transform skillReadyCharacter = FindSkillReadyCharacter();

        if (skillReadyCharacter != null)
        {
            // 화면 클릭 차단 해제 (스킬 이미 준비됨)
            blockScreenClick = false;

            // 게임 일시정지
            Time.timeScale = 0f;

            arrow.gameObject.SetActive(true);
            ArrowSkillReadyCharacter(skillReadyCharacter);
            StartArrowAnimation().Forget();
            return;
        }

        // 스킬이 아직 준비되지 않았으면 대기 상태로 설정
        isWaitingForSkillReady = true;

        await UniTask.WaitUntil(() => !isWaitingForSkillReady);
    }

    // 스킬이 준비되었을 때 호출되는 이벤트 핸들러
    private void OnSkillReady()
    {
        if (isWaitingForSkillReady)
        {
            isWaitingForSkillReady = false;

            // 화면 클릭 차단 해제
            blockScreenClick = false;

            // 게임 일시정지
            Time.timeScale = 0f;

            // 스킬 준비된 캐릭터 찾기
            Transform skillReadyCharacter = FindSkillReadyCharacter();
            if (skillReadyCharacter != null)
            {
                // 다음 스크립트로 이동
                currentScriptIndex++;

                // 현재 스크립트가 유효한지 확인
                if (currentScriptIndex < currentScripts.Count)
                {
                    ShowArrowOnSkillReadyCharacter(skillReadyCharacter);
                }
                else
                {
                    // 스크립트가 끝났으면 다음 로케이션으로
                    CheckNextLocation();
                }
            }
            else
            {
                NextScript();
            }
        }
    }

    // 스킬 준비된 캐릭터 위에 화살표 표시
    private void ShowArrowOnSkillReadyCharacter(Transform characterTransform)
    {
        if (arrow == null || characterTransform == null) return;

        ShowCurrentScriptTextOnly();

        arrow.gameObject.SetActive(true);
        ArrowSkillReadyCharacter(characterTransform);
        StartArrowAnimation().Forget();
    }

    // 텍스트만 표시하고 액션은 실행하지 않는 메서드
    private void ShowCurrentScriptTextOnly()
    {
        if (currentScriptIndex >= currentScripts.Count)
        {
            CheckNextLocation();
            return;
        }

        var script = currentScripts[currentScriptIndex];

        // 현재 스크립트 UI가 비활성화된 경우 활성화
        if (currentScriptUI != null && !currentScriptUI.gameObject.activeInHierarchy)
        {
            currentScriptUI.gameObject.SetActive(true);
        }

        SoundManager.Instance?.StopVoiceSFX();
        PlayVoiceForCurrentScript(script);

        // 액션 없이 텍스트만 타이핑 효과로 표시
        StartTypingEffect(script.Text, executeAction: false).Forget();
    }

    // 스킬 준비된 캐릭터 위에 화살표 위치 지정
    private void ArrowSkillReadyCharacter(Transform characterTransform)
    {
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        Camera uiCamera = Camera.main;
        if (uiCamera == null) return;

        // 캐릭터의 상단 위치 계산
        Vector3 characterTopPos = characterTransform.position;

        Renderer characterRenderer = characterTransform.GetComponent<Renderer>();
        if (characterRenderer != null)
        {
            characterTopPos.y += characterRenderer.bounds.size.y * 0.5f;
        }

        // 추가 오프셋
        characterTopPos.y += 2.5f;

        Vector3 screenPos = uiCamera.WorldToScreenPoint(characterTopPos);

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            arrowRect.parent as RectTransform,
            screenPos,
            null,
            out localPos
        );

        arrowRect.localPosition = localPos;
    }

    // 스킬이 준비된 캐릭터를 찾는 메서드
    private Transform FindSkillReadyCharacter()
    {
        if (ActiveSkillManager.Instance == null) return null;

        var readyCasters = ActiveSkillManager.Instance.GetAllReadySkillCasters();

        if (readyCasters.Count > 0)
        {
            return readyCasters[0].transform;
        }

        return null;
    }

    // 스킬 사용 - 음성 재생 후 스크립트 닫고 게임 재개
    private void ActionSkillUse()
    {
        ActionSkillUseAsync().Forget();
    }

    private async UniTaskVoid ActionSkillUseAsync()
    {
        // 화면 클릭 차단 (음성 재생 중 + BossAlert까지)
        blockScreenClick = true;

        // 음성 재생 완료 대기
        await WaitForVoiceComplete();

        HideAllArrows();

        // 스크립트 UI 닫기
        if (currentScriptUI != null)
        {
            currentScriptUI.gameObject.SetActive(false);
        }

        // 게임 재개
        StageManager.Instance.RestoreTimeScale();
        // 다음 스크립트(BossAlert)로 인덱스 이동 (ShowCurrentScript 호출 안함)
        currentScriptIndex++;
        
        // BossAlert 액션 직접 실행 (스크립트 표시는 보스 알림 후에 ActionBossAlertAsync에서 처리)
        ActionBossAlert();
    }

    #endregion

    #region Other Action Methods

    private void ActionBuffStageLine()
    {
        ActionBuffStageLineAsync().Forget();
    }

    private async UniTaskVoid ActionBuffStageLineAsync()
    {
        // stageBorderImage 영역 UI 테두리 표시
        await ShowStageBorderImageBorder();
    }

    private void ActionInfoArrow()
    {
        if (infoButton != null)
        {
            ShowArrowOnTarget(infoButton.transform);
        }
    }

    private void ActionReturnArrow()
    {
        HideAllArrows();
        if (returnButton != null)
        {
            ShowArrowOnTarget(returnButton.transform); 
        }
    }

    private void ActionStartArrow()
    {
        // 즉시 화면 클릭 차단 및 스타트 버튼 대기 상태 설정
        blockScreenClick = true;
        isWaitingForStartButton = true;
        ActionStartArrowAsync().Forget();
    }


    //private void ActionStartArrow()
    //{
    //    HideAllArrows();

    //    if (startButton != null)
    //    {
    //        // 패널을 투명하게 설정
    //        SetPanelTransparent();

    //        // 다른 버튼들 비활성화
    //        DisableOtherButtons();

    //        // 캐릭터 드래그/클릭 비활성화
    //        DisableCharacterInteraction();

    //        // 배치된 캐릭터들을 빼는 것 방지
    //        DisableCharacterSlotDragging();

    //        // 스타트 버튼 클릭 이벤트 등록
    //        startButton.onClick.AddListener(OnStartButtonClicked);

    //        // 스타트 버튼 대기 상태로 설정
    //        isWaitingForStartButton = true;

    //        ShowArrowOnTarget(startButton.transform);
    //    }
    //}

    private async UniTaskVoid ActionStartArrowAsync()
    {
        HideAllArrows();

        if (startButton != null)
        {
            // 패널을 투명하게 설정
            SetPanelTransparent();

            // 다른 버튼들 비활성화
            DisableOtherButtons();

            // 캐릭터 드래그/클릭 비활성화
            DisableCharacterInteraction();

            // 배치된 캐릭터들을 빼는 것 방지
            DisableCharacterSlotDragging();

            // 스타트 버튼을 일시적으로 비활성화 (음성 재생이 끝날 때까지)
            startButton.interactable = false;

            // 음성 재생 완료까지 대기
            await WaitForVoiceComplete();

            // 음성이 끝난 후에 스타트 버튼 활성화 및 이벤트 등록
            if (startButton != null)
            {
                startButton.interactable = true;
                startButton.onClick.AddListener(OnStartButtonClicked);

                ShowArrowOnTarget(startButton.transform);
            }

            // 화면 클릭 차단 해제는 하지 않음 (스타트 버튼 클릭할 때까지 차단 유지)
            // blockScreenClick은 OnStartButtonClicked에서 해제
        }
    }

    private void ActionStopLineArrow()
    {
        HideAllArrows();

        if (fence != null)
        {
            ShowArrowOnFence();
        }
    }

    private void ActionFeverArrow()
    {
        HideAllArrows();

        // 게임 일시정지
        Time.timeScale = 0f;

        if (feverButton != null)
        {
            ShowArrowOnTarget(feverButton.transform);
        }
    }

    //private void ActionBossFight()
    //{
    //    HideAllArrows();

    //    // 게임 재개
    //    Time.timeScale = 1f;

    //    if (currentScriptUI != null)
    //    {
    //        currentScriptUI.gameObject.SetActive(false);
    //    }

    //    isPlaying = false;

    //    // 바로 StageClear 대기 시작
    //    ActionStageClearAsync().Forget();
    //}

    private void ActionBossFight()
    {
        // 화면 클릭 즉시 차단 (StageClear로 넘어가는 것 방지)
        blockScreenClick = true;
        ActionBossFightAsync().Forget();
    }

    private async UniTaskVoid ActionBossFightAsync()
    {
        HideAllArrows();

        // 음성 재생 완료까지 대기 (대사 재생)
        await WaitForVoiceComplete();

        // 게임 재개
        StageManager.Instance.RestoreTimeScale();

        if (currentScriptUI != null)
        {
            currentScriptUI.gameObject.SetActive(false);
        }

        isPlaying = false;

        // 스테이지 클리어 대기 시작 (보스 처치까지)
        ActionStageClearAsync().Forget();
    }
    private void ActionResumeBattle()
    {
        // 화면 클릭 차단 (스킬 준비될 때까지)
        blockScreenClick = true;

        StageManager.Instance.RestoreTimeScale();

        // 스킬이 이미 준비되었는지 확인
        Transform skillReadyCharacter = FindSkillReadyCharacter();

        if (skillReadyCharacter != null)
        {
            // 이미 스킬이 준비된 경우, 음성 재생 후 바로 다음 스크립트로
            WaitForVoiceAndNextScript().Forget();
        }
        else
        {
            // 스킬이 아직 준비되지 않은 경우, 음성 재생 후 스킬 준비 대기
            WaitForVoiceAndSkillReady().Forget();
        }
    }

    // 음성 재생 후 바로 다음 스크립트 진행
    private async UniTaskVoid WaitForVoiceAndNextScript()
    {
        await WaitForVoiceComplete();
        NextScript();
    }

    // 음성 재생 후 스킬 준비 대기
    private async UniTaskVoid WaitForVoiceAndSkillReady()
    {
        await WaitForVoiceComplete();

        // 화면 클릭 UI 숨기기
        HideAllArrows();
        SetPanelTransparent();

        if (currentScriptUI != null)
        {
            currentScriptUI.gameObject.SetActive(false);
        }

        // 스킬 준비 대기 상태로 설정
        isWaitingForSkillReady = true;
        Debug.Log("[TutorialStage] 스킬이 준비될 때까지 대기 중...");
    }

    private void ActionStopGame()
    {
        Time.timeScale = 0f;

        if (currentScriptUI != null)
        {
            currentScriptUI.gameObject.SetActive(true);
        }
    }

    #endregion

    #region Boss and Stage Clear Methods

    private void ActionBossAlert()
    {
        ActionBossAlertAsync().Forget();
    }

    private async UniTaskVoid ActionBossAlertAsync()
    {
        // 화면 클릭 차단 (보스 알람이 나오고 닫힐 때까지)
        blockScreenClick = true;

        // 스크립트 UI 숨기기 (보스 알람 나올 때까지)
        if (currentScriptUI != null)
        {
            currentScriptUI.gameObject.SetActive(false);
        }

        // BossAlert UI가 활성화될 때까지 대기
        await UniTask.WaitUntil(() =>
        {
            bool isActive = bossAlertUI != null && bossAlertUI.gameObject.activeInHierarchy;
            return isActive;
        });

        Debug.Log("[TutorialStage] BossAlert UI 활성화됨, 닫힐 때까지 대기...");

        // BossAlert UI가 닫힐 때까지 대기
        await UniTask.WaitUntil(() =>
        {
            bool isClosed = bossAlertUI == null || !bossAlertUI.gameObject.activeInHierarchy;
            return isClosed;
        });

        Debug.Log("[TutorialStage] BossAlert UI 닫힘, 스크립트 표시 시작");

        // 화면 클릭 차단 해제
        blockScreenClick = false;

        // 게임 일시정지
        Time.timeScale = 0f;

        if (currentScriptUI != null)
        {
            currentScriptUI.gameObject.SetActive(true);
        }

        // 튜토리얼 다시 시작
        isPlaying = true;

        // 현재 스크립트(BossAlert)의 텍스트를 표시 (NextScript 하지 않음)
        var script = currentScripts[currentScriptIndex];
        
        // 음성 재생
        PlayVoiceForCurrentScript(script);
        
        // 타이핑 효과로 텍스트 표시 (executeAction=false로 다시 BossAlert 액션 실행 방지)
        StartTypingEffect(script.Text, false).Forget();
    }

    // StageClear 액션이 있는 스크립트 찾기
    private void FindAndSetStageClearScriptIndex()
    {
        if (currentScripts == null) return;

        for (int i = 0; i < currentScripts.Count; i++)
        {
            if (currentScripts[i].Action == "StageClear")
            {
                currentScriptIndex = i;
                return;
            }
        }
    }

    private async UniTaskVoid ActionStageClearAsync()
    {
        // VictoryPanel이 활성화될 때까지 대기 (스테이지 클리어 시 나타남)
        await UniTask.WaitUntil(() =>
        {
            return stageManager != null &&
                   stageManager.VictoryPanel != null &&
                   stageManager.VictoryPanel.gameObject.activeInHierarchy;
        });

        // 화면 클릭 차단 해제
        blockScreenClick = false;

        this.transform.SetAsLastSibling();

        Canvas tutorialCanvas = this.GetComponent<Canvas>();
        if (tutorialCanvas == null)
        {
            tutorialCanvas = this.gameObject.AddComponent<Canvas>();
            tutorialCanvas.overrideSorting = true;
        }

        // VictoryPanel보다 높은 Sort Order 설정
        tutorialCanvas.sortingOrder = 1000;

        // 튜토리얼 스크립트 UI 다시 활성화
        if (currentScriptUI != null)
        {
            currentScriptUI.gameObject.SetActive(true);
        }
        else
        {
            CreateScriptUI();
        }

        // 튜토리얼 다시 시작
        isPlaying = true;

        // StageClear 스크립트 찾아서 표시 ("튜토리얼을 클리어했어!")
        FindAndSetStageClearScriptIndex();
        ShowCurrentScript();
    }

    #endregion

    #region UI Helper Methods

    private Transform FindFirstCharacterSlot()
    {
        if (ownedCharacterSetup?.content != null && ownedCharacterSetup.content.childCount > 0)
        {
            Transform firstSlot = ownedCharacterSetup.content.GetChild(0);
            if (firstSlot.GetComponent<DragMe>() != null)
            {
                return firstSlot;
            }
        }
        return null;
    }

    private Transform FindNextAvailableCharacterSlot()
    {
        if (ownedCharacterSetup?.content == null) return null;

        for (int i = 0; i < ownedCharacterSetup.content.childCount; i++)
        {
            Transform slot = ownedCharacterSetup.content.GetChild(i);
            DragMe dragMe = slot.GetComponent<DragMe>();

            // 잠겨있지 않은(배치되지 않은) 캐릭터 슬롯 찾기
            if (dragMe != null && !dragMe.IsLocked)
            {
                return slot;
            }
        }
        return null;
    }

    private void SetPanelTransparent()
    {
        if (panelBackgroundImage != null)
        {
            Color bgColor = panelBackgroundImage.color;
            bgColor.a = 0f;
            panelBackgroundImage.color = bgColor;
        }

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = false;
    }

    private void RestorePanel()
    {
        if (panelBackgroundImage != null)
        {
            Color bgColor = panelBackgroundImage.color;
            bgColor.a = 0.5f;
            panelBackgroundImage.color = bgColor;
        }

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
    }

    // 다른 버튼들 비활성화
    private void DisableOtherButtons()
    {
        if (returnButton != null)
            returnButton.interactable = false;

        if (infoButton != null)
            infoButton.interactable = false;

        if (characterInfoCloseButton != null)
            characterInfoCloseButton.interactable = false;
    }

    // 다른 버튼들 다시 활성화
    private void EnableOtherButtons()
    {
        if (returnButton != null)
            returnButton.interactable = true;

        if (infoButton != null)
            infoButton.interactable = true;

        if (characterInfoCloseButton != null)
            characterInfoCloseButton.interactable = true;
    }

    // 캐릭터 상호작용 비활성화
    private void DisableCharacterInteraction()
    {
        if (ownedCharacterSetup?.content != null)
        {
            // OwnedCharacterSetup의 CanvasGroup으로 상호작용 차단
            CanvasGroup characterCanvasGroup = ownedCharacterSetup.content.GetComponent<CanvasGroup>();
            if (characterCanvasGroup == null)
            {
                characterCanvasGroup = ownedCharacterSetup.content.gameObject.AddComponent<CanvasGroup>();
            }
            characterCanvasGroup.blocksRaycasts = false;

            // 추가적으로 각 DragMe 컴포넌트의 raycastTarget 비활성화
            DragMe[] dragMeComponents = ownedCharacterSetup.content.GetComponentsInChildren<DragMe>();
            foreach (var dragMe in dragMeComponents)
            {
                Image dragMeImage = dragMe.GetComponent<Image>();
                if (dragMeImage != null)
                {
                    dragMeImage.raycastTarget = false;
                }
            }
        }
    }

    // 모든 캐릭터 상호작용 활성화
    private void EnableCharacterInteraction()
    {
        if (ownedCharacterSetup?.content != null)
        {
            // OwnedCharacterSetup의 CanvasGroup 상호작용 복원
            CanvasGroup characterCanvasGroup = ownedCharacterSetup.content.GetComponent<CanvasGroup>();
            if (characterCanvasGroup != null)
            {
                characterCanvasGroup.blocksRaycasts = true;
            }

            // 모든 DragMe 컴포넌트의 raycastTarget 복원
            DragMe[] dragMeComponents = ownedCharacterSetup.content.GetComponentsInChildren<DragMe>();
            foreach (var dragMe in dragMeComponents)
            {
                // DragMe 컴포넌트 활성화
                dragMe.enabled = true;

                Image dragMeImage = dragMe.GetComponent<Image>();
                if (dragMeImage != null)
                {
                    dragMeImage.raycastTarget = true;
                }

                // 임시로 추가된 Button 컴포넌트 제거
                Button tempButton = dragMe.GetComponent<Button>();
                if (tempButton != null)
                {
                    DestroyImmediate(tempButton);
                }
            }
        }
    }

    private void DisableCharacterSlotDragging()
    {
        // DraggableSlot들을 찾아서 드래그 기능 비활성화
        foreach (var slot in draggableSlots)
        {
            // DraggableSlot 컴포넌트 비활성화 (드래그 방지)
            slot.enabled = false;

            // 혹시 Image 컴포넌트가 있다면 raycast 차단
            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.raycastTarget = false;
            }

            // receivingImage도 raycast 차단
            if (slot.receivingImage != null)
            {
                slot.receivingImage.raycastTarget = false;
            }
        }

        Debug.Log("[TutorialStage] 배치된 캐릭터 슬롯 드래그 비활성화 완료");
    }

    // 배치된 캐릭터 슬롯(DraggableSlot)의 드래그 기능 복원
    private void EnableCharacterSlotDragging()
    {
        // DraggableSlot들을 찾아서 드래그 기능 복원
        foreach (var slot in draggableSlots)
        {
            // DraggableSlot 컴포넌트 활성화
            slot.enabled = true;

            // Image 컴포넌트 raycast 복원
            Image slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.raycastTarget = true;
            }

            // receivingImage raycast 복원
            if (slot.receivingImage != null)
            {
                slot.receivingImage.raycastTarget = true;
            }
        }
    }

    #endregion

    #region Arrow and Animation Methods

    private void ShowArrowOnTarget(Transform target)
    {
        if (arrow == null || target == null) return;

        arrow.gameObject.SetActive(true);
        PositionArrowOverTarget(target);
        StartArrowAnimation().Forget();
    }

    private void PositionArrowOverTarget(Transform target)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();

        if (targetRect != null && arrowRect != null)
        {
            Vector3 targetWorldPos = targetRect.TransformPoint(targetRect.rect.center);
            Vector3 arrowLocalPos = arrowRect.parent.InverseTransformPoint(targetWorldPos);
            arrowLocalPos.y += targetRect.rect.height * 0.5f + 70f;
            arrowRect.localPosition = arrowLocalPos;
        }
    }

    private async UniTaskVoid StartArrowAnimation()
    {
        if (arrow == null) return;

        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        Vector3 originalPos = arrowRect.localPosition;
        float animationTime = 0f;

        while (arrow.gameObject.activeSelf && isPlaying)
        {
            if (arrow == null || arrowRect == null) break;

            animationTime += Time.unscaledDeltaTime;
            float yOffset = Mathf.Sin(animationTime * 2f) * 10f;
            arrowRect.localPosition = originalPos + new Vector3(0, yOffset, 0);
            await UniTask.Yield();
        }
    }

    // fence 위에 화살표 표시
    private void ShowArrowOnFence()
    {
        if (arrow == null || fence == null) return;

        arrow.gameObject.SetActive(true);
        PositionArrowOverFence();
        StartArrowAnimation().Forget();
    }

    // fence 위에 화살표 위치 지정
    private void PositionArrowOverFence()
    {
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        Camera uiCamera = Camera.main;

        if (uiCamera != null)
        {
            // fence의 상단 위치 계산 (Renderer 바운드 사용)
            Vector3 fenceTopPos = fence.transform.position;

            Renderer fenceRenderer = fence.GetComponent<Renderer>();
            if (fenceRenderer != null)
            {
                fenceTopPos.y += fenceRenderer.bounds.size.y * 0.5f;
            }

            // 추가 오프셋
            fenceTopPos.y += 1f; // fence 위 1유닛 위에 화살표 표시

            Vector3 screenPos = uiCamera.WorldToScreenPoint(fenceTopPos);

            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                arrowRect.parent as RectTransform,
                screenPos,
                null,
                out localPos
            );

            arrowRect.localPosition = localPos;
        }
    }

    private void HideAllArrows()
    {
        if (arrow != null)
        {
            arrow.gameObject.SetActive(false);
        }

        HideStageAreaBorder();
    }

    // 화살표 숨기기 + 패널 복원을 함께 수행하는 헬퍼 메서드
    private void ResetUIState()
    {
        HideAllArrows();
        RestorePanel();
    }

    #endregion

    #region Stage Border Effects

    private async UniTask ShowStageBorderImageBorder()
    {
        if (stageBorderImage == null)
        {
            Debug.LogWarning("[TutorialStage] stageBorderImage가 설정되지 않았습니다.");
            return;
        }

        // UI 테두리 생성
        await DrawStageBorderImageBorder();
    }

    private async UniTask DrawStageBorderImageBorder()
    {
        RectTransform stageRect = stageBorderImage.GetComponent<RectTransform>();
        if (stageRect == null)
        {
            Debug.LogWarning("[TutorialStage] stageBorderImage에 RectTransform이 없습니다.");
            return;
        }

        // 기존 테두리 정리
        if (stageBorderParent != null)
        {
            Destroy(stageBorderParent);
            stageBorderParent = null;
        }

        // UI 테두리 오브젝트 생성
        await CreateUIBorderEffect(stageRect);

        Debug.Log("[TutorialStage] stageBorderImage UI 테두리 표시 완료");
    }

    private async UniTask CreateUIBorderEffect(RectTransform targetRect)
    {
        // 테두리 효과를 위한 부모 오브젝트 생성
        GameObject borderParent = new GameObject("StageBorderEffect");

        // stageBorderImage와 같은 부모에 배치하여 정확한 위치 매칭
        borderParent.transform.SetParent(targetRect.parent, false);

        RectTransform borderParentRect = borderParent.AddComponent<RectTransform>();

        // stageBorderImage와 동일한 앵커, 위치, 크기로 설정
        borderParentRect.anchorMin = targetRect.anchorMin;
        borderParentRect.anchorMax = targetRect.anchorMax;
        borderParentRect.anchoredPosition = targetRect.anchoredPosition;
        borderParentRect.sizeDelta = targetRect.sizeDelta;

        // stageBorderImage의 실제 크기 가져오기
        float width = targetRect.rect.width;
        float height = targetRect.rect.height;

        // 사각형의 4개 모서리 점 (로컬 좌표, 중앙 기준)
        Vector2 bottomLeft = new Vector2(-width * 0.5f, -height * 0.5f);
        Vector2 topLeft = new Vector2(-width * 0.5f, height * 0.5f);
        Vector2 topRight = new Vector2(width * 0.5f, height * 0.5f);
        Vector2 bottomRight = new Vector2(width * 0.5f, -height * 0.5f);

        // 4개의 테두리 라인 생성 (두껍게)
        CreateBorderLine(borderParent, topLeft, topRight, "TopBorder");         // 상단
        CreateBorderLine(borderParent, bottomLeft, topLeft, "LeftBorder");     // 좌측
        CreateBorderLine(borderParent, topRight, bottomRight, "RightBorder");   // 우측
        CreateBorderLine(borderParent, bottomLeft, bottomRight, "BottomBorder"); // 하단

        // stageBorderImage보다 위에 표시되도록 설정
        borderParent.transform.SetSiblingIndex(targetRect.GetSiblingIndex() + 1);

        // 애니메이션 시작
        StartUIBorderAnimation(borderParent).Forget();

        // 참조 저장 (정리를 위해)
        stageBorderParent = borderParent;

        await UniTask.CompletedTask;
    }

    private void CreateBorderLine(GameObject parent, Vector2 start, Vector2 end, string name)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(parent.transform, false);

        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        Image lineImage = lineObj.AddComponent<Image>();

        // 라인 색상 설정 (밝은 노란색, 불투명)
        lineImage.color = Color.yellow;
        lineImage.raycastTarget = false; // 클릭 이벤트 차단 방지

        // 라인 위치와 크기 계산
        Vector2 direction = end - start;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 라인 설정 (중앙 기준점 사용, 두껍게)
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.sizeDelta = new Vector2(length, 12f); // 12픽셀 두께로 두껍게
        lineRect.anchoredPosition = (start + end) * 0.5f;
        lineRect.rotation = Quaternion.Euler(0, 0, angle);
    }

    private async UniTaskVoid StartUIBorderAnimation(GameObject borderParent)
    {
        if (borderParent == null) return;

        Image[] borderImages = borderParent.GetComponentsInChildren<Image>();
        Color originalColor = Color.yellow;
        float animationTime = 0f;

        while (borderParent != null && borderParent.activeSelf && isPlaying)
        {
            animationTime += Time.unscaledDeltaTime * 2f; // 애니메이션 속도

            // 깜빡이는 효과 (부드러운 페이드)
            float alpha = (Mathf.Sin(animationTime) + 1f) * 0.5f;
            alpha = Mathf.Lerp(0.6f, 1f, alpha); // 60%~100% 투명도로 더 선명하게

            Color newColor = originalColor;
            newColor.a = alpha;

            foreach (var borderImage in borderImages)
            {
                if (borderImage != null)
                {
                    borderImage.color = newColor;
                }
            }

            await UniTask.Yield();
        }
    }

    private void HideStageAreaBorder()
    {
        if (stageBorderParent != null)
        {
            stageBorderParent.SetActive(false);
        }
    }

    #endregion

    #region Button Event Handlers

    private void OnStartButtonClicked()
    {
        // 입력 처리 중이면 무시
        if (isProcessingInput) return;

        // 클릭 쿨다운 체크
        if (Time.unscaledTime - lastClickTime < clickCool) return;

        // 클릭 시간 업데이트 및 입력 처리 플래그 설정
        lastClickTime = Time.unscaledTime;
        isProcessingInput = true;

        try
        {
            // 버튼 이벤트 해제
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartButtonClicked);
            }

            // 화살표 숨기기 및 패널 복원
            ResetUIState();

            // 다른 버튼들 다시 활성화
            EnableOtherButtons();

            // 캐릭터 드래그/클릭 다시 활성화
            EnableCharacterInteraction();

            EnableCharacterSlotDragging();

            // 스타트 버튼 대기 상태 해제 및 화면 클릭 차단 해제
            isWaitingForStartButton = false;
            blockScreenClick = false; // 추가: 화면 클릭 차단 해제

            if (currentScriptUI != null)
            {
                currentScriptUI.gameObject.SetActive(false);
            }

            isPlaying = false;

            DelayedStopGameAsync().Forget();
        }
        finally
        {
            ResetInputProcessingFlag().Forget();
        }
    }

    // 후에 StopGame 액션을 실행하는 메서드 추가
    private async UniTaskVoid DelayedStopGameAsync()
    {
        await UniTask.Delay(5000, DelayType.UnscaledDeltaTime);

        isPlaying = true;

        // StopGame 액션 실행
        ActionStopGame();

        // 다음 스크립트로 진행
        NextScript();
    }

    #endregion

    #region Audio Methods

    private void PlayVoiceForCurrentScript(TutorialScriptCSVData script)
    {
        if (string.IsNullOrEmpty(script.Voice))
        {
            return;
        }

        SoundManager.Instance?.PlayVoiceSFX(script.Voice);
    }

    // 음성 재생 완료를 기다리는 공통 메서드
    private async UniTask WaitForVoiceComplete()
    {
        if (currentScripts != null && currentScriptIndex < currentScripts.Count)
        {
            var currentScript = currentScripts[currentScriptIndex];

            if (!string.IsNullOrEmpty(currentScript.Voice))
            {
                await UniTask.WaitUntil(() => !SoundManager.Instance.IsVoicePlaying());
                await UniTask.Delay(500, DelayType.UnscaledDeltaTime);
            }
        }
    }

    // 음성 재생 완료를 기다리고 UI를 숨기는 메서드
    private async UniTaskVoid WaitForVoiceAsync()
    {
        await WaitForVoiceComplete();

        // 화살표 숨기기
        HideAllArrows();
        SetPanelTransparent();

        // 현재 스크립트 UI 숨기기
        if (currentScriptUI != null)
        {
            currentScriptUI.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Unity Event Handlers

    private void OnDisable()
    {
        DraggableSlot.OnAnySlotChanged -= OnCharacterPlaced;

        if (isPlaying)
        {
            ForceReactivate().Forget();
        }
    }

    private async UniTaskVoid ForceReactivate()
    {
        await UniTask.Yield();

        // 오브젝트가 파괴되었는지 체크
        if (this == null) return;

        if (!gameObject.activeSelf && isPlaying)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
    }

    #endregion
}