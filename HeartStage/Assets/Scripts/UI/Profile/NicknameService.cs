using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

public static class NicknameService
{
    // 라이트 스틱 아이템 ID (네 아이템테이블 ID로 바꿔서 쓰면 됨)
    private const int LightStickItemId = 7101;
    private const int NicknameChangeCost = 2000;

    private static DatabaseReference Root => FirebaseDatabase.DefaultInstance.RootReference;
    private static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

    /// 닉네임 변경 전체 처리:
    /// - 형식/금칙어 체크
    /// - 처음 설정인지 / 변경인지 판단
    /// - 변경인 경우 라이트 스틱 2000개 소모
    /// - Firebase에 중복 닉 체크 및 인덱스 갱신
    /// - SaveData + 서버 세이브 + publicProfiles + nicknameIndex 동기화
    public static async UniTask<(bool ok, string errorMessage)> TryChangeNicknameAsync(string rawNickname)
    {
        // 1) 형식/금칙어 체크
        if (!NicknameValidator.ValidateNickname(rawNickname, out string error))
        {
            return (false, error);
        }

        if (SaveLoadManager.Data is not SaveDataV1 data)
        {
            return (false, "세이브 데이터를 찾을 수 없습니다.");
        }

        var user = Auth.CurrentUser;
        if (user == null || string.IsNullOrEmpty(user.UserId))
        {
            return (false, "로그인 정보가 없습니다.");
        }

        string uid = user.UserId;
        string trimmed = rawNickname.Trim();
        string normalizedNew = NormalizeNickname(trimmed);
        string normalizedOld = NormalizeNickname(data.nickname);

        bool isFirstSet = string.IsNullOrEmpty(data.nickname);

        // 2) 변경일 경우 라이트 스틱 비용 체크
        if (!isFirstSet)
        {
            if (!TryConsumeLightSticks(data, NicknameChangeCost, out error))
            {
                return (false, error); // "라이트 스틱이 부족합니다." 같은 메시지
            }
        }

        // 3) Firebase에서 중복 닉 체크 + nicknameReservation 등록
        bool reserved = await ReserveNicknameAsync(normalizedNew, uid);
        if (!reserved)
        {
            // 중복이어서 실패 → 이미 소모한 라이트스틱은 돌려주는게 깔끔
            if (!isFirstSet)
            {
                RefundLightSticks(data, NicknameChangeCost);
            }
            return (false, "이미 사용 중인 닉네임입니다.");
        }

        // 4) 이전 닉네임 예약 제거 (닉이 있었고, 값이 바뀐 경우)
        if (!isFirstSet && !string.IsNullOrEmpty(normalizedOld) && normalizedOld != normalizedNew)
        {
            await RemoveOldNicknameReservationAsync(normalizedOld, uid);
        }

        // 5) SaveData에 반영
        data.nickname = trimmed;

        // 6) 서버 세이브 + publicProfiles + nicknameIndex 동기화
        await SaveLoadManager.SaveToServer();

        int achievementCount = AchievementUtil.GetCompletedAchievementCount(data);
        await PublicProfileService.UpdateMyPublicProfileWithIndexAsync(data, achievementCount);

        return (true, null);
    }

    /// 닉네임을 소문자/트림해서 인덱스 키로 사용
    private static string NormalizeNickname(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick))
            return string.Empty;

        return nick.Trim().ToLowerInvariant();
    }

    /// 라이트 스틱 수량 확인 + 차감
    private static bool TryConsumeLightSticks(SaveDataV1 data, int amount, out string error)
    {
        error = null;

        if (data.itemList == null)
            data.itemList = new Dictionary<int, int>();

        data.itemList.TryGetValue(LightStickItemId, out int current);

        if (current < amount)
        {
            error = "라이트 스틱이 부족합니다.";
            return false;
        }

        data.itemList[LightStickItemId] = current - amount;
        return true;
    }

    
    /// (중복 등으로 실패했을 때) 라이트 스틱 복구
    private static void RefundLightSticks(SaveDataV1 data, int amount)
    {
        if (data.itemList == null)
            data.itemList = new Dictionary<int, int>();

        data.itemList.TryGetValue(LightStickItemId, out int current);
        data.itemList[LightStickItemId] = current + amount;
    }

    /// nicknameReservation/{normalized} 에서 해당 uid로 예약 시도.
    /// 이미 다른 uid가 쓰고 있으면 false.
    /// ★ 중복 체크 전용 노드 (친구 추천용 nicknameIndex와 분리)
    private static async UniTask<bool> ReserveNicknameAsync(string normalized, string uid)
    {
        if (string.IsNullOrEmpty(normalized))
            return false;

        var nickRef = Root.Child("nicknameReservation").Child(normalized);

        bool duplicate = false;

        await nickRef.RunTransaction(mutable =>
        {
            if (mutable.Value == null)
            {
                // 비어 있다 → 내가 선점
                mutable.Value = new Dictionary<string, object>
                {
                    ["uid"] = uid,
                    ["updatedAt"] = ServerValue.Timestamp
                };
            }
            else
            {
                try
                {
                    // 누가 이미 쓰고 있는지 확인
                    if (mutable.Value is IDictionary<string, object> existingDict &&
                        existingDict.TryGetValue("uid", out var existingUidObj))
                    {
                        string existingUid = existingUidObj?.ToString();
                        if (!string.IsNullOrEmpty(existingUid) && existingUid != uid)
                        {
                            // 다른 사람이 선점 중 → 중복
                            duplicate = true;
                            // 기존 값 유지
                        }
                        else
                        {
                            // 같은 유저가 재설정하는 경우 → 그냥 통과
                            mutable.Value = new Dictionary<string, object>
                            {
                                ["uid"] = uid,
                                ["updatedAt"] = ServerValue.Timestamp
                            };
                        }
                    }
                    else
                    {
                        // 형식이 이상해도 그냥 내가 덮어씀
                        mutable.Value = new Dictionary<string, object>
                        {
                            ["uid"] = uid,
                            ["updatedAt"] = ServerValue.Timestamp
                        };
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Nickname] Transaction parse error: {e}");
                    duplicate = true; // 안전하게 막아두자
                }
            }

            return TransactionResult.Success(mutable);
        });

        return !duplicate;
    }

    /// 예전 닉네임 예약 삭제 (해당 uid일 때만)
    /// ★ nicknameReservation 노드에서 삭제
    private static async UniTask RemoveOldNicknameReservationAsync(string normalizedOld, string uid)
    {
        if (string.IsNullOrEmpty(normalizedOld))
            return;

        var nickRef = Root.Child("nicknameReservation").Child(normalizedOld);

        await nickRef.RunTransaction(mutable =>
        {
            if (mutable.Value is IDictionary<string, object> dict &&
                dict.TryGetValue("uid", out var existingUidObj) &&
                existingUidObj?.ToString() == uid)
            {
                // 내가 소유하던 닉이면 삭제
                mutable.Value = null;
            }

            return TransactionResult.Success(mutable);
        });
    }
}
