using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("로딩 UI 프리팹")]
    [SerializeField] private LoadingUI loadingUIPrefab;

    private LoadingUI _loadingUI;

    // 🔹 다른 스크립트(Owned/Stage/SceneController)가 찍는 "목표 프로그레스"
    private float _targetProgress = 0f;

    // 🔹 실제로 로딩바에 그리는 값 (이게 서서히 _targetProgress를 따라감)
    private float _displayProgress = 0f;

    // 🔹 프로그레스바 스무스 보간 속도 (높을수록 빠름)
    [SerializeField] private float smoothSpeed = 12.0f;

    // 🔹 가짜 진행 상태
    private bool _isFakeProgressRunning = false;
    private float _fakeProgressElapsed = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingUIPrefab != null)
        {
            _loadingUI = Instantiate(loadingUIPrefab, transform);
            _loadingUI.Hide();
        }
    }

    private void Update()
    {
        if (_loadingUI == null)
            return;

        // 🔹 가짜 진행 업데이트 (100% 도달 전까지 계속)
        if (_isFakeProgressRunning && _targetProgress < 1f)
        {
            _fakeProgressElapsed += Time.unscaledDeltaTime;

            const float fastPhaseEnd = 6f;
            const float fastMax = 0.95f;
            const float slowSpeed = 0.01f;
            const float absoluteMax = 0.99f;

            float fakeValue;
            if (_fakeProgressElapsed < fastPhaseEnd)
            {
                // 🔹 Ease-in 커브: t² (처음엔 느리게, 점점 빨라짐)
                float t = _fakeProgressElapsed / fastPhaseEnd; // 0~1 정규화
                fakeValue = t * t * fastMax; // 제곱 커브
            }
            else
            {
                fakeValue = Mathf.Min(fastMax + (_fakeProgressElapsed - fastPhaseEnd) * slowSpeed, absoluteMax);
            }

            // 가짜 진행 값을 목표로 설정 (Max 처리로 뒤로 안 감)
            _targetProgress = Mathf.Max(_targetProgress, fakeValue);
        }

        if (_displayProgress < _targetProgress)
        {
            // 🔹 Lerp 기반 감쇠로 부드러운 보간
            _displayProgress = Mathf.Lerp(
                _displayProgress,
                _targetProgress,
                smoothSpeed * Time.unscaledDeltaTime
            );

            // 목표에 거의 도달하면 스냅
            if (_targetProgress - _displayProgress < 0.001f)
                _displayProgress = _targetProgress;

            _loadingUI.SetProgress(_displayProgress);
        }
    }

    /// <summary>
    /// Addressables 주소 기반으로 씬 로딩 + 로딩 패널 표시.
    /// 예: await SceneLoader.LoadSceneWithLoading("StageScene", LoadSceneMode.Single);
    /// </summary>
    public static UniTask LoadSceneWithLoading(string address, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (Instance == null)
        {
            Debug.LogError("[SceneLoader] Instance 없음. 부트스트랩 씬에 SceneLoader 배치 필요.");
            return UniTask.CompletedTask;
        }

        return Instance.InternalLoadScene(address, mode);
    }

    private async UniTask InternalLoadScene(string address, LoadSceneMode mode)
    {
        // 🔹 가짜 진행은 Update()에서 처리 (ShowLoading에서 시작됨)

        // 씬 로딩 수행
        var handle = Addressables.LoadSceneAsync(address, mode, activateOnLoad: false);

        while (!handle.IsDone)
            await UniTask.Yield();

        var sceneInstance = handle.Result;
        var activateOp = sceneInstance.ActivateAsync();

        while (!activateOp.isDone)
            await UniTask.Yield();

        // 🔹 씬 로드 완료 후 각 Controller가 준비 완료 시 100% 설정
    }

    /// <summary>
    /// 로딩 UI 즉시 표시 (버튼 클릭 시 바로 호출)
    /// </summary>
    public static void ShowLoading()
    {
        if (Instance == null || Instance._loadingUI == null)
            return;

        Instance._targetProgress = 0f;
        Instance._displayProgress = 0f;
        Instance._fakeProgressElapsed = 0f;
        Instance._isFakeProgressRunning = true;
        Instance._loadingUI.SetProgress(0f);
        Instance._loadingUI.Show();
    }

    public static void HideLoading()
    {
        if (Instance == null || Instance._loadingUI == null)
            return;

        Instance._isFakeProgressRunning = false; // 🔹 가짜 진행 중지
        Instance._loadingUI.Hide();
    }

    public static async UniTask HideLoadingWithDelay(int ms = 300)
    {
        if (Instance == null || Instance._loadingUI == null)
            return;

        await UniTask.Delay(ms);
        Instance._loadingUI.Hide();
    }

    // 🔹 내부에서 목표 프로그레스만 갱신 (바로 UI를 건드리지 않음)
    private void SetProgressInternal(float value01)
    {
        if (_loadingUI == null)
            return;

        // 0~1 클램프 + "지금까지 목표 값보다 작아지지 않도록" 보장
        float clamped = Mathf.Clamp01(value01);
        _targetProgress = Mathf.Max(_targetProgress, clamped);

        // 🔹 100%에 도달하면 가짜 진행 중지
        if (clamped >= 1f)
            _isFakeProgressRunning = false;
    }

    // 🔹 외부(다른 스크립트)에서 불러쓰는 함수
    public static void SetProgressExternal(float value01)
    {
        if (Instance == null)
            return;

        Instance.SetProgressInternal(value01);
    }
}
