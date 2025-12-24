using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private readonly Dictionary<string, Object> _assetCache = new Dictionary<string, Object>();
    private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    // 해당 Addressable Label이 할당된 에셋들을 모두 로드, bootStrap에서 호출
    public async UniTask PreloadLabelAsync(string label)
    {
        // 의존성 다운로드 (PAD 대응)
        var downloadHandle = Addressables.DownloadDependenciesAsync(label);
        await downloadHandle.Task;

        var handle = Addressables.LoadAssetsAsync<Object>(label, asset =>
        {
            if (!_assetCache.ContainsKey(asset.name))
            {
                _assetCache[asset.name] = asset;
            }
        });

        _handles.Add(handle);
        await handle.Task;
        Debug.Log($"[ResourceManager] Label {label} 로드 완료 ({_assetCache.Count}개 캐싱됨)");
    }

    public T Get<T>(string assetName) where T : Object
    {
        if (_assetCache.TryGetValue(assetName, out var asset))
            return asset as T;

        return null;
    }

    // 로드된 리소스들 해제
    public void ReleaseAll()
    {
        foreach (var handle in _handles)
            Addressables.Release(handle);

        _handles.Clear();
        _assetCache.Clear();
    }

    //에셋 목록 출력 
    public void LogCachedAssets() 
    {
        foreach (var kvp in _assetCache)
        {
            Debug.Log($"[ResourceManager] - {kvp.Key} ({kvp.Value?.GetType().Name})");
        }
    }

    public Sprite GetSprite(string key)
    {
        if (!_assetCache.TryGetValue(key, out var obj) || obj == null)
            return null;

        // 이미 Sprite면 바로 리턴
        if (obj is Sprite s)
            return s;

        // Texture2D면 여기서 Sprite로 생성해서 캐시에 갈아끼우기
        if (obj is Texture2D tex)
        {
            var spr = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f, // pixelsPerUnit 적당한 값
                0,
                SpriteMeshType.Tight
            );

            _assetCache[key] = spr;   // 다음부터는 Sprite로 바로 꺼낼 수 있게 교체
            return spr;
        }

        return null;
    }
}
