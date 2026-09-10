using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Actions;

/// <summary>
/// 行动：设置悬浮时间表开关。执行时把插件的"启用悬浮时间表"开关设为设置界面中开关的状态。
/// </summary>
[ActionInfo("advancedtimeisland.set_floating_schedule", "设置悬浮时间表开关", "\uef27")]
public class SetFloatingScheduleAction : ActionBase<SetFloatingScheduleActionSettings>
{
    protected override async Task OnInvoke()
    {
        await base.OnInvoke();

        var pluginSettings = Plugin.Instance?.Settings;
        if (pluginSettings == null) return;

        // FloatingScheduleService 订阅该属性变更并在 UI 线程上开/关悬浮窗，此处直接赋值即可。
        pluginSettings.EnableFloatingSchedule = Settings.Enabled;
    }
}

/// <summary>
/// "设置悬浮时间表开关"行动的设置项。
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
/// "设置悬浮时间表开关"行动的设置界面：在行动内容右侧显示一个开关。
/// </summary>
public class SetFloatingScheduleActionSettingsControl : ActionSettingsControlBase<SetFloatingScheduleActionSettings>
{
    private ToggleSwitch _enabledToggle = null!;

    public SetFloatingScheduleActionSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        _enabledToggle = new ToggleSwitch
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        _enabledToggle.IsCheckedChanged += OnEnabledToggleChanged;
        Content = _enabledToggle;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _enabledToggle.IsChecked = Settings.Enabled;
    }

    private void OnEnabledToggleChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Enabled = _enabledToggle.IsChecked ?? false;
    }
}
