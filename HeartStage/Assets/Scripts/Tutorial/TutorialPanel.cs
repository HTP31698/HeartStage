using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TutorialPanel : GenericWindow
{
    [SerializeField] private GameObject tutorialScriptPrefab;
    [SerializeField] private Transform contentParent; // TutorialScriptPrefab 생성할 부모

    private TutorialScriptPrefab currentScriptUI;
    private List<TutorialScriptCSVData> currentScripts;
    private int currentScriptIndex = 0;
    private bool isPlaying = false;
    private bool isTyping = false;

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

        if (currentScriptUI != null)
        {
            Destroy(currentScriptUI.gameObject);
            currentScriptUI = null;
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

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
    }

    private void OnScreenClicked()
    {
        // TutorialManager의 IsClickOnButton() 로직은 제외 (오버레이 UI이므로)

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
        // Location 1 → 2 → 3 ... 순서대로 진행
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
        Debug.Log("[TutorialPanel] 전체 튜토리얼 완료");

        // 튜토리얼 완료 플래그 설정
        if (SaveLoadManager.Data != null)
        {
            SaveLoadManager.Data.isTutorialCompleted = true;
            SaveLoadManager.SaveToServer().Forget();
        }

        // 패널 닫기
        Close();
    }
}