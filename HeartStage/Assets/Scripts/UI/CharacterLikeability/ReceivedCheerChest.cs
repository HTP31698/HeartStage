using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class ReceivedCheerChest : MonoBehaviour
{
    public GameObject receivedCheerItemPrefab;
    public Transform itemParent;
    public Button getAllButton;

    private string characterName;

    private void Awake()
    {
        getAllButton.onClick.AddListener(AllGet);
    }

    public void Init(string characterName)
    {
        this.characterName = characterName;
        gameObject.SetActive(true);

        for (int i = itemParent.childCount - 1; i >= 0; i--)
            Destroy(itemParent.GetChild(i).gameObject);

        LoadFromServer().Forget();
    }

    // 응원 리스트 서버에서 가져오기
    private async UniTask LoadFromServer()
    {
        string snapshotCharacter = characterName;
        string targetUid = AuthManager.Instance.UserId;

        Dictionary<string, int> cheerDict = await FriendCheerService.GetCheerListAsync(targetUid, snapshotCharacter);

        // Init 다시 호출됐으면 무시
        if (this == null || characterName != snapshotCharacter)
            return;

        foreach (var pair in cheerDict)
        {
            string fromUid = pair.Key;
            int count = pair.Value;

            for (int i = 0; i < count; i++)
            {
                if (this == null) return;

                var go = Instantiate(receivedCheerItemPrefab, itemParent);
                go.GetComponent<ReceivedCheerItem>().Init(snapshotCharacter, fromUid);
            }
        }
    }

    private async void AllGet()
    {
        var items = itemParent.GetComponentsInChildren<ReceivedCheerItem>();

        foreach (var item in items)
        {
            if (item.receiveButton.interactable)
                await item.ReceiveAsync();
        }
    }
}