using UnityEngine;

public class CharacterLikeabilityWindow : MonoBehaviour
{
    public static CharacterLikeabilityWindow Instance;

    public CharacterLikeabilityPanel wholePanel;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenPanel(int characterId)
    {
        wholePanel.gameObject.SetActive(true);
        wholePanel.Init(characterId);
    }

    public void ClosePanel()
    {
        wholePanel.gameObject.SetActive(false);
    }
}