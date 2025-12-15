using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LobbyHomeInitializer : MonoBehaviour
{
    public GameObject characterBase; // 캐릭터 이미지 프리팹의 부모가 될 베이스 프리팹

    // 캐릭터 sorting order 조절
    private readonly List<Transform> characterRoots = new();
    private readonly List<SortingGroup> sortingGroups = new();
    private const int BaseOrder = 201;

    public void Init()
    {
        characterRoots.Clear();
        sortingGroups.Clear();

        // 이전꺼 삭제
        var prevObjects = GameObject.FindGameObjectsWithTag(Tag.LobbyHomeObject);
        foreach (var obj in prevObjects)
        {
            Destroy(obj);
        }

        Bounds bounds = DragZoomPanManager.Instance.InnerBounds;

        foreach (var characterId in SaveLoadManager.Data.ownedIds)
        {
            var characterData = DataTableManager.CharacterTable.Get(characterId);
            var imagePrefab = ResourceManager.Instance.Get<GameObject>(characterData.image_PrefabName);

            var root = Instantiate(characterBase, transform);
            var image = Instantiate(imagePrefab, root.transform);

            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            root.transform.position = new Vector3(x, y, 0f);

            var sg = image.GetComponent<SortingGroup>();

            characterRoots.Add(root.transform);
            sortingGroups.Add(sg);

            // 초기 정렬
            sg.sortingOrder = BaseOrder + Mathf.RoundToInt(-y);
        }
    }

    private void Update()
    {
        for (int i = 0; i < characterRoots.Count; i++)
        {
            var root = characterRoots[i];
            var sg = sortingGroups[i];

            if (root == null || sg == null)
                continue;

            sg.sortingOrder =
                BaseOrder + Mathf.RoundToInt(-root.position.y);
        }
    }
}