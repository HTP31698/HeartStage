using Cysharp.Threading.Tasks;
using UnityEngine;

public class TutorialCutSceneController : MonoBehaviour
{
    [Header("튜토리얼 매니저 참조")]
    [SerializeField] private TutorialManager tutorialManager;

    private bool isReady = false;

    private async void Awake()
    {
        await InitializeTutorialScene();
    }

    private async UniTask InitializeTutorialScene()
    {
        // 1) 데이터 테이블이 로드될 때까지 대기
        while (DataTableManager.TutorialScriptTable == null)
        {
            await UniTask.Yield();
        }

        // 2) TutorialManager 참조 확인 및 대기
        while (tutorialManager == null)
        {
            await UniTask.Yield();
        }

        // 3) TutorialManager 초기화 대기
        while (!tutorialManager.IsInitialized)
        {
            await UniTask.Yield();
        }

        // 4) 모든 준비가 완료됨
        isReady = true;

        // 5) 로딩 프로그레스를 100%로 설정
        SceneLoader.SetProgressExternal(1.0f);

        // 6) 잠깐 100% 상태 보여주기
        await UniTask.Delay(300, DelayType.UnscaledDeltaTime);

        // 7) 게임 씬 준비 완료 알림
        GameSceneManager.NotifySceneReady(SceneType.TutorialCutScene, 100);

        // 8) 로딩 UI 닫기
        await SceneLoader.HideLoadingWithDelay(0);

        Debug.Log("[TutorialCutSceneController] 씬 로딩 완료");

        // 9) 튜토리얼 컷씬 시작 (TutorialManager에게 위임)
        if (tutorialManager != null)
        {
            tutorialManager.StartCutscene();
        }
    }

    /// <summary>
    /// 씬 준비 상태 확인
    /// </summary>
    public bool IsReady => isReady;

    /// <summary>
    /// TutorialManager 참조 반환
    /// </summary>
    public TutorialManager GetTutorialManager() => tutorialManager;
}