using UnityEngine;

public class LobbyHomeWindow : GenericWindow
{
    public LobbyHomeInitializer initializer;

    private void OnEnable()
    {
        initializer.Init();
        // 스토리 보상창은 LobbyUI.CheckReturnToStoryDungeon()에서 스토리 던전 UI 복원 시 표시됨
    }
}