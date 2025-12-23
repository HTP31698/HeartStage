using UnityEngine;

public class ReceivedCheerChest : MonoBehaviour
{
    public GameObject receivedCheerItemPrefab;
    public Transform itemParent;

    public void Init(string characterName)
    {
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
            go.GetComponent<ReceivedCheerItem>().Init(nickname);
        }
    }
}