using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀘스트 이벤트 타입 (7가지로 통합)
/// </summary>
public enum QuestEventType
{
    None = 0,

    /// <summary>로그인/출석 횟수</summary>
    Attendance = 1,

    /// <summary>스테이지 클리어 횟수 (targetId=0: 전체, targetId>0: 특정 스테이지)</summary>
    ClearStage = 2,

    /// <summary>몬스터 처치 (targetId=0: 전체, targetId>0: 특정 몬스터)</summary>
    MonsterKill = 3,

    /// <summary>보스 처치 (targetId=0: 전체 보스, targetId>0: 특정 보스)</summary>
    BossKill = 4,

    /// <summary>뽑기 횟수</summary>
    GachaDraw = 5,

    /// <summary>상점 구매 횟수</summary>
    ShopPurchase = 6,

    /// <summary>팬수 달성 (Quest_required로 목표값 비교)</summary>
    FanAmountReach = 7,
}

/// <summary>
/// 퀘스트 이벤트 매핑 (Inspector에서 설정)
/// </summary>
[System.Serializable]
public class QuestEventMapping
{
    [Tooltip("이벤트 타입")]
    public QuestEventType eventType;

    [Tooltip("연결할 퀘스트 ID (QuestTable의 Quest_ID)")]
    public int questId;

    [Tooltip("특정 대상 ID (0이면 전체 대상)\n- MonsterKill/BossKill: 몬스터 ID\n- ClearStage: 스테이지 ID")]
    public int targetId;

    public QuestEventMapping() { }

    public QuestEventMapping(QuestEventType type, int qId, int tId = 0)
    {
        eventType = type;
        questId = qId;
        targetId = tId;
    }
}

/// <summary>
/// 이벤트 타입별 정보 (Tool에서 사용)
/// </summary>
public static class QuestEventTypeInfo
{
    public static string GetDisplayName(QuestEventType type)
    {
        switch (type)
        {
            case QuestEventType.Attendance: return "출석/로그인";
            case QuestEventType.ClearStage: return "스테이지 클리어";
            case QuestEventType.MonsterKill: return "몬스터 처치";
            case QuestEventType.BossKill: return "보스 처치";
            case QuestEventType.GachaDraw: return "뽑기";
            case QuestEventType.ShopPurchase: return "상점 구매";
            case QuestEventType.FanAmountReach: return "팬수 달성";
            default: return "없음";
        }
    }

    public static bool RequiresTargetId(QuestEventType type)
    {
        switch (type)
        {
            case QuestEventType.ClearStage:
                return true; // 특정 스테이지 ID (0=전체)
            case QuestEventType.BossKill:
                return true; // 특정 보스 ID (0=전체 보스) - 보스도 MonsterTable에 있음
            case QuestEventType.MonsterKill:
                return false; // 마리수로 카운트, targetId 불필요
            default:
                return false;
        }
    }

    public static string GetTargetIdDescription(QuestEventType type)
    {
        switch (type)
        {
            case QuestEventType.ClearStage: return "스테이지 ID (0=전체)";
            case QuestEventType.BossKill: return "보스 몬스터 ID (0=전체 보스)";
            default: return "";
        }
    }
}
