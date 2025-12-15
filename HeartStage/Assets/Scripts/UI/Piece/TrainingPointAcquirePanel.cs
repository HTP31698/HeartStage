using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainingPointAcquirePanel : MonoBehaviour
{
    public Image trainingPointImage;
    public TextMeshProUGUI characterAcquireText;

    private void Awake()
    {
        var trainingPointData = DataTableManager.ItemTable.Get(ItemID.TrainingPoint);
        var texture = ResourceManager.Instance.Get<Texture2D>(trainingPointData.prefab);
        trainingPointImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    public void Open(int amount)
    {
        characterAcquireText.text = $"x{amount}";
    }

    public void Close()
    {
        gameObject.SetActive(false);
        PieceExchangePanel.Instance.AfterAcquireTrainingPoint();
    }
}