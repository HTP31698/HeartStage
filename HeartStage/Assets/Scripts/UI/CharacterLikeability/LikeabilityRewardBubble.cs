using UnityEngine;
using UnityEngine.UI;

public class LikeabilityRewardBubble : MonoBehaviour
{
    [SerializeField] private Button button;

    private CharacterLikeabilityPanel panel;

    public void Init(CharacterLikeabilityPanel owner)
    {
        panel = owner;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        panel.ReceiveNextLikeabilityReward();
    }
}