using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Actions;

/// <summary>
/// "设置悬浮时间表开关"行动的设置项（WPF 版）。
/// </summary>
public class SetFloatingScheduleActionSettings : INotifyPropertyChanged
{
    private bool _enabled = true;

    /// <summary>
    /// 执行行动时要设置成的悬浮时间表开关状态（默认开启）。
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// "设置悬浮时间表开关"行动的设置界面（WPF 版）：在行动内容右侧显示一个开关。
/// </summary>
public class SetFloatingScheduleActionSettingsControl : ActionSettingsControlBase<SetFloatingScheduleActionSettings>
{
    private ToggleButton _enabledToggle = null!;

    public SetFloatingScheduleActionSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        _enabledToggle = new ToggleButton
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Style = System.Windows.Application.Current?.TryFindResource("MaterialDesignSwitchToggleButton") as Style
        };
        FluentAvaloniaCompatibilityHelper.AddCheckedHandler(_enabledToggle, OnEnabledToggleChanged);
        FluentAvaloniaCompatibilityHelper.AddUncheckedHandler(_enabledToggle, OnEnabledToggleChanged);
        Content = _enabledToggle;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Settings == null) return;
        _enabledToggle.IsChecked = Settings.Enabled;
    }

    private void OnEnabledToggleChanged(object? sender, RoutedEventArgs e)
    {
        if (Settings == null) return;
        Settings.Enabled = _enabledToggle.IsChecked == true;
    }
}
