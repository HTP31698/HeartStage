using System;
using System.Collections.Generic;

[Serializable]
public abstract class SaveData
{
    public int Version { get; protected set; }

    public abstract SaveData VersionUp();
}

[Serializable]
public class SaveDataV1 : SaveData
{
    // ================== 1. 시간 / 버전 ==================
    // 마지막 접속 시간 (DateTime.Binary)
    public long lastLoginBinary;
    public DateTime LastLoginTime => DateTime.FromBinary(lastLoginBinary);

    // ================== 2. 인벤토리 / 성장 / 해금 ==================
    // 아이템 인벤토리: 아이템 ID → 수량
    public Dictionary<int, int> itemList = new Dictionary<int, int>();
    // 클리어한 웨이브 ID 목록 (최초 보상 체크용)
    public List<int> clearWaveList = new List<int>();
    // 보유 캐릭터 ID 집합 (중복 방지)
    public List<int> ownedIds = new List<int>();
    // 시스템 해금 여부 (이름 기준)
    public Dictionary<string, bool> unlockedByName = new Dictionary<string, bool>();
    // 경험치를 ID별로 저장
    public Dictionary<int, int> expById = new Dictionary<int, int>();

    // ================== 3. 상점 / 파견 / 자원 ==================
    // 데일리 샵 슬롯 정보 (상점테이블ID, 구매여부 등)
    public List<DailyShopSlot> dailyShopSlotList = new List<DailyShopSlot>();
    // 캐릭터 파견 횟수 (캐릭터ID → 파견 횟수)
    public Dictionary<int, int> characterDispatchCounts = new Dictionary<int, int>();
    // 마지막 파견 리셋 날짜 (예: "2025-12-02")
    public string lastDispatchResetDate = "";

    // ================== 4. 스테이지 / 웨이브 진행 ==================
    public int selectedStageID = -1;
    public int selectedStageStep1 = -1;
    public int selectedStageStep2 = -1;
    public int startingWave = 1;
    public bool isInfiniteMode = false;      // 무한 모드 진입 플래그
    public int infiniteStageId = 0;          // 무한 스테이지 ID

    public List<int> completedStoryStages = new List<int>();

    // 스토리 컷씬 후 보상창 표시 플래그
    public bool showStoryRewardAfterScene = false;
    public bool StoryAfterLobby = false;
    // 클리어한 스토리 스테이지 ID 목록 (보상 중복 지급 방지용)
    public List<int> clearedStoryStages = new List<int>();

    // ================== 5. 퀘스트 진행 상태 ==================
    public DailyQuestState dailyQuest = new DailyQuestState();
    public WeeklyQuestState weeklyQuest = new WeeklyQuestState();
    public AchievementQuestState achievementQuest = new AchievementQuestState();

    // ================== 6. 프로필 / 소셜 ==================
    public string nickname = "";                    // ""이면 uid 사용
    public string statusMessage = "";               // 상태 메시지
    public string profileIconKey = "";              // 현재 장착 아이콘
    public List<string> ownedProfileIconKeys = new();
    public int equippedTitleId = 0;                 // 장착 칭호
    public List<int> ownedTitleIds = new();         // 획득 칭호
    public int fanAmount = 0;                       // 팬 수


    public int mainStageStep1 = 0;                  // 3-2
    public int mainStageStep2 = 0;

    public int bestFanMeetingSeconds = 0;           // MM:SS로 변환해 표시
    public int specialStageBestSeconds = 0;         // 지금은 공석 → 0이면 "--:--"
    public int infiniteStageBestSeconds = 0;        // 무한 스테이지 최고 생존 시간 (초)
    public int infiniteStagePlayCountToday = 0;     // 무한 스테이지 오늘 플레이 횟수
    public int infiniteStageLastPlayDate = 0;       // 무한 스테이지 마지막 플레이 날짜 (yyyyMMdd)
    public int dreamEnergy = 0;
    // 드림 에너지 교환(선물) 관련
    public int dreamSendDailyLimit = 20;            // 하루 최대 선물 횟수
    public int dreamSendTodayCount = 0;             // 오늘 보낸 횟수
    // 친구 관련 (내가 맺은 친구들의 uid 리스트)
    public int dreamLastSendDate = 0;               // yyyymmdd
    // 드림 에너지 받기 제한
    public int dreamReceiveDailyLimit = 20;         // 하루 최대 받기 횟수
    public int dreamReceiveTodayCount = 0;          // 오늘 받은 횟수
    public int dreamLastReceiveDate = 0;            // yyyymmdd
    public List<string> friendUidList = new List<string>();

    // ================== 7. 공지 / 기타 ==================
    // 마지막으로 본 공지 ID (1부터 시작)
    public int lastSeenNoticeId = 0;
    // 셋업 윈도우에서 돌아가기 버튼으로 로비에 왔는지 여부
    public bool returnToStageInfo = false;

    public bool isTutorialCompleted = false; // 튜토리얼 완료 여부
    public bool isTutorialCutsceneCompleted = false; // 튜토리얼 컷씬 완료 여부
    public bool isStageTutorialCompleted = false; // 스테이지 튜토리얼 완료 여부

    // ================== 8. 볼륨  ==================
    public float bgmVolume = 0.5f; // BGM 볼륨 (0~1)
    public float sfxVolume = 0.5f; // SFX 볼륨 (0~1)

    // ================== 9. 캐릭터 호감도 관련  ==================
    public Dictionary<string, int> likeabilityDict = new Dictionary<string, int>(); // 캐릭터 이름, 호감도 수치
    public Dictionary<string, LikeabilityRewardState> likeabilityRewardStates = new Dictionary<string, LikeabilityRewardState>(); // 캐릭터 이름, 보상 받았는지 상태 여부

    // ================== 10. 의상 시스템 ==================
    // 캐릭터별 장착 의상 (캐릭터 이름 → 장착 의상 정보)
    // 의상 보유 여부는 기존 itemList에서 확인 (itemList[costumeId] > 0)
    public Dictionary<string, EquippedCostume> equippedCostumeByChar = new Dictionary<string, EquippedCostume>();

    // ================== 생성자 / 버전업 ==================
    public SaveDataV1()
    {
        Version = 1;
    }

    public override SaveData VersionUp()
    {
        // 나중에 V2로 넘어갈 때 마이그레이션
        throw new NotImplementedException();
    }
}

/// <summary>
/// 캐릭터별 장착 의상 정보
/// </summary>
[Serializable]
public class EquippedCostume
{
    public int topItemId;      // 상의 아이템 ID (0이면 기본)
    public int pantsItemId;    // 하의 아이템 ID (0이면 기본)
    public int shoesItemId;    // 신발 아이템 ID (0이면 기본)

    public EquippedCostume()
    {
        topItemId = 0;
        pantsItemId = 0;
        shoesItemId = 0;
    }

    public EquippedCostume(int top, int pants, int shoes)
    {
        topItemId = top;
        pantsItemId = pants;
        shoesItemId = shoes;
    }
}