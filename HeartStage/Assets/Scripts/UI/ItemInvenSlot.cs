using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInvenSlot : MonoBehaviour
{
	public Image itemImage;
    public TextMeshProUGUI amountText;

    private int itemId;

    public void Init(int id, int amount)
    {
        var itemData = DataTableManager.ItemTable.Get(id);
        if (itemData == null)
        {
            return;
        }

        var texture = ResourceManager.Instance.Get<Texture2D>(itemData.prefab);
        if (texture == null)
        {
            return;
        }

        itemImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        amountText.text = $"X{amount}";
        itemImage.enabled = true;
        amountText.enabled = true;
        itemId = id;
    }

    private void OnDisable()
    {
        Clear();
    }

    // 이미지 지우기
    public void Clear()
    {
        itemImage.enabled = false;
        amountText.enabled = false;
    }

    // 아이템 슬롯 클릭시 아이템 설명창 띄우기
    public void OnItemImageClicked()
    {
        if (!itemImage.enabled)
            return;

        ItemInventoryUI.Instance.OpenItemInfoPanel(itemId);
    }
}