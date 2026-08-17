using System;
using System.Text;

namespace AdvancedTimeIsland.Helpers;

public static class OptimizedTimeFormatter
{
    private const int BufferCapacity = 256;

    public static string FormatTime(string format, long secondsLeft, double millisecondsLeft, DateTime now, DateTime targetDate, long startTime, long targetTime, bool enableTimeCorrection)
    {
        var totalSeconds = secondsLeft;
        var totalMilliseconds = millisecondsLeft;
        var totalMinutes = (long)Math.Ceiling(totalSeconds / 60.0);
        var totalHours = (long)Math.Ceiling(totalSeconds / 3600.0);
        var totalDays = (long)Math.Ceiling(totalSeconds / 86400.0);

        var days = (int)(totalSeconds / 86400);
        var remainingSeconds = totalSeconds % 86400;
        var hours = (int)(remainingSeconds / 3600);
        remainingSeconds %= 3600;
        var minutes = (int)(remainingSeconds / 60);
        var seconds = (int)(remainingSeconds % 60);
        var milliseconds = (int)(totalMilliseconds % 1000);

        bool hasMillisecond = format.IndexOf('%') >= 0;
        if (hasMillisecond)
        {
            hasMillisecond = format.Contains("%x") || format.Contains("%X");
        }

        if (enableTimeCorrection && !hasMillisecond && secondsLeft > 0)
        {
            bool hasSeconds = format.Contains("%s") || format.Contains("%S");
            bool hasMinutes = format.Contains("%m") || format.Contains("%M");
            bool hasHours = format.Contains("%h") || format.Contains("%H");
            bool hasDays = format.Contains("%d") || format.Contains("%D");

            if (hasSeconds)
            {
                seconds++;
                if (seconds >= 60)
                {
                    seconds = 0;
                    minutes++;
                    if (minutes >= 60)
                    {
                        minutes = 0;
                        hours++;
                        if (hours >= 24)
                        {
                            hours = 0;
                            days++;
                        }
                    }
                }
            }
            else if (hasMinutes)
            {
                minutes++;
                if (minutes >= 60)
                {
                    minutes = 0;
                    hours++;
                    if (hours >= 24)
                    {
                        hours = 0;
                        days++;
                    }
                }
            }
            else if (hasHours)
            {
                hours++;
                if (hours >= 24)
                {
                    hours = 0;
                    days++;
                }
            }
            else if (hasDays)
            {
                days++;
            }
        }

        var totalDuration = targetTime - startTime;
        var elapsedSeconds = targetTime - startTime - secondsLeft;

        string remainingPercent = "0";
        string elapsedPercent = "0";
        string elapsedPercentDecimal = "0.00";

        if (totalDuration > 0)
        {
            remainingPercent = ((int)(secondsLeft * 100.0 / totalDuration)).ToString();
            elapsedPercent = ((int)(elapsedSeconds * 100.0 / totalDuration)).ToString();
            elapsedPercentDecimal = (elapsedSeconds * 100.0 / totalDuration).ToString("F2");
        }

        bool hasMonth = format.Contains("%mo") || format.Contains("%MO");
        bool hasYear = format.Contains("%yy") || format.Contains("%YY");

        int displayYears = 0;
        int displayMonths = 0;
        int displayDays = days;

        if (hasYear || hasMonth)
        {
            var tempDate = now;
            displayYears = 0;

            while (tempDate.AddYears(1) <= targetDate)
            {
                tempDate = tempDate.AddYears(1);
                displayYears++;
            }

            if (hasMonth)
            {
                displayMonths = 0;
                while (tempDate.AddMonths(1) <= targetDate)
                {
                    tempDate = tempDate.AddMonths(1);
                    displayMonths++;
                }

                var dayDiff = (targetDate - tempDate).Days;
                displayDays = Math.Max(0, dayDiff);
            }
            else
            {
                var dayDiff = (targetDate - tempDate).Days;
                displayDays = Math.Max(0, dayDiff);
            }
        }

        var yy = displayYears.ToString();
        var mo = ((int)(totalSeconds / (30.4375 * 86400.0))).ToString();
        var YY = (totalSeconds / (365.25 * 86400.0)).ToString("F2");
        var MO = (totalSeconds / (30.4375 * 86400.0)).ToString("F2");

        var sb = new StringBuilder(format, BufferCapacity);
        ReplaceAll(sb, "%D", ((int)totalDays).ToString());
        ReplaceAll(sb, "%H", ((int)totalHours).ToString());
        ReplaceAll(sb, "%M", totalMinutes.ToString());
        ReplaceAll(sb, "%S", totalSeconds.ToString());
        ReplaceAll(sb, "%X", ((int)totalMilliseconds).ToString());
        ReplaceAll(sb, "%L", remainingPercent);
        ReplaceAll(sb, "%P", elapsedPercent);
        ReplaceAll(sb, "%p", elapsedPercentDecimal);
        ReplaceAll(sb, "%yy", yy);
        ReplaceAll(sb, "%YY", YY);
        ReplaceAll(sb, "%mo", displayMonths.ToString());
        ReplaceAll(sb, "%MO", displayMonths.ToString());
        ReplaceAll(sb, "%d", displayDays.ToString());
        ReplaceAll(sb, "%h", hours.ToString());
        ReplaceAll(sb, "%m", minutes.ToString("D2"));
        ReplaceAll(sb, "%s", seconds.ToString("D2"));
        ReplaceAll(sb, "%x", milliseconds.ToString("D3"));

        return sb.ToString();
    }

    public static string FormatForwardTime(string format, long secondsElapsed, double millisecondsElapsed, DateTime startTimeDate, DateTime now)
    {
        var totalSeconds = secondsElapsed;
        var totalMilliseconds = millisecondsElapsed;
        var totalMinutes = (long)Math.Ceiling(totalSeconds / 60.0);
        var totalHours = (long)Math.Ceiling(totalSeconds / 3600.0);
        var totalDays = (long)Math.Ceiling(totalSeconds / 86400.0);

        var days = (int)(totalSeconds / 86400);
        var remainingSeconds = totalSeconds % 86400;
        var hours = (int)(remainingSeconds / 3600);
        remainingSeconds %= 3600;
        var minutes = (int)(remainingSeconds / 60);
        var seconds = (int)(remainingSeconds % 60);
        var milliseconds = (int)(totalMilliseconds % 1000);

        var totalDuration = totalSeconds;

        string remainingPercent = "0";
        string elapsedPercent = "0";
        string elapsedPercentDecimal = "0.00";

        if (totalDuration > 0)
        {
            elapsedPercent = "100";
            elapsedPercentDecimal = "100.00";
            remainingPercent = "0";
        }

        bool hasMonth = format.Contains("%mo") || format.Contains("%MO");
        bool hasYear = format.Contains("%yy") || format.Contains("%YY");

        int displayYears = 0;
        int displayMonths = 0;
        int displayDays = days;

        if (hasYear || hasMonth)
        {
            var tempDate = startTimeDate;
            displayYears = 0;

            while (LunarHelper.SolarAddYears(tempDate, 1) <= now)
            {
                tempDate = LunarHelper.SolarAddYears(tempDate, 1);
                displayYears++;
            }

            if (hasMonth)
            {
                displayMonths = 0;
                while (LunarHelper.SolarAddMonths(tempDate, 1) <= now)
                {
                    tempDate = LunarHelper.SolarAddMonths(tempDate, 1);
                    displayMonths++;
                }

                var dayDiff = (int)Math.Floor(LunarHelper.DaysBetween(tempDate, now));
                displayDays = Math.Max(0, dayDiff);
            }
            else
            {
                var dayDiff = (int)Math.Floor(LunarHelper.DaysBetween(tempDate, now));
                displayDays = Math.Max(0, dayDiff);
            }
        }

        var yy = displayYears.ToString();
        var mo = ((int)(totalSeconds / (30.4375 * 86400.0))).ToString();
        var YY = (totalSeconds / (365.25 * 86400.0)).ToString("F2");
        var MO = (totalSeconds / (30.4375 * 86400.0)).ToString("F2");

        var sb = new StringBuilder(format, BufferCapacity);
        ReplaceAll(sb, "%D", ((int)totalDays).ToString());
        ReplaceAll(sb, "%H", ((int)totalHours).ToString());
        ReplaceAll(sb, "%M", totalMinutes.ToString());
        ReplaceAll(sb, "%S", totalSeconds.ToString());
        ReplaceAll(sb, "%X", ((int)totalMilliseconds).ToString());
        ReplaceAll(sb, "%L", remainingPercent);
        ReplaceAll(sb, "%P", elapsedPercent);
        ReplaceAll(sb, "%p", elapsedPercentDecimal);
        ReplaceAll(sb, "%yy", yy);
        ReplaceAll(sb, "%YY", YY);
        ReplaceAll(sb, "%mo", displayMonths.ToString());
        ReplaceAll(sb, "%MO", displayMonths.ToString());
        ReplaceAll(sb, "%d", displayDays.ToString());
        ReplaceAll(sb, "%h", hours.ToString());
        ReplaceAll(sb, "%m", minutes.ToString("D2"));
        ReplaceAll(sb, "%s", seconds.ToString("D2"));
        ReplaceAll(sb, "%x", milliseconds.ToString("D3"));

        return sb.ToString();
    }

    public static string FormatPeriodicCountdownTime(string format, long secondsLeft, double millisecondsLeft, DateTime now, DateTime targetDate, bool enableTimeCorrection)
    {
        var totalSeconds = secondsLeft;
        var totalMilliseconds = millisecondsLeft;
        var totalMinutes = (long)Math.Ceiling(totalSeconds / 60.0);
        var totalHours = (long)Math.Ceiling(totalSeconds / 3600.0);
        var totalDays = (long)Math.Ceiling(totalSeconds / 86400.0);

        var days = (int)(totalSeconds / 86400);
        var remainingSeconds = totalSeconds % 86400;
        var hours = (int)(remainingSeconds / 3600);
        remainingSeconds %= 3600;
        var minutes = (int)(remainingSeconds / 60);
        var seconds = (int)(remainingSeconds % 60);
        var milliseconds = (int)(totalMilliseconds % 1000);

        bool hasMillisecond = format.IndexOf('%') >= 0;
        if (hasMillisecond)
        {
            hasMillisecond = format.Contains("%x") || format.Contains("%X");
        }

        if (enableTimeCorrection && !hasMillisecond && secondsLeft > 0)
        {
            bool hasSeconds = format.Contains("%s") || format.Contains("%S");
            bool hasMinutes = format.Contains("%m") || format.Contains("%M");
            bool hasHours = format.Contains("%h") || format.Contains("%H");
            bool hasDays = format.Contains("%d") || format.Contains("%D");

            if (hasSeconds)
            {
                seconds++;
                if (seconds >= 60)
                {
                    seconds = 0;
                    minutes++;
                    if (minutes >= 60)
                    {
                        minutes = 0;
                        hours++;
                        if (hours >= 24)
                        {
                            hours = 0;
                            days++;
                        }
                    }
                }
            }
            else if (hasMinutes)
            {
                minutes++;
                if (minutes >= 60)
                {
                    minutes = 0;
                    hours++;
                    if (hours >= 24)
                    {
                        hours = 0;
                        days++;
                    }
                }
            }
            else if (hasHours)
            {
                hours++;
                if (hours >= 24)
                {
                    hours = 0;
                    days++;
                }
            }
            else if (hasDays)
            {
                days++;
            }
        }

        bool hasMonth = format.Contains("%mo") || format.Contains("%MO");
        bool hasYear = format.Contains("%yy") || format.Contains("%YY");

        int displayYears = 0;
        int displayMonths = 0;
        int displayDays = days;

        if (hasYear || hasMonth)
        {
            var tempDate = now;
            displayYears = 0;

            while (tempDate.AddYears(1) <= targetDate)
            {
                tempDate = tempDate.AddYears(1);
                displayYears++;
            }

            if (hasMonth)
            {
                displayMonths = 0;
                while (tempDate.AddMonths(1) <= targetDate)
                {
                    tempDate = tempDate.AddMonths(1);
                    displayMonths++;
                }

                var dayDiff = (targetDate - tempDate).Days;
                displayDays = Math.Max(0, dayDiff);
            }
            else
            {
                var dayDiff = (targetDate - tempDate).Days;
                displayDays = Math.Max(0, dayDiff);
            }
        }

        var yy = displayYears.ToString();
        var mo = ((int)(totalSeconds / (30.4375 * 86400.0))).ToString();
        var YY = (totalSeconds / (365.25 * 86400.0)).ToString("F2");
        var MO = (totalSeconds / (30.4375 * 86400.0)).ToString("F2");

        var sb = new StringBuilder(format, BufferCapacity);
        ReplaceAll(sb, "%D", ((int)totalDays).ToString());
        ReplaceAll(sb, "%H", ((int)totalHours).ToString());
        ReplaceAll(sb, "%M", totalMinutes.ToString());
        ReplaceAll(sb, "%S", totalSeconds.ToString());
        ReplaceAll(sb, "%X", ((int)totalMilliseconds).ToString());
        ReplaceAll(sb, "%L", "0");
        ReplaceAll(sb, "%P", "0");
        ReplaceAll(sb, "%p", "0.00");
        ReplaceAll(sb, "%yy", yy);
        ReplaceAll(sb, "%YY", YY);
        ReplaceAll(sb, "%mo", displayMonths.ToString());
        ReplaceAll(sb, "%MO", displayMonths.ToString());
        ReplaceAll(sb, "%d", displayDays.ToString());
        ReplaceAll(sb, "%h", hours.ToString());
        ReplaceAll(sb, "%m", minutes.ToString("D2"));
        ReplaceAll(sb, "%s", seconds.ToString("D2"));
        ReplaceAll(sb, "%x", milliseconds.ToString("D3"));

        return sb.ToString();
    }

    public static string FormatLunarCountdownTime(string format, long secondsLeft, double millisecondsLeft, DateTime now, DateTime targetDate)
    {
        var totalSeconds = secondsLeft;
        var totalMilliseconds = millisecondsLeft;
        var totalMinutes = (long)Math.Ceiling(totalSeconds / 60.0);
        var totalHours = (long)Math.Ceiling(totalSeconds / 3600.0);
        var totalDays = (long)Math.Ceiling(totalSeconds / 86400.0);

        var days = (int)(totalSeconds / 86400);
        var remainingSeconds = totalSeconds % 86400;
        var hours = (int)(remainingSeconds / 3600);
        remainingSeconds %= 3600;
        var minutes = (int)(remainingSeconds / 60);
        var seconds = (int)(remainingSeconds % 60);
        var milliseconds = (int)(totalMilliseconds % 1000);

        bool hasMonth = format.Contains("%mo") || format.Contains("%MO");
        bool hasYear = format.Contains("%yy") || format.Contains("%YY");

        int displayYears = 0;
        int displayMonths = 0;
        int displayDays = days;

        if (hasYear || hasMonth)
        {
            var tempDate = now;
            displayYears = 0;

            while (tempDate.AddYears(1) <= targetDate)
            {
                tempDate = tempDate.AddYears(1);
                displayYears++;
            }

            if (hasMonth)
            {
                displayMonths = 0;
                while (tempDate.AddMonths(1) <= targetDate)
                {
                    tempDate = tempDate.AddMonths(1);
                    displayMonths++;
                }

                var dayDiff = (targetDate - tempDate).Days;
                displayDays = Math.Max(0, dayDiff);
            }
            else
            {
                var dayDiff = (targetDate - tempDate).Days;
                displayDays = Math.Max(0, dayDiff);
            }
        }

        var yy = displayYears.ToString();
        var mo = ((int)(totalSeconds / (30.4375 * 86400.0))).ToString();
        var YY = (totalSeconds / (365.25 * 86400.0)).ToString("F2");
        var MO = (totalSeconds / (30.4375 * 86400.0)).ToString("F2");

        var sb = new StringBuilder(format, BufferCapacity);
        ReplaceAll(sb, "%D", ((int)totalDays).ToString());
        ReplaceAll(sb, "%H", ((int)totalHours).ToString());
        ReplaceAll(sb, "%M", totalMinutes.ToString());
        ReplaceAll(sb, "%S", totalSeconds.ToString());
        ReplaceAll(sb, "%X", ((int)totalMilliseconds).ToString());
        ReplaceAll(sb, "%L", "0");
        ReplaceAll(sb, "%P", "0");
        ReplaceAll(sb, "%p", "0.00");
        ReplaceAll(sb, "%yy", yy);
        ReplaceAll(sb, "%YY", YY);
        ReplaceAll(sb, "%mo", displayMonths.ToString());
        ReplaceAll(sb, "%MO", displayMonths.ToString());
        ReplaceAll(sb, "%d", displayDays.ToString());
        ReplaceAll(sb, "%h", hours.ToString());
        ReplaceAll(sb, "%m", minutes.ToString("D2"));
        ReplaceAll(sb, "%s", seconds.ToString("D2"));
        ReplaceAll(sb, "%x", milliseconds.ToString("D3"));

        return sb.ToString();
    }

    private static void ReplaceAll(StringBuilder sb, string oldValue, string newValue)
    {
        int index = sb.ToString().IndexOf(oldValue, StringComparison.Ordinal);
        while (index >= 0)
        {
            sb.Remove(index, oldValue.Length);
            sb.Insert(index, newValue);
            index = sb.ToString().IndexOf(oldValue, index + newValue.Length, StringComparison.Ordinal);
        }
    }
}