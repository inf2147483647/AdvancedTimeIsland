using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Models;

public class CountdownItem : INotifyPropertyChanged
{
    private Guid _id;
    private string _name = string.Empty;
    private long _targetTimestamp;
    private bool _enableNotification;
    private string _notificationTitle = string.Empty;
    private string _notificationContent = string.Empty;
    private int _notificationMaskDurationSeconds = 3;
    private int _notificationOverlayDurationSeconds = 10;
    private bool _isCompleted;
    private bool _countOnlyInSchoolTime;

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

    public long TargetTimestamp
    {
        get => _targetTimestamp;
        set
        {
            if (_targetTimestamp != value)
            {
                _targetTimestamp = value;
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

    /// <summary>
    /// 仅计在校时长：开启后倒计时显示的剩余时间只累计「在校时间」——
    /// 每个在校日内仅统计当天课表第一节课开始到最后一节课下课之间的时段，
    /// 非在校日（周末/节假日/寒暑假）与在校时段之外倒计时暂停递减。
    /// 仅影响显示值，到期通知仍在真实时间到达目标时触发。
    /// </summary>
    public bool CountOnlyInSchoolTime
    {
        get => _countOnlyInSchoolTime;
        set
        {
            if (_countOnlyInSchoolTime != value)
            {
                _countOnlyInSchoolTime = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static CountdownItem CreateDefault()
    {
        return new CountdownItem
        {
            Id = Guid.NewGuid(),
            Name = "新倒计时",
            TargetTimestamp = (long)(LunarHelper.SolarAddHours(Plugin.GetCurrentTime(), 1) - new DateTime(1970, 1, 1)).TotalSeconds,
            EnableNotification = true,
            NotificationTitle = "倒计时到达",
            NotificationContent = "目标时间已到达！",
            NotificationMaskDurationSeconds = 3,
            NotificationOverlayDurationSeconds = 10,
            IsCompleted = false
        };
    }

    /// <summary>创建副本（参考档案编辑的「克隆」，分配新 Id 避免冲突）。</summary>
    public CountdownItem Clone()
    {
        return new CountdownItem
        {
            Id = Guid.NewGuid(),
            Name = Name,
            TargetTimestamp = TargetTimestamp,
            EnableNotification = EnableNotification,
            NotificationTitle = NotificationTitle,
            NotificationContent = NotificationContent,
            NotificationMaskDurationSeconds = NotificationMaskDurationSeconds,
            NotificationOverlayDurationSeconds = NotificationOverlayDurationSeconds,
            CountOnlyInSchoolTime = CountOnlyInSchoolTime,
            IsCompleted = false
        };
    }
}


