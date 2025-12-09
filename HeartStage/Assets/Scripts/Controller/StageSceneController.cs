using Cysharp.Threading.Tasks;
using UnityEngine;

public class StageSceneController : MonoBehaviour
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

        // 2) UI 세팅 + 몬스터/보스 풀 & 스킬 프리웜까지 전부 끝날 때까지 대기
        while (!(stageSetup.IsReady
                 && ownedSetup.IsReady
                 && monsterSpawner.isInitialized))   // 여기 추가
        {
            await UniTask.Yield();
        }

        // 3) 진짜로 다 준비된 시점에서만 100% 찍기
        SceneLoader.SetProgressExternal(1.0f);

        // 4) 100% 상태 잠깐 보여주고
        await UniTask.Delay(300, DelayType.UnscaledDeltaTime);

        // 5) 게임 씬 준비 완료 알림
        GameSceneManager.NotifySceneReady(SceneType.StageScene, 100);

        // 6) 로딩 UI 닫기
        await SceneLoader.HideLoadingWithDelay(0);
    }
}
