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

    private TutorialScriptPrefab currentScriptUI;
    private List<TutorialScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool isWaitingForCharacterClick = false; // 캐릭터 클릭 대기 상태
    private Transform waitingCharacterSlot; // 대기 중인 캐릭터 슬롯

    private bool isWaitingForCharacterDrag = false; // 캐릭터 드래그 대기 상태

    private void Awake()
    {
        // 캐릭터 배치 이벤트 구독
        DraggableSlot.OnAnySlotChanged += OnCharacterPlaced;

        characterInfoCloseButton.onClick.AddListener(OnCharacterInfoCloseButtonClicked);
    }

    private void OnEnable()
    {
        StartLocationScript(3);
    }

    public void Close()
    {
        isPlaying = false;
        isTyping = false;
        isWaitingForCharacterClick = false;
        isWaitingForCharacterDrag = false; 
        waitingCharacterSlot = null;

        HideAllArrows();
        RestorePanel();

        if (currentScriptUI != null)
        {
            Destroy(currentScriptUI.gameObject);
            currentScriptUI = null;
        }

        this.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (isWaitingForCharacterClick) return;
        if (isWaitingForCharacterDrag) return; // 드래그 대기 중에도 화면 클릭 무시


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

    private void OnCharacterSlotClicked(Transform clickedSlot)
    {
        if (!isWaitingForCharacterClick || waitingCharacterSlot != clickedSlot)
            return;

        // 대사창을 최상위로
        if (currentScriptUI != null)
        {
            currentScriptUI.transform.SetAsLastSibling();
        }

        isWaitingForCharacterClick = false;
        waitingCharacterSlot = null;
        HideAllArrows();
        //RestorePanel();
        NextScript();
    }

    private void ActionDragArrow()
    {
        ActionDragArrowAsync().Forget();
    }

    private async UniTaskVoid ActionDragArrowAsync()
    {
        Debug.Log("[TutorialStage] ActionDragArrowAsync 시작");

        // OwnedCharacterSetup이 준비될 때까지 대기
        if (ownedCharacterSetup != null)
        {
            Debug.Log($"[TutorialStage] OwnedCharacterSetup 대기 - IsReady: {ownedCharacterSetup.IsReady}");
            await UniTask.WaitUntil(() => ownedCharacterSetup.IsReady);
            Debug.Log("[TutorialStage] OwnedCharacterSetup 준비 완료");
        }

        // 첫 번째 캐릭터 슬롯 찾기
        Transform firstCharacterSlot = FindFirstCharacterSlot();
        Debug.Log($"[TutorialStage] 찾은 캐릭터 슬롯: {firstCharacterSlot?.name ?? "null"}");

        if (firstCharacterSlot != null)
        {
            // 패널을 투명하게 설정
            SetPanelTransparent();
            Debug.Log("[TutorialStage] 패널 투명화 완료");

            // 화살표를 대상 위에 표시하고 드래그 애니메이션 시작
            Debug.Log("[TutorialStage] ShowDragArrowOnTarget 호출 시작");
            ShowDragArrowOnTarget(firstCharacterSlot);
            Debug.Log("[TutorialStage] ShowDragArrowOnTarget 호출 완료");
        }
        else
        {
            Debug.LogError("[TutorialStage] 캐릭터 슬롯을 찾을 수 없습니다!");
        }
    }

    private void ShowDragArrowOnTarget(Transform target)
    {
        Debug.Log($"[TutorialStage] ShowDragArrowOnTarget - target: {target?.name}, arrow: {arrow?.name}");

        if (arrow == null)
        {
            Debug.LogError("[TutorialStage] ★★★ arrow가 null입니다! Inspector에서 할당 확인 필요! ★★★");
            return;
        }

        if (target == null)
        {
            Debug.LogError("[TutorialStage] ★★★ target이 null입니다! ★★★");
            return;
        }

        Debug.Log($"[TutorialStage] 화살표 활성화 전 상태: {arrow.gameObject.activeSelf}");
        arrow.gameObject.SetActive(true);
        Debug.Log($"[TutorialStage] 화살표 활성화 후 상태: {arrow.gameObject.activeSelf}");

        // 화살표를 최상위로 이동
        arrow.transform.SetAsLastSibling();
        Debug.Log($"[TutorialStage] 화살표 최상위 이동 완료");

        PositionArrowOverTarget(target);
        Debug.Log($"[TutorialStage] 화살표 위치 설정 완료");

        StartDragArrowAnimation().Forget();
        Debug.Log($"[TutorialStage] 드래그 애니메이션 시작");
    }

    private async UniTaskVoid StartDragArrowAnimation()
    {
        if (arrow == null) return;

        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        Vector3 originalPos = arrowRect.localPosition;
        float animationTime = 0f;

        // 드래그 방향 설정 (위 대각선 방향)
        Vector3 dragDirection = new Vector3(1f, 1f, 0f).normalized; // 우상단 방향
        float dragDistance = 50f; // 드래그 거리
        float dragSpeed = 2f; // 드래그 속도

        while (arrow.gameObject.activeSelf && isPlaying)
        {
            animationTime += Time.unscaledDeltaTime * dragSpeed;

            // 드래그 방향으로 부드럽게 움직이는 애니메이션
            // 사인파를 이용해서 위 대각선 방향으로 반복 움직임
            float dragOffset = Mathf.Sin(animationTime) * dragDistance;

            // 기본 위아래 움직임도 추가 (더 역동적으로)
            float yBounce = Mathf.Sin(animationTime * 1.5f) * 8f;

            // 최종 위치 계산
            Vector3 dragMovement = dragDirection * dragOffset;
            Vector3 finalPosition = originalPos + dragMovement + new Vector3(0, yBounce, 0);

            arrowRect.localPosition = finalPosition;

            await UniTask.Yield();
        }
    }

    // 캐릭터 배치 이벤트 핸들러
    private void OnCharacterPlaced()
    {
        // 드래그 대기 중이 아니면 무시
        if (!isWaitingForCharacterDrag)
            return;

        Debug.Log("[TutorialStage] 캐릭터가 스테이지에 배치되었습니다! 튜토리얼 계속 진행");

        // 대기 상태 해제
        isWaitingForCharacterDrag = false;

        // 화살표 숨기기 및 패널 복원
        HideAllArrows();
        RestorePanel();

        // 다음 스크립트로 진행
        NextScript();
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