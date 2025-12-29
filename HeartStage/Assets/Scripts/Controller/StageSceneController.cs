using Cysharp.Threading.Tasks;
using UnityEngine;

public class StageSceneController : MonoBehaviour
{
    [Header("세팅이 끝나야 하는 애들")]
    public StageSetupWindow stageSetup;
    public OwnedCharacterSetup ownedSetup;
    public MonsterSpawner monsterSpawner;

    [Header("튜토리얼")]
    public TutorialStage tutorialStage; 

    private async void Awake()
    {
        // 1) 참조 들어올 때까지 (Awake/Start 순서 안전망)
        while (stageSetup == null || ownedSetup == null || monsterSpawner == null)
            await UniTask.Yield();

        // 2) 🔹 컴포넌트 준비 대기 (가짜 진행은 SceneLoader.Update()에서 자동 처리)
        while (!(ownedSetup.IsReady && stageSetup.IsReady && monsterSpawner.isInitialized))
            await UniTask.Yield();

        // 🔹 완료 시 100%로 스냅
        SceneLoader.SetProgressExternal(1.0f);

        // 3) 100% 상태 잠깐 보여주기
        await UniTask.Delay(300, DelayType.UnscaledDeltaTime);

        // 4) 게임 씬 준비 완료 알림
        GameSceneManager.NotifySceneReady(SceneType.StageScene, 100);

        // 5) 로딩 UI 닫기
        await SceneLoader.HideLoadingWithDelay(0);

        // 6) 로딩 완료 후 튜토리얼 시작
        StartTutorialIfNeeded();
    }

    private void StartTutorialIfNeeded()
    {
        // TutorialStage가 존재하고 활성화되어 있다면 튜토리얼 시작
        if (tutorialStage != null && tutorialStage.gameObject.activeSelf)
        {
            tutorialStage.StartLocationScript(3);
            Debug.Log("[StageSceneController] 로딩 완료 후 튜토리얼 시작");
        }
    }
}