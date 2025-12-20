using System;

public static class TimeFormatUtil
{
    /// <summary>
    /// Unix milliseconds를 약어 형식으로 변환 (NOW, 5M, 2H, 3D, 2W, 3MO, 1Y)
    /// </summary>
    public static string FormatLastLogin(long unixMillis)
    {
        if (unixMillis <= 0)
            return "NOW";

        var lastLogin = DateTimeOffset.FromUnixTimeMilliseconds(unixMillis).LocalDateTime;
        var now = DateTime.Now;
        var diff = now - lastLogin;

        // 미래 시간이거나 1분 미만
        if (diff.TotalMinutes < 1)
            return "NOW";

        // 1시간 미만: 분 단위
        if (diff.TotalHours < 1)
            return $"{(int)diff.TotalMinutes}M";

        // 24시간 미만: 시간 단위
        if (diff.TotalDays < 1)
            return $"{(int)diff.TotalHours}H";

        // 7일 미만: 일 단위
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}D";

        // 4주 미만: 주 단위
        if (diff.TotalDays < 28)
            return $"{(int)(diff.TotalDays / 7)}W";

        // 12개월 미만: 월 단위
        if (diff.TotalDays < 365)
            return $"{(int)(diff.TotalDays / 30)}MO";

        // 1년 이상: 년 단위
        return $"{(int)(diff.TotalDays / 365)}Y";
    }
}
