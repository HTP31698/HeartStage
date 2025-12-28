using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ClothItemSlotCreator : MonoBehaviour
{
    public ShopItemSlot slotPrefab;
    public int startShopID = 0;
    public int createCount = 50;

    [ContextMenu("Rebuild Shop Item Slots")]
    private void RebuildSlots()
    {
#if UNITY_EDITOR
        if (slotPrefab == null)
        {
            Debug.LogError("Slot Prefab이 없습니다.");
            return;
        }

        // 1️⃣ 기존 자식 전부 삭제
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // 2️⃣ 새로 생성
        int currentID = startShopID;

        for (int i = 0; i < createCount; i++)
        {
            ShopItemSlot slot = PrefabUtility.InstantiatePrefab(
                slotPrefab,
                transform
            ) as ShopItemSlot;

            slot.shopTableID = currentID;
            slot.name = $"ShopItemSlot_{currentID}";
            currentID++;
        }

        Debug.Log("Shop Item Slot 재생성 완료");
#endif
    }
}