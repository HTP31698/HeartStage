using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class NoticeWindowUI : GenericWindow
{
    [Header("리스트 컨테이너 (ScrollView Content)")]
    [SerializeField] private RectTransform listContent;

    [Header("아이템 프리팹")]
    [SerializeField] private NoticeItemUI itemPrefab;

    [Header("닫기 버튼")]
    [SerializeField] private Button closeButton;

    private bool _initialized = false;
    private readonly List<NoticeItemUI> _spawned = new();

    protected override void Awake()
    {
        base.Awake();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    /// <summary>
    /// 로비 로딩 중에 한 번 호출해서 리스트를 미리 만들어두는 함수.
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (_initialized)
            return;

        RebuildList();
        _initialized = true;

        // 레이아웃 한 프레임 돌리기용 (필수는 아님)
        await UniTask.Yield();
    }

    private void RebuildList()
    {
        if (listContent == null || itemPrefab == null)
            return;

        foreach (var ui in _spawned)
        {
            if (ui != null)
                Destroy(ui.gameObject);
        }
        _spawned.Clear();

        var notices = LiveConfigManager.Instance?.Notices;
        if (notices == null)
            return;

        foreach (var data in notices)
        {
            var item = Instantiate(itemPrefab, listContent);
            item.Init(data);
            _spawned.Add(item);
        }
    }

    public override void Open()
    {
        if (!_initialized)
        {
            // 혹시나 InitializeAsync를 못 탔을 경우를 대비한 방어 코드
            RebuildList();
            _initialized = true;
        }

        base.Open();
    }
}
