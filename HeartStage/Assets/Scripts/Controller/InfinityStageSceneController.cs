using Cysharp.Threading.Tasks;
using UnityEngine;

public class InfinityStageSceneController : MonoBehaviour
{
    [Header("세팅이 끝나야 하는 애들")]
    public StageSetupWindow stageSetup;
    public OwnedCharacterSetup ownedSetup;
    public MonsterSpawner monsterSpawner;

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

        // 4) 무한 스테이지 씬 준비 완료 알림
        GameSceneManager.NotifySceneReady(SceneType.InfinityStage, 100);

        // 5) 로딩 UI 닫기
        await SceneLoader.HideLoadingWithDelay(0);
    }
}
