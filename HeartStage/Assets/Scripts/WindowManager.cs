using UnityEngine;
using UnityEngine.UI;
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

    public static WindowType currentWindow { get; set; }
    private Dictionary<WindowType, GenericWindow> windows;

    private List<WindowType> activeOverlays = new List<WindowType>(); // 활성화된 오버레이 목록

    private CanvasGroup _dimCanvasGroup;
    private Image _dimImage;
    private float _dimTargetAlpha;
    private Tween _dimTween;

    private void Awake()
    {
        Instance = this;

        windows = new Dictionary<WindowType, GenericWindow>();
        foreach (var pair in windowList)
        {
            if(pair.window != null && !windows.ContainsKey(pair.windowType))
            {
                windows[pair.windowType] = pair.window;
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
            _dimImage = sharedDimmedBackground.GetComponent<Image>();
            if (_dimImage != null)
            {
                _dimTargetAlpha = _dimImage.color.a;
            }
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
        // 안전한 배열 접근
        if (!IsValidWindow(id)) return;

        // 이미 같은 타입의 오버레이가 열려있으면 열지 않음
        if (windows[id].gameObject.activeSelf)
            return;

        // 공용 딤 배경 페이드 인
        ShowDimmedBackground();

        windows[id].Open();

        // 오버레이 목록에 추가
        if (!activeOverlays.Contains(id))
        {
            activeOverlays.Add(id);
        }
    }

    public void Open(WindowType id)
    {
        if (!IsValidWindow(id))
            return;

        if (id == WindowType.LobbyHome && currentWindow == WindowType.LobbyHome
            && windows[currentWindow].gameObject.activeSelf)
            return;

        // 모든 활성화된 오버레이 닫기
        CloseAllOverlays();

        // 현재 윈도우 닫기
        if (IsValidWindow(currentWindow))
        {            
            windows[currentWindow].Close();
        }

        currentWindow = id;
        windows[currentWindow].gameObject.SetActive(true);
        windows[currentWindow].Open();
    }

    public void CloseAllOverlays()
    {
        for (int i = activeOverlays.Count - 1; i >= 0; i--)
        {
            WindowType overlayType = activeOverlays[i];
            if (IsValidWindow(overlayType) && windows[overlayType].gameObject.activeSelf)
            {
                windows[overlayType].Close();
            }
            activeOverlays.RemoveAt(i);
        }

        // 공용 딤 배경 페이드 아웃 (즉시)
        HideDimmedBackground(immediate: true);
    }

    // 오버레이를 수동으로 닫을 때 사용
    public void CloseOverlay(WindowType id)
    {
        if (!IsValidWindow(id)) return;

        windows[id].Close();
        activeOverlays.Remove(id);

        // 활성 오버레이가 없으면 공용 딤 배경 페이드 아웃
        if (activeOverlays.Count == 0)
        {
            HideDimmedBackground();
        }
    }

    #region Dim Background

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