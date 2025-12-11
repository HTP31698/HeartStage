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
        Application.targetFrameRate = 60;

        if (!BootStrap.IsInitialized)
        {
            await Addressables.InitializeAsync();
            await ResourceManager.Instance.PreloadLabelAsync(AddressableLabel.Stage);
            await ResourceManager.Instance.PreloadLabelAsync("SFX");
            await DataTableManager.Initialization;
            await LiveConfigManager.Instance.InitializeAsync();

            // StageAssets 라벨로 등록된 SO 유틸리티 초기화
            await PassivePatternUtil.InitializeAsync();
            await StageLayoutUtil.InitializeAsync();

            BootStrap.IsInitialized = true;
        }

        s_initialized = true;
    }
}
