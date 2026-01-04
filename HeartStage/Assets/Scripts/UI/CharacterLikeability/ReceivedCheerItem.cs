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
    private string fromUid;

    private void Awake()
    {
        receiveButton.onClick.AddListener(() => ReceiveAsync().Forget());
    }

    public void Init(string characterName, string fromUid)
    {
        this.characterName = characterName;
        this.fromUid = fromUid;

        var profile = LobbySceneController.GetCachedFriendProfile(fromUid);
        playerIcon.sprite = ResourceManager.Instance.GetSprite(profile.profileIconKey);
        receiveText.text = $"{profile.nickname} 님이 응원하셨습니다.";
    }

    public async UniTask ReceiveAsync()
    {
        if (!receiveButton.interactable)
            return;

        receiveButton.interactable = false;
        receiveEffect.Play();

        string myUid = AuthManager.Instance.UserId;

        bool success = await FriendCheerService.ConsumeCheerAsync(myUid, characterName, fromUid);

        if (!success)
        {
            receiveButton.interactable = true;
            return;
        }

        SoundManager.Instance.PlaySFX(SoundName.SFX_LobbyCharacter_Cheer);
        int current = CharacterHelper.GetLikeability(characterName);
        int add = CharacterLikeabilityPanel.Instance.likeabilityData.like_point;
        CharacterHelper.SetLikeability(characterName, current + add);

        CharacterLikeabilityPanel.Instance.RefreshLikeabilityUI();

        await UniTask.Delay(TimeSpan.FromSeconds(receiveEffect.main.duration));
        Destroy(gameObject);
    }
}