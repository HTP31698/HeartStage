using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

[DisallowMultipleComponent]
public class BootSceneController : MonoBehaviour
{
    private static bool s_initialized = false;
    private static UniTask s_initTask = UniTask.CompletedTask;
    private static readonly string TitleScene = "Assets/Scenes/TitleScene.unity";

    private async void Start()
    {
        await RunPreLoginInitOnceAsync();

        // 초기화 끝나면 바로 타이틀 씬으로
        await Addressables.LoadSceneAsync(TitleScene);
    }

    /// <summary>
    /// Addressables / 리소스 / 데이터테이블 같은 공통 초기화만 담당.
    /// 로그인/세이브는 전부 TitleScene에서 처리.
    /// </summary>
    public static UniTask RunPreLoginInitOnceAsync()
    {
        if (s_initialized)
            return UniTask.CompletedTask;

        if (s_initTask.Status == UniTaskStatus.Pending)
            return s_initTask;

        s_initTask = PreLoginInitInternalAsync();
        return s_initTask;
    }

    private static async UniTask PreLoginInitInternalAsync()
    {
        // 타겟 프레임 올리니까 로딩시간 빨라짐
        Application.targetFrameRate = 200;

        if (!BootStrap.IsInitialized)
        {
            // 1. Addressables 초기화 (필수 선행)
            await Addressables.InitializeAsync();

            // 2. 리소스 프리로드 + 데이터테이블 병렬 로드
            await UniTask.WhenAll(
                ResourceManager.Instance.PreloadLabelAsync(AddressableLabel.Stage),
                ResourceManager.Instance.PreloadLabelAsync("SFX"),
                DataTableManager.Initialization
            );

            // 3. 나머지 초기화 병렬 실행
            await UniTask.WhenAll(
                LiveConfigManager.Instance.InitializeAsync(),
                PassivePatternUtil.InitializeAsync(),
                StageLayoutUtil.InitializeAsync()
            );

            BootStrap.IsInitialized = true;
        }

        Application.targetFrameRate = 60;
        s_initialized = true;
    }
}
