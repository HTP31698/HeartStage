using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class ScriptAttacher
{
    private static readonly Dictionary<string, Type> _cache;

    // 데이터 테이블에 있는 ID들과 짝에 맞는 스크립트 등록하기
    private static readonly Dictionary<int, string> _idToScript = new()
    {
        { 31202, "FaceGeniusSkill" }, // 얼굴 천재 
        { 31203, "FaceGeniusSkillV2" }, // 화려한 얼굴 천재
        { 31204, "SonicAttackSkill" }, // 만능 엔터테이너
        { 31205, "SonicAttackSkillV2" }, // 다재다능한 만능 엔터테이너
        { 31206, "ReverseCharmSkill" }, // 반전매력
        { 31207, "ReverseCharmSkillV2" }, // 넘치는 반전매력
        { 31208, "HeartBombSkill" }, // 섹시 다이너마이트
        { 31209, "HeartBombSkillV2" }, // 폭룡적인 섹시 다이너마이트

        { 31001, "DeceptionBossSkill" }, // 대량 현혹 튜토리얼 근접
        { 31002, "DeceptionBossSkill" }, // 대량 현혹 튜토리얼 원거리
        { 31003, "DeceptionBossSkill" }, // 대량 현혹 근접
        { 31004, "DeceptionBossSkill" }, // 대량 현혹 원거리

    { 31101, "SpeedBuffBossSkill"}, // 광기의 행진 (이동속도 버프)
    { 31201, "BooingBossSkill"},    // 야유 공격 (공격속도 디버프)
    };

    // 등록된 스크립트들 캐싱
    static ScriptAttacher()
    {
        _cache = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t))
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => g.First());
    }

    // 해당 object에 ID와 짝이 되는 스크립트 붙여줌
    public static void AttachById(GameObject obj, int id)
    {
        if (_idToScript.TryGetValue(id, out var scriptName))
        {
            AttachByName(obj, scriptName);
        }
        else
        {
            //Debug.Log($"ID {id}에 해당하는 스크립트를 찾을 수 없습니다!");
        }
    }

    // 스크립트 이름으로도 가능
    public static void AttachByName(GameObject obj, string scriptName)
    {
        if (_cache.TryGetValue(scriptName, out var type))
        {
            obj.AddComponent(type);
        }
        else
        {
            //Debug.LogError($"'{scriptName}' 타입을 찾을 수 없습니다!");
        }
    }
}
