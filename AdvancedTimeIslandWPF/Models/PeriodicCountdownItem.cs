using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

public enum PeriodType
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public class PeriodicCountdownItem : INotifyPropertyChanged
{
    private Guid _id;
    private string _name = "新周期性倒计时";
    private PeriodType _periodType = PeriodType.Daily;
    private int _hour = 0;
    private int _minute = 0;
    private int _second = 0;
    private int _dayOfWeek = 1;
    private int _dayOfMonth = 1;
    private int _month = 1;
    private bool _enableNotification;
    private string _notificationTitle = "周期性倒计时到达";
    private string _notificationContent = "目标时间已到达！";
    private int _notificationMaskDurationSeconds = 3;
    private int _notificationOverlayDurationSeconds = 10;
    private bool _isCompleted;

    public Guid Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public PeriodType PeriodType
    {
        get => _periodType;
        set
        {
            if (_periodType != value)
            {
                _periodType = value;
                OnPropertyChanged();
            }
        }
    }

    public int Hour
    {
        get => _hour;
        set
        {
            if (_hour != value)
            {
                _hour = value;
                OnPropertyChanged();
            }
        }
    }

    public int Minute
    {
        get => _minute;
        set
        {
            if (_minute != value)
            {
                _minute = value;
                OnPropertyChanged();
            }
        }
    }

    public int Second
    {
        get => _second;
        set
        {
            if (_second != value)
            {
                _second = value;
                OnPropertyChanged();
            }
        }
    }

    public int DayOfWeek
    {
        get => _dayOfWeek;
        set
        {
            if (_dayOfWeek != value)
            {
                _dayOfWeek = value;
                OnPropertyChanged();
            }
        }
    }

    public int DayOfMonth
    {
        get => _dayOfMonth;
        set
        {
            if (_dayOfMonth != value)
            {
                _dayOfMonth = value;
                OnPropertyChanged();
            }
        }
    }

    public int Month
    {
        get => _month;
        set
        {
            if (_month != value)
            {
                _month = value;
                OnPropertyChanged();
            }
        }
    }

    public bool EnableNotification
    {
        get => _enableNotification;
        set
        {
            if (_enableNotification != value)
            {
                _enableNotification = value;
                OnPropertyChanged();
            }
        }
    }

    public string NotificationTitle
    {
        get => _notificationTitle;
        set
        {
            if (_notificationTitle != value)
            {
                _notificationTitle = value;
                OnPropertyChanged();
            }
        }
    }

    public string NotificationContent
    {
        get => _notificationContent;
        set
        {
            if (_notificationContent != value)
            {
                _notificationContent = value;
                OnPropertyChanged();
            }
        }
    }

    public int NotificationMaskDurationSeconds
    {
        get => _notificationMaskDurationSeconds;
        set
        {
            if (_notificationMaskDurationSeconds != value)
            {
                _notificationMaskDurationSeconds = Math.Max(1, Math.Min(60, value));
                OnPropertyChanged();
            }
        }
    }

    public int NotificationOverlayDurationSeconds
    {
        get => _notificationOverlayDurationSeconds;
        set
        {
            if (_notificationOverlayDurationSeconds != value)
            {
                _notificationOverlayDurationSeconds = Math.Max(1, Math.Min(60, value));
                OnPropertyChanged();
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (_isCompleted != value)
            {
                _isCompleted = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public long GetNextTargetTimestamp(DateTime now)
    {
        DateTime targetDate;
        var timeOfDay = new TimeSpan(Hour, Minute, Second);

        switch (PeriodType)
        {
            case PeriodType.Hourly:
                var nextHour = now.AddHours(1);
                targetDate = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, Minute, Second);
                break;
            case PeriodType.Daily:
                targetDate = now.Date.Add(timeOfDay);
                if (targetDate <= now)
                {
                    targetDate = targetDate.AddDays(1);
                }
                break;
            case PeriodType.Weekly:
                var daysToAdd = (DayOfWeek - (int)now.DayOfWeek + 7) % 7;
                if (daysToAdd == 0 && now.TimeOfDay >= timeOfDay)
                {
                    daysToAdd = 7;
                }
                targetDate = now.Date.AddDays(daysToAdd).Add(timeOfDay);
                break;
            case PeriodType.Monthly:
                var currentMonth = now.Month;
                var currentYear = now.Year;
                var targetMonth = currentMonth;
                var targetYear = currentYear;
                var targetDay = DayOfMonth;

                var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);
                targetDay = Math.Min(targetDay, daysInMonth);

                targetDate = new DateTime(targetYear, targetMonth, targetDay).Add(timeOfDay);
                if (targetDate <= now)
                {
                    targetMonth++;
                    if (targetMonth > 12)
                    {
                        targetMonth = 1;
                        targetYear++;
                    }
                    daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
                    targetDay = Math.Min(DayOfMonth, daysInMonth);
                    targetDate = new DateTime(targetYear, targetMonth, targetDay).Add(timeOfDay);
                }
                break;
            case PeriodType.Yearly:
                targetDate = new DateTime(now.Year, Month, Math.Min(DayOfMonth, DateTime.DaysInMonth(now.Year, Month))).Add(timeOfDay);
                if (targetDate <= now)
                {
                    targetDate = new DateTime(now.Year + 1, Month, Math.Min(DayOfMonth, DateTime.DaysInMonth(now.Year + 1, Month))).Add(timeOfDay);
                }
                break;
            default:
                targetDate = now.AddHours(1);
                break;
        }

        return (long)(targetDate.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public long GetPreviousTargetTimestamp(DateTime now)
    {
        var nextTargetTimestamp = GetNextTargetTimestamp(now);
        var timeOfDay = new TimeSpan(Hour, Minute, Second);

        DateTime previousTargetDate;
        switch (PeriodType)
        {
            case PeriodType.Hourly:
                previousTargetDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, Minute, Second);
                break;
            case PeriodType.Daily:
                previousTargetDate = now.Date.Add(timeOfDay);
                if (previousTargetDate > now)
                {
                    previousTargetDate = previousTargetDate.AddDays(-1);
                }
                break;
            case PeriodType.Weekly:
                var daysToAdd = (DayOfWeek - (int)now.DayOfWeek + 7) % 7;
                previousTargetDate = now.Date.AddDays(daysToAdd).Add(timeOfDay);
                if (previousTargetDate > now)
                {
                    previousTargetDate = previousTargetDate.AddDays(-7);
                }
                break;
            case PeriodType.Monthly:
                var nextTargetDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(nextTargetTimestamp).ToLocalTime();
                previousTargetDate = nextTargetDate.AddMonths(-1);
                var daysInPrevMonth = DateTime.DaysInMonth(previousTargetDate.Year, previousTargetDate.Month);
                var prevDay = Math.Min(DayOfMonth, daysInPrevMonth);
                previousTargetDate = new DateTime(previousTargetDate.Year, previousTargetDate.Month, prevDay).Add(timeOfDay);
                break;
            case PeriodType.Yearly:
                var nextTargetDateY = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(nextTargetTimestamp).ToLocalTime();
                previousTargetDate = nextTargetDateY.AddYears(-1);
                var daysInPrevYearMonth = DateTime.DaysInMonth(previousTargetDate.Year, previousTargetDate.Month);
                var prevDayY = Math.Min(DayOfMonth, daysInPrevYearMonth);
                previousTargetDate = new DateTime(previousTargetDate.Year, previousTargetDate.Month, prevDayY).Add(timeOfDay);
                break;
            default:
                previousTargetDate = now.Date.Add(timeOfDay);
                break;
        }

        return (long)(previousTargetDate.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public long GetPeriodSeconds(DateTime now)
    {
        switch (PeriodType)
        {
            case PeriodType.Hourly:
                return 3600;
            case PeriodType.Daily:
                return 86400;
            case PeriodType.Weekly:
                return 604800;
            case PeriodType.Monthly:
                return (long)DateTime.DaysInMonth(now.Year, now.Month) * 86400L;
            case PeriodType.Yearly:
                return DateTime.IsLeapYear(now.Year) ? 31622400L : 31536000L;
            default:
                return 86400;
        }
    }

    public long GetCycleStartTimeStamp(DateTime now)
    {
        DateTime cycleStart;
        var timeOfDay = new TimeSpan(Hour, Minute, Second);

        switch (PeriodType)
        {
            case PeriodType.Hourly:
                cycleStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
                break;
            case PeriodType.Daily:
                cycleStart = now.Date;
                break;
            case PeriodType.Weekly:
                var daysToStart = ((int)now.DayOfWeek - DayOfWeek + 7) % 7;
                if (daysToStart < 0) daysToStart += 7;
                cycleStart = now.Date.AddDays(-daysToStart);
                break;
            case PeriodType.Monthly:
                cycleStart = new DateTime(now.Year, now.Month, 1);
                break;
            case PeriodType.Yearly:
                cycleStart = new DateTime(now.Year, 1, 1);
                break;
            default:
                cycleStart = now.Date;
                break;
        }

        return (long)(cycleStart.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public static PeriodicCountdownItem CreateDefault()
    {
        var now = DateTime.Now;
        return new PeriodicCountdownItem
        {
            Id = Guid.NewGuid(),
            Name = "新周期性倒计时",
            PeriodType = PeriodType.Daily,
            Hour = now.Hour + 1,
            Minute = 0,
            Second = 0,
            DayOfWeek = 1,
            DayOfMonth = 1,
            Month = 1,
            EnableNotification = true,
            NotificationTitle = "周期性倒计时到达",
            NotificationContent = "目标时间已到达！",
            NotificationMaskDurationSeconds = 3,
            NotificationOverlayDurationSeconds = 10,
            IsCompleted = false
        };
    }
}