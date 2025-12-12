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
        { 31210, "MeteorMelodySkill" }, // 유성의 멜로디
        { 31211, "MeteorMelodySkillV2" }, // 쏟아지는 유성의 멜로디
        { 31212, "FairySkill" }, // 입덕요정
        { 31213, "FairySkillV2" }, // 유입을 부르는 입덕요정
        { 31214, "TwinkleSkill" }, // 시선을 끄는 눈빛
        { 31215, "TwinkleSkillV2" }, // 시선을 집중시키는 눈빛
        { 31216, "AcrobatSkill" }, // 곡예사
        { 31217, "AcrobatSkillV2" }, // 비트 위에 곡예사
        { 31218, "MaknaeOnTopSkill" }, // 막내온탑
        { 31219, "MaknaeOnTopSkillV2" }, // 미워할 수 없는 막내온탑
        { 31220, "AbsolutePitchSkill" }, // 절대음감
        { 31221, "AbsolutePitchSkillV2" }, // 천재적인 절대음감
        { 31222, "DancingMachineSkill" }, // 댄싱머신
        { 31223, "DancingMachineSkillV2" }, // 현란한 댄싱머신

        { 30001, "DeceptionBossSkill" }, // 대량 현혹 튜토리얼 근접
        { 30002, "DeceptionBossSkill" }, // 대량 현혹 튜토리얼 원거리
        { 30003, "DeceptionBossSkill" }, // 1스테이지 대량 현혹 근접
        { 30004, "DeceptionBossSkill" }, // 1스테이지 대량 현혹 원거리
        { 30005, "DeceptionBossSkill" }, // 2스테이지 대량 현혹 근접
        { 30006, "DeceptionBossSkill" }, // 2스테이지 대량 현혹 원거리
        { 30007, "DeceptionBossSkill" }, // 3스테이지 대량 현혹 근접
        { 30008, "DeceptionBossSkill" }, // 3스테이지 대량 현혹 원거리
        { 30009, "DeceptionBossSkill" }, // 4스테이지 대량 현혹 근접
        { 30010, "DeceptionBossSkill" }, // 4스테이지 대량 현혹 원거리

        { 30201, "SpeedBuffBossSkill"}, // 광기의 행진 (이동속도 버프)
        { 30101, "BooingBossSkill"},    // 야유 공격 (공격속도 디버프)
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
