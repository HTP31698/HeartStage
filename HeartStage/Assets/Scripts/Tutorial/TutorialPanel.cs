using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanel : GenericWindow
{
    [SerializeField] private GameObject tutorialScriptPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Image arrow;

    [Header("Reference")]
    [SerializeField] private TutorialNickNameScript nicknameWindow;

    [Header("Battle Button Reference")]
    [SerializeField] private Button battleButton;

    [Header("Tutorial UI Control")]
    [SerializeField] private GameObject backgroundPanel; // BackGroundPanel GameObject

    private TutorialScriptPrefab currentScriptUI;
    private List<TutorialScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;
    private bool isWaitingForBattleButton = false; // 배틀 버튼 대기 상태

    public override void Open()
    {
        base.Open();

        // Location 1부터 시작
        StartLocationScript(1);
    }

    public override void Close()
    {
        base.Close();

        // 정리
        isPlaying = false;
        isTyping = false;
        isWaitingForBattleButton = false;

        // 백그라운드 패널 복원
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }

        // 화살표 숨기기
        if (arrow != null)
        {
            arrow.gameObject.SetActive(false);
        }

        if (currentScriptUI != null)
        {
            Destroy(currentScriptUI.gameObject);
            currentScriptUI = null;
        }

        // 배틀 버튼 이벤트 해제
        if (battleButton != null)
        {
            battleButton.onClick.RemoveListener(OnBattleButtonClicked);
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        // 배틀 버튼 대기 중이면 화면 클릭 무시
        if (isWaitingForBattleButton) return;

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

        Debug.Log($"[TutorialPanel] 스크립트 {currentScriptIndex}: {script.Name} - {script.Text.Substring(0, Mathf.Min(20, script.Text.Length))}...");

        // 타이핑 효과로 텍스트 표시
        StartTypingEffect(script.Text).Forget();
    }

    private async UniTask StartTypingEffect(string text)
    {
        if (currentScriptUI == null) return;

        isTyping = true;
        currentScriptUI.SetTutorialText("");

        float textSpeed = 0.1f;

        for (int i = 0; i <= text.Length; i++)
        {
            if (!isTyping || !isPlaying)
            {
                currentScriptUI.SetTutorialText(text);
                break;
            }

            currentScriptUI.SetTutorialText(text.Substring(0, i));
            await UniTask.Delay((int)(textSpeed * 1000), DelayType.UnscaledDeltaTime);
        }

        isTyping = false;

        ExecuteScriptAction(currentScripts[currentScriptIndex]);
    }

    private void OnScreenClicked()
    {
        if (nicknameWindow != null && nicknameWindow.IsOpen)
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
        HideBattleArrow();

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
                ShowBattleArrow();
                break;
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

    private void ShowBattleArrow()
    {
        if (arrow == null || battleButton == null)
        {
            return;
        }

        battleButton.gameObject.SetActive(true);

        // 배틀 버튼 클릭 이벤트 등록
        battleButton.onClick.AddListener(OnBattleButtonClicked);

        // 배틀 버튼 대기 상태로 설정
        isWaitingForBattleButton = true;

        // **백그라운드 패널 완전히 비활성화 (버튼 클릭 가능하게)**
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(false);
        }

        // 화살표 활성화
        arrow.gameObject.SetActive(true);

        // battleButton의 위치 가져오기
        RectTransform buttonRect = battleButton.GetComponent<RectTransform>();
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();

        if (buttonRect != null && arrowRect != null)
        {
            // 버튼의 중심점을 월드 좌표로 변환
            Vector3 buttonWorldPos = buttonRect.TransformPoint(buttonRect.rect.center);
            Vector3 arrowLocalPos = arrowRect.parent.InverseTransformPoint(buttonWorldPos);

            // 화살표를 버튼 위쪽으로 더 높게 오프셋 적용
            arrowLocalPos.y += buttonRect.rect.height * 0.5f + 80f;

            arrowRect.localPosition = arrowLocalPos;
        }

        StartArrowAnimation().Forget();
    }

    private void OnBattleButtonClicked()
    {
        Debug.Log("[TutorialPanel] 배틀 버튼 클릭됨 - 다음 튜토리얼로 진행");

        // 배틀 버튼 이벤트 해제
        if (battleButton != null)
        {
            battleButton.onClick.RemoveListener(OnBattleButtonClicked);
        }

        // 백그라운드 패널 복원
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }

        // 대기 상태 해제
        isWaitingForBattleButton = false;

        // 화살표 숨기기
        HideBattleArrow();

        // 다음 스크립트로 진행
        NextScript();
    }

    private void HideBattleArrow()
    {
        if (arrow != null)
        {
            arrow.gameObject.SetActive(false);
        }

        if (battleButton != null)
        {
            battleButton.gameObject.SetActive(false);
        }
    }

    private async UniTaskVoid StartArrowAnimation()
    {
        if (arrow == null) return;

        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        Vector3 originalPos = arrowRect.localPosition;
        float animationTime = 0f;

        while (arrow.gameObject.activeSelf)
        {
            animationTime += Time.unscaledDeltaTime;

            // 위아래로 부드럽게 움직이는 애니메이션
            float yOffset = Mathf.Sin(animationTime * 2f) * 10f;
            arrowRect.localPosition = originalPos + new Vector3(0, yOffset, 0);

            await UniTask.Yield();
        }
    }

    /// Inspector에서 BattleButton을 설정하는 메소드 (선택사항)
    public void SetBattleButton(Button button)
    {
        battleButton = button;
    }
}