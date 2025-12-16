using Cysharp.Threading.Tasks;
using UnityEngine;

public class StorySceneController : MonoBehaviour
{
    [Header("스토리 매니저 참조")]
    [SerializeField] private StoryManager storyManager;

    private bool isReady = false;

    private async void Awake()
    {
        await InitializeStoryScene();
    }

    private async UniTask InitializeStoryScene()
    {
        // 1) 데이터 테이블이 로드될 때까지 대기
        while (DataTableManager.StoryTable == null || DataTableManager.StoryScriptTable == null)
        {
            await UniTask.Yield();
        }

        // 2) StoryManager 참조 확인 및 대기
        while (storyManager == null)
        {
            storyManager = FindObjectOfType<StoryManager>();
            await UniTask.Yield();
        }

        // 3) StoryManager 초기화 대기
        while (!storyManager.IsInitialized)
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
        GameSceneManager.NotifySceneReady(SceneType.StoryScene, 100);

        // 8) 로딩 UI 닫기
        await SceneLoader.HideLoadingWithDelay(0);

        Debug.Log("[StorySceneController] 씬 로딩 완료");

        // 9) 컷씬 시작 (StoryManager에게 위임)
        if (storyManager != null)
        {
            storyManager.StartCutscene();
        }
    }

    /// <summary>
    /// 씬 준비 상태 확인
    /// </summary>
    public bool IsReady => isReady;

    /// <summary>
    /// StoryManager 참조 반환
    /// </summary>
    public StoryManager GetStoryManager() => storyManager;
}