using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class TutorialStage : GenericWindow
{
    [SerializeField] private GameObject tutorialScriptPrefab;
    [SerializeField] private Transform contentParent;

    private TutorialScriptPrefab currentScriptUI;
    private List<TutorialScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;

    protected override void Awake()
    {
        base.Awake();
        windowType = WindowType.TutorialStage;
        isOverlayWindow = true;
    }

    public override void Open()
    {
        base.Open();

        // Location 3부터 시작 (스테이지 씬 전용)
        StartLocationScript(3);
    }

    public override void Close()
    {
        base.Close();

        // 정리
        isPlaying = false;
        isTyping = false;

        if (currentScriptUI != null)
        {
            Destroy(currentScriptUI.gameObject);
            currentScriptUI = null;
        }
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

    public void StartLocationScript(int locationId)
    {
        // 스크립트 로드
        currentScripts = DataTableManager.TutorialScriptTable.GetLocationScripts(locationId);

        if (currentScripts == null || currentScripts.Count == 0)
        {
            Debug.LogWarning($"[TutorialStage] Location {locationId} 스크립트가 없음");
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

        Debug.Log($"[TutorialStage] 선배 천사 대사 {currentScriptIndex}: {script.Name} - {script.Text.Substring(0, Mathf.Min(20, script.Text.Length))}...");

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
            if (!isTyping || !isPlaying)
            {
                currentScriptUI.SetTutorialText(text);
                break;
            }

            currentScriptUI.SetTutorialText(text.Substring(0, i));
            await UniTask.Delay((int)(textSpeed * 1000), DelayType.UnscaledDeltaTime);
        }

        isTyping = false;
    }

    private void OnScreenClicked()
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

    private void NextScript()
    {
        currentScriptIndex++;
        ShowCurrentScript();
    }

    private void CheckNextLocation()
    {
        // Location 3 → 4 → 5... (필요에 따라 확장)
        int nextLocationId = GetNextLocationId();

        if (nextLocationId > 0)
        {
            Debug.Log($"[TutorialStage] 다음 Location {nextLocationId}로 이동");
            StartLocationScript(nextLocationId);
        }
        else
        {
            Debug.Log("[TutorialStage] 스테이지 튜토리얼 완료");
            CompleteTutorial();
        }
    }

    private int GetNextLocationId()
    {
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
        // 스테이지 튜토리얼 완료 플래그 설정
        if (SaveLoadManager.Data != null)
        {
            var saveData = SaveLoadManager.Data as SaveDataV1;
            if (saveData != null)
            {
                saveData.isStageTutorialCompleted = true;
                SaveLoadManager.SaveToServer().Forget();
            }
        }

        // 패널 닫기
        Close();
    }

    private void OnDisable()
    {
        // 튜토리얼이 진행 중이면 무조건 막기
        if (isPlaying)
        {
            // 다음 프레임에 강제로 다시 켜기
            ForceReactivate().Forget();
        }
    }

    // 재활성화
    private async UniTaskVoid ForceReactivate()
    {
        await UniTask.Yield();

        if (!gameObject.activeSelf && isPlaying)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
    }
}