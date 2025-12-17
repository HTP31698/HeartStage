#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage 씬용 디버그 컨트롤러
/// F1: 시작, F2: 정지/재개, F3: 이전 웨이브, F4: 다음 웨이브, F5: 씬 새로고침, F6: 테스트 캐릭터 UI (배치 중에만)
/// 삭제 방법: 이 스크립트 파일 삭제 + 씬에서 컴포넌트 제거
/// </summary>
public class StageDebugController : MonoBehaviour
{
    [Header("F6 테스트 UI - Inspector 연결 필수")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject testCharacterPrefab;   // TestCharacterSelect 프리팹
    [SerializeField] private GameObject dropdownFilterPrefab;  // CharacterDropdowns 프리팹

    private GameObject testCharacterUI;
    private GameObject dropdownFilterUI;
    private bool isPaused = false;

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

        // F3: 이전 웨이브
        if (Input.GetKeyDown(KeyCode.F3))
        {
            OnF3_PreviousWave();
        }

        // F4: 다음 웨이브
        if (Input.GetKeyDown(KeyCode.F4))
        {
            OnF4_NextWave();
        }

        // F5: 씬 새로고침
        if (Input.GetKeyDown(KeyCode.F5))
        {
            OnF5_ReloadScene();
        }

        // F6: 테스트 캐릭터 UI 토글
        if (Input.GetKeyDown(KeyCode.F6))
        {
            OnF6_ToggleTestUI();
        }
    }

    private void OnF1_Start()
    {
        Debug.Log("[StageDebug] F1: 게임 시작");

        // StageSetupWindow.OnStageStarted 이벤트 발생시키기
        // 이미 시작됐으면 무시됨 (MonsterSpawner에서 체크)
        StageSetupWindow.OnStageStarted?.Invoke();
    }

    private void OnF2_TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log($"[StageDebug] F2: {(isPaused ? "정지" : "재개")} (TimeScale: {Time.timeScale})");
    }

    private void OnF3_PreviousWave()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogWarning("[StageDebug] F3: StageManager 없음");
            return;
        }

        // 웨이브 감소 (최소 1)
        int currentWave = StageManager.Instance.waveOrder;
        int newWave = Mathf.Max(1, currentWave - 1);

        if (newWave == currentWave)
        {
            Debug.Log("[StageDebug] F3: 이미 첫 번째 웨이브입니다");
            return;
        }

        StageManager.Instance.waveOrder = newWave;
        Debug.Log($"[StageDebug] F3: 이전 웨이브 ({currentWave} → {newWave})");
    }

    private void OnF4_NextWave()
    {
        if (StageManager.Instance == null)
        {
            Debug.LogWarning("[StageDebug] F4: StageManager 없음");
            return;
        }

        // 웨이브 증가
        int currentWave = StageManager.Instance.waveOrder;
        int newWave = currentWave + 1;

        // 최대 웨이브 체크 (wave1_id ~ wave4_id 중 유효한 웨이브 수)
        var stageData = StageManager.Instance.GetCurrentStageData();
        if (stageData != null)
        {
            int maxWave = 0;
            if (stageData.wave1_id > 0) maxWave = 1;
            if (stageData.wave2_id > 0) maxWave = 2;
            if (stageData.wave3_id > 0) maxWave = 3;
            if (stageData.wave4_id > 0) maxWave = 4;

            if (maxWave > 0)
            {
                newWave = Mathf.Min(newWave, maxWave);
            }

            if (newWave == currentWave)
            {
                Debug.Log($"[StageDebug] F4: 이미 마지막 웨이브입니다 ({maxWave})");
                return;
            }
        }

        StageManager.Instance.waveOrder = newWave;
        Debug.Log($"[StageDebug] F4: 다음 웨이브 ({currentWave} → {newWave})");
    }

    private void OnF5_ReloadScene()
    {
        Debug.Log("[StageDebug] F5: 씬 새로고침");
        Time.timeScale = 1f;

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
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
            Debug.LogError("[StageDebug] targetCanvas가 연결되지 않았습니다. Inspector에서 Canvas를 연결하세요.");
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
            Debug.LogWarning("[StageDebug] testCharacterPrefab이 연결되지 않았습니다.");
        }

        // CharacterDropdowns 프리팹 생성
        if (dropdownFilterPrefab != null)
        {
            dropdownFilterUI = Instantiate(dropdownFilterPrefab, targetCanvas.transform);
            dropdownFilterUI.name = "[Debug] CharacterDropdowns";
        }
        else
        {
            Debug.LogWarning("[StageDebug] dropdownFilterPrefab이 연결되지 않았습니다.");
        }

        Debug.Log("[StageDebug] F6: 테스트 UI 열기");
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

        Debug.Log("[StageDebug] F6: 테스트 UI 닫기");
    }

    private void OnDestroy()
    {
        CloseTestUI();
    }
}
#endif
