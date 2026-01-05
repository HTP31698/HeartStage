using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gacha5TryResultPrefabUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;

    private Sprite currentSprite; // 현재 스프라이트 참조 저장

    public void Init(GachaResult gachaResult)
    {
        if (gachaResult.gachaData == null)
            return;

        var gachaData = gachaResult.gachaData;
        var characterData = gachaResult.characterData;

        if (characterData != null)
        {
            // 중복일 때 아이템 표시
            if (gachaResult.isDuplicate && gachaData.Gacha_have > 0)
            {
                var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_have);
                if (itemData != null)
                {
                    SetImage(itemData.prefab);
                    SetCharacterNameText($"{itemData.item_name} x{gachaData.Gacha_have_amount}");
                    return;
                }
            }

            SetImage(characterData.card_imageName);
            SetCharacterNameText(characterData.char_name);
        }

        else
        {
            var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_item);
            if(itemData != null)
            {
                SetImage(itemData.prefab);
                SetCharacterNameText($"{itemData.item_name} x{gachaData.Gacha_item_amount}");
            }
            else
            {
                SetCharacterNameText($"아이템 ID: {gachaData.Gacha_item}");
            }
        }
    }

    private void SetCharacterNameText(string name)
    {
        if (characterNameText != null)
        {
            characterNameText.text = name;
        }
    }

    private void SetImage(string imageName)
    {
        if (characterImage == null || string.IsNullOrEmpty(imageName))
        {
            return;
        }

        // 기존 스프라이트 정리
        //if (currentSprite != null)
        //{
        //    DestroyImmediate(currentSprite);
        //    currentSprite = null;
        //}

        characterImage.sprite = ResourceManager.Instance.GetSprite(imageName);
    }

    private void OnDestroy()
    {
        // 컴포넌트 파괴시 스프라이트 정리
        //if (currentSprite != null)
        //{
        //    DestroyImmediate(currentSprite);
        //    currentSprite = null;
        //}
    }
}