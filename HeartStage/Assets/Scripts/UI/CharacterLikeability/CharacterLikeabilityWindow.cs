using UnityEngine;

public class CharacterLikeabilityWindow : MonoBehaviour
{
    public static CharacterLikeabilityWindow Instance;

    public GameObject wholePanel;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenPanel()
    {
        wholePanel.SetActive(true);
    }

    public void ClosePanel()
    {
        wholePanel.SetActive(false);
    }
}