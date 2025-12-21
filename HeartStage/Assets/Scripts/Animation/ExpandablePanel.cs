using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections.Generic;

/// <summary>
/// 토글 방식 확장 패널 (아코디언)
/// 헤더 클릭 시 콘텐츠가 슬라이드 다운/업되고 아래 항목들이 밀려남
///
/// 사용법:
/// 1. VerticalLayoutGroup이 있는 부모 아래에 배치
/// 2. headerButton에 클릭할 버튼 할당
/// 3. contentRect에 펼쳐질 영역 할당
/// 4. ContentSizeFitter + VerticalLayoutGroup을 contentRect에 설정 (동적 콘텐츠용)
/// </summary>
public class ExpandablePanel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("클릭하면 토글되는 헤더 버튼")]
    [SerializeField] private Button headerButton;

    [Tooltip("펼쳐지는 콘텐츠 영역")]
    [SerializeField] private RectTransform contentRect;

    [Tooltip("콘텐츠의 CanvasGroup (페이드용, 없으면 자동 생성)")]
    [SerializeField] private CanvasGroup contentCanvasGroup;

    [Tooltip("화살표 아이콘 (회전 애니메이션용, 선택사항)")]
    [SerializeField] private RectTransform arrowIcon;

    [Header("Animation Settings")]
    [SerializeField] private float expandDuration = 0.25f;
    [SerializeField] private float collapseDuration = 0.2f;
    [SerializeField] private Ease expandEase = Ease.OutCubic;
    [SerializeField] private Ease collapseEase = Ease.InCubic;
    [SerializeField] private float arrowRotationExpanded = 180f;
    [SerializeField] private float arrowRotationCollapsed = 0f;

    [Header("Options")]
    [Tooltip("시작 시 펼쳐진 상태로 시작")]
    [SerializeField] private bool startExpanded = false;

    [Tooltip("펼칠 때 페이드 효과 사용")]
    [SerializeField] private bool useFade = true;

    [Tooltip("LayoutElement.preferredHeight 사용 (VerticalLayoutGroup 호환)")]
    [SerializeField] private bool usePreferredHeight = true;

    [Tooltip("그룹 ID (같은 그룹 내에서 하나만 펼침, 0이면 그룹 없음)")]
    [SerializeField] private int accordionGroupId = 0;

    // 상태
    private bool _isExpanded;
    private float _expandedHeight;
    private float _headerHeight;
    private LayoutElement _layoutElement;
    private Tween _currentTween;
    private RectTransform _selfRect;

    // 아코디언 그룹 관리 (같은 그룹 내에서 하나만 펼침)
    private static Dictionary<int, List<ExpandablePanel>> _accordionGroups = new();
    private bool _isInitialized = false;

    // 이벤트
    public event Action<bool> OnExpandChanged;

    public bool IsExpanded => _isExpanded;
    public RectTransform ContentRect => contentRect;

    private void Awake()
    {
        _selfRect = transform as RectTransform;
        _layoutElement = GetComponent<LayoutElement>();

        // LayoutElement가 없으면 추가
        if (_layoutElement == null && usePreferredHeight)
        {
            _layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        // contentRect가 없으면 Start에서 다시 시도 (다른 컴포넌트가 설정할 수 있음)
        if (contentRect != null)
        {
            SetupCanvasGroup();
        }

        // 헤더 버튼 이벤트 연결
        if (headerButton != null)
        {
            headerButton.onClick.AddListener(Toggle);
        }

        // 아코디언 그룹에 등록
        RegisterToGroup();
    }

    /// <summary>
    /// CanvasGroup 설정 (contentRect가 있을 때만)
    /// </summary>
    private void SetupCanvasGroup()
    {
        if (contentRect == null) return;

        // CanvasGroup 확보
        if (contentCanvasGroup == null && useFade)
        {
            contentCanvasGroup = contentRect.GetComponent<CanvasGroup>();
            if (contentCanvasGroup == null)
            {
                contentCanvasGroup = contentRect.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 초기화 (동적 생성 시 수동 호출 가능)
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        if (contentRect == null)
        {
            Debug.LogWarning("[ExpandablePanel] contentRect가 할당되지 않았습니다. 패널이 작동하지 않습니다.");
            return;
        }

        // Awake에서 못한 경우 여기서 CanvasGroup 설정
        SetupCanvasGroup();

        // 헤더 높이 계산 (자신의 높이 - 콘텐츠 높이)
        LayoutRebuilder.ForceRebuildLayoutImmediate(_selfRect);

        // 콘텐츠 높이 측정을 위해 임시 활성화
        bool wasActive = contentRect.gameObject.activeSelf;
        contentRect.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        _expandedHeight = contentRect.rect.height;

        // 헤더 높이 = 전체 높이 - 콘텐츠 높이 (콘텐츠가 활성화된 상태에서)
        _headerHeight = _selfRect.rect.height - _expandedHeight;
        if (_headerHeight < 0) _headerHeight = 0;

        if (!wasActive)
            contentRect.gameObject.SetActive(false);

        // 초기 상태 설정 (애니메이션 없이)
        SetExpandedImmediate(startExpanded);
    }

    private void OnDestroy()
    {
        _currentTween?.Kill();

        if (headerButton != null)
        {
            headerButton.onClick.RemoveListener(Toggle);
        }

        // 아코디언 그룹에서 제거
        UnregisterFromGroup();
    }

    private void RegisterToGroup()
    {
        if (accordionGroupId <= 0) return;

        if (!_accordionGroups.ContainsKey(accordionGroupId))
        {
            _accordionGroups[accordionGroupId] = new List<ExpandablePanel>();
        }
        _accordionGroups[accordionGroupId].Add(this);
    }

    private void UnregisterFromGroup()
    {
        if (accordionGroupId <= 0) return;

        if (_accordionGroups.ContainsKey(accordionGroupId))
        {
            _accordionGroups[accordionGroupId].Remove(this);
            if (_accordionGroups[accordionGroupId].Count == 0)
            {
                _accordionGroups.Remove(accordionGroupId);
            }
        }
    }

    private void CollapseOthersInGroup()
    {
        if (accordionGroupId <= 0) return;

        if (_accordionGroups.ContainsKey(accordionGroupId))
        {
            foreach (var panel in _accordionGroups[accordionGroupId])
            {
                if (panel != this && panel._isExpanded)
                {
                    panel.Collapse();
                }
            }
        }
    }

    /// <summary>
    /// 토글 (현재 상태 반전)
    /// </summary>
    public void Toggle()
    {
        if (_isExpanded)
            Collapse();
        else
            Expand();
    }

    /// <summary>
    /// 펼치기
    /// </summary>
    public void Expand()
    {
        if (_isExpanded) return;

        // 같은 그룹의 다른 패널 접기
        CollapseOthersInGroup();

        _currentTween?.Kill();
        _isExpanded = true;

        // 콘텐츠 활성화
        contentRect.gameObject.SetActive(true);

        // 현재 높이에서 목표 높이로 애니메이션
        float startHeight = 0f;
        float targetHeight = _expandedHeight;

        var sequence = DOTween.Sequence();

        // 높이 애니메이션
        sequence.Join(DOVirtual.Float(startHeight, targetHeight, expandDuration, value =>
        {
            SetContentHeight(value);
        }).SetEase(expandEase));

        // 페이드 인
        if (useFade && contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = 0f;
            sequence.Join(contentCanvasGroup.DOFade(1f, expandDuration).SetEase(Ease.OutQuad));
        }

        // 화살표 회전
        if (arrowIcon != null)
        {
            sequence.Join(arrowIcon.DORotate(new Vector3(0, 0, arrowRotationExpanded), expandDuration).SetEase(expandEase));
        }

        sequence.OnComplete(() =>
        {
            OnExpandChanged?.Invoke(true);
        });

        _currentTween = sequence;
    }

    /// <summary>
    /// 접기
    /// </summary>
    public void Collapse()
    {
        if (!_isExpanded) return;

        _currentTween?.Kill();
        _isExpanded = false;

        float startHeight = _expandedHeight;
        float targetHeight = 0f;

        var sequence = DOTween.Sequence();

        // 높이 애니메이션
        sequence.Join(DOVirtual.Float(startHeight, targetHeight, collapseDuration, value =>
        {
            SetContentHeight(value);
        }).SetEase(collapseEase));

        // 페이드 아웃
        if (useFade && contentCanvasGroup != null)
        {
            sequence.Join(contentCanvasGroup.DOFade(0f, collapseDuration).SetEase(Ease.InQuad));
        }

        // 화살표 회전
        if (arrowIcon != null)
        {
            sequence.Join(arrowIcon.DORotate(new Vector3(0, 0, arrowRotationCollapsed), collapseDuration).SetEase(collapseEase));
        }

        sequence.OnComplete(() =>
        {
            contentRect.gameObject.SetActive(false);
            OnExpandChanged?.Invoke(false);
        });

        _currentTween = sequence;
    }

    /// <summary>
    /// 애니메이션 없이 즉시 상태 설정
    /// </summary>
    public void SetExpandedImmediate(bool expanded)
    {
        _currentTween?.Kill();
        _isExpanded = expanded;

        if (expanded)
        {
            contentRect.gameObject.SetActive(true);
            SetContentHeight(_expandedHeight);
            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = 1f;
            if (arrowIcon != null)
                arrowIcon.localRotation = Quaternion.Euler(0, 0, arrowRotationExpanded);
        }
        else
        {
            SetContentHeight(0f);
            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = 0f;
            if (arrowIcon != null)
                arrowIcon.localRotation = Quaternion.Euler(0, 0, arrowRotationCollapsed);
            contentRect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 콘텐츠 높이 변경 후 레이아웃 업데이트
    /// </summary>
    private void SetContentHeight(float height)
    {
        // 콘텐츠 높이 설정
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        // LayoutElement preferredHeight 사용 시 (VerticalLayoutGroup 호환)
        if (usePreferredHeight && _layoutElement != null)
        {
            _layoutElement.preferredHeight = _headerHeight + height;
        }

        // 부모 LayoutGroup이 있으면 강제 업데이트
        var parentRect = transform.parent as RectTransform;
        if (parentRect != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }
    }

    /// <summary>
    /// 콘텐츠 높이가 변경되었을 때 호출 (동적 콘텐츠용)
    /// </summary>
    public void RefreshExpandedHeight()
    {
        if (contentRect == null) return;

        // 임시로 활성화해서 높이 측정
        bool wasActive = contentRect.gameObject.activeSelf;
        contentRect.gameObject.SetActive(true);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        _expandedHeight = contentRect.rect.height;

        // 현재 펼쳐진 상태면 높이 업데이트
        if (_isExpanded)
        {
            SetContentHeight(_expandedHeight);
        }

        if (!wasActive && !_isExpanded)
        {
            contentRect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 외부에서 헤더 버튼 설정
    /// </summary>
    public void SetHeaderButton(Button button)
    {
        if (headerButton != null)
        {
            headerButton.onClick.RemoveListener(Toggle);
        }

        headerButton = button;

        if (headerButton != null)
        {
            headerButton.onClick.AddListener(Toggle);
        }
    }

    /// <summary>
    /// 외부에서 콘텐츠 영역 설정
    /// </summary>
    public void SetContentRect(RectTransform rect)
    {
        contentRect = rect;
        _isInitialized = false;
        Initialize();
    }

    /// <summary>
    /// 아코디언 그룹 ID 설정 (런타임에서 동적 그룹화용)
    /// </summary>
    public void SetAccordionGroup(int groupId)
    {
        UnregisterFromGroup();
        accordionGroupId = groupId;
        RegisterToGroup();
    }

    /// <summary>
    /// 현재 확장된 높이 반환
    /// </summary>
    public float GetExpandedHeight() => _expandedHeight;

    /// <summary>
    /// 헤더 높이 반환
    /// </summary>
    public float GetHeaderHeight() => _headerHeight;
}
