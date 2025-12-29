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
        trainingPointImage.sprite = ResourceManager.Instance.GetSprite(trainingPointData.prefab);
    }

    public void Open(int amount)
    {
        characterAcquireText.text = $"x{amount}";
    }

    public void Close()
    {
        gameObject.SetActive(false);
        PieceExchangePanel.Instance.AfterAcquireTrainingPoint();
        SoundManager.Instance.PlayUICloseClickSound();
    }
}