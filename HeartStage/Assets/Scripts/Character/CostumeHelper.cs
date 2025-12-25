using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 의상 스프라이트 로드 및 적용 유틸리티.
/// Addressable 시스템을 통해 스프라이트를 로드.
/// </summary>
public static class CostumeHelper
{
    // 스프라이트 캐시 (Address → Sprite)
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    /// <summary>
    /// 의상 타입별 스프라이트 개수
    /// </summary>
    private static int GetSpriteCount(CostumeType type)
    {
        return type switch
        {
            CostumeType.Top => 5,    // Top_1 ~ Top_5
            CostumeType.Pants => 6,  // Pants_1 ~ Pants_6 (일부 의상은 6번 없음)
            CostumeType.Shoes => 2,  // Shoes_1, Shoes_2
            _ => 0
        };
    }

    /// <summary>
    /// 의상 스프라이트 주소 생성
    /// 예: Top/1/i/Top_1.png, Pants/2/i/Pants_3.png, Shoes/5/Shoes_1.png
    /// </summary>
    public static string GetSpriteAddress(CostumeType type, int spriteId, int index)
    {
        string typeName = type.ToString();

        // Shoes는 피부색 폴더(i) 없음
        if (type == CostumeType.Shoes)
        {
            // Shoes 인덱스는 1부터 시작
            return $"{typeName}/{spriteId}/{typeName}_{index + 1}.png";
        }

        // Top, Pants는 피부색 폴더(i) 있음, 인덱스 1부터 시작
        return $"{typeName}/{spriteId}/i/{typeName}_{index + 1}.png";
    }

    /// <summary>
    /// 의상 스프라이트 로드 및 적용
    /// </summary>
    public static async UniTask ApplyCostume(CostumeController controller, CostumeType type, int itemId)
    {
        if (itemId <= 0) return;

        int spriteId = CostumeItemID.GetSpriteId(itemId);
        if (spriteId <= 0) return;

        int count = GetSpriteCount(type);
        Sprite[] sprites = new Sprite[count];

        // 모든 스프라이트 로드
        for (int i = 0; i < count; i++)
        {
            string address = GetSpriteAddress(type, spriteId, i);
            sprites[i] = await LoadSprite(address);
        }

        // 컨트롤러에 적용
        controller.SetSprites(type, sprites);
    }

    // 실패한 주소 캐시 (재시도 방지)
    private static HashSet<string> failedAddresses = new HashSet<string>();

    /// <summary>
    /// Addressable에서 스프라이트 로드 (캐시 사용)
    /// </summary>
    public static async UniTask<Sprite> LoadSprite(string address)
    {
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
            // 먼저 키가 존재하는지 확인 (InvalidKeyException 방지)
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
            // 없는 스프라이트는 조용히 스킵 (에러 로깅 안 함)
            failedAddresses.Add(address);
            return null;
        }
    }

    /// <summary>
    /// 캐시 클리어
    /// </summary>
    public static void ClearCache()
    {
        spriteCache.Clear();
    }

    /// <summary>
    /// 캐릭터의 장착 의상 미리 로드 (씬 전환 시 호출)
    /// </summary>
    public static async UniTask PreloadEquippedCostumes(string characterName)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return;

        if (!saveData.equippedCostumeByChar.TryGetValue(characterName, out var costume))
            return;

        // 모든 장착 의상 병렬 로드
        var tasks = new List<UniTask>();

        if (costume.topItemId > 0)
            tasks.Add(PreloadCostume(CostumeType.Top, costume.topItemId));
        if (costume.pantsItemId > 0)
            tasks.Add(PreloadCostume(CostumeType.Pants, costume.pantsItemId));
        if (costume.shoesItemId > 0)
            tasks.Add(PreloadCostume(CostumeType.Shoes, costume.shoesItemId));

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 특정 의상의 모든 스프라이트 미리 로드
    /// </summary>
    public static async UniTask PreloadCostume(CostumeType type, int itemId)
    {
        int spriteId = CostumeItemID.GetSpriteId(itemId);
        if (spriteId <= 0) return;

        int count = GetSpriteCount(type);
        var tasks = new List<UniTask<Sprite>>();

        for (int i = 0; i < count; i++)
        {
            string address = GetSpriteAddress(type, spriteId, i);
            tasks.Add(LoadSprite(address));
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 모든 캐릭터의 장착 의상 미리 로드
    /// </summary>
    public static async UniTask PreloadAllEquippedCostumes()
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return;

        var tasks = new List<UniTask>();
        foreach (var charName in saveData.equippedCostumeByChar.Keys)
        {
            tasks.Add(PreloadEquippedCostumes(charName));
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 보유한 모든 의상 미리 로드 (의상 선택 UI 열 때 호출)
    /// </summary>
    public static async UniTask PreloadOwnedCostumes(CostumeType? typeFilter = null)
    {
        var ownedCostumes = GetOwnedCostumes(typeFilter);
        if (ownedCostumes.Count == 0) return;

        var tasks = new List<UniTask>();
        foreach (var itemId in ownedCostumes)
        {
            var type = CostumeItemID.GetCostumeType(itemId);
            tasks.Add(PreloadCostume(type, itemId));
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 프로그레스 콜백과 함께 의상 프리로드
    /// </summary>
    /// <param name="onProgress">진행률 콜백 (0.0 ~ 1.0)</param>
    public static async UniTask PreloadOwnedCostumesWithProgress(
        CostumeType? typeFilter,
        System.Action<float> onProgress)
    {
        var ownedCostumes = GetOwnedCostumes(typeFilter);
        if (ownedCostumes.Count == 0)
        {
            onProgress?.Invoke(1f);
            return;
        }

        // 총 스프라이트 개수 계산
        int totalSprites = 0;
        foreach (var itemId in ownedCostumes)
        {
            var type = CostumeItemID.GetCostumeType(itemId);
            totalSprites += GetSpriteCount(type);
        }

        // 공유 카운터 (클로저에서 안전하게 사용)
        int[] counter = { 0 };
        int total = totalSprites;
        var tasks = new List<UniTask>();

        foreach (var itemId in ownedCostumes)
        {
            var type = CostumeItemID.GetCostumeType(itemId);
            int spriteId = CostumeItemID.GetSpriteId(itemId);
            if (spriteId <= 0) continue;

            int count = GetSpriteCount(type);
            for (int i = 0; i < count; i++)
            {
                string address = GetSpriteAddress(type, spriteId, i);
                tasks.Add(LoadSpriteWithProgress(address, () =>
                {
                    counter[0]++;
                    onProgress?.Invoke((float)counter[0] / total);
                }));
            }
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 완료 콜백과 함께 스프라이트 로드
    /// </summary>
    private static async UniTask LoadSpriteWithProgress(string address, System.Action onComplete)
    {
        await LoadSprite(address);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 모든 장착 의상 프리로드 (프로그레스 포함)
    /// </summary>
    public static async UniTask PreloadAllEquippedCostumesWithProgress(System.Action<float> onProgress)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null || saveData.equippedCostumeByChar.Count == 0)
        {
            onProgress?.Invoke(1f);
            return;
        }

        // 총 스프라이트 개수 계산
        int totalSprites = 0;
        foreach (var costume in saveData.equippedCostumeByChar.Values)
        {
            if (costume.topItemId > 0) totalSprites += GetSpriteCount(CostumeType.Top);
            if (costume.pantsItemId > 0) totalSprites += GetSpriteCount(CostumeType.Pants);
            if (costume.shoesItemId > 0) totalSprites += GetSpriteCount(CostumeType.Shoes);
        }

        if (totalSprites == 0)
        {
            onProgress?.Invoke(1f);
            return;
        }

        // 공유 카운터 (클로저에서 안전하게 사용)
        int[] counter = { 0 };
        int total = totalSprites;
        var tasks = new List<UniTask>();

        foreach (var costume in saveData.equippedCostumeByChar.Values)
        {
            if (costume.topItemId > 0)
                AddSpriteTasks(tasks, CostumeType.Top, costume.topItemId, counter, total, onProgress);
            if (costume.pantsItemId > 0)
                AddSpriteTasks(tasks, CostumeType.Pants, costume.pantsItemId, counter, total, onProgress);
            if (costume.shoesItemId > 0)
                AddSpriteTasks(tasks, CostumeType.Shoes, costume.shoesItemId, counter, total, onProgress);
        }

        await UniTask.WhenAll(tasks);
    }

    private static void AddSpriteTasks(
        List<UniTask> tasks,
        CostumeType type,
        int itemId,
        int[] counter,
        int totalSprites,
        System.Action<float> onProgress)
    {
        int spriteId = CostumeItemID.GetSpriteId(itemId);
        if (spriteId <= 0) return;

        int count = GetSpriteCount(type);

        for (int i = 0; i < count; i++)
        {
            string address = GetSpriteAddress(type, spriteId, i);
            tasks.Add(LoadSpriteWithProgress(address, () =>
            {
                counter[0]++;
                onProgress?.Invoke((float)counter[0] / totalSprites);
            }));
        }
    }

    /// <summary>
    /// 의상이 캐시에 있는지 확인
    /// </summary>
    public static bool IsCostumeCached(int itemId)
    {
        var type = CostumeItemID.GetCostumeType(itemId);
        int spriteId = CostumeItemID.GetSpriteId(itemId);
        if (spriteId <= 0) return false;

        int count = GetSpriteCount(type);
        for (int i = 0; i < count; i++)
        {
            string address = GetSpriteAddress(type, spriteId, i);
            if (!spriteCache.ContainsKey(address))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 보유 의상 중 캐시 안 된 게 있는지 확인
    /// </summary>
    public static bool NeedsPreload(CostumeType? typeFilter = null)
    {
        var ownedCostumes = GetOwnedCostumes(typeFilter);
        foreach (var itemId in ownedCostumes)
        {
            if (!IsCostumeCached(itemId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 의상 아이템 보유 확인 (itemList 기반)
    /// </summary>
    public static bool HasCostume(int itemId)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return false;

        return saveData.itemList.TryGetValue(itemId, out int count) && count > 0;
    }

    /// <summary>
    /// 보유 의상 목록 가져오기 (타입별 필터링, itemList + ItemTable 기반)
    /// </summary>
    public static List<int> GetOwnedCostumes(CostumeType? typeFilter = null)
    {
        var saveData = SaveLoadManager.Data;
        if (saveData == null) return new List<int>();

        var itemTable = DataTableManager.ItemTable;

        var result = new List<int>();
        foreach (var kvp in saveData.itemList)
        {
            int itemId = kvp.Key;
            int count = kvp.Value;

            // 의상 아이템이고 보유 중인 경우
            if (count > 0 && CostumeItemID.IsCostumeItem(itemId))
            {
                // ItemTable에 존재하는지 확인
                if (itemTable != null && itemTable.Get(itemId) == null)
                    continue;

                // 타입 필터가 있으면 체크
                if (!typeFilter.HasValue || CostumeItemID.GetCostumeType(itemId) == typeFilter.Value)
                {
                    result.Add(itemId);
                }
            }
        }
        return result;
    }
}
