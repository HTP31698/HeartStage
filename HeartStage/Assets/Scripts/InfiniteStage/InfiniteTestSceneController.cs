using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 무한 스테이지 테스트 씬 컨트롤러
/// - 기존 StageSetupWindow, TestCharacterDataLoad 재사용
/// - 시작 시 InfiniteStageManager와 연결
/// </summary>
public class InfiniteTestSceneController : MonoBehaviour
{
    [Header("세팅이 끝나야 하는 애들")]
    public StageSetupWindow stageSetup;
    public TestCharacterDataLaod testCharacterDataLoad;

    [Header("무한 스테이지 매니저")]
    public InfiniteStageManager infiniteStageManager;

    [Header("무한 스테이지 설정")]
    [Tooltip("최대 배치 가능 캐릭터 수")]
    [SerializeField] private int maxDeployCount = 5;

    private async void Awake()
    {
        // 테스트 씬에서도 전투 시작 전엔 멈추고 시작
        Time.timeScale = 0f;

        // 1) 참조 들어올 때까지
        while (stageSetup == null || testCharacterDataLoad == null)
            await UniTask.Yield();

        // 2) 둘 다 IsReady 될 때까지 대기
        while (!(stageSetup.IsReady && testCharacterDataLoad.IsReady))
            await UniTask.Yield();

        // 3) 무한 스테이지 설정 적용 (Full 레이아웃 + 5명 배치 제한)
        stageSetup.ApplyInfiniteStageConfig(maxDeployCount);

        // 4) InfiniteStageManager 대기
        while (InfiniteStageManager.Instance == null)
            await UniTask.Yield();

        infiniteStageManager = InfiniteStageManager.Instance;

        // 5) 스테이지 시작 이벤트 구독
        StageSetupWindow.OnStageStarted += OnStageStarted;

        // 6) 진짜 준비 끝난 시점에서만 100% 찍기
        SceneLoader.SetProgressExternal(1.0f);

        // 7) 100% 상태를 잠깐 보여주고
        await UniTask.Delay(300, DelayType.UnscaledDeltaTime);

        // 8) 게임 씬 준비 완료 알림
        GameSceneManager.NotifySceneReady(SceneType.InfiniteTestScene, 100);

        // 9) 로딩 UI 닫기
        await SceneLoader.HideLoadingWithDelay(0);
    }

    private void OnDestroy()
    {
        StageSetupWindow.OnStageStarted -= OnStageStarted;
    }

    /// <summary>
    /// StageSetupWindow에서 시작 버튼 클릭 시 호출됨
    /// </summary>
    private void OnStageStarted()
    {
        Debug.Log("[InfiniteTestScene] 스테이지 시작 - InfiniteStageManager.StartGame() 호출");
        infiniteStageManager?.StartGame();
    }
}
