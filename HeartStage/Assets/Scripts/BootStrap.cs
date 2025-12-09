using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.Linq;
#endif

public class BootStrap : MonoBehaviour
{
    private const string LastSceneKey = "LastSceneType"; // 나중에 enum 기반으로 쓰고 싶으면 재활용 가능
    public static bool IsInitialized = false;

    private async UniTask Start()
    {
        // 1) 공통 초기화 (Addressables / 리소스 / 데이터테이블)
        if (!IsInitialized)
        {
            // Addressables 초기화
            await Addressables.InitializeAsync();

            // 부트에서 미리 로드해둘 애셋들
            await ResourceManager.Instance.PreloadLabelAsync(AddressableLabel.Stage);
            await ResourceManager.Instance.PreloadLabelAsync("SFX");
            // await ResourceManager.Instance.PreloadLabelAsync("BGM");

            // 데이터테이블 초기화
            await DataTableManager.Initialization;

            IsInitialized = true;
        }

        // 기본 타겟은 로비
        SceneType targetScene = SceneType.LobbyScene;
        // 에디터에서 EditPlayScene으로 찾은 Addressables 주소 (또는 씬 이름)
        string targetAddress = null;

#if UNITY_EDITOR
        #region TargetSceneCheck
        // ---------------- 에디터 전용: EditPlayScene.GetLastScene() 호출 ----------------
        string lastScenePath = string.Empty;

        try
        {
            // 1) 전역 타입으로 바로 찾기
            Type type = Type.GetType("EditPlayScene");

            // 2) 못 찾으면 어셈블리 전체에서 이름 기반 검색
            if (type == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    Type t = null;
                    try
                    {
                        t = asm
                            .GetTypes()
                            .FirstOrDefault(x => x.Name == "EditPlayScene");
                    }
                    catch
                    {
                        // 일부 어셈블리는 GetTypes()에서 예외 던질 수 있으니 무시
                    }

                    if (t != null)
                    {
                        type = t;
                        break;
                    }
                }
            }

            if (type != null)
            {
                var method = type.GetMethod(
                    "GetLastScene",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)
                {
                    var result = method.Invoke(null, null) as string;
                    lastScenePath = result ?? string.Empty;
                }
                else
                {
                    Debug.LogWarning("EditPlayScene 타입은 찾았으나 GetLastScene 메서드를 찾지 못했습니다.");
                }
            }
            else
            {
                Debug.LogWarning("EditPlayScene 타입을 찾지 못했습니다. Editor 유틸이 올바르게 위치했는지 확인하세요.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"EditPlayScene.GetLastScene() 호출 중 예외 발생: {ex.Message}");
            lastScenePath = string.Empty;
        }

        if (!string.IsNullOrEmpty(lastScenePath))
        {
            // 예) "Assets/Scenes/StageScene.unity" -> "StageScene"
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(lastScenePath);

            // 부트/타이틀 같은 씬은 스킵
            if (!string.IsNullOrEmpty(sceneName) &&
                sceneName != "bootScene" &&
                sceneName != "TitleScene")
            {
                var settings = UnityEditor.AddressableAssets
                    .AddressableAssetSettingsDefaultObject.Settings;

                if (settings != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(lastScenePath);
                    var entry = settings.FindAssetEntry(guid);

                    if (entry != null && !string.IsNullOrEmpty(entry.address))
                    {
                        // Addressables에 등록된 주소 사용
                        targetAddress = entry.address;
                    }
                    else
                    {
                        // Addressables에 없으면 씬 이름을 주소처럼 사용 (직접 LoadSceneAsync에서 사용)
                        targetAddress = sceneName;
                    }
                }
                else
                {
                    // Settings 자체를 못 찾으면 씬 이름만 저장
                    targetAddress = sceneName;
                }
            }
        }
        #endregion
#else
        Application.targetFrameRate = 60;
#endif

        // 2) Firebase 로그인까지 대기
        await UniTask.WaitUntil(() =>
            AuthManager.Instance != null &&
            AuthManager.Instance.IsLoggedIn);

        // 3) 서버 시간 초기화 + 세이브 로드 / 첫 세이브 생성 + 출석 처리
        FirebaseTime.Initialize();

        await TryLoad();
        await UpdateLastLoginTime();

        // 4) 실제 씬 전환
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(targetAddress))
        {
            var handle = Addressables.LoadSceneAsync(
                targetAddress,
                UnityEngine.SceneManagement.LoadSceneMode.Single);

            await handle.Task;
            return;
        }
#endif
        if (!string.IsNullOrEmpty(targetAddress))
        {
            await SceneLoader.LoadSceneWithLoading(targetAddress);
        }
        else
        {
            await GameSceneManager.ChangeScene(targetScene);
        }
    }

    // 마지막 접속 시간 갱신 + 출석 퀘스트 처리
    private async UniTask UpdateLastLoginTime()
    {
        DateTime now = FirebaseTime.GetServerTime();
        DateTime last = SaveLoadManager.Data.LastLoginTime;

        // 날짜만 비교 (시/분/초는 무시)
        if (last.Date != now.Date)
        {
            // 일일 로그인 보상 / 출석 퀘스트 처리
            QuestManager.Instance.OnAttendance();
        }

        // 현재 시간을 마지막 접속 시간으로 저장
        SaveLoadManager.Data.lastLoginBinary = now.ToBinary();
        await SaveLoadManager.SaveToServer();

        // ★ nicknameIndex 로그인 시간 업데이트 (닉네임 설정된 유저만)
        await PublicProfileService.UpdateNicknameIndexLoginTimeAsync();
    }

    // 서버에서 세이브 로드, 없으면 기본 세이브 생성
    private async UniTask TryLoad()
    {
        bool loaded = await SaveLoadManager.LoadFromServer();

        if (!loaded)
        {
            // 기본 세이브 생성 로직
            var charTable = DataTableManager.CharacterTable;

            charTable.BuildDefaultSaveDictionaries(
                new[] { "하나", "세라", "리아" },          // 처음 해금할 캐릭터 이름
                out var unlockedByName,
                out var expById,
                out var ownedBaseIds
            );

            SaveLoadManager.Data.unlockedByName = unlockedByName;
            SaveLoadManager.Data.expById = expById;

            foreach (var id in ownedBaseIds)
                SaveLoadManager.Data.ownedIds.Add(id);

            // 처음에 드림에너지 100개 지급
            ItemInvenHelper.AddItem(ItemID.DreamEnergy, 100);

            // 첫 접속 시에도 출석 퀘스트 처리
            QuestManager.Instance.OnAttendance();

            // 첫 저장은 확실하게 await
            await SaveLoadManager.SaveToServer();
        }
    }
}
