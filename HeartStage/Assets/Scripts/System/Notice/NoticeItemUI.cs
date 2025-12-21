using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoticeItemUI : MonoBehaviour
{
    [Header("헤더 영역 (항상 보이는 부분)")]
    [SerializeField] private Button headerButton;          // 카드 전체 클릭용
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private TextMeshProUGUI dateText;

    [Header("펼쳐지는 본문 영역")]
    [SerializeField] private GameObject bodyRoot;          // 펼쳐질 컨테이너
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button cafeButton;            // "카페에서 자세히 보기"

    [Header("레이아웃 갱신용 (상위 Content 등)")]
    [SerializeField] private RectTransform layoutRoot;     // 비워두면 자기 RectTransform 사용

    [Header("애니메이션 (선택사항)")]
    [Tooltip("ExpandablePanel 컴포넌트가 있으면 애니메이션 사용")]
    [SerializeField] private ExpandablePanel expandablePanel;

    private NoticeData _data;
    private bool _expanded = false;
    private RectTransform _selfRect;
    private bool _useExpandablePanel = false;

    private void Awake()
    {
        // 자기 RectTransform 캐시
        _selfRect = transform as RectTransform;

        // 인스펙터에서 layoutRoot 안 넣어줬으면 자기 자신으로 사용
        if (layoutRoot == null)
            layoutRoot = _selfRect;

        // ExpandablePanel 자동 검색
        if (expandablePanel == null)
            expandablePanel = GetComponent<ExpandablePanel>();

        _useExpandablePanel = expandablePanel != null;
    }

    /// <summary>
    /// 외부에서 NoticeData를 넘겨줘서 카드 내용을 세팅하는 함수
    /// </summary>
    public void Init(NoticeData data)
    {
        _data = data;

        // 제목
        if (titleText != null)
            titleText.text = data.title;

        // 요약 (summary가 없으면 body 첫 줄 사용)
        if (summaryText != null)
        {
            if (!string.IsNullOrEmpty(data.summary))
            {
                summaryText.text = data.summary;
            }
            else
            {
                var lines = (data.body ?? "").Split(new[] { '\n' }, StringSplitOptions.None);
                summaryText.text = lines.Length > 0 ? lines[0] : "";
            }
        }

        // 날짜
        if (dateText != null)
        {
            if (!string.IsNullOrEmpty(data.createdAt))
            {
                int idx = data.createdAt.IndexOf('T');
                dateText.text = (idx > 0) ? data.createdAt.Substring(0, idx) : data.createdAt;
            }
            else
            {
                dateText.text = "";
            }
        }

        // 본문
        if (bodyText != null)
            bodyText.text = data.body ?? "";

        // 처음에는 접힌 상태
        _expanded = false;

        // ExpandablePanel 사용 시 초기화는 ExpandablePanel이 처리
        if (!_useExpandablePanel && bodyRoot != null)
        {
            bodyRoot.SetActive(false);
        }

        // 헤더 클릭 → 펼치기/접기 토글
        // ExpandablePanel이 있으면 자체 버튼 리스너를 사용하므로 여기서는 등록 안함
        if (!_useExpandablePanel && headerButton != null)
        {
            headerButton.onClick.RemoveAllListeners();
            headerButton.onClick.AddListener(ToggleExpanded);
        }

        // 카페 버튼
        if (cafeButton != null)
        {
            bool hasUrl = !string.IsNullOrEmpty(data.externalUrl);
            cafeButton.gameObject.SetActive(hasUrl);
            cafeButton.onClick.RemoveAllListeners();

            if (hasUrl)
            {
                cafeButton.onClick.AddListener(() =>
                {
                    Application.OpenURL(data.externalUrl);
                });
            }
        }
    }

    private void ToggleExpanded()
    {
        // ExpandablePanel 사용 시 ExpandablePanel.Toggle() 호출
        if (_useExpandablePanel)
        {
            expandablePanel.Toggle();
            _expanded = expandablePanel.IsExpanded;
            return;
        }

        // 기존 로직 (애니메이션 없이 즉시 토글)
        _expanded = !_expanded;

        if (bodyRoot != null)
            bodyRoot.SetActive(_expanded);

        // 레이아웃 강제 갱신
        // 1) 일단 자기(또는 layoutRoot) 먼저
        if (layoutRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }

        // 2) 그 다음 부모(Content)도 한 번 더
        var parent = layoutRoot != null ? layoutRoot.parent as RectTransform : transform.parent as RectTransform;
        if (parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
    }

    /// <summary>
    /// 현재 펼침 상태 반환
    /// </summary>
    public bool IsExpanded => _useExpandablePanel ? expandablePanel.IsExpanded : _expanded;
}
