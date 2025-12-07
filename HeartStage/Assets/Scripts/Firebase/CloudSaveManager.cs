using Firebase.Database;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance;

    private DatabaseReference db;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async UniTask Start()
    {
        await FirebaseInitializer.Instance.WaitForInitilazationAsync();
        db = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // 서버에 저장
    public async UniTask SaveAsync(string userId, string json)
    {
        string path = AuthManager.Instance.GetUserDataPath("saveData");
        await db.Child(path).SetRawJsonValueAsync(json);
    }

    // 서버에서 로드
    public async UniTask<string> LoadAsync(string userId)
    {
        string path = AuthManager.Instance.GetUserDataPath("saveData");
        var snapshot = await db.Child(path).GetValueAsync();

        if (snapshot.Exists)
        {
            return snapshot.GetRawJsonValue();
        }
        return null;
    }
}