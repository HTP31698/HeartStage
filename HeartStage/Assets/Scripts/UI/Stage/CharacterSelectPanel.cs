using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        CharacterAttributeIcon.ChangeIcon(attributeIcon, characterData.char_type);

        // 포토카드 교체 반영
        LoadPhotocardAsync(characterData).Forget();
    }

    private async UniTaskVoid LoadPhotocardAsync(CharacterData characterData)
    {
        if (characterData == null)
        {
            cardImage.sprite = null;
            return;
        }

        string charCode = PhotocardHelper.ExtractCharCode(characterData.char_id);
        // 스테이지 배치 UI에서는 Frame 버전 사용
        var sprite = await PhotocardHelper.LoadDisplaySpriteWithFrame(charCode);

        if (sprite != null)
        {
            cardImage.sprite = sprite;
        }
        else
        {
            // fallback: 기존 방식
            cardImage.sprite = ResourceManager.Instance.GetSprite(characterData.card_imageName);
        }
    }
}
