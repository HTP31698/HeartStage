using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaPercentageItemPrefab : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private TextMeshProUGUI countText;

    public void Init(GachaData gachaData, int count)
    {
        if (gachaData == null)
            return;

        // 먼저 캐릭터인지 확인
        var characterData = DataTableManager.CharacterTable.Get(gachaData.Gacha_item);
        if (characterData != null)
        {
            //  캐릭터인 경우
            SetCharacterImage(characterData);
            SetCharacterNameText(characterData.char_name);
            SetPercentageText(gachaData.Gacha_per);
            SetCountText(gachaData.Gacha_item_amount);
        }
        else
        {
            //  일반 아이템인 경우
            var itemData = DataTableManager.ItemTable.Get(gachaData.Gacha_item);
            if (itemData != null)
            {
                SetItemImage(itemData.prefab);
                SetCharacterNameText(itemData.item_name);
                SetPercentageText(gachaData.Gacha_per);
                SetCountText(gachaData.Gacha_item_amount);
            }
            else
            {
                Debug.LogWarning($"가챠 아이템을 찾을 수 없습니다: {gachaData.Gacha_item}");
                return;
            }
        }
    }

    private void SetCharacterImage(CharacterCSVData characterData)
    {
        if (characterImage == null || string.IsNullOrEmpty(characterData.card_imageName))
        {
            return;
        }

        characterImage.sprite = ResourceManager.Instance.GetSprite(characterData.card_imageName);
    }

    // 아이템 이미지 설정 메서드 추가
    private void SetItemImage(string imageName)
    {
        if (characterImage == null || string.IsNullOrEmpty(imageName))
        {
            return;
        }

        characterImage.sprite = ResourceManager.Instance.GetSprite(imageName);
    }

    // 텍스트 설정 메서드들 분리
    private void SetCharacterNameText(string name)
    {
        if (characterNameText != null)
        {
            characterNameText.text = name;
        }
    }

    private void SetPercentageText(int percentage)
    {
        if (percentageText != null)
        {
            percentageText.text = $"{percentage}%";
        }
    }

    private void SetCountText(int count)
    {
        if (countText != null)
        {
            countText.text = count.ToString();
        }
    }

}
