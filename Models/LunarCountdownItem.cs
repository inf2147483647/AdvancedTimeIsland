using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Models;

public class LunarCountdownItem : INotifyPropertyChanged
{
    private Guid _id;
    private string _name = "新农历倒计时";
    private int _lunarYear;
    private int _lunarMonth;
    private bool _isLeapMonth;
    private int _lunarDay;
    private int _hour;
    private int _minute;
    private int _second;
    private bool _enableNotification;
    private string _notificationTitle = "农历倒计时到达";
    private string _notificationContent = "目标农历日期已到达！";
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

    public int LunarYear
    {
        get => _lunarYear;
        set
        {
            if (_lunarYear != value)
            {
                _lunarYear = value;
                OnPropertyChanged();
            }
        }
    }

    public int LunarMonth
    {
        get => _lunarMonth;
        set
        {
            if (_lunarMonth != value)
            {
                _lunarMonth = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLeapMonth
    {
        get => _isLeapMonth;
        set
        {
            if (_isLeapMonth != value)
            {
                _isLeapMonth = value;
                OnPropertyChanged();
            }
        }
    }

    public int LunarDay
    {
        get => _lunarDay;
        set
        {
            if (_lunarDay != value)
            {
                _lunarDay = value;
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

    public long GetTargetTimestamp()
    {
        var solarDate = LunarCalendarHelper.LunarToSolar(LunarYear, LunarMonth, IsLeapMonth, LunarDay, Hour, Minute, Second);
        if (solarDate.HasValue)
        {
            return UnixTimeHelper.ToUnixTimestamp(solarDate.Value);
        }
        return 0;
    }

    public void SetFromTimestamp(long timestamp)
    {
        var solarDate = UnixTimeHelper.FromUnixTimestamp(timestamp);
        LunarYear = LunarCalendarHelper.GetLunarYear(solarDate);
        LunarMonth = LunarCalendarHelper.GetLunarMonth(solarDate);
        IsLeapMonth = LunarCalendarHelper.IsLeapMonth(solarDate);
        LunarDay = LunarCalendarHelper.GetLunarDay(solarDate);
        Hour = solarDate.Hour;
        Minute = solarDate.Minute;
        Second = solarDate.Second;
    }

    public static LunarCountdownItem CreateDefault()
    {
        var now = DateTime.Now;
        var lunarYear = LunarCalendarHelper.GetLunarYear(now);
        var lunarMonth = LunarCalendarHelper.GetLunarMonth(now);
        var lunarDay = LunarCalendarHelper.GetLunarDay(now);

        var targetDate = now.AddMonths(1);
        var targetLunarYear = LunarCalendarHelper.GetLunarYear(targetDate);
        var targetLunarMonth = LunarCalendarHelper.GetLunarMonth(targetDate);
        var targetLunarDay = LunarCalendarHelper.GetLunarDay(targetDate);

        return new LunarCountdownItem
        {
            Id = Guid.NewGuid(),
            Name = "新农历倒计时",
            LunarYear = targetLunarYear,
            LunarMonth = targetLunarMonth,
            IsLeapMonth = false,
            LunarDay = targetLunarDay,
            Hour = 0,
            Minute = 0,
            Second = 0,
            EnableNotification = true,
            NotificationTitle = "农历倒计时到达",
            NotificationContent = "目标农历日期已到达！",
            NotificationMaskDurationSeconds = 3,
            NotificationOverlayDurationSeconds = 10,
            IsCompleted = false
        };
    }
}
