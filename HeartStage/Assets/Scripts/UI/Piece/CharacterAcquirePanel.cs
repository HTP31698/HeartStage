using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterAcquirePanel : MonoBehaviour
{
    public TextMeshProUGUI characterAcquireText;
    public Image characterCardImage;

    private int characterId = 0;

    public void Open(int characterId)
    {
        this.characterId = characterId;
        var characterData = DataTableManager.CharacterTable.Get(characterId);
        characterAcquireText.text = $"{characterData.char_name} 획득";
        characterCardImage.sprite = ResourceManager.Instance.GetSprite(characterData.card_imageName);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        PieceExchangePanel.Instance.AfterAcquirCharacter(characterId);
    }
}
