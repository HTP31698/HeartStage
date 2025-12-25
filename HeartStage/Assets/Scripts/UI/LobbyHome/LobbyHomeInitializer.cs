using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LobbyHomeInitializer : MonoBehaviour
{
    public static LobbyHomeInitializer Instance;

    public WindowManager windowManager;
    public CanvasGroup lobbyUiCanvasGroup;
    public GameObject returnButton;
    public List<GameObject> hideObjects;

    public GameObject characterBase; // 캐릭터 이미지 프리팹의 부모가 될 베이스 프리팹

    // 캐릭터 sorting order 조절
    private readonly List<SortingGroup> sortingGroups = new();
    private const int BaseOrder = 201;

    [HideInInspector]
    public bool isFriendHome = false;
    [HideInInspector]
    public SaveDataV1 friendSaveData;
    [HideInInspector]
    public string friendUID;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Init()
    {
        Init(SaveLoadManager.Data);
        isFriendHome = false;
        friendSaveData = null;
        friendUID = string.Empty;
    }

    public void Init(SaveDataV1 data)
    {
        sortingGroups.Clear();

        var prevObjects = GameObject.FindGameObjectsWithTag(Tag.LobbyHomeObject);
        foreach (var obj in prevObjects)
        {
            Destroy(obj);
        }

        Bounds bounds = DragZoomPanManager.Instance.InnerBounds;

        foreach (var characterId in data.ownedIds)
        {
            var characterData = DataTableManager.CharacterTable.Get(characterId);
            var imagePrefab = ResourceManager.Instance.Get<GameObject>(characterData.image_PrefabName);

            var root = Instantiate(characterBase, transform);
            var charObj = Instantiate(imagePrefab, root.transform);

            // 의상 적용
            var costumeController = charObj.GetComponent<CostumeController>();
            if (costumeController != null)
                costumeController.Initialize(characterData.char_name);

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

    // 친구 숙소 방문
    public void OpenLobbyHomeWindow(SaveDataV1 friendData)
    {
        if (friendData == null)
        {
            Debug.LogWarning("[LobbyHome] friendData is null");
            return;
        }

        lobbyUiCanvasGroup.interactable = false;
        returnButton.SetActive(true);
        foreach(var hide in hideObjects)
        {
            hide.SetActive(false);
        }

        windowManager.Open(WindowType.LobbyHome);
        isFriendHome = true;
        friendSaveData = friendData;
        Init(friendData);
    }

    // 내 숙소로 돌아가기
    public void ReturnMyLobbyHome()
    {
        lobbyUiCanvasGroup.interactable = true;
        returnButton.SetActive(false);
        foreach (var hide in hideObjects)
        {
            hide.SetActive(true);
        }
        Init();
    }
}