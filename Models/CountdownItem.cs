using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
            TargetTimestamp = (long)(Plugin.GetCurrentTime().AddHours(1) - new DateTime(1970, 1, 1)).TotalSeconds,
            EnableNotification = true,
            NotificationTitle = "倒计时到达",
            NotificationContent = "目标时间已到达！",
            NotificationMaskDurationSeconds = 3,
            NotificationOverlayDurationSeconds = 10,
            IsCompleted = false
        };
    }
}