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
        // 4등급 변환 로직이 가챠로 이동되어 더 이상 사용되지 않음
        SoundManager.Instance.PlayUICloseClickSound();
    }
}