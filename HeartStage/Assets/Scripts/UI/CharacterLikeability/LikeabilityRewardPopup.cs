using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LikeabilityRewardPopup : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Button receiveButton;
    [SerializeField] private Button closeButton;

    private CharacterLikeabilityPanel panel;

    private void Awake()
    {
        receiveButton.onClick.AddListener(OnClickReceive);
        closeButton.onClick.AddListener(Close);
    }

    public void Open(CharacterLikeabilityPanel owner, int itemId, int amount)
    {
        panel = owner;
        // 아이템 이미지 세팅
        var itemData = DataTableManager.ItemTable.Get(itemId);
        var texture = ResourceManager.Instance.Get<Texture2D>(itemData.prefab);
        itemImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        amountText.text = $"x{amount}";
        gameObject.SetActive(true);
    }

    private void OnClickReceive()
    {
        panel.ReceiveNextLikeabilityReward();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}