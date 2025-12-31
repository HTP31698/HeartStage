using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 포토카드 스프라이트 로드 및 관리 유틸리티.
/// Addressable 시스템을 통해 스프라이트를 로드.
/// </summary>
public static class PhotocardHelper
{
    // 스프라이트 캐시 (Address → Sprite)
    private static readonly Dictionary<string, Sprite> spriteCache = new();
    // 실패한 주소 캐시 (재시도 방지)
    private static readonly HashSet<string> failedAddresses = new();

    /// <summary>
    /// 포토카드 아이템인지 확인 (item_type == 4)
    /// </summary>
    public static bool IsPhotocardItem(int itemId)
    {
        var itemTable = DataTableManager.ItemTable;
        if (itemTable == null) return false;

        var item = itemTable.Get(itemId);
        return item != null && item.item_type == 4;
    }

    /// <summary>
    /// 포토카드 보유 확인
    /// - itemList에 있으면 보유
    /// - 기본 포토카드(첫 번째)는 캐릭터 보유시 자동 보유
    /// </summary>
    public static bool HasPhotocard(int itemId)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return false;

        // 1. itemList에 있으면 보유
        if (saveData.itemList.TryGetValue(itemId, out int count) && count > 0)
            return true;

        // 2. 기본 포토카드인 경우: 해당 캐릭터 보유시 자동 보유
        var itemTable = DataTableManager.ItemTable;
        var item = itemTable?.Get(itemId);
        if (item != null && item.item_type == 4 && !string.IsNullOrEmpty(item.char_code))
        {
            // 해당 char_code의 첫 번째 포토카드(기본)인지 확인
            var allCards = itemTable.GetPhotocardsByCharCode(item.char_code);
            if (allCards.Count > 0 && allCards[0].item_id == itemId)
            {
                // 캐릭터 보유 여부 체크 (이름 기준)
                string charName = GetCharNameByCode(item.char_code);
                if (!string.IsNullOrEmpty(charName))
                {
                    return saveData.unlockedByName.TryGetValue(charName, out bool unlocked) && unlocked;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 특정 캐릭터의 장착된 포토카드 아이템 ID 반환
    /// 명시적 장착이 없으면 기본 포토카드(첫 번째) 반환
    /// </summary>
    public static int GetEquippedPhotocardId(string charCode)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return 0;

        // 명시적으로 장착된 포토카드가 있으면 반환
        if (saveData.equippedPhotocardByChar.TryGetValue(charCode, out int itemId) && itemId > 0)
            return itemId;

        // 없으면 기본 포토카드(첫 번째) 반환
        var itemTable = DataTableManager.ItemTable;
        if (itemTable != null)
        {
            var allCards = itemTable.GetPhotocardsByCharCode(charCode);
            if (allCards.Count > 0)
                return allCards[0].item_id;
        }

        return 0;
    }

    /// <summary>
    /// 포토카드 장착
    /// </summary>
    public static void EquipPhotocard(string charCode, int itemId)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return;

        saveData.equippedPhotocardByChar[charCode] = itemId;
        SaveLoadManager.Save();
    }

    /// <summary>
    /// 포토카드 장착 해제 (기본 카드로)
    /// </summary>
    public static void UnequipPhotocard(string charCode)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return;

        saveData.equippedPhotocardByChar.Remove(charCode);
        SaveLoadManager.Save();
    }

    /// <summary>
    /// 특정 캐릭터의 보유 포토카드 목록 반환
    /// </summary>
    public static List<ItemCSVData> GetOwnedPhotocards(string charCode)
    {
        var result = new List<ItemCSVData>();
        var itemTable = DataTableManager.ItemTable;
        if (itemTable == null) return result;

        var photocards = itemTable.GetPhotocardsByCharCode(charCode);
        foreach (var card in photocards)
        {
            if (HasPhotocard(card.item_id))
            {
                result.Add(card);
            }
        }
        return result;
    }

    /// <summary>
    /// 특정 캐릭터의 모든 포토카드 목록 반환 (보유/미보유 포함)
    /// </summary>
    public static List<ItemCSVData> GetAllPhotocards(string charCode)
    {
        var itemTable = DataTableManager.ItemTable;
        if (itemTable == null) return new List<ItemCSVData>();

        return itemTable.GetPhotocardsByCharCode(charCode);
    }

    /// <summary>
    /// 포토카드 스프라이트 주소 반환
    /// ItemTable.prefab 값을 그대로 사용
    /// </summary>
    public static string GetSpriteAddress(int itemId)
    {
        var itemTable = DataTableManager.ItemTable;
        if (itemTable == null) return null;

        var item = itemTable.Get(itemId);
        if (item == null || string.IsNullOrEmpty(item.prefab)) return null;

        // Addressable 주소: CardImage/{prefab}
        return $"CardImage/{item.prefab}";
    }

    /// <summary>
    /// 포토카드 Frame 버전 스프라이트 주소 반환
    /// ItemTable.prefab_frame 값을 사용 (스테이지 배치용)
    /// </summary>
    public static string GetSpriteAddressWithFrame(int itemId)
    {
        var itemTable = DataTableManager.ItemTable;
        if (itemTable == null) return null;

        var item = itemTable.Get(itemId);
        if (item == null) return null;

        // prefab_frame이 있으면 사용, 없으면 prefab으로 fallback
        string spriteName = !string.IsNullOrEmpty(item.prefab_frame) ? item.prefab_frame : item.prefab;
        if (string.IsNullOrEmpty(spriteName)) return null;

        return $"CardImage/{spriteName}";
    }

    /// <summary>
    /// 캐릭터의 현재 표시할 포토카드 스프라이트 주소 반환
    /// 장착된 포토카드가 없으면 CharacterTable.card_imageName 사용
    /// </summary>
    public static string GetDisplaySpriteAddress(string charCode)
    {
        int equippedId = GetEquippedPhotocardId(charCode);

        if (equippedId > 0)
        {
            return GetSpriteAddress(equippedId);
        }

        // 기본 카드 이미지 (CharacterTable.card_imageName)
        // CardImage/FDhana 형식
        var charTable = DataTableManager.CharacterTable;
        if (charTable == null) return null;

        // charCode로 캐릭터 찾기 (레벨1, 랭크1 기준)
        var charData = charTable.GetByCharCode(charCode);
        if (charData == null || string.IsNullOrEmpty(charData.card_imageName)) return null;

        return $"CardImage/{charData.card_imageName}";
    }

    /// <summary>
    /// 캐릭터의 현재 표시할 포토카드 Frame 버전 스프라이트 주소 반환
    /// 스테이지 배치 UI에서 사용
    /// </summary>
    public static string GetDisplaySpriteAddressWithFrame(string charCode)
    {
        int equippedId = GetEquippedPhotocardId(charCode);

        if (equippedId > 0)
        {
            return GetSpriteAddressWithFrame(equippedId);
        }

        // 기본 카드 이미지: CharacterTable.card_imageName + "_Frame" 시도
        var charTable = DataTableManager.CharacterTable;
        if (charTable == null) return null;

        var charData = charTable.GetByCharCode(charCode);
        if (charData == null || string.IsNullOrEmpty(charData.card_imageName)) return null;

        // 기본 카드는 Frame 버전이 없으므로 그냥 반환
        return $"CardImage/{charData.card_imageName}";
    }

    /// <summary>
    /// 포토카드 스프라이트 로드 (캐시 사용)
    /// </summary>
    public static async UniTask<Sprite> LoadSprite(string address)
    {
        if (string.IsNullOrEmpty(address)) return null;

        // 캐시 확인
        if (spriteCache.TryGetValue(address, out var cached))
        {
            return cached;
        }

        // 이미 실패한 주소는 스킵
        if (failedAddresses.Contains(address))
        {
            return null;
        }

        try
        {
            // 먼저 키가 존재하는지 확인
            var locHandle = Addressables.LoadResourceLocationsAsync(address);
            await locHandle.ToUniTask();

            if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result.Count == 0)
            {
                Addressables.Release(locHandle);
                failedAddresses.Add(address);
                return null;
            }
            Addressables.Release(locHandle);

            // 키가 존재하면 로드
            var handle = Addressables.LoadAssetAsync<Sprite>(address);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                spriteCache[address] = handle.Result;
                return handle.Result;
            }
            else
            {
                failedAddresses.Add(address);
                return null;
            }
        }
        catch (System.Exception)
        {
            failedAddresses.Add(address);
            return null;
        }
    }

    /// <summary>
    /// 포토카드 스프라이트 로드 (아이템 ID로)
    /// </summary>
    public static async UniTask<Sprite> LoadPhotocardSprite(int itemId)
    {
        string address = GetSpriteAddress(itemId);
        return await LoadSprite(address);
    }

    /// <summary>
    /// 캐릭터의 현재 표시할 포토카드 스프라이트 로드
    /// ResourceManager 먼저 시도, 실패시 Addressables 로드
    /// </summary>
    public static async UniTask<Sprite> LoadDisplaySprite(string charCode)
    {
        // 1. 장착된 포토카드 ID 확인
        int equippedId = GetEquippedPhotocardId(charCode);

        // 2. prefab 이름으로 ResourceManager에서 먼저 시도
        string prefabName = null;
        if (equippedId > 0)
        {
            var itemTable = DataTableManager.ItemTable;
            var item = itemTable?.Get(equippedId);
            prefabName = item?.prefab;
        }
        else
        {
            // 기본 카드: CharacterTable.card_imageName
            var charTable = DataTableManager.CharacterTable;
            var charData = charTable?.GetByCharCode(charCode);
            prefabName = charData?.card_imageName;
        }

        // 3. ResourceManager에서 먼저 시도
        if (!string.IsNullOrEmpty(prefabName))
        {
            var sprite = ResourceManager.Instance?.GetSprite(prefabName);
            if (sprite != null) return sprite;
        }

        // 4. Addressables fallback
        string address = GetDisplaySpriteAddress(charCode);
        return await LoadSprite(address);
    }

    /// <summary>
    /// 캐릭터의 현재 표시할 포토카드 Frame 버전 스프라이트 로드
    /// 스테이지 배치 UI에서 사용 (프레임이 있는 카드 이미지)
    /// ResourceManager 먼저 시도, 실패시 Addressables 로드
    /// </summary>
    public static async UniTask<Sprite> LoadDisplaySpriteWithFrame(string charCode)
    {
        // 1. 장착된 포토카드 ID 확인
        int equippedId = GetEquippedPhotocardId(charCode);

        // 2. prefab_frame 이름으로 ResourceManager에서 먼저 시도
        string prefabFrameName = null;
        string prefabName = null;
        if (equippedId > 0)
        {
            var itemTable = DataTableManager.ItemTable;
            var item = itemTable?.Get(equippedId);
            prefabFrameName = item?.prefab_frame;
            prefabName = item?.prefab;
        }
        else
        {
            // 기본 카드: CharacterTable.card_imageName (Frame 버전 없음)
            var charTable = DataTableManager.CharacterTable;
            var charData = charTable?.GetByCharCode(charCode);
            prefabName = charData?.card_imageName;
        }

        // 3. ResourceManager에서 Frame 버전 먼저 시도
        if (!string.IsNullOrEmpty(prefabFrameName))
        {
            var sprite = ResourceManager.Instance?.GetSprite(prefabFrameName);
            if (sprite != null) return sprite;
        }

        // 4. Frame 버전이 없으면 일반 버전으로 fallback
        if (!string.IsNullOrEmpty(prefabName))
        {
            var sprite = ResourceManager.Instance?.GetSprite(prefabName);
            if (sprite != null) return sprite;
        }

        // 5. Addressables fallback
        string address = GetDisplaySpriteAddressWithFrame(charCode);
        return await LoadSprite(address);
    }

    /// <summary>
    /// 캐시 클리어
    /// </summary>
    public static void ClearCache()
    {
        spriteCache.Clear();
        failedAddresses.Clear();
    }

    /// <summary>
    /// char_id에서 char_code 추출 (마지막 4자리)
    /// 예: 11010101 → 0101
    /// </summary>
    public static string ExtractCharCode(int charId)
    {
        return (charId % 10000).ToString("D4");
    }

    /// <summary>
    /// 캐릭터 이름으로 char_code 반환
    /// </summary>
    public static string GetCharCodeByName(string charName)
    {
        return charName switch
        {
            "하나" => "0101",
            "세라" => "0502",
            "리아" => "0403",
            "지안" => "0504",
            "승아" => "0105",
            "에리" => "0406",
            "레나" => "0707",
            "지우" => "0208",
            "지아" => "0609",
            "아윤" => "0310",
            _ => null
        };
    }

    /// <summary>
    /// char_code로 캐릭터 이름 반환
    /// </summary>
    public static string GetCharNameByCode(string charCode)
    {
        return charCode switch
        {
            "0101" => "하나",
            "0502" => "세라",
            "0403" => "리아",
            "0504" => "지안",
            "0105" => "승아",
            "0406" => "에리",
            "0707" => "레나",
            "0208" => "지우",
            "0609" => "지아",
            "0310" => "아윤",
            _ => null
        };
    }
}
