using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectPanel : MonoBehaviour
{
    public List<Image> rankImages;
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

        // 랭크 세팅
        for (int i = 0; i < rankImages.Count; i++)
        {
            if (i < characterData.char_rank - 1)
                rankImages[i].enabled = true;
            else
                rankImages[i].enabled = false;
        }
        //
        characterName.text = characterData.char_name;
        idolPowerCount.text = $"{characterData.GetTotalPower()}";
        levelText.text = $"LV {characterData.char_lv}";
        CharacterAttributeIcon.ChangeIcon(attributeIcon,characterData.char_type);
        cardImage.sprite = ResourceManager.Instance.GetSprite(characterData.card_imageName);
    }
}
