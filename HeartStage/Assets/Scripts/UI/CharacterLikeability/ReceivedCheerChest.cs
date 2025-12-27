using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ReceivedCheerChest : MonoBehaviour
{
    public GameObject receivedCheerItemPrefab;
    public Transform itemParent;
    public Button getAllButton;

    private string characterName;
    private CancellationTokenSource allGetCts;

    private void Awake()
    {
        getAllButton.onClick.AddListener(() => AllGet().Forget());
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

    private async UniTaskVoid AllGet()
    {
        if (allGetCts != null)
            return;

        allGetCts = new CancellationTokenSource();
        var token = allGetCts.Token;

        try
        {
            SoundManager.Instance.PlayUIButtonClickSound();

            var items = itemParent.GetComponentsInChildren<ReceivedCheerItem>();

            foreach (var item in items)
            {
                if (token.IsCancellationRequested)
                    return;

                if (item.receiveButton.interactable)
                    await item.ReceiveAsync().AttachExternalCancellation(token);
            }
        }
        finally
        {
            allGetCts?.Dispose();
            allGetCts = null; 
        }
    }

    private void OnDisable()
    {
        allGetCts?.Cancel();
        allGetCts?.Dispose();
        allGetCts = null;
    }
}