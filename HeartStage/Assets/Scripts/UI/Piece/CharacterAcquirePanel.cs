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
        var texture = ResourceManager.Instance.Get<Texture2D>(characterData.card_imageName);
        characterCardImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public void Close()
    {
        gameObject.SetActive(false);
        PieceExchangePanel.Instance.AfterAcquirCharacter(characterId);
    }
}
