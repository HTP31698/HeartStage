using Cysharp.Threading.Tasks;
using UnityEngine;

public class LobbyHomeWindow : GenericWindow
{
    public LobbyHomeInitializer initializer;

    private void OnEnable()
    {
        initializer.Init();

        // 스토리 보상창 표시 플래그 확인
        CheckStoryRewardFlag().Forget();
    }

    /// 스토리 컷씬 후 보상창 표시 플래그 확인
    private async UniTaskVoid CheckStoryRewardFlag()
    {
        // WindowManager가 준비될 때까지 대기
        while (WindowManager.Instance == null)
        {
            await UniTask.Yield();
        }

        var gameData = SaveLoadManager.Data;

        // 스토리 보상창 표시 플래그가 설정된 경우
        if (gameData.showStoryRewardAfterScene)
        {
            Debug.Log($"[LobbyHomeWindow] 스토리 보상창 표시 시작");

            // 플래그 리셋
            gameData.showStoryRewardAfterScene = false;
            SaveLoadManager.SaveToServer().Forget();

            // 약간의 딜레이 후 보상창 표시 (로비 UI가 완전히 로드된 후)
            await UniTask.Delay(100, DelayType.UnscaledDeltaTime);

            // 로비에서는 StoryStageRewardUI 사용
            WindowManager.Instance.OpenOverlay(WindowType.StoryStageRewardUI);
        }
    }
}