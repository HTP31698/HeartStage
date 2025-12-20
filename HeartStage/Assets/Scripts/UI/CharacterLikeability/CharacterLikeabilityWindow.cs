using UnityEngine;

public class CharacterLikeabilityWindow : MonoBehaviour
{
    public static CharacterLikeabilityWindow Instance;

    public CharacterLikeabilityPanel wholePanel;
    public CharacterSpeechBubbleFollower bubbleFollower;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenPanel(int characterId)
    {
        wholePanel.gameObject.SetActive(true);
        wholePanel.Init(characterId);
        Debug.Log(characterId);
    }

    public void ClosePanel()
    {
        wholePanel.gameObject.SetActive(false);
    }

    public void SetBubbleTarget(Transform target)
    {
        bubbleFollower.SetTarget(target);
    }
}