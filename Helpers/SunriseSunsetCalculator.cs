namespace AdvancedTimeIsland.Helpers;

public static class SunriseSunsetCalculator
{
    private const double Zenith = -0.833;

    public static (TimeSpan Sunrise, TimeSpan Sunset) Calculate(DateTime date, double latitude, double longitude, TimeZoneInfo timeZone)
    {
        var startOfDayLocal = date.Date;

        var n = date.DayOfYear;

        var delta = 23.45 * Math.Sin(ToRadians(360.0 / 365.0 * (284 + n)));

        var B = 360.0 / 365.0 * (n - 81);
        var E = 9.87 * Math.Sin(ToRadians(2 * B)) - 7.53 * Math.Cos(ToRadians(B)) - 1.5 * Math.Sin(ToRadians(B));

        var cosOmega = (Math.Sin(ToRadians(Zenith)) - Math.Sin(ToRadians(latitude)) * Math.Sin(ToRadians(delta))) /
                       (Math.Cos(ToRadians(latitude)) * Math.Cos(ToRadians(delta)));

        if (cosOmega > 1)
            return (TimeSpan.Zero, TimeSpan.Zero);
        if (cosOmega < -1)
            return (TimeSpan.Zero, TimeSpan.FromHours(24));

        var omega = ToDegrees(Math.Acos(cosOmega));

        var sunriseLocalSolar = 12.0 - omega / 15.0;
        var sunsetLocalSolar = 12.0 + omega / 15.0;

        var sunriseLocalMean = sunriseLocalSolar - E / 60.0;
        var sunsetLocalMean = sunsetLocalSolar - E / 60.0;

        var standardMeridian = timeZone.BaseUtcOffset.TotalHours * 15;
        var timeDiffMinutes = (longitude - standardMeridian) * 4;

        var sunriseStandardTime = sunriseLocalMean - timeDiffMinutes / 60.0;
        var sunsetStandardTime = sunsetLocalMean - timeDiffMinutes / 60.0;

        var sunriseTime = startOfDayLocal.AddHours(sunriseStandardTime);
        var sunsetTime = startOfDayLocal.AddHours(sunsetStandardTime);

        return (sunriseTime.TimeOfDay, sunsetTime.TimeOfDay);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private static double ToDegrees(double radians)
    {
        return radians * 180 / Math.PI;
    }
}