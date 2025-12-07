using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IconChangeWindow : MonoBehaviour
{
    [Header("로비 아이콘")]
    [SerializeField] private LobbyUI lobbyProfileIcon;

    [Header("리스트")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject iconItemPrefab;

    [Header("버튼")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button closeButton;

    public bool IsOpen => gameObject.activeSelf;

    private readonly List<GameObject> _spawnedItems = new();
    private readonly List<IconChangeItemUI> _items = new();

    private string _selectedKey;

    private void Awake()
    {
        if (applyButton != null)
            applyButton.onClick.AddListener(OnClickApply);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        RebuildList();           // 열 때 한 번 새로 만들고
        InitSelectionFromSave(); // 세이브 기준으로 기본 선택 세팅
    }

    public void Close()
    {
        gameObject.SetActive(false);

        // 여기서 ProfileWindow에 "팝업 닫혔다" 알려줘야 모달이 같이 꺼짐
        ProfileWindow.Instance?.OnPopupClosed();
    }

    // ===== Prewarm / RebuildList =====

    public void Prewarm()
    {
        bool wasActive = gameObject.activeSelf;
        gameObject.SetActive(true);
        RebuildList();
        InitSelectionFromSave();
        gameObject.SetActive(wasActive);
    }

    private void RebuildList()
    {
        // 기존 생성된 애들 정리
        foreach (var go in _spawnedItems)
        {
            if (go != null)
                Destroy(go);
        }
        _spawnedItems.Clear();
        _items.Clear();

        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null)
            return;

        var charTable = DataTableManager.CharacterTable;
        if (charTable == null)
            return;

        var unlocked = data.unlockedByName;

        HashSet<string> iconKeys = new();

        if (unlocked != null)
        {
            foreach (var kv in unlocked)
            {
                string charName = kv.Key;
                bool isUnlocked = kv.Value;

                if (!isUnlocked)
                    continue;

                var row = charTable.GetByName(charName);
                if (row == null)
                    continue;

                string iconKey = row.icon_imageName;
                if (string.IsNullOrEmpty(iconKey))
                    continue;

                var sprite = ResourceManager.Instance.GetSprite(iconKey);
                if (sprite == null)
                    continue;

                iconKeys.Add(iconKey);
            }
        }

        if (data.ownedProfileIconKeys != null)
        {
            foreach (var key in data.ownedProfileIconKeys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                var sprite = ResourceManager.Instance.GetSprite(key);

                if (sprite == null)
                    continue;

                iconKeys.Add(key);
            }
        }

        if (iconKeys.Count == 0)
        {
            const string fallback = "hanaicon";
            var fallbackSprite = ResourceManager.Instance.GetSprite(fallback);
            if (fallbackSprite != null)
                iconKeys.Add(fallback);
        }

        // 아이템 생성 + IconChangeItemUI 연결
        foreach (var key in iconKeys)
        {
            var sprite = ResourceManager.Instance.GetSprite(key);
            if (sprite == null)
                continue;

            var go = Instantiate(iconItemPrefab, contentRoot);
            go.SetActive(true);
            _spawnedItems.Add(go);

            var item = go.GetComponent<IconChangeItemUI>();
            if (item != null)
            {
                item.Setup(this, key, sprite);
                _items.Add(item);
            }
        }

        // 리스트 새로 만들고 나면 선택 상태도 재세팅
        InitSelectionFromSave();
    }

    // ===== 선택 / 적용 로직 =====

    private void InitSelectionFromSave()
    {
        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null)
            return;

        _selectedKey = data.profileIconKey;

        foreach (var item in _items)
        {
            bool selected = item.IconKey == _selectedKey;
            item.SetSelected(selected);
        }
    }

    // 아이콘 한 개 클릭했을 때(IconChangeItemUI에서 호출)
    public void OnClickItem(IconChangeItemUI item)
    {
        _selectedKey = item.IconKey;

        foreach (var i in _items)
        {
            i.SetSelected(i == item); // 선택된 애만 초록 테두리 ON
        }
    }

    // 적용 버튼 눌렀을 때
    private void OnClickApply()
    {
        if (string.IsNullOrEmpty(_selectedKey))
            return;

        // 실제 저장 + 프로필 반영 + 창닫기까지 여기서 처리
        OnClickIcon(_selectedKey).Forget();
    }

    // 실제로 세이브/서버/프로필 반영하는 부분
    private async UniTaskVoid OnClickIcon(string key)
    {
        var data = SaveLoadManager.Data as SaveDataV1;
        if (data == null)
            return;

        data.profileIconKey = key;

        if (!data.ownedProfileIconKeys.Contains(key))
            data.ownedProfileIconKeys.Add(key);

        await SaveLoadManager.SaveToServer();

        int achievementCount = AchievementUtil.GetCompletedAchievementCount(data);
        await PublicProfileService.UpdateMyPublicProfileAsync(data, achievementCount);

        ProfileWindow.Instance?.RefreshAll();

        lobbyProfileIcon?.RefreshProfileIcon();

        // 여기서 한 번만 Close() 호출 (OnClickApply에서는 호출 안함!)
        Close();
    }
}
