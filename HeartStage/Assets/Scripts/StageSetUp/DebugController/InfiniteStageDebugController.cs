#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// InfinityStage 씬용 디버그 컨트롤러
/// F1: 시작, F2: 정지/재개, F3: 이전 강화 레벨, F4: 다음 강화 레벨, F5: 씬 새로고침, F6: 테스트 캐릭터 UI (배치 중에만)
/// 삭제 방법: 이 스크립트 파일 삭제 + 씬에서 컴포넌트 제거
/// </summary>
public class InfiniteStageDebugController : MonoBehaviour
{
    [Header("F6 테스트 UI - Inspector 연결 필수")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject testCharacterPrefab;   // TestCharacterSelect 프리팹
    [SerializeField] private GameObject dropdownFilterPrefab;  // CharacterDropdowns 프리팹

    private GameObject testCharacterUI;
    private GameObject dropdownFilterUI;
    private bool isPaused = false;

    private void OnDestroy()
    {
        CloseTestUI();
    }

    private void Update()
    {
        // F1: 게임 시작
        if (Input.GetKeyDown(KeyCode.F1))
        {
            OnF1_Start();
        }

        // F2: 정지/재개 토글
        if (Input.GetKeyDown(KeyCode.F2))
        {
            OnF2_TogglePause();
        }

        // F3: 이전 강화 레벨
        if (Input.GetKeyDown(KeyCode.F3))
        {
            OnF3_PreviousLevel();
        }

        // F4: 다음 강화 레벨
        if (Input.GetKeyDown(KeyCode.F4))
        {
            OnF4_NextLevel();
        }

        // F5: 씬 새로고침
        if (Input.GetKeyDown(KeyCode.F5))
        {
            OnF5_ReloadScene();
        }

        // F6: 테스트 캐릭터 UI 토글 (배치 중에만)
        if (Input.GetKeyDown(KeyCode.F6))
        {
            OnF6_ToggleTestUI();
        }
    }

    private void OnF1_Start()
    {
        Debug.Log("[InfiniteDebug] F1: 게임 시작");

        // StageSetupWindow.OnStageStarted 이벤트 발생시키기
        StageSetupWindow.OnStageStarted?.Invoke();
    }

    private void OnF2_TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log($"[InfiniteDebug] F2: {(isPaused ? "정지" : "재개")} (TimeScale: {Time.timeScale})");
    }

    private void OnF3_PreviousLevel()
    {
        if (StageManager.Instance == null || !StageManager.Instance.isInfiniteMode)
        {
            Debug.LogWarning("[InfiniteDebug] F3: 무한 모드가 아니거나 StageManager 없음");
            return;
        }

        var sm = StageManager.Instance;
        int currentLevel = sm.infiniteEnhanceLevel;
        int newLevel = Mathf.Max(1, currentLevel - 1);

        if (newLevel == currentLevel)
        {
            Debug.Log("[InfiniteDebug] F3: 이미 Lv.1 입니다");
            return;
        }

        // 레벨에 맞는 시간으로 조정
        var data = sm.infiniteStageData;
        if (data != null)
        {
            sm.infiniteEnhanceLevel = newLevel;
            sm.infiniteElapsedTime = (newLevel - 1) * data.enhance_interval;

            Debug.Log($"[InfiniteDebug] F3: 이전 레벨 (Lv.{currentLevel} → Lv.{newLevel}, 시간: {sm.infiniteElapsedTime:F1}초)");
        }
    }

    private void OnF4_NextLevel()
    {
        if (StageManager.Instance == null || !StageManager.Instance.isInfiniteMode)
        {
            Debug.LogWarning("[InfiniteDebug] F4: 무한 모드가 아니거나 StageManager 없음");
            return;
        }

        var sm = StageManager.Instance;
        int currentLevel = sm.infiniteEnhanceLevel;
        int newLevel = currentLevel + 1;

        // 최대 레벨 제한 (100레벨)
        int maxLevel = 100;
        newLevel = Mathf.Min(newLevel, maxLevel);

        if (newLevel == currentLevel)
        {
            Debug.Log($"[InfiniteDebug] F4: 이미 최대 레벨입니다 (Lv.{maxLevel})");
            return;
        }

        // 레벨에 맞는 시간으로 조정
        var data = sm.infiniteStageData;
        if (data != null)
        {
            sm.infiniteEnhanceLevel = newLevel;
            sm.infiniteElapsedTime = (newLevel - 1) * data.enhance_interval;

            Debug.Log($"[InfiniteDebug] F4: 다음 레벨 (Lv.{currentLevel} → Lv.{newLevel}, 시간: {sm.infiniteElapsedTime:F1}초)");
        }
    }

    private void OnF5_ReloadScene()
    {
        Debug.Log("[InfiniteDebug] F5: 씬 새로고침");
        Time.timeScale = 1f;

        // 무한 스테이지 재진입 (SaveData 설정 포함)
        if (LoadSceneManager.Instance != null)
        {
            // 기본 무한 스테이지 ID로 재진입
            LoadSceneManager.Instance.GoInfiniteStage(90001);
        }
        else
        {
            // fallback: 현재 씬 리로드
            var currentScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentScene);
        }
    }

    private void OnF6_ToggleTestUI()
    {
        // 이미 열려있으면 닫기
        if (testCharacterUI != null || dropdownFilterUI != null)
        {
            CloseTestUI();
            return;
        }

        // 열기
        OpenTestUI();
    }

    private void OpenTestUI()
    {
        // Canvas 체크
        if (targetCanvas == null)
        {
            Debug.LogError("[InfiniteDebug] targetCanvas가 연결되지 않았습니다. Inspector에서 Canvas를 연결하세요.");
            return;
        }

        // TestCharacterSelect 프리팹 생성
        if (testCharacterPrefab != null)
        {
            testCharacterUI = Instantiate(testCharacterPrefab, targetCanvas.transform);
            testCharacterUI.name = "[Debug] TestCharacterSelect";
        }
        else
        {
            Debug.LogWarning("[InfiniteDebug] testCharacterPrefab이 연결되지 않았습니다.");
        }

        // CharacterDropdowns 프리팹 생성
        if (dropdownFilterPrefab != null)
        {
            dropdownFilterUI = Instantiate(dropdownFilterPrefab, targetCanvas.transform);
            dropdownFilterUI.name = "[Debug] CharacterDropdowns";
        }
        else
        {
            Debug.LogWarning("[InfiniteDebug] dropdownFilterPrefab이 연결되지 않았습니다.");
        }

        Debug.Log("[InfiniteDebug] F6: 테스트 UI 열기");
    }

    private void CloseTestUI()
    {
        if (testCharacterUI != null)
        {
            Destroy(testCharacterUI);
            testCharacterUI = null;
        }

        if (dropdownFilterUI != null)
        {
            Destroy(dropdownFilterUI);
            dropdownFilterUI = null;
        }

        Debug.Log("[InfiniteDebug] F6: 테스트 UI 닫기");
    }
}
#endif
