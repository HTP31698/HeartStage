using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonitoringItemPrefab : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemCount;

    public void SetItemData(int itemId, int count)
    {
        // ItemTable에서 아이템 데이터 가져오기
        var itemData = DataTableManager.ItemTable.Get(itemId);
        if (itemData == null)
        {
            return;
        }

        // 아이템 이름 설정
        if (itemName != null)
            itemName.text = itemData.item_name;

        // 아이템 개수 설정
        if (itemCount != null)
            itemCount.text = count.ToString();

        // 아이템 아이콘 설정
        if (itemIcon != null && !string.IsNullOrEmpty(itemData.prefab))
        {
            itemIcon.sprite = ResourceManager.Instance.GetSprite(itemData.prefab);
        }
    }
}