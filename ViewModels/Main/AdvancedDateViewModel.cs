using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.ViewModels.Main;

public partial class AdvancedDateViewModel : INotifyPropertyChanged
{
    private readonly TimeBaseService _timeBaseService;
    private readonly System.Timers.Timer _updateTimer;
    private DateTime _currentTime;
    private string _formattedDate = string.Empty;

    public DateTime CurrentTime
    {
        get => _currentTime;
        set
        {
            _currentTime = value;
            OnPropertyChanged();
            UpdateFormattedDate();
        }
    }

    public string FormattedDate
    {
        get => _formattedDate;
        set
        {
            _formattedDate = value;
            OnPropertyChanged();
        }
    }

    public AdvancedDateViewModel(TimeBaseService timeBaseService)
    {
        _timeBaseService = timeBaseService;
        
        // 初始化定时器，每秒更新一次时间，明确指定System.Timers.Timer避免冲突
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += UpdateTime;
        _updateTimer.AutoReset = true;
        _updateTimer.Start();
        
        // 初始更新一次
        UpdateTime(null, null);
    }

    private void UpdateTime(object? sender, System.Timers.ElapsedEventArgs? e)
    {
        CurrentTime = _timeBaseService.GetCurrentTime();
    }

    private void UpdateFormattedDate()
    {
        // 格式化日期：周几/星期几 年-月-日（YYYY-MM-DD）
        // 对应格式：ddd 是星期简称（如周一），dddd 是星期全称（如星期一）
        FormattedDate = CurrentTime.ToString("ddd/dddd yyyy-MM-dd");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _updateTimer.Stop();
        _updateTimer.Dispose();
    }
}