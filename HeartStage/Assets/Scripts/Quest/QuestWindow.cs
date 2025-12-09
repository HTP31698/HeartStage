using UnityEngine;
using UnityEngine.UI;

public class QuestWindow : GenericWindow
{
    [Header("퀘스트 UI 총괄 창")]
    [SerializeField] private GameObject questWindow;

    [Header("일일 / 주간 / 업적 퀘스트 창")]
    [SerializeField] private GameObject dailyQuests;
    [SerializeField] private GameObject weeklyQuests;
    [SerializeField] private GameObject achievementQuests;

    [Header("각 탭의 스크립트 (Daily/Weekly/업적)")]
    [SerializeField] private DailyQuests dailyQuestsComponent;
    [SerializeField] private WeeklyQuests weeklyQuestsComponent;
    [SerializeField] private ArchivementQuests achievementQuestsComponent;

    [Header("종료 버튼")]
    [SerializeField] private Button ExitButton;

    [Header("일일 / 주간 / 업적 버튼")]
    [SerializeField] private Button DailyButton;
    [SerializeField] private Button WeeklyButton;
    [SerializeField] private Button AchievementButton;

    [Header("전체 보상 받기 버튼")]
    [SerializeField] private Button AllReceiveButton;

    private void OnEnable()
    {
        ExitButton.onClick.AddListener(CloseQuestWindow);
        DailyButton.onClick.AddListener(OpenDailyQuests);
        WeeklyButton.onClick.AddListener(OpenWeeklyQuests);
        AchievementButton.onClick.AddListener(OpenAchievementQuests);
        AllReceiveButton.onClick.AddListener(AllReceiveButtonFunction);

        // 기본적으로 일일 퀘스트 창을 엽니다.
        // 버튼도 눌린 상태로 유지해줍니다.
        DailyButton.Select();
        OpenDailyQuests();
    }

    public void CloseQuestWindow()
    {
        questWindow.SetActive(false);
    }

    public void OpenDailyQuests()
    {
        dailyQuests.SetActive(true);
        weeklyQuests.SetActive(false);
        achievementQuests.SetActive(false);

        DailyButton.interactable = false;
        WeeklyButton.interactable = true;
        AchievementButton.interactable = true;

        AllReceiveButton.gameObject.SetActive(true);
    }

    public void OpenWeeklyQuests()
    {
        dailyQuests.SetActive(false);
        weeklyQuests.SetActive(true);
        achievementQuests.SetActive(false);

        DailyButton.interactable = true;
        WeeklyButton.interactable = false;
        AchievementButton.interactable = true;

        AllReceiveButton.gameObject.SetActive(true);
    }

    public void OpenAchievementQuests()
    {
        dailyQuests.SetActive(false);
        weeklyQuests.SetActive(false);
        achievementQuests.SetActive(true);

        DailyButton.interactable = true;
        WeeklyButton.interactable = true;
        AchievementButton.interactable = false;

        // 업적 탭에서는 전체받기 버튼 숨김
        AllReceiveButton.gameObject.SetActive(false);
    }

    public void AllReceiveButtonFunction()
    {
        if (dailyQuests.activeSelf && dailyQuestsComponent != null)
        {
            dailyQuestsComponent.ClaimAllAvailableRewards();
        }

        if (weeklyQuests.activeSelf && weeklyQuestsComponent != null)
        {
            weeklyQuestsComponent.ClaimAllAvailableRewards();
        }

        if (achievementQuests.activeSelf && achievementQuestsComponent != null)
        {
            achievementQuestsComponent.ClaimAllAvailableRewards();
        }
    }

    public void OnDisable()
    {
        dailyQuests.SetActive(false);
        weeklyQuests.SetActive(false);
        achievementQuests.SetActive(false);
        ExitButton.onClick.RemoveListener(CloseQuestWindow);
        DailyButton.onClick.RemoveListener(OpenDailyQuests);
        WeeklyButton.onClick.RemoveListener(OpenWeeklyQuests);
        AchievementButton.onClick.RemoveListener(OpenAchievementQuests);
        AllReceiveButton.onClick.RemoveListener(AllReceiveButtonFunction);
    }
}

