using System.Collections.Generic;
using UnityEngine;
public struct GachaResult
{
    public GachaData gachaData; // 뽑힌 가챠 데이터
    public CharacterCSVData characterData; // 뽑힌 캐릭터 데이터
    public bool isDuplicate; // 중복 여부 

    public GachaResult(GachaData gachaData, CharacterCSVData characterData, bool isDuplicate = false)
    {
        this.gachaData = gachaData;
        this.characterData = characterData;
        this.isDuplicate = isDuplicate;
    }
}

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 1회 뽑기
    public GachaResult? DrawGacha(int gachaTypeId)
    {
        // 가챠 타입 정보 가져오기
        var gachaType = DataTableManager.GachaTypeTable.Get(gachaTypeId);
        if (gachaType == null)
        {
            return null;
        }

        // 재화 차감
        if (!ItemInvenHelper.TryConsumeItem(ItemID.HeartStick, 50))
        {
            return null;
        }

        // 해당 타입의 가챠 아이템들 가져오기
        var gachaItems = GachaTable.GetGachaByType(gachaTypeId);
        if (gachaItems == null || gachaItems.Count == 0)
        {
            return null;
        }

        // 확률에 따른 랜덤 뽑기
        var selectedItem = DrawRandomItem(gachaItems);
        if (selectedItem == null)
        {
            return null;
        }

        var characterData = DataTableManager.CharacterTable.Get(selectedItem.Gacha_item);
        bool isCharacter = characterData != null;
        bool alreadyOwned = false;

        if (isCharacter)
        {
            alreadyOwned = SaveLoadManager.Data.unlockedByName.TryGetValue(characterData.char_name, out bool isUnlocked) && isUnlocked;

            if (!alreadyOwned)
            {
                // 캐릭터 획득 처리 
                CharacterHelper.AcquireCharacter(selectedItem.Gacha_item, DataTableManager.CharacterTable);
                QuestManager.Instance.OnGachaDraw();
            }
            else
            {
                // 중복 캐릭터 보상 처리
                if (selectedItem.Gacha_have > 0)
                {
                    var itemData = DataTableManager.ItemTable.Get(selectedItem.Gacha_have);
                    if (itemData != null)
                    {
                        ItemInvenHelper.AddItem(selectedItem.Gacha_have, selectedItem.Gacha_have_amount);
                        QuestManager.Instance.OnGachaDraw();
                        Debug.Log($"중복 보상 아이템 획득: {itemData.item_name} x{selectedItem.Gacha_have_amount}");
                    }
                }
            }
        }
        else
        {
            // 일반 아이템 획득 처리
            ItemInvenHelper.AddItem(selectedItem.Gacha_item, selectedItem.Gacha_item_amount);
            QuestManager.Instance.OnGachaDraw();

            var itemData = DataTableManager.ItemTable.Get(selectedItem.Gacha_item);
            if (itemData != null)
            {
                Debug.Log($"아이템 획득: {itemData.item_name} x{selectedItem.Gacha_item_amount}");
            }
        }

        // 결과 반환
        return new GachaResult
        {
            gachaData = selectedItem,
            characterData = characterData,
            isDuplicate = alreadyOwned
        };
    }

    // 5회 뽑기
    public List<GachaResult> DrawGachaFiveTimes(int gachaTypeId)
    {
        var result = new List<GachaResult>();

        var gachaType = DataTableManager.GachaTypeTable.Get(gachaTypeId);
        if (gachaType == null)
        {
            return result;
        }

        if (!ItemInvenHelper.TryConsumeItem(ItemID.HeartStick, 250))
        {
            return result;
        }

        var gachaItems = GachaTable.GetGachaByType(gachaTypeId);
        if (gachaItems == null || gachaItems.Count == 0)
        {
            return result;
        }

        for (int i = 0; i < 5; i++)
        {
            var selectedItem = DrawRandomItem(gachaItems);
            if (selectedItem != null)
            {
                var characterData = DataTableManager.CharacterTable.Get(selectedItem.Gacha_item);
                bool isCharacter = characterData != null;
                bool alreadyOwned = false;

                if (isCharacter)
                {
                    alreadyOwned = SaveLoadManager.Data.unlockedByName.TryGetValue(characterData.char_name, out bool isUnlocked) && isUnlocked;

                    if (!alreadyOwned)
                    {
                        // 캐릭터 획득 처리     
                        CharacterHelper.AcquireCharacter(selectedItem.Gacha_item, DataTableManager.CharacterTable);

                        // 뽑기 퀘스트 완료 처리
                        QuestManager.Instance.OnGachaDraw();
                    }
                    else
                    {
                        if (selectedItem.Gacha_have > 0)
                        {
                            var itemData = DataTableManager.ItemTable.Get(selectedItem.Gacha_have);
                            if (itemData != null)
                            {
                                ItemInvenHelper.AddItem(selectedItem.Gacha_have, selectedItem.Gacha_have_amount); // 중복 보상 아이템 추가

                                //뽑기 퀘스트 완료 처리
                                QuestManager.Instance.OnGachaDraw();
                            }
                        }
                    }
                    result.Add(new GachaResult(selectedItem, characterData, alreadyOwned));
                }

                else
                {
                    ItemInvenHelper.AddItem(selectedItem.Gacha_item, selectedItem.Gacha_item_amount);
                    QuestManager.Instance.OnGachaDraw();

                    var itemData = DataTableManager.ItemTable.Get(selectedItem.Gacha_item);
                    result.Add(new GachaResult(selectedItem, null, false));
                }
            }
        }

        return result;
    }

    // 확률에 따른 랜덤 아이템 선택
    private GachaData DrawRandomItem(List<GachaData> gachaItems)
    {
        // 전체 확률의 합 계산
        int totalPercentage = 0;
        foreach (var item in gachaItems)
        {
            totalPercentage += item.Gacha_per;
        }

        // 랜덤 값 생성
        int randomValue = Random.Range(0, totalPercentage);

        // 확률에 따른 아이템 선택
        int currentSum = 0;
        foreach (var item in gachaItems)
        {
            currentSum += item.Gacha_per;
            if (randomValue < currentSum)
            {
                return item;
            }
        }

        // 예외 상황 (마지막 아이템 반환)
        return gachaItems[gachaItems.Count - 1];
    }
}

