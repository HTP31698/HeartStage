using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무한 스테이지 진입 버튼
/// Inspector에서 infiniteStageId 설정 후 버튼에 연결
/// </summary>
public class InfiniteStageButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int infiniteStageId = 90001; // 기본 무한 스테이지 ID

    [Header("Optional")]
    [SerializeField] private Button button; // 자동으로 찾거나 Inspector에서 설정

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    public void OnClick()
    {
        if (LoadSceneManager.Instance == null)
        {
            Debug.LogError("[InfiniteStageButton] LoadSceneManager.Instance가 null입니다!");
            return;
        }

        LoadSceneManager.Instance.GoInfiniteStage(infiniteStageId);
    }

    // 외부에서 ID 변경 가능
    public void SetInfiniteStageId(int id)
    {
        infiniteStageId = id;
    }
}
