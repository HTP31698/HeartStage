using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LobbyHomeInitializer : MonoBehaviour
{
    public GameObject characterBase; // 캐릭터 이미지 프리팹의 부모가 될 베이스 프리팹

    // 캐릭터 sorting order 조절
    private readonly List<SortingGroup> sortingGroups = new();
    private const int BaseOrder = 201;

    public void Init()
    {
        sortingGroups.Clear();

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
            Instantiate(imagePrefab, root.transform);

            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            root.transform.position = new Vector3(x, y, 0f);

            var sg = root.GetComponent<SortingGroup>();
            sortingGroups.Add(sg);
            sg.sortingOrder = BaseOrder + Mathf.RoundToInt(-y);

            var characterAi = root.GetComponent<LobbyCharacterAI>();
            characterAi.characterId = characterId;
        }
    }

    private void Update()
    {
        foreach (var sg in sortingGroups)
        {
            if (sg == null)
                continue;

            float y = sg.transform.position.y;
            sg.sortingOrder = BaseOrder + Mathf.RoundToInt(-y);
        }
    }
}