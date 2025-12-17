using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class LoadSceneManager : MonoBehaviour
{
    public static LoadSceneManager Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    public void GoStage()
    {
        GameSceneManager.ChangeScene(SceneType.StageScene);
        Time.timeScale = 1.0f;
    }

    public void GoStage(int stageId, int startingWave = 1)
    {
        var gameData = SaveLoadManager.Data;
        gameData.selectedStageID = stageId;
        gameData.startingWave = startingWave;
        // 일반 스테이지 진입 시 무한 모드 플래그 리셋
        gameData.isInfiniteMode = false;
        gameData.infiniteStageId = 0;
        SaveLoadManager.SaveToServer().Forget();
        GoStage();
    }

    public void GoTestStage()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        GameSceneManager.ChangeScene(SceneType.TestStageScene);
#endif
    }

    public void GoTestStage(int stageId, int startingWave = 1)
    {
        var gameData = SaveLoadManager.Data;
        gameData.selectedStageID = stageId;
        gameData.startingWave = startingWave;
        SaveLoadManager.SaveToServer().Forget();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        GoTestStage();
#endif
    }

    public void GoLobby()
    {
        GameSceneManager.ChangeScene(SceneType.LobbyScene);
    }

    // ========== 무한 스테이지 ==========
    public void GoInfiniteStage(int infiniteStageId)
    {
        var gameData = SaveLoadManager.Data;
        gameData.isInfiniteMode = true;
        gameData.infiniteStageId = infiniteStageId;
        // 기본 스테이지 ID 설정 (UI/레이아웃 참조용)
        gameData.selectedStageID = 601;
        gameData.startingWave = 1;
        SaveLoadManager.SaveToServer().Forget();

        // InfinityStage 전용 씬으로 이동
        GameSceneManager.ChangeScene(SceneType.InfinityStage);
        Time.timeScale = 1.0f;
    }
}