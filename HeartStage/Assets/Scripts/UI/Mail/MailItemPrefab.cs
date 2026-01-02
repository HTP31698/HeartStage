using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MailItemPrefab : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemCountText;

    public void Setup(ItemAttachment itemAttachment)
    {
        if (itemAttachment == null) return;

        if (int.TryParse(itemAttachment.itemId, out int itemId))
        {
            SetItemData(itemId, itemAttachment.count);
        }
    }

    private void SetItemData(int itemId, int count)
    {
        var itemData = DataTableManager.ItemTable.Get(itemId);
        if (itemData == null) return;

        if (itemNameText != null)
            itemNameText.text = itemData.item_name;

        if (itemCountText != null)
            itemCountText.text = $"x{count}";

        SetItemIcon(itemData.prefab);
    }

    private void SetItemIcon(string prefabName)
    {
        if (itemIcon == null || string.IsNullOrEmpty(prefabName)) return;

        itemIcon.sprite = ResourceManager.Instance.GetSprite(prefabName);
    }
}