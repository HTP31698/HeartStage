using AssetKits.ParticleImage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceivedCheerItem : MonoBehaviour
{
    public Image playerIcon;
    public TextMeshProUGUI receiveText;
    public Button receiveButton;
    public ParticleImage receiveEffect;

    public void Init(string nickName)
    {
        receiveText.text = $"{nickName} 님이 응원하셨습니다.";
    }
}