﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Services;

namespace AdvancedTimeIsland.ViewModels.Main;

/// <summary>
/// 日期/地方时/区时显示视图模型
/// </summary>
public class AdvancedDateViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly System.Timers.Timer _updateTimer;
    private string _exactTime = string.Empty;
    private string _dateDisplay = string.Empty;
    private string _timeDisplay = string.Empty;
    private bool _isDisposed;

    public AdvancedDateViewModel(TimeBaseService timeBaseService)
    {
        _timeBaseService = timeBaseService;
        
        // 初始化时间显示
        UpdateTime();
        
        // 创建定时器，每秒更新一次
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    /// <summary>
    /// 精确时间字符串 (YYYY-MM-DD-hh-mm-ss)
    /// </summary>
    public string ExactTime
    {
        get => _exactTime;
        private set
        {
            if (_exactTime != value)
            {
                _exactTime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 日期显示
    /// </summary>
    public string DateDisplay
    {
        get => _dateDisplay;
        private set
        {
            if (_dateDisplay != value)
            {
                _dateDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 时间显示
    /// </summary>
    public string TimeDisplay
    {
        get => _timeDisplay;
        private set
        {
            if (_timeDisplay != value)
            {
                _timeDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 定时器触发事件
    /// </summary>
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // 使用异步方式更新，避免阻塞
        _ = UpdateTimeAsync();
    }

    /// <summary>
    /// 更新时间显示
    /// </summary>
    private void UpdateTime()
    {
        try
        {
            var now = _timeBaseService.GetCurrentTime();
            
            // 更新精确时间字符串
            ExactTime = now.ToString("yyyy-MM-dd-HH-mm-ss");
            
            // 更新日期显示
            DateDisplay = now.ToString("yyyy年MM月dd日 dddd");
            
            // 更新时间显示
            TimeDisplay = now.ToString("HH:mm:ss");
        }
        catch (Exception)
        {
            // 忽略更新错误
        }
    }

    /// <summary>
    /// 异步更新时间显示
    /// </summary>
    private async System.Threading.Tasks.Task UpdateTimeAsync()
    {
        try
        {
            var now = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
            
            // 更新精确时间字符串
            ExactTime = now.ToString("yyyy-MM-dd-HH-mm-ss");
            
            // 更新日期显示
            DateDisplay = now.ToString("yyyy年MM月dd日 dddd");
            
            // 更新时间显示
            TimeDisplay = now.ToString("HH:mm:ss");
        }
        catch (Exception)
        {
            // 忽略更新错误
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
