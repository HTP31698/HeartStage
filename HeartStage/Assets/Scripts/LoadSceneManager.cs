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
    private int pendingInfiniteStageId = 0;

    public void GoInfiniteStage(int infiniteStageId)
    {
        pendingInfiniteStageId = infiniteStageId;

        // 기존 Stage 씬으로 이동 (무한 모드 데이터는 씬 로드 후 적용)
        var gameData = SaveLoadManager.Data;

        // InfiniteStageTable에서 stage_position 가져와서 적절한 stageId 설정
        var infiniteData = DataTableManager.InfiniteStageTable?.Get(infiniteStageId);
        if (infiniteData != null)
        {
            // stage_position에 맞는 기본 스테이지 설정 (배경/위치용)
            gameData.selectedStageID = GetBaseStageIdForPosition(infiniteData.stage_position);
            gameData.startingWave = 1;
            gameData.isInfiniteMode = true;
            gameData.infiniteStageId = infiniteStageId;
            SaveLoadManager.SaveToServer().Forget();
        }

        GameSceneManager.ChangeScene(SceneType.StageScene);
        Time.timeScale = 1.0f;
    }

    // stage_position에 맞는 기본 스테이지 ID 반환
    private int GetBaseStageIdForPosition(int stagePosition)
    {
        // 무한 스테이지용 기본 스테이지 ID
        // position에 관계없이 기존 스테이지 중 하나 사용 (배경/레이아웃 참조용)
        // 현재 모든 스테이지가 position 3이므로 601(튜토리얼) 사용
        return 601;
    }

    public int GetPendingInfiniteStageId()
    {
        int id = pendingInfiniteStageId;
        pendingInfiniteStageId = 0;
        return id;
    }
}