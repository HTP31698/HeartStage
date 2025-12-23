using AssetKits.ParticleImage;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class LikeabilityRewardState
{
    public bool reward1Received;
    public bool reward2Received;
    public bool reward3Received;
}

public class CharacterLikeabilityPanel : MonoBehaviour
{
    public Image attributeIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI characterLvRank;
    public Slider likeabilityGuage;
    public TextMeshProUGUI guageAmountText;
    public Button cheerUpButton;
    public Image cheerCurrencyIcon;
    public TextMeshProUGUI cheerPriceText;
    public TextMeshProUGUI characterDialogue;

    public RectTransform rewardRoot1;
    public RectTransform rewardRoot2;
    public RectTransform rewardRoot3;
    public Image rewardImage1;
    public Image rewardImage2;
    public Image rewardImage3;

    public GameObject rewardSpeechBubble;
    public TextMeshProUGUI rewardCountText;
    public LikeabilityRewardPopup rewardPopup;
    public FriendCheerRewardUI friendCheerRewardUI;
    public ParticleImage cheerEffect;

    private LikeabilityData likeabilityData;
    private CharacterCSVData characterData;

    private LikeabilityRewardBubble rewardBubble;

    private void Awake()
    {
        rewardBubble = rewardSpeechBubble.GetComponent<LikeabilityRewardBubble>();
    }

    private void Start()
    {
        cheerUpButton.onClick.AddListener(OnClickCheerUp);
    }

    public void Init(int characterId)
    {
        // 데이터 세팅
        characterData = DataTableManager.CharacterTable.Get(characterId);
        likeabilityData = DataTableManager.LikeabilityTable.Get(characterData.char_name);
        // 기본 정보 UI 세팅
        CharacterAttributeIcon.ChangeIcon(attributeIcon, characterData.char_type);
        characterName.text = characterData.char_name;
        characterLvRank.text = $"Lv{characterData.char_lv} {RankName.Get(characterData.char_rank)}";
        // 호감도 게이지 세팅
        likeabilityGuage.maxValue = likeabilityData.max_like_point;
        // 재화 UI 세팅
        CurrencyIcon.CurrencyIconChange(cheerCurrencyIcon, likeabilityData.User_need_Item);
        cheerPriceText.text = GetCheerNeedAmount().ToString();
        // 호감도 보상 UI 세팅
        SetRewardUI();
        // 현재 호감도 UI 반영
        RefreshLikeabilityUI();
        UpdateRewardBubble();
        rewardBubble.Init(this);
    }
    // 응원하기
    private void OnClickCheerUp()
    {
        if (LobbyHomeInitializer.Instance.isFriendHome)
        {
            CheerUpFriend();
        }
        else
        {
            CheerUpMyHome();
        }
    }
    // 응원하기 내 숙소일때
    private void CheerUpMyHome()
    {
        if (!ItemInvenHelper.TryConsumeItem(likeabilityData.User_need_Item, likeabilityData.User_need_amount))
            return;

        cheerEffect.Play();
        int newLike = Mathf.Min((int)likeabilityGuage.value + likeabilityData.like_point, (int)likeabilityGuage.maxValue);
        CharacterHelper.SetLikeability(characterData.char_name, newLike);
        RefreshLikeabilityUI();
    }
    // 응원하기 친구 숙소일때
    private void CheerUpFriend()
    {
        if (!ItemInvenHelper.TryConsumeItem(likeabilityData.User_need_Item, likeabilityData.Friend_need_amount))
            return;

        cheerEffect.Play();
        int rewardAmount = UnityEngine.Random.Range(likeabilityData.random_item_min, likeabilityData.random_item_max + 1);
        ItemInvenHelper.AddItem(likeabilityData.random_item, rewardAmount);
        friendCheerRewardUI.Init(likeabilityData.like_reward_item1, rewardAmount);
        UpdateCheerUpButtonInteractable();
    }
    // 호감도 보상 UI 세팅
    private void SetRewardUI()
    {
        // 위치 세팅
        float percent1 = (float)likeabilityData.like_amount1 / likeabilityData.max_like_point;
        float percent2 = (float)likeabilityData.like_amount2 / likeabilityData.max_like_point;
        float percent3 = (float)likeabilityData.like_amount3 / likeabilityData.max_like_point;
        SetRewardPositionByPercent(rewardRoot1, percent1);
        SetRewardPositionByPercent(rewardRoot2, percent2);
        SetRewardPositionByPercent(rewardRoot3, percent3);
        // 이미지 세팅
        var rewardItemData1 = DataTableManager.ItemTable.Get(likeabilityData.like_reward_item1);
        var rewardItemData2 = DataTableManager.ItemTable.Get(likeabilityData.like_reward_item2);
        var rewardItemData3 = DataTableManager.ItemTable.Get(likeabilityData.like_reward_item3);
        rewardImage1.sprite = ResourceManager.Instance.GetSprite(rewardItemData1.prefab);
        rewardImage2.sprite = ResourceManager.Instance.GetSprite(rewardItemData2.prefab);
        rewardImage3.sprite = ResourceManager.Instance.GetSprite(rewardItemData3.prefab);
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
    // 현재 호감도 UI 반영
    private void RefreshLikeabilityUI()
    {
        var data = GetTargetSaveData();

        int currentLike = CharacterHelper.GetLikeability(characterData.char_name, data);
        likeabilityGuage.value = currentLike;
        guageAmountText.text = $"♥ {currentLike}";

        UpdateDialogue(currentLike);
        UpdateCheerUpButtonInteractable();
        UpdateRewardBubble();
    }
    // 호감도 대사 업데이트
    private void UpdateDialogue(int currentLike)
    {
        if (currentLike < likeabilityData.like_amount1)
        {
            characterDialogue.text = likeabilityData.line1;
        }
        else if (currentLike < likeabilityData.like_amount2)
        {
            characterDialogue.text = likeabilityData.line2;
        }
        else if (currentLike < likeabilityData.like_amount3)
        {
            characterDialogue.text = likeabilityData.line3;
        }
        else
        {
            characterDialogue.text = likeabilityData.line4;
        }
    }
    // 응원 버튼 Interactable 세팅
    private void UpdateCheerUpButtonInteractable()
    {
        int needAmount = GetCheerNeedAmount();
        bool interactable;

        if (LobbyHomeInitializer.Instance.isFriendHome)
        {
            // 친구 숙소: 재화만 기준
            interactable = SaveLoadManager.Data.itemList.TryGetValue(likeabilityData.User_need_Item, out int count) && count >= needAmount;
        }
        else
        {
            // 내 숙소: 호감도 max면 비활성
            if (likeabilityGuage.value >= likeabilityGuage.maxValue)
            {
                interactable = false;
            }
            else if (SaveLoadManager.Data.itemList.TryGetValue(likeabilityData.User_need_Item, out int count))
            {
                interactable = count >= needAmount;
            }
            else
            {
                interactable = false;
            }
        }

        var animator = cheerUpButton.GetComponent<Animator>();
        animator.enabled = interactable;
        var canvasGroup = cheerUpButton.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = interactable;
        canvasGroup.alpha = interactable ? 1f : 0.5f;
    }
    // 보상 말풍선 업데이트
    public void UpdateRewardBubble()
    {
        int count = GetAvailableRewardCount();
        rewardSpeechBubble.gameObject.SetActive(count > 0);
        rewardCountText.text = count.ToString(); // TMP
    }
    // 받을 수 있는 호감도 보상 개수 리턴
    private int GetAvailableRewardCount()
    {
        // 친구 숙소에서는 보상 표시 안 함
        if (LobbyHomeInitializer.Instance.isFriendHome)
            return 0;

        int currentLike = CharacterHelper.GetLikeability(characterData.char_name);
        var state = CharacterHelper.GetLikeabilityRewardState(characterData.char_name);

        int count = 0;
        if (currentLike >= likeabilityData.like_amount1 && !state.reward1Received)
            count++;
        if (currentLike >= likeabilityData.like_amount2 && !state.reward2Received)
            count++;
        if (currentLike >= likeabilityData.like_amount3 && !state.reward3Received)
            count++;
        return count;
    }
    // 호감도 보상 받기
    public void ReceiveNextLikeabilityReward()
    {
        int currentLike = CharacterHelper.GetLikeability(characterData.char_name);
        var state = CharacterHelper.GetLikeabilityRewardState(characterData.char_name);
        // 1번 보상
        if (currentLike >= likeabilityData.like_amount1 && !state.reward1Received)
        {
            CharacterHelper.ReceiveLikeabilityReward(characterData.char_name, 1, likeabilityData.like_reward_item1, likeabilityData.reward_amount1);
        }
        // 2번 보상
        else if (currentLike >= likeabilityData.like_amount2 && !state.reward2Received)
        {
            CharacterHelper.ReceiveLikeabilityReward(characterData.char_name, 2, likeabilityData.like_reward_item2, likeabilityData.reward_amount2);
        }
        // 3번 보상
        else if (currentLike >= likeabilityData.like_amount3 && !state.reward3Received)
        {
            CharacterHelper.ReceiveLikeabilityReward(characterData.char_name, 3, likeabilityData.like_reward_item3, likeabilityData.reward_amount3);
        }
        else
        {
            return;
        }
        RefreshLikeabilityUI();
    }
    // 보상 획득창 열기
    public void OpenRewardPopup()
    {
        int currentLike = CharacterHelper.GetLikeability(characterData.char_name);
        var state = CharacterHelper.GetLikeabilityRewardState(characterData.char_name);
        // 다음 받을 보상 하나 찾기 (기존 순서 그대로)
        if (currentLike >= likeabilityData.like_amount1 && !state.reward1Received)
        {
            rewardPopup.Open(this, likeabilityData.like_amount1, likeabilityData.like_reward_item1, likeabilityData.reward_amount1);
        }
        else if (currentLike >= likeabilityData.like_amount2 && !state.reward2Received)
        {
            rewardPopup.Open(this, likeabilityData.like_amount2, likeabilityData.like_reward_item2, likeabilityData.reward_amount2);
        }
        else if (currentLike >= likeabilityData.like_amount3 && !state.reward3Received)
        {
            rewardPopup.Open(this, likeabilityData.like_amount3, likeabilityData.like_reward_item3, likeabilityData.reward_amount3);
        }
    }
    // 어떤 SaveData로 할지
    private SaveDataV1 GetTargetSaveData()
    {
        return LobbyHomeInitializer.Instance.isFriendHome ? LobbyHomeInitializer.Instance.friendSaveData : SaveLoadManager.Data;
    }
    // 응원 필요 재화량
    private int GetCheerNeedAmount()
    {
        return LobbyHomeInitializer.Instance.isFriendHome ? likeabilityData.Friend_need_amount : likeabilityData.User_need_amount;
    }
    // 테스트 코드
    // 선택된 캐릭터 호감도 10씩 증가
    public void GetLikeAbility()
    {
        if (characterData == null || likeabilityData == null)
            return;

        int currentAmount = CharacterHelper.GetLikeability(characterData.char_name);
        currentAmount = Mathf.Min(currentAmount + 10, likeabilityData.max_like_point);
        CharacterHelper.SetLikeability(characterData.char_name, currentAmount);
        RefreshLikeabilityUI();
    }
    //
}