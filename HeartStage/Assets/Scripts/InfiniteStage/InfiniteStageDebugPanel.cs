using UnityEngine;

/// <summary>
/// 무한 스테이지 테스트용 디버그 단축키
/// F1: 게임 시작
/// F2: 게임 패배
/// F3: 시간 +30초
/// F4: 시간 +60초
/// F5: 씬 리셋
/// </summary>
public class InfiniteStageDebugPanel : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        // F1: 게임 시작
        if (Input.GetKeyDown(KeyCode.F1))
        {
            StartGame();
        }

        // F2: 게임 패배
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ForceGameOver();
        }

        // F3: 시간 +30초
        if (Input.GetKeyDown(KeyCode.F3))
        {
            AddTime(30f);
        }

        // F4: 시간 +60초
        if (Input.GetKeyDown(KeyCode.F4))
        {
            AddTime(60f);
        }

        // F5: 씬 리셋
        if (Input.GetKeyDown(KeyCode.F5))
        {
            ResetScene();
        }
    }

    private void StartGame()
    {
        var manager = InfiniteStageManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[Debug] InfiniteStageManager not found");
            return;
        }

        manager.StartGame();
        Debug.Log("[Debug] F1: 게임 시작");
    }

    private void ForceGameOver()
    {
        var manager = InfiniteStageManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[Debug] InfiniteStageManager not found");
            return;
        }

        manager.GameOver();
        Debug.Log("[Debug] F2: 게임 패배");
    }

    private void AddTime(float seconds)
    {
        var manager = InfiniteStageManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[Debug] InfiniteStageManager not found");
            return;
        }

        // elapsedTime 필드 수정 (리플렉션)
        var field = manager.GetType().GetField("elapsedTime",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            float currentTime = (float)field.GetValue(manager);
            field.SetValue(manager, currentTime + seconds);
            Debug.Log($"[Debug] F3/F4: 시간 +{seconds}초 (Total: {currentTime + seconds}초)");
        }
        else
        {
            Debug.LogWarning("[Debug] elapsedTime field not found");
        }
    }

    private void ResetScene()
    {
        Debug.Log("[Debug] F5: 씬 리셋");
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
#endif
}
