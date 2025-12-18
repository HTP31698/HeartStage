using UnityEngine;
using UnityEngine.UI;

public class AddFriendButton : MonoBehaviour
{
    [SerializeField] private Button friendListButton;

    private void OnEnable()
    {
        friendListButton.onClick.AddListener(OpenFriendList);
    }

    private void OnDisable()
    {
        friendListButton.onClick.RemoveListener(OpenFriendList);
    }

    private void OpenFriendList()
    {
        WindowManager.Instance?.OpenOverlay(WindowType.Friend);
    }
}
