using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SceneEntry
{
    public SceneType sceneType;
    public string address;   // Addressables 씬 주소
}

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("씬 타입 ↔ Addressables 주소 매핑")]
    [SerializeField] private List<SceneEntry> sceneEntries = new List<SceneEntry>();

    private Dictionary<SceneType, string> _sceneMap;
    public SceneType CurrentSceneType { get; private set; } = SceneType.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sceneMap = new Dictionary<SceneType, string>();
        foreach (var e in sceneEntries)
        {
            if (e == null) continue;
            if (e.sceneType == SceneType.None) continue;
            if (string.IsNullOrEmpty(e.address)) continue;

            if (!_sceneMap.ContainsKey(e.sceneType))
            {
                _sceneMap.Add(e.sceneType, e.address);
            }
        }
    }

    public static UniTask ChangeScene(SceneType sceneType, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (Instance == null)
        {
            Debug.LogError("[GameSceneManager] Instance 없음. 부트스트랩 씬에 배치 필요.");
            return UniTask.CompletedTask;
        }

        // 🔹 로딩 UI 즉시 표시 (버튼 클릭 즉시 반응)
        SceneLoader.ShowLoading();

        if (!Instance._sceneMap.TryGetValue(sceneType, out var address))
        {
            Debug.LogError($"[GameSceneManager] {sceneType} 에 해당하는 씬 주소가 설정되지 않음.");
            SceneLoader.HideLoading();
            return UniTask.CompletedTask;
        }

        Instance.CurrentSceneType = sceneType;
        return SceneLoader.LoadSceneWithLoading(address);
    }

    /// <summary>
    /// 새 씬에서 준비가 끝났음을 알려줄 때 사용.
    /// (필요하면 type 체크해서 잘못된 호출 방지도 가능)
    /// </summary>
    public static void NotifySceneReady(SceneType sceneType, int hideDelayMs = 100)
    {
        if (Instance == null)
            return;

        // 씬 전환 도중에 이전 씬에서 잘못 불리는 거 방지용 체크
        if (sceneType != Instance.CurrentSceneType)
        {
            Debug.LogWarning($"[GameSceneManager] {sceneType} 에서 Ready 통보 왔지만, 현재 타겟은 {Instance.CurrentSceneType}.");
            // 그래도 로딩 끄고 싶으면 아래 줄 유지 / 막고 싶으면 return;
            // return;
        }

        Instance.HideLoadingInternal(hideDelayMs).Forget();
    }

    private async UniTask HideLoadingInternal(int ms)
    {
        if (ms > 0)
            await UniTask.Delay(ms, DelayType.UnscaledDeltaTime);

        SceneLoader.HideLoading();
    }
}
