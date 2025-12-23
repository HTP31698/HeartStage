using UnityEngine;
using UnityEngine.UI;

public class ReceivedCheerChest : MonoBehaviour
{
    public GameObject receivedCheerItemPrefab;
    public Transform itemParent;
    public Button getAllButton;

    private void Awake()
    {
        getAllButton.onClick.AddListener(AllGet);
    }

    public void Init(string characterName)
    {
        gameObject.SetActive(true);
        // 기존 아이템 제거
        for (int i = itemParent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(itemParent.transform.GetChild(i).gameObject);
        }

        var dict = SaveLoadManager.Data.characterCheeredFriends;
        if (!dict.TryGetValue(characterName, out var cheerDict))
            return;

        foreach (var pair in cheerDict)
        {
            string uid = pair.Key;
            string nickname = pair.Value;

            var go = Instantiate(receivedCheerItemPrefab, itemParent.transform);
            go.GetComponent<ReceivedCheerItem>().Init(characterName, uid);
        }
    }

    private void AllGet()
    {
        var items = itemParent.GetComponentsInChildren<ReceivedCheerItem>();

        foreach (var item in items)
        {
            if (item.receiveButton.interactable)
                item.Receive();
        }
    }
}