using UnityEngine;
using UnityEngine.UI;

public static class CharacterImageHelper
{
    public static void SetCharacterImage(Image targetImage, CharacterData characterData)
    {
        if (targetImage == null) 
            return;

        if (characterData != null && !string.IsNullOrEmpty(characterData.card_imageName))
        {
            targetImage.sprite = ResourceManager.Instance.GetSprite(characterData.card_imageName);
            return;
        }

        targetImage.sprite = null;
    }
}