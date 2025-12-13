using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectPanel : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public Image attributeIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI idolPowerCount;
    public TextMeshProUGUI levelText;
    public Slider expSlider;
    public Image cardImage;

    public void Init(CharacterData characterData)
    {
        if (characterData == null)
        {
            Debug.LogWarning("[CharacterSelectPanel] characterData is null");
            return;
        }

        rankText.text = $"{characterData.char_rank}";
        characterName.text = characterData.char_name;
        idolPowerCount.text = $"{characterData.GetTotalPower()}";
        levelText.text = $"LV {characterData.char_lv}";
        CharacterAttributeIcon.ChangeIcon(attributeIcon,characterData.char_type);

        // 카드 이미지 세팅
        var texture2D = ResourceManager.Instance.Get<Texture2D>(characterData.card_imageName);
        if (texture2D != null)
        {
            cardImage.sprite = Sprite.Create(
                texture2D,
                new Rect(0, 0, texture2D.width, texture2D.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        else
        {
            Debug.LogWarning($"[CharacterSelectPanel] Texture not found: {characterData.card_imageName}");
        }

        // expSlider는 나중에 경험치 시스템 붙일 때 계산해서 세팅
    }
}
