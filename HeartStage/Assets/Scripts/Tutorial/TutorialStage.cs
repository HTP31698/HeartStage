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

    private GameObject stageBorderParent;
    [SerializeField] private Image stageBorderImage;

    private TutorialScriptPrefab currentScriptUI;
    private List<TutorialScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool isWaitingForCharacterClick = false; // 캐릭터 클릭 대기 상태
    private Transform waitingCharacterSlot; // 대기 중인 캐릭터 슬롯

    private bool isWaitingForCharacterDrag = false; // 캐릭터 드래그 대기 상태
    private bool isWaitingForStartButton = false; // 스타트 버튼 대기 상태 추가

    private int characterPlaceCount = 0; // 배치된 캐릭터 수 추적
    private int requiredCharacterCount = 3; // 필요한 캐릭터 수

    [SerializeField] private GameObject characterStage; // 캐릭터가 배치되는 스테이지
    [SerializeField] private Button infoButton; 

    private void Awake()
    {
        // 캐릭터 배치 이벤트 구독
        DraggableSlot.OnAnySlotChanged += OnCharacterPlaced;

        characterInfoCloseButton.onClick.AddListener(OnCharacterInfoCloseButtonClicked);
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

        StartLocationScript(3);
    }

    public void Close()
    {
        isPlaying = false;
        isTyping = false;
        isWaitingForCharacterClick = false;
        isWaitingForCharacterDrag = false;
        isWaitingForStartButton = false; // 추가
        waitingCharacterSlot = null;
        characterPlaceCount = 0; // 초기화

        HideAllArrows();
        RestorePanel();

        // 모든 버튼 다시 활성화
        EnableOtherButtons();

        // 캐릭터 상호작용 다시 활성화
        EnableCharacterInteraction();

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

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnScreenClicked();
        }
    }

    private void OnScreenClicked()
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
        StartTypingEffect(script.Text).Forget();
    }

    private async UniTask StartTypingEffect(string text)
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
            await UniTask.Delay(50, DelayType.UnscaledDeltaTime);
        }

        isTyping = false;
        ExecuteScriptAction(currentScripts[currentScriptIndex]);
    }

    private void NextScript()
    {
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
        HideAllArrows();
        RestorePanel();
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
                ActionIdolArrow();
                break;
            case "DragArrow":
                ActionDragArrow();
                break;
            case "BuffStageLine":
                ActionBuffStageLine();
                break;
            case "InfoArrow":
                ActionInfoArrow();            
                break;
            case "ReturnArrow":
                ActionReturnArrow();
                break;
            case "StartArrow":
                ActionStartArrow();
                break;
        }
    }

    private void ActionIdolArrow()
    {
        ActionIdolArrowAsync().Forget();
    }

    private async UniTaskVoid ActionIdolArrowAsync()
    {
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

            // 캐릭터 클릭 대기 상태로 설정
            isWaitingForCharacterClick = true;
            waitingCharacterSlot = firstCharacterSlot;

            // 화살표를 대상 위에 표시
            ShowArrowOnTarget(firstCharacterSlot);
        }
    }

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

    private void ActionDragArrow()
    {
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

            isWaitingForCharacterDrag = true;

            // 화살표를 대상 위에 표시하고 드래그 애니메이션 시작
            ShowDragArrowOnTarget(nextCharacterSlot);
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

        // 배치 횟수 증가
        characterPlaceCount++;

        Debug.Log($"[TutorialStage] {characterPlaceCount}번째 캐릭터 배치 완료!");

        // 화살표 숨기기 및 패널 복원
        HideAllArrows();
        RestorePanel();

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
            // 모든 캐릭터 배치 완료, 다음 스크립트로 진행
            Debug.Log($"[TutorialStage] 모든 캐릭터({requiredCharacterCount}명) 배치 완료!");
            NextScript();
        }
    }

    private async UniTaskVoid DelayedDragArrowAsync()
    {
        // 0.5초 대기 후 다시 드래그 안내
        await UniTask.Delay(500, DelayType.UnscaledDeltaTime);

        if (isPlaying)
        {
            ActionDragArrowAsync().Forget();
        }
    }

    private void ActionBuffStageLine()
    {
        ActionBuffStageLineAsync().Forget();
    }

    private async UniTaskVoid ActionBuffStageLineAsync()
    {
        // stageBorderImage 영역 UI 테두리 표시
        await ShowStageBorderImageBorder();
    }

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
        HideAllArrows();

        if (startButton != null)
        {
            // 패널을 투명하게 설정
            SetPanelTransparent();

            // 다른 버튼들 비활성화
            DisableOtherButtons();

            // 캐릭터 드래그/클릭 비활성화
            DisableCharacterInteraction();

            // 스타트 버튼 클릭 이벤트 등록
            startButton.onClick.AddListener(OnStartButtonClicked);

            // 스타트 버튼 대기 상태로 설정
            isWaitingForStartButton = true;

            ShowArrowOnTarget(startButton.transform);
        }
    }

    private void OnStartButtonClicked()
    {
        // 버튼 이벤트 해제
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        // 화살표 숨기기 및 패널 복원
        HideAllArrows();
        RestorePanel();

        // 다른 버튼들 다시 활성화
        EnableOtherButtons();

        // 캐릭터 드래그/클릭 다시 활성화
        EnableCharacterInteraction();

        // 스타트 버튼 대기 상태 해제
        isWaitingForStartButton = false;

        // 다음 스크립트로 진행
        NextScript();
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

    // 캐릭터 상호작용 활성화
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

            // 각 DragMe 컴포넌트의 raycastTarget 복원
            DragMe[] dragMeComponents = ownedCharacterSetup.content.GetComponentsInChildren<DragMe>();
            foreach (var dragMe in dragMeComponents)
            {
                Image dragMeImage = dragMe.GetComponent<Image>();
                if (dragMeImage != null)
                {
                    dragMeImage.raycastTarget = true;
                }
            }
        }
    }

    private void DisableOtherButtons()
    {
        if (returnButton != null)
            returnButton.interactable = false;

        if (infoButton != null)
            infoButton.interactable = false;

        if (characterInfoCloseButton != null)
            characterInfoCloseButton.interactable = false;
    }
    private void EnableOtherButtons()
    {
        if (returnButton != null)
            returnButton.interactable = true;

        if (infoButton != null)
            infoButton.interactable = true;

        if (characterInfoCloseButton != null)
            characterInfoCloseButton.interactable = true;
    }
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
            animationTime += Time.unscaledDeltaTime;
            float yOffset = Mathf.Sin(animationTime * 2f) * 10f;
            arrowRect.localPosition = originalPos + new Vector3(0, yOffset, 0);
            await UniTask.Yield();
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
        if (!gameObject.activeSelf && isPlaying)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
    }

    private void OnCharacterInfoCloseButtonClicked()
    {
        NextScript();
        HideAllArrows();
    }
}