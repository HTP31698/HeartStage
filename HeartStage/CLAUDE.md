# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

HeartStage 프로젝트의 Claude Code 가이드 문서입니다.

## Project Overview

HeartStage는 Unity + Firebase 기반 한국어 아이돌 디펜스 모바일 게임입니다.
- **코드 규모**: ~312개 C# 파일 (Assets/Scripts/)
- **비동기 패턴**: UniTask (Cysharp) - Coroutine 사용 금지
- **직렬화**: Newtonsoft.Json (TypeNameHandling.All)
- **에셋 관리**: Addressables

---

## Architecture

### Manager Singletons (DontDestroyOnLoad)

| Manager | 파일 | 역할 |
|---------|------|------|
| **AuthManager** | Firebase/AuthManager.cs | Firebase 인증 (익명/이메일), 사용자 상태 관리 |
| **QuestManager** | Quest/QuestManager.cs | 일일/주간/업적 퀘스트, Dirty Flag 저장 최적화 |
| **GameSceneManager** | GameSceneManager.cs | Addressables 기반 씬 전환 |
| **ResourceManager** | ResourceManager.cs | 에셋 캐싱, 라벨 프리로드 |
| **SoundManager** | SoundManager.cs | BGM/SFX 재생, 볼륨 조절, 오디오 믹서, Hit Sound Pool |
| **PoolManager** | PoolManager.cs | 오브젝트 풀링 (기본 30개, 최대 300개) |
| **GachaManager** | GachaManager.cs | 가챠 드로우 (1회/5회), 확률 계산 |
| **MailManager** | UI/Mail/MailManager.cs | 우편함 관리, Firebase 메일 동기화 (전역/개인) |
| **LiveConfigManager** | System/LiveConfigManager.cs | 점검/공지/버전 관리 (Firebase 실시간) |
| **LoadSceneManager** | LoadSceneManager.cs | 스테이지 씬 전환, 저장 데이터 셋업 |
| **SceneLoader** | SceneLoader.cs | Addressable 씬 로딩, 로딩 UI 진행도 |

### Static Classes

| Class | 파일 | 역할 |
|-------|------|------|
| **SaveLoadManager** | Json/SaveLoadManager.cs | 로컬/클라우드 저장 (Firebase RTDB) |
| **DataTableManager** | Csv/DataTableManager.cs | CSV 테이블 로드 |
| **ItemInvenHelper** | Json/Helper/ItemInvenHelper.cs | 아이템 추가/소비 |
| **CharacterHelper** | Json/Helper/CharacterHelper.cs | 캐릭터 획득/경험치/프로필 아이콘 |
| **SynergyManager** | StageSetUp/SynergyManager.cs | 캐릭터 시너지 계산/적용 |
| **FirebaseTime** | Firebase/FirebaseTime.cs | 서버 시간 동기화 |
| **PassivePatternUtil** | StageSetUp/PassivePatternUtil.cs | 패시브 타일 패턴 로드/관리 |
| **StageLayoutUtil** | StageSetUp/StageLayoutUtil.cs | 스테이지 캐릭터 슬롯 레이아웃 |

### Static Services (Firebase 연동)

| Service | 파일 | 역할 |
|---------|------|------|
| **PublicProfileService** | UI/Profile/PublicProfileService.cs | 공개 프로필 조회/관리 (닉네임, 팬수) |
| **FriendService** | UI/FriendUI/FriendService.cs | 친구 요청/수락/삭제, 캐싱 (최대 20명) |
| **NicknameService** | UI/Profile/NicknameService.cs | 닉네임 설정/변경/중복 검사 |
| **NicknameValidator** | UI/Profile/NicknameValidator.cs | 닉네임 유효성 검사 (비속어 필터) |
| **FriendSearchService** | UI/FriendUI/FriendSearchService.cs | 유저 닉네임 검색 |
| **DreamEnergyGiftService** | UI/FriendUI/DreamEnergyGiftService.cs | 꿈의 에너지 선물 송수신 |

### Instance Singletons (씬 종속)

| Manager | 파일 | 역할 |
|---------|------|------|
| **StageManager** | StageManager.cs | 스테이지 진행, 웨이브 관리, 피버타임 |
| **LobbyManager** | LobbyManager.cs | 로비 UI, 재화 표시 |
| **WindowManager** | WindowManager.cs | 윈도우 상태 관리 |
| **ActiveSkillManager** | ActiveSkill/ActiveSkillManager.cs | 스킬 등록/쿨다운/발동 |
| **MonsterSpawner** | Monster/MonsterSpawner.cs | 몬스터 웨이브 스폰/풀링 |
| **ItemManager** | Item/ItemManager.cs | 드롭 아이템 스폰/풀링 |
| **DamagePopupManager** | Monster/DamagePopupManager.cs | 데미지 팝업 생성/풀링 |
| **InfiniteStageManager** | InfiniteStage/InfiniteStageManager.cs | 무한 스테이지 진행, 강화/보상 계산 |
| **InfiniteMonsterSpawner** | InfiniteStage/InfiniteMonsterSpawner.cs | 무한 스테이지 몬스터 스폰 |

---

## Directory Structure

```
Assets/Scripts/
├── BootStrap.cs              # 진입점, 초기화 시퀀스
├── Defines.cs                # Enum 정의 (SceneType, WindowType, ItemID 등)
├── GameSceneManager.cs       # 씬 전환
├── ResourceManager.cs        # 에셋 캐싱
├── WindowManager.cs          # 윈도우 관리
├── SoundManager.cs           # 오디오
├── GachaManager.cs           # 가챠 시스템
├── PoolManager.cs            # 오브젝트 풀링
├── StageManager.cs           # 스테이지 진행
│
├── Firebase/                 # Firebase 연동
│   ├── AuthManager.cs        # 인증 (익명/이메일)
│   ├── CloudSaveManager.cs   # 클라우드 저장
│   ├── FirebaseInitializer.cs # Firebase SDK 초기화
│   ├── FirebaseTime.cs       # 서버 시간
│   └── Login/                # 로그인 UI (LoginUI, LinkAccountPopup)
│
├── System/                   # 시스템 설정
│   ├── LiveConfigManager.cs  # 점검/공지/버전 관리 (실시간)
│   └── ClientVersion.cs      # 클라이언트 버전 정보
│
├── Json/                     # 저장 시스템
│   ├── SaveLoadManager.cs    # 저장/로드 로직
│   ├── SaveData.cs           # SaveDataV1 구조체
│   ├── JsonConverter.cs      # Newtonsoft.Json 커스텀 변환
│   └── Helper/               # ItemInvenHelper, CharacterHelper
│
├── Quest/                    # 퀘스트 시스템
│   ├── QuestManager.cs       # 퀘스트 매니저
│   ├── QuestWindow.cs        # 퀘스트 UI 컨테이너
│   ├── QuestEventSystem.cs   # 퀘스트 이벤트 발행
│   ├── QuestTabBase.cs       # 퀘스트 탭 기본 클래스
│   ├── DailyQuests.cs        # 일일 퀘스트 UI
│   ├── WeeklyQuests.cs       # 주간 퀘스트 UI
│   └── ArchivementQuests.cs  # 업적 UI
│
├── Csv/                      # 데이터 테이블 (25개)
│   ├── DataTableManager.cs   # 테이블 로더
│   └── [25개 테이블 클래스]
│
├── Data/                     # 데이터 모델 (ScriptableObject, 17개)
│   ├── CharacterData.cs, MonsterData.cs, ItemData.cs
│   ├── SkillData.cs, EffectData.cs, StageData.cs
│   ├── StageWaveData.cs, SynergyData.cs, QuestData.cs
│   ├── PassivePatternData.cs, StageLayoutData.cs
│   └── ...
│
├── StageSetUp/               # 스테이지 설정
│   ├── StageSetupWindow.cs   # 스테이지 셋업 메인 UI
│   ├── OwnedCharacterSetup.cs # 보유 캐릭터 선택
│   ├── SynergyManager.cs     # 시너지 시스템
│   ├── SynergyPanel.cs       # 시너지 표시
│   ├── DraggableSlot.cs      # 드래그 가능 슬롯
│   ├── PassivePatternUtil.cs # 패시브 타일 패턴
│   ├── StageLayoutUtil.cs    # 스테이지 레이아웃
│   └── Tutorial/             # 튜토리얼
│
├── ActiveSkill/              # 액티브 스킬 (30개)
│   ├── ActiveSkillManager.cs
│   ├── ISkillBehavior.cs
│   ├── BaseProjectileSkill.cs
│   ├── CooldownUIHandler.cs
│   ├── SkillRangeDisplayer.cs
│   └── [스킬 구현체 V1/V2: AbsolutePitch, Acrobat, DancingMachine,
│        FaceGenius, Fairy, HeartBomb, MaknaeOnTop, MeteorMelody,
│        ReverseCharm, SonicAttack, Twinkle 등]
│
├── Effect/                   # 효과 시스템 (24개)
│   ├── EffectBase.cs         # 효과 기본 클래스
│   ├── EffectRegistry.cs     # 효과 등록 시스템
│   ├── StatTypes.cs          # 스탯 열거형
│   ├── Conditions/           # 상태이상 (Stun, Confuse, Paralyze, Knockback, Penetrate)
│   └── Stats/                # 스탯 효과 (AttackMul, MaxHpMul, MoveSpeedMul,
│                             #   CritChance, CritDamage, AttackRange, AttackSpeed,
│                             #   IncomingDamageMul, DropAmountMul, ShoutGainMul,
│                             #   ExtraAttackChance, ProjectileCount, StatCalc)
│
├── InfiniteStage/            # 무한 스테이지 시스템
│   ├── InfiniteStageManager.cs    # 무한 스테이지 진행 관리
│   ├── InfiniteMonsterSpawner.cs  # 몬스터 스폰 (점진적 강화)
│   ├── InfiniteMonsterComponent.cs # 몬스터 컴포넌트
│   └── InfiniteStageUI.cs         # UI (시간/강화 표시)
│
├── Character/                # 캐릭터 시스템
│   ├── CharacterAttack.cs    # 캐릭터 공격 (발사체 생성)
│   ├── CharacterFence.cs     # 캐릭터 배치/표시
│   ├── CharacterProjectile.cs # 캐릭터 발사체
│   └── CharacterSkillController.cs # 캐릭터 스킬 제어
│
├── Monster/                  # 몬스터 시스템 (24개)
│   ├── MonsterBehavior.cs    # 몬스터 기본 동작
│   ├── MonsterSpawner.cs     # 웨이브별 몬스터 스폰
│   ├── MonsterMovement.cs    # 몬스터 이동
│   ├── MonsterProjectile.cs  # 몬스터 발사체
│   ├── DamagePopupManager.cs # 데미지 팝업 풀링
│   ├── IBossMonsterSkill.cs  # 보스 스킬 인터페이스
│   ├── BossImmunity.cs       # 보스 면역 설정
│   └── [보스 스킬: BooingBossSkill, DarkBallBossSkill,
│        ShadowAwakeningBossSkill, SlowBossSkill, SpeedBuffBossSkill,
│        DeceptionBossSkill, DarkBallProjectile, TenevisAttackEffect]
│
├── Controller/               # 씬 컨트롤러
│   ├── BootSceneController.cs
│   ├── TitleSceneController.cs
│   ├── LobbySceneController.cs
│   └── StageSceneController.cs
│
├── UI/                       # UI 시스템 (80+ 파일)
│   ├── LobbyHome/            # 로비 메인 UI
│   ├── FriendUI/             # 친구 시스템 (목록/추가/검색/메시지)
│   ├── Profile/              # 프로필/닉네임/아이콘/칭호
│   ├── Encyclopedia/         # 캐릭터 도감
│   ├── Gacha/                # 가챠 UI (1회/5회, 확률, 결과)
│   ├── Mail/                 # 우편함 (MailManager, MailData, MailText)
│   ├── Shopping/             # 상점 (데일리 샵, 구매 확인)
│   ├── Piece/                # 캐릭터 조각 교환
│   ├── ItemInventory/        # 아이템 인벤토리
│   ├── Stage/                # 스테이지 UI (승리/패배, 보스 알림, 레벨업)
│   └── SpecialDungeon/       # 특수 던전 (팬미팅/스토리)
│
├── Item/                     # 아이템 시스템
│   └── ItemManager.cs        # 드롭 아이템 스폰/풀링
│
├── Monitoring/               # 모니터링/디버그 (7개)
│   └── [MonitoringRewardUI, CharacterSelectUI 등]
│
└── Test/                     # 테스트 시스템 (19개)
    ├── StageWaveTest/        # 웨이브 테스트
    └── AddSkillTestFeature/  # 스킬 테스트

Assets/DataTables/            # CSV 설정 파일 (25개)
Assets/Scenes/                # bootScene, TitleScene, Lobby, Stage
Assets/Prefabs/               # 프리팹 (Manager, Character, Monster, UI 등)
```

---

## Initialization Flow

```
bootScene
  └─ BootSceneController.Start()
       ├─ Addressables.InitializeAsync()
       ├─ ResourceManager.PreloadLabelAsync("StageAssets", "SFX")
       ├─ LiveConfigManager.InitializeAsync()
       ├─ DataTableManager.Initialization (25 CSV 로드)
       ├─ AuthManager → 로그인 대기
       ├─ FirebaseTime.Initialize()
       ├─ SaveLoadManager.LoadFromServer()
       └─ LoadSceneAsync("TitleScene")

TitleScene
  └─ 점검 확인 (LiveConfigManager.Maintenance)
  └─ 로그인 처리 후 Lobby로 전환

Lobby
  └─ LobbySceneController.Awake()
       ├─ QuestManager.Initialize()
       ├─ PublicProfileService.SyncAsync()
       ├─ FriendService.RefreshAllCacheAsync()
       ├─ MailManager 초기화
       └─ GameSceneManager.NotifySceneReady()

Stage
  └─ StageSceneController.Awake()
       ├─ Wait for StageManager, MonsterSpawner ready
       └─ GameSceneManager.NotifySceneReady()
```

---

## Save System

### SaveDataV1 구조 (Json/SaveData.cs)

```csharp
public class SaveDataV1 {
    // 시간/버전
    public long lastLoginBinary;           // DateTime.ToBinary()

    // 인벤토리
    public Dictionary<int, int> itemList;  // 아이템ID → 수량
    public List<int> ownedIds;             // 보유 캐릭터 ID
    public Dictionary<int, int> expById;   // 캐릭터 경험치

    // 스테이지 진행
    public int selectedStageID;
    public int selectedStageStep1, selectedStageStep2;
    public int startingWave;

    // 퀘스트 상태
    public DailyQuestState dailyQuest;
    public WeeklyQuestState weeklyQuest;
    public AchievementQuestState achievementQuest;

    // 프로필/소셜
    public string nickname;
    public string statusMessage;
    public List<string> friendUidList;

    // 리소스
    public int dreamEnergy;
    public int dreamSendDailyLimit;
    public int dreamReceiveDailyLimit;

    // 무한 스테이지
    public float infiniteStageBestSeconds;     // 최고 생존 시간
    public int infiniteStagePlayCountToday;    // 오늘 플레이 횟수
    public string infiniteStageLastPlayDate;   // 마지막 플레이 날짜

    // 설정
    public float bgmVolume, sfxVolume;
}
```

### 저장 경로

```
로컬: {Application.persistentDataPath}/Save/SaveAuto.json
클라우드: Firebase RTDB → {UserPath}/saveData
```

### 저장 패턴

```csharp
// Fire-and-forget (비동기, 결과 무시)
SaveLoadManager.SaveToServer().Forget();

// 아이템 추가 시 자동 저장
ItemInvenHelper.AddItem(itemId, amount);  // 내부에서 SaveToServer().Forget() 호출
```

---

## Key Data Structures

### GachaResult (가챠 결과)

```csharp
public struct GachaResult {
    public GachaData gachaData;           // 뽑힌 가챠 데이터
    public CharacterCSVData characterData; // 뽑힌 캐릭터
    public bool isDuplicate;               // 중복 여부 (조각 변환)
}
```

### PublicProfileData (공개 프로필)

```csharp
public class PublicProfileData {
    public string uid;
    public string nickname;
    public string statusMessage;
    public string profileIconKey;
    public int fanAmount;                  // 총 팬 수
    public int equippedTitleId;           // 장착 칭호
    public int mainStageStep1, mainStageStep2;  // 스테이지 진행도
    public int achievementCompletedCount;  // 업적 완료 수
    public float bestFanMeetingSeconds;    // 팬미팅 최고 기록
    public float specialStageBestSeconds;  // 특수 스테이지 최고 기록
    public float infiniteStageBestSeconds; // 무한 스테이지 최고 기록
}
```

### SceneEntry (씬 전환)

```csharp
public class SceneEntry {
    public SceneType sceneType;
    public string address;  // Addressables 씬 주소
}
```

### Damage Interfaces

```csharp
public interface IAttack {
    void Attack(IDamageable target);
}

public interface IDamageable {
    void TakeDamage(float damage, bool isCritical);
}

public interface IDamaged {
    void OnDamaged(float damage);
}
```

---

## Firebase User Paths

```
users/
├── anonymous/
│   ├── active/{userId}/      # 활성 익명 (< 30일)
│   │   ├── saveData          # SaveDataV1 JSON
│   │   └── metadata          # createdAt, lastLoginAt, expireAt
│   └── cold/{userId}/        # 비활성 익명 (30-90일)
└── registered/{userId}/      # 이메일 연동 계정

publicProfiles/{userId}/      # 공개 프로필 (닉네임, 통계)
friends/{userId}/             # 친구 목록
friendRequests/{userId}/      # 받은 친구 요청
sentRequests/{userId}/        # 보낸 친구 요청
dreamGifts/{userId}/          # 받은 꿈의 에너지 선물
sentGiftsToday/{userId}/      # 오늘 보낸 선물 (일일 리셋)
mails/{userId}/               # 개인 우편함
globalMail/                   # 전역 메일 (관리자용)
nicknameIndex/{nickname}/     # 닉네임 → UID 매핑

liveConfig/
├── appConfig/                # 앱 버전 설정
├── maintenance/              # 점검 정보
└── notices/                  # 공지사항
```

---

## Quest System

### 퀘스트 상태 구조

```csharp
public class DailyQuestState {
    public string date;                    // "yyyyMMdd" (리셋 기준)
    public int attendanceCount;            // 연속 출석 일수
    public int progress;                   // 0-100 진행률
    public List<int> clearedQuestIds;      // 오늘 클리어한 퀘스트
    public List<int> completedQuestIds;    // 완료된 퀘스트
    public bool[] claimed;                 // 5개 보상 수령 여부
}
```

### Dirty Flag 패턴 (QuestManager)

```csharp
private bool _isDirty = false;

// 변경 시 마킹
public void OnQuestProgress() {
    UpdateProgress();
    _isDirty = true;
}

// 15-30초 간격 저장
void Update() {
    if (_isDirty && Time.time - _lastSaveTime >= saveInterval)
        SaveDailyStateIfDirty();
}

// 앱 종료/일시정지 시 즉시 저장
void OnApplicationPause(bool pause) {
    if (pause) SaveDailyStateIfDirty();
}
```

### 퀘스트 이벤트

```csharp
// 이벤트 발생
QuestManager.DailyQuestCompleted?.Invoke(questData);

// UI에서 구독
void OnEnable() {
    QuestManager.DailyQuestCompleted += OnQuestComplete;
}
```

---

## Code Patterns

### Singleton Pattern

```csharp
public class MyManager : MonoBehaviour {
    public static MyManager Instance { get; private set; }

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

### UniTask Async Pattern

```csharp
// 비동기 초기화
public async UniTaskVoid Start() {
    await DataTableManager.Initialization;
    await ResourceManager.PreloadLabelAsync("StageAssets");
    Initialize();
}

// Fire-and-forget
SaveLoadManager.SaveToServer().Forget();

// 조건 대기
await UniTask.WaitUntil(() => StageManager.Instance != null);

// 딜레이
await UniTask.Delay(100, DelayType.UnscaledDeltaTime);
```

### DataTable Access

```csharp
// 테이블 가져오기
var charTable = DataTableManager.Get<CharacterTable>(DataTableIds.Character);

// 단일 항목 조회
var charData = charTable.Get(charId);

// 전체 조회
var allChars = charTable.GetAll();

// 조건 검색 (QuestTable 예시)
var quest = DataTableManager.Get<QuestTable>(DataTableIds.Quest)
    .FirstOrDefault(q => q.Quest_ID == questId);
```

### Static Helper Pattern

```csharp
public static class ItemInvenHelper {
    private static Dictionary<int, int> Items => SaveLoadManager.Data.itemList;

    public static void AddItem(int id, int amount) {
        if (!Items.ContainsKey(id)) Items[id] = 0;
        Items[id] += amount;
        SaveLoadManager.SaveToServer().Forget();
    }

    public static bool ConsumeItem(int id, int amount) {
        if (GetAmount(id) < amount) return false;
        Items[id] -= amount;
        SaveLoadManager.SaveToServer().Forget();
        return true;
    }
}
```

### Static Service + Firebase Pattern

```csharp
public static partial class ServiceName {
    private static DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;
    private static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

    public static async UniTask<Result> DoSomethingAsync() {
        var snapshot = await Root.Child("path").GetValueAsync();
        // ...
    }
}
```

---

## DataTables (25 Tables)

| ID | 테이블 | 용도 |
|----|--------|------|
| Character | CharacterTable | 캐릭터 스탯, 스킬 |
| Monster | MonsterTable | 몬스터 정보 |
| Item | ItemTable | 아이템 정보 |
| Skill | SkillTable | 스킬 데이터 |
| Effect | EffectTable | 이펙트/버프 |
| Stage | StageTable | 스테이지 정보 |
| StageWave | StageWaveTable | 웨이브 구성 |
| Quest | QuestTable | 퀘스트 정보 |
| QuestType | QuestTypeTable | 퀘스트 유형 |
| QuestProgress | QuestProgressTable | 진행 보상 |
| Reward | RewardTable | 보상 구성 |
| Shop | ShopTable | 상점 아이템 |
| Gacha | GachaTable | 가챠 확률 |
| GachaType | GachaTypeTable | 가챠 유형 |
| RankUp | RankUpTable | 랭크업 재료 |
| LevelUp | LevelUpTable | 레벨업 경험치 |
| Select | SelectTable | 선택 UI |
| Synergy | SynergyTable | 시너지 효과 |
| Piece | PieceTable | 캐릭터 조각 |
| Title | TitleTable | 칭호 |
| Slang | SlangTable | 비속어 필터 |
| String | StringTable | 다국어 문자열 |
| InfiniteMonster | InfiniteMonsterTable | 무한 스테이지 몬스터 |
| InfiniteStage | InfiniteStageTable | 무한 스테이지 설정 |

---

## Key Systems

### Active Skill System

```csharp
// 스킬 등록 (캐릭터 스폰 시)
ActiveSkillManager.Instance.RegisterSkill(skillId, skillBehavior);

// 스킬 사용 시도
if (ActiveSkillManager.Instance.TryUseSkill(skillId)) {
    // 스킬 발동
}

// 스킬 구현 인터페이스
public interface ISkillBehavior {
    void Execute(GameObject caster, Vector3 targetPos);
}
```

**스킬 구현체 (V1/V2 버전)**:
- AbsolutePitchSkill - 절대 음감
- AcrobatSkill - 아크로뱃
- DancingMachineSkill - 댄싱 머신
- FaceGeniusSkill - 페이스 지니어스
- FairySkill - 페어리
- HeartBombSkill - 하트 폭탄
- MaknaeOnTopSkill - 막내온탑
- MeteorMelodySkill - 메테오 멜로디
- ReverseCharmSkill - 리버스 참
- SonicAttackSkill - 소닉 어택
- TwinkleSkill - 트윙클

### Synergy System

```csharp
// 시너지 평가
var activeSynergies = SynergyManager.Evaluate(selectedSlots);

// 시너지 적용
SynergyManager.ApplySynergies(selectedSlots, characterObjects);
```

### Monster Spawning

```csharp
// 오브젝트 풀링 사용
// MonsterSpawner가 웨이브별 몬스터 스폰 큐 관리
// Dictionary<int, List<GameObject>> monsterPools
```

### Effect System

버프/디버프/상태이상 처리 시스템 (Effect/ 폴더):

```csharp
// EffectBase - 모든 효과의 기본 클래스
public abstract class EffectBase {
    public float Duration { get; }
    public float Magnitude { get; }
    public float TickInterval { get; }

    public void Initialize(float duration, float magnitude, float tickInterval);
    protected abstract void OnApply();    // 효과 적용 시
    protected abstract void OnTick();     // 주기적 실행
    protected abstract void OnRemove();   // 효과 제거 시
}

// 제네릭 추가 메서드
EffectBase.Add<T>(gameObject, duration, magnitude);
```

**상태이상 효과** (Effect/Conditions/):
- `StunEffect` - 기절 (행동 불가)
- `ConfuseEffect` - 혼란 (이동 방향 반전)
- `ParalyzeEffect` - 마비
- `KnockbackEffect` - 넉백
- `PenetrateEffect` - 관통
- `ConditionState` - 상태 추적

**스탯 효과** (Effect/Stats/):
- `AttackMulEffect`, `MaxHpMulEffect`, `MoveSpeedMulEffect` - 배수 스탯
- `CritChanceAddEffect`, `CritDamageMulEffect` - 치명타
- `AttackRangeAddEffect`, `AttackSpeedAddEffect` - 공격 범위/속도
- `IncomingDamageMulEffect` - 받는 데미지 배율
- `DropAmountMulEffect` - 드롭량 배율
- `ShoutGainMulEffect` - 응원 획득 배율
- `ExtraAttackChanceAddEffect` - 추가 공격 확률
- `ProjectileCountAddEffect` - 투사체 수
- `StatCalc` - 최종 스탯 계산 엔진
- `StatMultiplier` - 배수 기반 스탯 조정

### Pool Manager System

```csharp
// 풀 생성 (초기 30개, 최대 300개)
PoolManager.Instance.CreatePool(poolId, prefab, defaultCapacity: 30, maxSize: 300);

// 풀에서 객체 가져오기
GameObject obj = PoolManager.Instance.Get(poolId);

// 사용 완료 후 풀로 반환
PoolManager.Instance.Release(poolId, obj);
```

### Live Config System

Firebase 실시간 데이터베이스 기반 점검/공지/버전 관리:

```csharp
// LiveConfigManager 이벤트 구독
LiveConfigManager.OnMaintenanceChanged += OnMaintenance;
LiveConfigManager.OnAppConfigChanged += OnAppConfig;
LiveConfigManager.OnNoticesChanged += OnNotices;

// 데이터 구조
public class MaintenanceData {
    public bool active;
    public string message;
    public long startAt, endAt;
    public bool showRemainTime;
}

public class AppConfigData {
    public int minVersionCodeAndroid;
    public int recommendVersionCode;
}

public class NoticeData {
    public string id, title, body, summary;
    public long createdAt, startAt, endAt;
    public bool isImportant;
    public string externalUrl;
}
```

### Monster System (상세)

몬스터 시스템의 핵심 구조:

#### MonsterSpawner (웨이브 스폰 시스템)

```csharp
// 웨이브 몬스터 정보 구조체
public struct WaveMonsterInfo {
    public int monsterId;
    public int count;          // 스폰할 총 수량
    public int spawned;        // 이미 스폰된 수량
    public int remainMonster;  // 남은 수량 (처치 추적)
}

// 주요 기능
- 스테이지 웨이브별 몬스터 스폰 관리
- 몬스터 ID별 오브젝트 풀 (Dictionary<int, List<GameObject>>)
- MonsterData SO 캐싱 (Dictionary<int, MonsterData>)
- 스폰 대기열 시스템 (Queue<int>)
- 보스 ID별 전용 프리팹 로드 (Boss_{id})
- 웨이브 클리어 보상 처리

// 이벤트
public static Action OnWaveCleared;  // 웨이브 클리어 시 발생
```

#### MonsterBehavior (몬스터 기본 동작)

```csharp
// 주요 메서드
public void Init(MonsterData data);              // 초기화
public void TakeDamage(float damage, bool isCrit); // 피해 처리
public void Die();                                // 사망 처리
public void SetEnhancedStats(float atk, float hp, float speed); // 스탯 강화
public MonsterData GetMonsterData();             // 데이터 접근
public int GetCurrentHP();                       // 현재 체력
public float GetCurrentAttackRange();            // 개별 랜덤 사거리

// 보스 판별
public static bool IsBossMonster(int monsterId); // 보스 여부 확인
public bool IsBossMonster();                     // 인스턴스 메서드
```

#### MonsterMovement (몬스터 이동)

```csharp
// 상태이상 이동 처리
- CanMove(): 이동 가능 여부 체크 (사망, 넉백, 혼란, 스턴, 마비)
- ConfuseMove(): 혼란 상태 - 가장 가까운 몬스터에게 이동
- KnockbackMove(): 넉백 상태 - 이동 방향 반대로 밀림

// 분리 행동 (Separation)
- allActiveMonsters: 모든 활성 몬스터 리스트 (static)
- GetHorizontalSeparationForce(): 좌우 분리 힘 계산
- 보스는 분리 계산에서 제외

// 스테이지 방향 처리
- stage_position 1: 상단 → 아래로
- stage_position 2: 중앙 → 위아래 양방향
- stage_position 3: 하단 → 위로

// 무한 스테이지용
public void SetEnhancedSpeed(float speed);  // 강화 속도 설정
```

### Infinite Stage System (상세)

시간 기반 서바이벌 모드:

#### InfiniteStageManager (게임 진행 관리)

```csharp
// 게임 상태
public enum GameState { Ready, Playing, Paused, GameOver }

// 강화 시스템
- 기본 강화 주기: 30초 (enhance_interval)
- 강화 배율: ATK 1.1x, HP 1.15x, Speed 1.05x (매 주기 누적)
- 보상 배율: 강화당 +10% (rewardMulPerEnhance)

// 주요 속성
public float ElapsedTime;              // 경과 시간
public int EnhanceCount;               // 강화 횟수
public float CurrentAtkMultiplier;     // 현재 공격력 배율
public float CurrentHpMultiplier;      // 현재 체력 배율
public float CurrentSpeedMultiplier;   // 현재 이동속도 배율
public float CurrentRewardMultiplier;  // 현재 보상 배율
public int KillCount;                  // 처치 수
public int TotalCheerGained;           // 획득한 함성 게이지

// 이벤트
public event Action OnGameStart;
public event Action OnGameOver;
public event Action<int> OnEnhance;       // enhanceCount
public event Action<int> OnMonsterKilled; // killCount
public event Action<float> OnTimeUpdate;  // elapsedTime

// 기록 저장 (SaveDataV1)
- infiniteStageBestSeconds    // 최고 생존 시간
- infiniteStagePlayCountToday // 오늘 플레이 횟수
- infiniteStageLastPlayDate   // 마지막 플레이 날짜
```

#### InfiniteStageCSVData (스테이지 설정)

```csharp
// 기본 정보
int infinite_stage_id;      // 무한 스테이지 ID (65001)
string infinite_stage_name; // 스테이지 이름

// 제한 설정
int daily_limit;            // 일일 플레이 제한 횟수
int level_max;              // 함성게이지 최대치 (레벨 상한)
int Fever_Time_stack;       // 피버타임 중복 횟수

// 스폰 설정
float enemy_spown_time;     // 적군 스폰 간격 (초)
int enemy_filed_count;      // 필드 몬스터 제한 수량

// 강화 설정
int enforce_time;           // 강화 주기 (초)
float attack_growth_value;  // 공격력 강화 배율 (1회당)
float hp_growth_value;      // 체력 강화 배율 (1회당)
float speed_growth_value;   // 이동속도 강화 배율 (1회당)

// 기본 몬스터
int base_monster1_id;       // 기본 몬스터 1 ID
int base_monster2_id;       // 기본 몬스터 2 ID

// 이속형 특수 몬스터
int fast_mon_id;            // 이속형 몬스터 ID
int fast_mon_count;         // 이속형 몬스터 스폰 수량
int fast_spawn_time;        // 이속형 첫 등장 시간 (초)
int fast_spawn_interval;    // 이속형 반복 스폰 간격 (초)

// 탱커형 특수 몬스터
int tank_mon_id;            // 탱커형 몬스터 ID
int tank_mon_count;         // 탱커형 몬스터 스폰 수량
int tank_spawn_time;        // 탱커형 첫 등장 시간 (초)
int tank_spawn_interval;    // 탱커형 반복 스폰 간격 (초)

// 힐러형 특수 몬스터
int heal_mon_id;            // 힐러형 몬스터 ID
int heal_spawn_count;       // 힐러형 몬스터 스폰 수량
int heal_spawn_time;        // 힐러형 첫 등장 시간 (초)
int heal_spawn_interval;    // 힐러형 반복 스폰 간격 (초)

// 보상 설정
int reward_per_second;      // 보상 누적 시간 (초)
int reward_item_id;         // 보상 아이템 ID
```

#### 무한 스테이지 몬스터 (MonsterCSVData 사용)

무한 스테이지는 별도 테이블 없이 기존 `MonsterTable`을 사용합니다.
- **mon_type 4**: 무한 스테이지 전용 몬스터 (24xxx)
- **mon_type 5**: 스토리 전용 몬스터 (25xxx)

```csharp
// 무한 스테이지 몬스터 ID 규칙
24101 - 무한근접 (일반)
24201 - 무한원거리 (일반)
24102 - 무한이속형 (특수)
24103 - 무한탱커형 (특수)
24202 - 무한힐러형 (특수, skill_id1=30202)

// MonsterCSVData 필드 중 무한 스테이지에서 사용하는 것들
int min_level, max_level;   // 함성 게이지 (cheer) 범위로 사용
string prefab1, prefab2;    // 랜덤 비주얼 프리팹 풀
```

#### InfiniteMonsterSpawner (몬스터 스폰)

```csharp
// Pool ID 상수
const string NormalMonsterPoolId = "InfiniteNormalMonster";
const string FastMonsterPoolId = "InfiniteFastMonster";
const string TankMonsterPoolId = "InfiniteTankMonster";
const string HealMonsterPoolId = "InfiniteHealMonster";
const string MonsterProjectilePoolId = "InfiniteMonsterProjectile";
const string MonsterHitEffectPoolId = "InfiniteMonsterHitEffect";

// 주요 기능
- PoolManager 기반 풀링
- 랜덤 외형 시스템 (prefab1/prefab2에서 랜덤 선택, "Visual_" 접두사)
- 특수 몬스터 타이머 (fast_spawn_time, tank_spawn_time, heal_spawn_time)
- 특수 몬스터 수량 스폰 (fast_mon_count, tank_mon_count, heal_spawn_count)
- 강화 스탯 적용 (ApplyEnhancedStats)
- InfiniteMonsterComponent로 개별 몬스터 상태 관리
- MonsterTable에서 데이터 조회 (InfiniteMonsterTable 대신)
```

#### InfiniteMonsterComponent (몬스터 컴포넌트)

```csharp
// 역할
- CSV 데이터 저장 (MonsterCSVData)
- Pool ID 저장 (풀 반환용)
- 사망 시 InfiniteStageManager에 알림

// 사망 처리
public void OnDeath() {
    // 함성 게이지 계산 (min_level ~ max_level 필드 사용)
    // 드롭 아이템 수집
    // 보상 배율 적용
    InfiniteStageManager.Instance.OnMonsterDeath(gameObject, cheerValue, dropItems);
}
```

#### 강화 몬스터 스탯 적용 흐름

```
InfiniteMonsterSpawner.SpawnMonster()
  → ApplyEnhancedStats()
      → MonsterBehavior.SetEnhancedStats(atk, hp, speed)
          → maxHP, currentHP 설정
          → MonsterMovement.SetEnhancedSpeed(speed)
          → healthBar 갱신
```

### Boss Monster System (상세)

#### 인터페이스 및 기본 구조

```csharp
// 보스 스킬 인터페이스 (레거시)
public interface IBossMonsterSkill {
    void useSkill(MonsterBehavior boss);
}

// 현재 스킬 인터페이스 (ISkillBehavior 사용)
public interface ISkillBehavior {
    void Init(SkillCSVData data);
    void Execute();
}

// BossAddScript - 보스 스킬 자동 등록
// - OnEnable에서 RegisterSkillsAsync() 호출
// - CSV의 skill_id1, skill_id2, skill_id3 기반 스킬 등록
// - BossImmunity 컴포넌트 자동 추가
```

#### BossImmunity (보스 면역 시스템)

```csharp
// 보스가 면역인 효과 ID
case 3013: // ConfuseEffect (혼란)
case 3011: // StunEffect (스턴)
case 3012: // ParalyzeEffect (마비)
case 3010: // SlowEffect (슬로우)
case 3001: // AttackDebuffEffect (공격력 감소)
case 3002: // AttackSpeedDebuffEffect (공격속도 감소)
```

#### 보스 스킬 구현체

| 스킬 | 클래스 | 스킬 ID | 기능 |
|------|--------|---------|------|
| **야유 공격** | BooingBossSkill | 30101 | 모든 소환 캐릭터의 공격속도 감소, Scream 파티클 |
| **광기의 행진** | SpeedBuffBossSkill | 30201 | 모든 몬스터에게 이동속도 버프/디버프 (3010 효과) |
| **그림자 각성** | ShadowAwakeningBossSkill | 30224 | 모든 몬스터 크기 +50%, 체력 +30%, 지속시간 관리 |
| **어둠의 구체** | DarkBallBossSkill | 30225 | Wall 방향으로 투사체 발사 (DarkBallProjectile) |
| **대량 현혹** | DeceptionBossSkill | 30001~30010 | 추가 몬스터 소환 (summon_type, summon_min~max) |
| **테네비스 공격** | TenevisAttackEffect | - | 몬스터 22224 전용 Sonar 범위 공격 |

#### DeceptionBossSkill (다중 스킬 지원)

```csharp
// 여러 소환 스킬을 하나의 컴포넌트에서 관리
private Dictionary<int, SkillCSVData> skillDataDict;     // 스킬 ID별 데이터
private Dictionary<int, float> nextSkillTimes;           // 스킬별 개별 쿨타임

// 스킬 ID 범위 (스테이지별)
30001~30002: 튜토리얼 (근접/원거리)
30003~30004: 1스테이지
30005~30006: 2스테이지
30007~30008: 3스테이지
30009~30010: 4스테이지
```

#### DarkBallProjectile (투사체)

```csharp
// 특징
- Wall 태그 충돌 시 데미지 (보스 공격력 x3)
- 풀 ID: "DarkBallPool"
- 히트 이펙트 풀: "monsterHitEffectPool"
- 생존 시간: 10초
```

---

## Scene Flow

| Scene | Controller | 주요 작업 |
|-------|------------|----------|
| bootScene | BootSceneController | Addressables, DataTable, LiveConfig 초기화 |
| TitleScene | TitleSceneController | 로그인, 점검 확인 |
| Lobby | LobbySceneController | 퀘스트, 프로필, 친구, 메일 초기화 |
| Stage | StageSceneController | 스테이지 셋업 대기, 로딩 완료 |

---

## Important Notes

1. **UniTask 전용**: Coroutine 대신 UniTask 사용
2. **Fire-and-forget**: 저장은 `.Forget()`으로 비동기 처리
3. **Inspector 설정**: 퀘스트 ID 등은 Inspector에서 설정 (하드코딩 X)
4. **Dirty Flag**: 빈번한 저장 대신 주기적 저장 (15-30초)
5. **익명 사용자 만료**: 90일 후 자동 삭제
6. **날짜 기반 리셋**: "yyyyMMdd" 형식으로 일일/주간 리셋
7. **Find 사용 금지**: `GameObject.Find`, `FindObjectOfType` 등 사용 금지 → 태그/직접 참조 사용
8. **친구 제한**: 최대 20명
9. **무한 스테이지**: 30초 주기 강화, 기록 저장

---

## Editor Tools

### 에디터 윈도우 (Assets/Editor/)

| 툴 | 파일 | 메뉴 경로 | 용도 |
|----|------|----------|------|
| **SO Balancing** | SOBalancingWindow.cs | `Window > SO Balancing` | SO 데이터 통합 편집, CSV 가져오기/내보내기 |
| **Passive Pattern** | PassivePatternEditorWindow.cs | `Window > Passive Pattern Editor` | 패시브 타일 패턴 편집 |
| **Stage Layout** | StageLayoutEditorWindow.cs | `Window > Stage Layout Editor` | 스테이지 레이아웃 편집 |
| **Quest Editor** | QuestEditorWindow.cs | `Window > Quest Editor` | 퀘스트 데이터 편집 |
| **Character Growth** | CharacterGrowthCompareWindow.cs | `Window > Character Growth Compare` | 캐릭터 성장 곡선 비교 |
| **Bookmark Folders** | BookmarkFoldersWindow.cs | `Window > Bookmark Folders` | 자주 쓰는 폴더 북마크 |

### 유틸리티 (Assets/Editor/)

| 툴 | 파일 | 용도 |
|----|------|------|
| **Find Usage Checker** | FindUsageChecker.cs | Find 계열 메서드 사용 감지/금지 (`Tools > Find 사용 검사`) |
| **NaN Checker** | NaNChecker.cs | 데이터에서 NaN 값 검출 |
| **TimeScale Fixer** | TimeScaleAutoFixer.cs | 플레이 종료 시 TimeScale 자동 복구 |
| **Edit Play Scene** | EditPlayScene.cs | 플레이 모드 씬 편집 지원 |

### SO 생성/내보내기 (Assets/Editor/SOExGene/)

| 파일 | 용도 |
|------|------|
| CharacterDataSOGenerator/Exporter | 캐릭터 SO ↔ CSV |
| MonsterDataSOGenerator/Exporter | 몬스터 SO ↔ CSV |
| SkillDataSOGenerator/Exporter | 스킬 SO ↔ CSV |
| ItemDataSOGenerator/Exporter | 아이템 SO ↔ CSV |
| StageDataSOGenerator/Exporter | 스테이지 SO ↔ CSV |
| StageWaveDataSOGenerator/Exporter | 웨이브 SO ↔ CSV |
| SynergyDataSOGenerator/Exporter | 시너지 SO ↔ CSV |

### Data Flow (SO ↔ CSV)

```
[에디터]                           [런타임]
ScriptableObject (SO)              CSV (DataTableManager)
     │                                  │
     │ ← CSV 가져오기                   │ ← Addressables로 로드
     │ → CSV 내보내기                   │
     │                                  │
     ▼                                  ▼
Inspector에서 직접 편집 가능      DataTableManager.SkillTable.Get()
ResourceManager.Instance.Get<T>()
```

**중요**:
- 런타임 데이터는 기본적으로 CSV에서 로드됨
- SO를 직접 사용하려면 `ResourceManager.Instance.Get<SkillData>(name)` 사용
- SO 수정 후 CSV 내보내기 필요 (빌드 시 CSV 사용)

### Passive Pattern Tool

패시브 스킬 타일 패턴 관리:
- `Assets/ScriptableObject/Tool/PassivePatterns.asset`
- `PassivePatternUtil.cs`에서 런타임 로드
- typeId로 패턴 구분 (1~9+)

### Addressables Labels

| Label | 내용 |
|-------|------|
| Stage (AddressableLabel.Stage) | SkillData, CharacterData, MonsterData SO |
| SFX | 사운드 이펙트 |
| StageAssets | PassivePatternData, StageLayoutData |

---

## Language

- 모든 사용자 대면 텍스트: 한국어
- 코드 주석: 한국어 또는 영어

---

## Infinite Mode (무한 스테이지) - 롤백 가이드

### 구현 방식
기존 Stage 씬과 StageManager/MonsterSpawner를 재사용하며, `isInfiniteMode` 플래그로 분기 처리합니다.

### 수정된 파일 및 롤백 방법

| 파일 | 추가된 코드 | 롤백 방법 |
|------|-------------|----------|
| **StageManager.cs** | `isInfiniteMode` 플래그, Update(), InitInfiniteMode(), GetInfiniteXxxMultiplier(), InfiniteDefeat() | `// ========== 무한 모드 ==========` 주석 아래 코드 전부 삭제 |
| **MonsterSpawner.cs** | `isInfiniteSpawning` 필드들, OnStageStarted() 분기, StartInfiniteSpawning() 등 | `// ========== 무한 모드 ==========` 주석 아래 코드 전부 삭제, OnStageStarted()의 if 분기 제거 |
| **MonsterMovement.cs** | `overrideMoveSpeed` 필드, SetMoveSpeed(), baseMoveSpeed 계산 | `overrideMoveSpeed` 필드 삭제, SetMoveSpeed() 메서드 삭제, Update의 baseMoveSpeed 라인을 원래대로 복구 |
| **LoadSceneManager.cs** | GoInfiniteStage(), GetPendingInfiniteStageId() | `// ========== 무한 스테이지 ==========` 주석 아래 코드 전부 삭제 |
| **SaveData.cs** | `isInfiniteMode`, `infiniteStageId` 필드 | 해당 필드 2개 삭제 |
| **CharacterFence.cs** | Die()의 무한 모드 분기 | if-else 제거하고 `StageManager.Instance.Defeat();`만 남김 |

### 유지되는 파일 (삭제 금지)
- `InfiniteStageTable.cs` - CSV 파서 (향후 확장용)
- `InfiniteStageTable.csv` - 데이터 테이블
- `MonsterTable.csv` - 24xxx 몬스터 데이터 포함

### 무한 모드 진입 방법
```csharp
LoadSceneManager.Instance.GoInfiniteStage(90001);
```

### 무한 모드 흐름
```
GoInfiniteStage(90001)
  → SaveData에 isInfiniteMode=true, infiniteStageId=90001 저장
  → Stage 씬 로드
  → StageManager.LoadSelectedStageData()
    → isInfiniteMode 감지 → InitInfiniteMode() 호출
  → MonsterSpawner.OnStageStarted()
    → isInfiniteMode 분기 → StartInfiniteSpawning() 호출
  → CharacterFence.Die()
    → isInfiniteMode 분기 → InfiniteDefeat() 호출
```

---

## 현재 진행 중인 작업 (TODO)

### 무한 스테이지 구현 상태

#### 완료된 기능 ✅
- [x] 플래그 기반 모드 전환 (`isInfiniteMode`)
- [x] 몬스터 랜덤 비주얼 (일반 몬스터만)
- [x] 배치 완료 후 타이머 시작 (`StageSetupWindow.OnStageStarted`)
- [x] UI 시간/강화레벨 표시 (`StageUI.SetInfiniteInfo`)
- [x] 강화 주기별 ATK/HP/Speed 배율 적용
- [x] 강화 알림 (BossAlertUI 재활용)
- [x] 터치 시 알림 닫기 (스킬 사용 가능하도록)
- [x] 패배 시 알림 닫기 (`Time.timeScale == 0` 감지)
- [x] 특수 몬스터 시간별 스폰 (이속형/탱커형/힐러형)
- [x] 특수 몬스터 전용 프리팹 사용
- [x] 특수 몬스터 스폰 로그 (색상별)
- [x] SpecialDungeonUI에 무한 버튼 추가
- [x] 보상 계산 (초당 보상)

#### 미구현 기능 ❌
- [ ] **deploy_limit**: 최대 배치 캐릭터 수 제한 (기본 6명)
  - `InfiniteStageCSVData.deploy_limit` 필드 활용
  - `StageSetupWindow`에서 슬롯 제한 적용
- [ ] **daily_limit**: 일일 플레이 횟수 제한
  - `SaveDataV1.infiniteStagePlayCountToday` 필드 활용
  - `SaveDataV1.infiniteStageLastPlayDate` 로 날짜 리셋
- [ ] **최고 기록 저장**: 생존 시간 기록
  - `SaveDataV1.infiniteStageBestSeconds` 필드 활용
  - `InfiniteDefeat()` 에서 갱신
- [ ] **CharacterFence 패배 연결 확인**
  - `CharacterFence.Die()` → `StageManager.Instance.InfiniteDefeat()` 호출 확인

#### Unity 에디터 작업 필요
- [ ] SpecialDungeonUI 프리팹에서 `infiniteButton` 연결
- [ ] 몬스터 프리팹 Addressables 등록 확인 (monster_23101, monster_23102, monster_23201)

#### 참고 파일
| 기능 | 파일 |
|------|------|
| 모드 초기화 | StageManager.cs (InitInfiniteMode, Update) |
| 몬스터 스폰 | MonsterSpawner.cs (StartInfiniteSpawning, SpawnInfiniteMonster) |
| 강화 알림 | BossAlertUI.cs (SetEnhanceAlert, Update, LateUpdate) |
| 진입 버튼 | SpecialDungeonUI.cs (OnInfiniteButtonClicked) |
| 패배 처리 | CharacterFence.cs (Die) |
| CSV 설정 | InfiniteStageTable.csv (90001)

---

## Stage Debug Tool 삭제 가이드

스테이지 디버그 도구를 제거할 때 아래 파일/코드를 삭제하세요.

### 삭제할 파일

| 파일 | 설명 |
|------|------|
| `Assets/Editor/StageDebugWindow.cs` | 에디터 윈도우 (Window > Stage Debug) |

### 삭제할 코드 블록

#### StageManager.cs

파일 끝의 `#if UNITY_EDITOR` 블록 전체 삭제:

```csharp
// 삭제 대상 (파일 끝부분)
#if UNITY_EDITOR
    public void Debug_SetInfiniteEnhanceLevel(int newLevel) { ... }
    public void Debug_SetWaveOrder(int newWave) { ... }
    public void Debug_ClearAllMonsters() { ... }
#endif
```

#### MonsterSpawner.cs

파일 끝의 `#if UNITY_EDITOR` 블록 전체 삭제:

```csharp
// 삭제 대상 (파일 끝부분)
#if UNITY_EDITOR
    public void Debug_JumpToWave(int waveIndex) { ... }
    public void Debug_ResetInfiniteSpawner() { ... }
    public (int currentIndex, int totalWaves) Debug_GetWaveInfo() { ... }
#endif
```

### 선택적 삭제 (구버전 F키 컨트롤러)

| 파일 | 설명 |
|------|------|
| `Assets/Scripts/StageSetUp/DebugController/StageDebugController.cs` | F1~F4 웨이브 디버그 (일반 모드) |
| `Assets/Scripts/StageSetUp/DebugController/InfiniteStageDebugController.cs` | F1~F4 강화 디버그 (무한 모드) |

**참고**: F키 컨트롤러는 모바일 시뮬레이터에서 동작하지 않아 EditorWindow로 대체되었습니다.
