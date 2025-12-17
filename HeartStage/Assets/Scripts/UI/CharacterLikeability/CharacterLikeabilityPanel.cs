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

    public RectTransform rewardRoot1;
    public RectTransform rewardRoot2;
    public RectTransform rewardRoot3;

    public void Init(int characterId)
    {
        var characterData = DataTableManager.CharacterTable.Get(characterId);
        var likeabilityData = DataTableManager.LikeabilityTable.Get(characterData.char_name);
        CharacterAttributeIcon.ChangeIcon(attributeIcon, characterData.char_type);
        characterName.text = characterData.char_name;
        characterLvRank.text = $"Lv{characterData.char_lv} {RankName.Get(characterData.char_rank)}";
        // 호감도 게이지 세팅
        likeabilityGuage.maxValue = likeabilityData.max_like_point;
        // 현재 호감도 세팅 코드 추가
        CurrencyIcon.CurrencyIconChange(cheerCurrencyIcon, likeabilityData.User_need_Item);
        cheerPriceText.text = $"{likeabilityData.User_need_amount}";
        //
        float percent1 = (float)likeabilityData.like_amount1 / likeabilityData.max_like_point;
        float percent2 = (float)likeabilityData.like_amount2 / likeabilityData.max_like_point;
        float percent3 = (float)likeabilityData.like_amount3 / likeabilityData.max_like_point;

        SetRewardPositionByPercent(rewardRoot1, percent1);
        SetRewardPositionByPercent(rewardRoot2, percent2);
        SetRewardPositionByPercent(rewardRoot3, percent3);
    }

    private void SetRewardPositionByPercent(RectTransform reward, float percent)
    {
        RectTransform sliderRect = likeabilityGuage.GetComponent<RectTransform>();
        float width = sliderRect.rect.width;
        float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, percent);
        Vector2 pos = reward.anchoredPosition;
        pos.x = x;
        reward.anchoredPosition = pos;
    }
}