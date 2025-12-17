using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterLikeabilityPanel : MonoBehaviour
{
    public Image attributeIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI characterLvRank;
    public Slider likeabilityGuage;
    public TextMeshProUGUI guageAmountText;
    public Image cheerCurrencyIcon;
    public TextMeshProUGUI cheerPriceText;
    public TextMeshProUGUI characterDialogue;

    public void Init(int characterId)
    {
        var characterData = DataTableManager.CharacterTable.Get(characterId);
        CharacterAttributeIcon.ChangeIcon(attributeIcon, characterData.char_type);
        characterName.text = characterData.char_name;
        characterLvRank.text = $"Lv{characterData.char_lv} 신인 아이돌";
    }
}
