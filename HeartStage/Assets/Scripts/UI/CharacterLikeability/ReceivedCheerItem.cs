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

    private string characterName;
    private string uid;

    private void Start()
    {
        receiveButton.onClick.AddListener(Receive);
    }

    public void Init(string characterName, string uid)
    {
        this.characterName = characterName;
        this.uid = uid;
        // 프로필 정보 세팅
        var profile = LobbySceneController.GetCachedFriendProfile(uid);
        playerIcon.sprite = ResourceManager.Instance.GetSprite(profile.profileIconKey);
        receiveText.text = $"{profile.nickname} 님이 응원하셨습니다.";
    }

    public void Receive()
    {
        receiveButton.interactable = false;
        receiveEffect.Play();
        // 여기서 현재 캐릭터 기준으로 호감도 지급        
        int current = CharacterHelper.GetLikeability(characterName);
        int add = CharacterLikeabilityPanel.Instance.likeabilityData.like_point;
        CharacterHelper.SetLikeability(characterName, current + add);
        // 세이브 데이터 갱신
        var data = SaveLoadManager.Data.characterCheeredFriends;
        if (data.TryGetValue(characterName, out var cheerDict))
        {
            cheerDict.Remove(uid);

            if (cheerDict.Count == 0)
                data.Remove(characterName);
        }
        SaveLoadManager.SaveToServer().Forget();
        // 호감도 UI 갱신
        CharacterLikeabilityPanel.Instance.RefreshLikeabilityUI();
        DestroyAfterEffect().Forget();
    }

    private async UniTaskVoid DestroyAfterEffect()
    {
        float duration = receiveEffect.main.duration;
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        Destroy(gameObject);
    }
}