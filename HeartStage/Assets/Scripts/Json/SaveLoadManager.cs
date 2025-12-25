using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using SaveDataVC = SaveDataV1;

public class SaveLoadManager
{
    public static int SaveDataVersion { get; } = 1;

    public static SaveDataVC Data { get; set; } = new SaveDataVC();

    static SaveLoadManager()
    {
        //Load();
    }

    private static readonly string[] SaveFilename =
    {
        "SaveAuto.json",
        "Save1.json",
        "Save2.json",
        "Save3.json",
    };

    public static string SaveDirectory => $"{Application.persistentDataPath}/Save";

    private static JsonSerializerSettings settings = new JsonSerializerSettings()
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.All
    };

    // 서버에서 로드, BootScene에서 하고 있음 다른 데서 쓰지 말기
    public static async UniTask<bool> LoadFromServer()
    {
        string userId = AuthManager.Instance.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("LoadFromServer: userId 없음");
            return false;
        }

        string json = await CloudSaveManager.Instance.LoadAsync(userId);

        if (json == null)
        {
            Debug.Log("LoadFromServer: 서버에 데이터 없음");
            return false;
        }

        Data = JsonConvert.DeserializeObject<SaveDataV1>(json);
        return true;
    }

    // 서버에 저장 SaveLoadManager.SaveToServer().Forget()으로 쓰기
    // 확실히 저장 해야할때만 await SaveToServer()하기 ex) 앱 종료시?
    public static async UniTask SaveToServer()
    {
        string userId = AuthManager.Instance.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("SaveToServer: userId 없음, 저장 불가");
            return;
        }

        string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
        await CloudSaveManager.Instance.SaveAsync(userId, json);
    }

    // 친구의 데이터 변경 후 저장용
    public static async UniTask SaveFriendDataToServer(string userId, SaveDataV1 data)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("SaveToServer: userId 없음");
            return;
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await CloudSaveManager.Instance.SaveFriendDataAsync(userId, json);
    }

    // 로컬에만 저장
    public static bool Save(int slot = 0)
    {
        if (Data == null || slot < 0 || slot > SaveFilename.Length)
            return false;

        try
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }

            var path = Path.Combine(SaveDirectory, SaveFilename[slot]);
            var json = JsonConvert.SerializeObject(Data, settings);
            File.WriteAllText(path, json);

            return true;
        }
        catch
        {
            Debug.Log("Save 예외 발생");
            return false;
        }
    }

    // 데이터 리셋
    public static void ResetData()
    {
        Data = new SaveDataV1();
    }

    //캐릭터 획득 처리

    public static void ReplaceOwnedId(int currentId, int nextId, int remainExp)
    {
        //레벨 업 후 or 랭크 업 후 호출
        // id 교체 및 경험치 갱신
        int idx = Data.ownedIds.IndexOf(currentId);
        if (idx < 0)
            return;

        Data.ownedIds[idx] = nextId;

        Data.expById.Remove(currentId);
        Data.expById[nextId] = remainExp;
    }
    public static void CommitUpgradeResult(int startId, int finalId, int remainExp)
    {
        //레벨 업/랭크 업 결과 확정 처리
        if (finalId != startId)
        {
            ReplaceOwnedId(startId, finalId, remainExp);
        }
        else
        {
            // 레벨업/랭크업 안 됐으면 exp만 업데이트
            Data.expById[startId] = remainExp;
        }

        Save(); // 최종 1회 저장
    }
}