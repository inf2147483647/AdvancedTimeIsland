using System;

namespace AdvancedTimeIsland.Helpers;

public static class UnixTimeHelper
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long ToUnixTimeSeconds(this DateTime dateTime)
    {
        var utcTime = dateTime.ToUniversalTime();
        return (long)(utcTime - UnixEpoch).TotalSeconds;
    }

    public static DateTime FromUnixTimeSeconds(long unixTime)
    {
        return UnixEpoch.AddSeconds(unixTime).ToLocalTime();
    }

    public static double ToUnixTimeSecondsWithMs(this DateTime dateTime)
    {
        var utcTime = dateTime.ToUniversalTime();
        return (utcTime - UnixEpoch).TotalSeconds;
    }

    public static DateTime FromUnixTimeSecondsWithMs(double unixTime)
    {
        return UnixEpoch.AddSeconds(unixTime).ToLocalTime();
    }
}