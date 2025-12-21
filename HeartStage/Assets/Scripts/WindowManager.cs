using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;

[System.Serializable]
public class WindowPair
{
    public WindowType windowType;
    public GenericWindow window;
}
public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance;

    [Header("Reference")]
    public List<WindowPair> windowList;

    [Header("공용 딤 배경")]
    [SerializeField] private GameObject sharedDimmedBackground;
    [SerializeField] private float dimFadeDuration = 0.2f;

    [Header("다이얼로그")]
    [SerializeField] private ConfirmDialog confirmDialog;

    public static WindowType currentWindow { get; set; }
    private Dictionary<WindowType, GenericWindow> windows;

    private List<WindowType> activeOverlays = new List<WindowType>(); // 활성화된 오버레이 목록
    private HashSet<WindowType> dimmedOverlays = new HashSet<WindowType>(); // 딤 배경과 함께 열린 오버레이

    /// <summary>
    /// 딤 클릭으로 모든 오버레이가 닫힐 때 발생하는 이벤트
    /// 수동으로 딤을 사용하는 팝업들이 구독하여 함께 닫히도록 함
    /// </summary>
    public event Action OnDimClicked;

    private CanvasGroup _dimCanvasGroup;
    private Image _dimImage;
    private float _dimTargetAlpha;
    private Tween _dimTween;

    // 하단 네비게이션 슬라이드 애니메이션
    private static readonly Dictionary<WindowType, int> _navIndex = new Dictionary<WindowType, int>
    {
        { WindowType.Shopping, 0 },      // 상점
        { WindowType.Gacha, 1 },         // 뽑기
        { WindowType.LobbyHome, 2 },     // 숙소
        { WindowType.CharacterDict, 3 }, // 도감
        { WindowType.StageSelect, 4 },   // 전투
        { WindowType.SpecialDungeon, 5 } // 던전
    };
    private const float SlideDuration = 0.25f;
    private bool _isTransitioning = false;
    private Tween _slideTween;
    private GenericWindow _slidePrevWindow; // 슬라이드 중 이전 윈도우 추적

    private void Awake()
    {
        Instance = this;

        windows = new Dictionary<WindowType, GenericWindow>();
        foreach (var pair in windowList)
        {
            if(pair.window != null && !windows.ContainsKey(pair.windowType))
            {
                windows[pair.windowType] = pair.window;
                pair.window.Init(this, pair.windowType);

                // 시작 시 currentWindow가 아닌 모든 윈도우 비활성화 (에디터에서 실수로 켜진 경우 대비)
                if (pair.windowType != currentWindow)
                {
                    pair.window.gameObject.SetActive(false);
                }
            }
        }

        // 딤 배경 CanvasGroup 초기화
        if (sharedDimmedBackground != null)
        {
            _dimCanvasGroup = sharedDimmedBackground.GetComponent<CanvasGroup>();
            if (_dimCanvasGroup == null)
            {
                _dimCanvasGroup = sharedDimmedBackground.AddComponent<CanvasGroup>();
            }
            // 딤 배경이 항상 레이캐스트를 차단하도록 설정
            _dimCanvasGroup.blocksRaycasts = true;
            _dimCanvasGroup.interactable = true;
            _dimImage = sharedDimmedBackground.GetComponent<Image>();
            if (_dimImage != null)
            {
                _dimTargetAlpha = _dimImage.color.a;
            }

            // 딤 배경 클릭 시 모든 오버레이 닫기
            var dimButton = sharedDimmedBackground.GetComponent<Button>();
            if (dimButton == null)
            {
                dimButton = sharedDimmedBackground.AddComponent<Button>();
                // 버튼 시각적 효과 제거 (색상 변화 없이 투명하게)
                dimButton.transition = Selectable.Transition.None;
            }
            dimButton.onClick.RemoveAllListeners();
            dimButton.onClick.AddListener(CloseAllOverlays);
        }

        // ConfirmDialog 초기화 (비활성 상태여도 초기화)
        if (confirmDialog != null)
        {
            confirmDialog.Initialize();
        }
    }

    private void OnEnable()
    {
        if(currentWindow != WindowType.None)
        {
            Open(currentWindow);
        }
    }

    public void OpenOverlay(WindowType id)
    {
        OpenOverlayInternal(id, showDim: true);
    }

    /// <summary>
    /// 딤 배경 없이 오버레이 열기 (Middle 영역 윈도우들 간 전환용)
    /// </summary>
    public void OpenOverlayNoDim(WindowType id)
    {
        OpenOverlayInternal(id, showDim: false);
    }

    private void OpenOverlayInternal(WindowType id, bool showDim)
    {
        // 안전한 배열 접근
        if (!IsValidWindow(id))
        {
            Debug.LogWarning($"[WindowManager] Invalid window: {id}");
            return;
        }

        // 이미 같은 타입의 오버레이가 열려있으면 열지 않음
        if (windows[id].gameObject.activeSelf)
        {
            // activeOverlays에 있으면 정상적으로 열린 상태 - 무시
            if (activeOverlays.Contains(id))
                return;
            // activeOverlays에 없으면 비정상 상태 - 먼저 끄고 다시 열기
            windows[id].gameObject.SetActive(false);
        }

        // 모든 오버레이는 isOverlayWindow = true로 설정 (activeOverlays 정리를 위해)
        // 딤 표시 여부는 showDim 파라미터로 따로 처리
        windows[id].SetAsOverlay(true);

        // 공용 딤 배경 페이드 인 (showDim이 true일 때만)
        if (showDim)
        {
            dimmedOverlays.Add(id);
            ShowDimmedBackground();
        }

        windows[id].Open();

        // 오버레이를 맨 앞으로 이동 (다른 창 위에 표시)
        windows[id].transform.SetAsLastSibling();

        // 오버레이 목록에 추가 (딤 여부 상관없이 모든 오버레이 추적)
        if (!activeOverlays.Contains(id))
        {
            activeOverlays.Add(id);
        }
    }

    public bool Open(WindowType id)
    {
        if (!IsValidWindow(id))
            return false;

        // 같은 윈도우를 열려고 하고 이미 활성화되어 있으면 무시
        if (id == currentWindow && windows[id].gameObject.activeSelf)
            return false;

        // 애니메이션 중이면 무시
        if (_isTransitioning)
        {
            Debug.LogWarning($"[WindowManager] Blocked by isTransitioning: {id}");
            return false;
        }

        // 모든 활성화된 오버레이 닫기 (즉시)
        CloseAllOverlaysImmediate();

        // 같은 윈도우인데 비활성화 상태면 직접 활성화 (씬 전환 후 복귀 시)
        if (id == currentWindow)
        {
            windows[currentWindow].gameObject.SetActive(true);
            windows[currentWindow].Open();
            return true;
        }

        // 네비게이션 버튼인 경우 슬라이드 애니메이션
        if (_navIndex.ContainsKey(id) && _navIndex.ContainsKey(currentWindow))
        {
            OpenWithSlide(id);
            return true;
        }

        // 일반 전환 (슬라이드 없음)
        if (IsValidWindow(currentWindow))
        {
            windows[currentWindow].gameObject.SetActive(false);
        }

        currentWindow = id;
        windows[currentWindow].gameObject.SetActive(true);
        windows[currentWindow].Open();
        return true;
    }

    private void OpenWithSlide(WindowType id)
    {
        // 기존 트윈 강제 종료 및 정리
        if (_slideTween != null && _slideTween.IsActive())
        {
            // 이전 윈도우 정리 (OnComplete가 실행되지 않으므로 여기서 처리)
            if (_slidePrevWindow != null)
            {
                _slidePrevWindow.gameObject.SetActive(false);
                _slidePrevWindow = null;
            }
            _slideTween.Kill(false);
        }
        _isTransitioning = true;

        int fromIndex = _navIndex[currentWindow];
        int toIndex = _navIndex[id];
        bool slideFromRight = toIndex > fromIndex;

        var prevWindow = windows[currentWindow];
        var nextWindow = windows[id];
        var nextRect = nextWindow.GetComponent<RectTransform>();

        // 이전 윈도우 추적 (연속 클릭 시 정리용)
        _slidePrevWindow = prevWindow;

        // Canvas 기준 너비 (부모 RectTransform 사용)
        float canvasWidth = nextRect.parent != null
            ? ((RectTransform)nextRect.parent).rect.width
            : 1920f;

        // 원래 offset 저장 (Stretch 앵커용)
        Vector2 originalOffsetMin = nextRect.offsetMin;
        Vector2 originalOffsetMax = nextRect.offsetMax;

        // 시작 위치 설정 (offsetMin/offsetMax를 동시에 이동)
        float startOffset = slideFromRight ? canvasWidth : -canvasWidth;
        nextRect.offsetMin = new Vector2(originalOffsetMin.x + startOffset, originalOffsetMin.y);
        nextRect.offsetMax = new Vector2(originalOffsetMax.x + startOffset, originalOffsetMax.y);
        nextWindow.gameObject.SetActive(true);
        nextRect.SetAsLastSibling(); // 새 창을 맨 앞으로 (이전 창 위에 표시)
        nextWindow.Open(); // Open() 호출해야 초기화됨

        // 슬라이드 애니메이션 (offset을 원래 위치로)
        _slideTween = DOVirtual.Float(startOffset, 0f, SlideDuration, value =>
        {
            nextRect.offsetMin = new Vector2(originalOffsetMin.x + value, originalOffsetMin.y);
            nextRect.offsetMax = new Vector2(originalOffsetMax.x + value, originalOffsetMax.y);
        })
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                // 원래 offset으로 확실히 복원
                nextRect.offsetMin = originalOffsetMin;
                nextRect.offsetMax = originalOffsetMax;
                // 이전 창 끄기
                if (_slidePrevWindow != null)
                {
                    _slidePrevWindow.gameObject.SetActive(false);
                    _slidePrevWindow = null;
                }
                _isTransitioning = false;
            })
            .OnKill(() =>
            {
                _isTransitioning = false;
                // OnKill에서는 _slidePrevWindow 정리하지 않음 (다음 OpenWithSlide에서 처리)
            });

        currentWindow = id;
    }

    public void CloseAllOverlays()
    {
        // 수동 딤 사용 팝업들에게 닫히라고 알림
        OnDimClicked?.Invoke();
        _manualDimRefCount = 0; // 수동 딤 카운터 리셋

        // 리스트 복사 후 순회 (Close()가 NotifyOverlayClosed()를 호출하여 activeOverlays를 수정하기 때문)
        var overlaysToClose = new List<WindowType>(activeOverlays);
        foreach (var overlayType in overlaysToClose)
        {
            if (IsValidWindow(overlayType) && windows[overlayType].gameObject.activeSelf)
            {
                windows[overlayType].Close();
            }
        }
        // NotifyOverlayClosed에서 이미 제거되므로 여기서 Clear 불필요하지만 안전하게 정리
        activeOverlays.Clear();
        dimmedOverlays.Clear();

        // 공용 딤 배경 페이드 아웃 (즉시)
        HideDimmedBackground(immediate: true);
    }

    /// <summary>
    /// 모든 오버레이를 애니메이션 없이 즉시 닫기 (화면 전환 시 사용)
    /// </summary>
    private void CloseAllOverlaysImmediate()
    {
        for (int i = activeOverlays.Count - 1; i >= 0; i--)
        {
            WindowType overlayType = activeOverlays[i];
            if (IsValidWindow(overlayType))
            {
                // 애니메이션 없이 즉시 비활성화
                windows[overlayType].gameObject.SetActive(false);
            }
            activeOverlays.RemoveAt(i);
        }
        dimmedOverlays.Clear();

        // 공용 딤 배경도 즉시 숨기기
        HideDimmedBackground(immediate: true);
    }

    // 오버레이를 수동으로 닫을 때 사용
    public void CloseOverlay(WindowType id)
    {
        if (!IsValidWindow(id)) return;

        windows[id].Close();
        activeOverlays.Remove(id);
        dimmedOverlays.Remove(id);

        // 딤 배경과 함께 열린 오버레이가 없으면 딤 숨기기
        if (dimmedOverlays.Count == 0)
        {
            HideDimmedBackground();
        }
    }

    // GenericWindow.Close()에서 호출 - 딤 배경만 처리 (Close는 이미 호출됨)
    public void NotifyOverlayClosed(WindowType id)
    {
        activeOverlays.Remove(id);
        dimmedOverlays.Remove(id);

        // 딤 배경과 함께 열린 오버레이가 없으면 딤 숨기기
        if (dimmedOverlays.Count == 0)
        {
            HideDimmedBackground();
        }
    }

    #region Dim Background

    private int _manualDimRefCount = 0; // 수동 딤 표시 참조 카운터

    /// <summary>
    /// 외부에서 수동으로 딤 배경 표시 (참조 카운터 방식)
    /// </summary>
    public void ShowDimManual()
    {
        _manualDimRefCount++;
        if (_manualDimRefCount == 1)
        {
            ShowDimmedBackground();
        }
    }

    /// <summary>
    /// 외부에서 수동으로 딤 배경 숨기기 (참조 카운터 방식)
    /// </summary>
    public void HideDimManual()
    {
        _manualDimRefCount = Mathf.Max(0, _manualDimRefCount - 1);
        if (_manualDimRefCount == 0 && dimmedOverlays.Count == 0)
        {
            HideDimmedBackground();
        }
    }

    private void ShowDimmedBackground()
    {
        if (sharedDimmedBackground == null || _dimCanvasGroup == null) return;

        _dimTween?.Kill();
        _dimCanvasGroup.alpha = 0f;
        sharedDimmedBackground.SetActive(true);

        _dimTween = _dimCanvasGroup.DOFade(1f, dimFadeDuration)
            .SetEase(Ease.OutQuad);
    }

    private void HideDimmedBackground(bool immediate = false)
    {
        if (sharedDimmedBackground == null || _dimCanvasGroup == null) return;

        _dimTween?.Kill();

        if (immediate)
        {
            _dimCanvasGroup.alpha = 0f;
            sharedDimmedBackground.SetActive(false);
        }
        else
        {
            _dimTween = _dimCanvasGroup.DOFade(0f, dimFadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => sharedDimmedBackground.SetActive(false));
        }
    }

    #endregion

    private bool IsValidWindow(WindowType windowType)
    {
        return windowType != WindowType.None && windows.ContainsKey(windowType) && windows[windowType] != null;
    }
}