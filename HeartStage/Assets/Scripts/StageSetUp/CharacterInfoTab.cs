using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public class CharacterInfoTab : MonoBehaviour, IPointerClickHandler
{
    private DragMe dragMe;

    private void Start()
    {
        dragMe = GetComponent<DragMe>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 로딩 중이면 클릭 즉시 무시
        if (NoteLoadingUI.IsLoading || SceneLoader.IsLoading)
            return;

        OpenInfoNextFrame().Forget();
    }

    private async UniTaskVoid OpenInfoNextFrame()
    {
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        // 로딩 중이면 탭 금지
        if (NoteLoadingUI.IsLoading || SceneLoader.IsLoading)
            return;

        // 드래그 직후면 탭 금지
        if (dragMe != null && dragMe.DragJustEnded)
            return;

        // 세로 드래그였으면 탭 금지
        if (dragMe != null && dragMe.IsVerticalDrag)
            return;

        WindowManager.Instance.OpenOverlay(WindowType.CharacterInfo);
        CharacterInfoWindow.Instance.Init(dragMe.characterData);
        SoundManager.Instance.PlayUIButtonClickSound();
    }
}
