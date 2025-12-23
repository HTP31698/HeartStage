using AssetKits.ParticleImage;
using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReceivedCheerItem : MonoBehaviour
{
    public Image playerIcon;
    public TextMeshProUGUI receiveText;
    public Button receiveButton;
    public ParticleImage receiveEffect;

    private void Start()
    {
        receiveButton.onClick.AddListener(Receive);
    }

    public void Init(string nickName)
    {
        receiveText.text = $"{nickName} 님이 응원하셨습니다.";
    }

    private void Receive()
    {
        var characterData = CharacterLikeabilityPanel.Instance.characterData;    
        string characterName = characterData.char_name;

        receiveButton.interactable = false;
        receiveEffect.Play();

        // 여기서 현재 캐릭터 기준으로 호감도 지급
        int current = CharacterHelper.GetLikeability(characterName);
        int add = 10; 
        CharacterHelper.SetLikeability(characterName, current + add);

        CharacterLikeabilityPanel.Instance.RefreshLikeabilityUI(); // UI 반영
        // 만드는 중~~~~~~~~~~~~~~
        DestroyAfterEffect().Forget();
    }

    private async UniTaskVoid DestroyAfterEffect()
    {
        float duration = receiveEffect.main.duration;
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        Destroy(gameObject);
    }
}