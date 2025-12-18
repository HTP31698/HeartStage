using UnityEngine;
using System.Collections.Generic;

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

    public static WindowType currentWindow { get; set; }
    private Dictionary<WindowType, GenericWindow> windows;

    private List<WindowType> activeOverlays = new List<WindowType>(); // 활성화된 오버레이 목록

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
    }

    // 오버레이를 수동으로 닫을 때 사용
    public void CloseOverlay(WindowType id)
    {
        if (!IsValidWindow(id)) return;

        windows[id].Close();
        activeOverlays.Remove(id);
    }

    private bool IsValidWindow(WindowType windowType)
    {
        return windowType != WindowType.None && windows.ContainsKey(windowType) && windows[windowType] != null;
    }
}