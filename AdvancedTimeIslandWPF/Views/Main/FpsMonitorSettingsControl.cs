using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class FpsMonitorSettingsControl : ComponentBase<FpsMonitorSettings>
{
    private WpfColorPicker _labelColorPicker = null!;
    private WpfNumericUpDown _labelFontSizeNumericUpDown = null!;
    private WpfColorPicker _valueColorPicker = null!;
    private WpfNumericUpDown _valueFontSizeNumericUpDown = null!;
    private CheckBox _labelEnableCustomFontSizeToggle = null!;
    private CheckBox _labelEnableCustomFontColorToggle = null!;
    private CheckBox _valueEnableCustomFontSizeToggle = null!;
    private CheckBox _valueEnableCustomFontColorToggle = null!;

    private bool _isInDialogFlow;

    public FpsMonitorSettingsControl()
    {
        InitializeComponent();
        InitializeControlReferences();
    }

    /// <summary>
    /// 从 XAML 的 SettingsControl.Switcher 中提取控件引用。
    /// （SettingsControl 模板内元素无法使用 x:Name，需通过 Switcher 属性访问）
    /// </summary>
    private void InitializeControlReferences()
    {
        _labelFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)LabelFontSizeItem.Switcher).Children[0];
        _labelEnableCustomFontSizeToggle = (CheckBox)((StackPanel)LabelFontSizeItem.Switcher).Children[1];

        _labelColorPicker = (WpfColorPicker)((StackPanel)LabelColorItem.Switcher).Children[0];
        _labelEnableCustomFontColorToggle = (CheckBox)((StackPanel)LabelColorItem.Switcher).Children[1];

        _valueFontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)ValueFontSizeItem.Switcher).Children[0];
        _valueEnableCustomFontSizeToggle = (CheckBox)((StackPanel)ValueFontSizeItem.Switcher).Children[1];

        _valueColorPicker = (WpfColorPicker)((StackPanel)ValueColorItem.Switcher).Children[0];
        _valueEnableCustomFontColorToggle = (CheckBox)((StackPanel)ValueColorItem.Switcher).Children[1];

        // SettingsCard 无内置事件，通过 IsOn 依赖属性变化订阅开关切换
        DependencyPropertyDescriptor.FromProperty(SettingsCard.IsOnProperty, typeof(SettingsCard))
            .AddValueChanged(EnableComponentCard, (s, e) => OnEnableComponentToggleChanged());
    }

    private void OnLabelEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.LabelEnableCustomFontSize = _labelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.LabelEnableCustomFontColor = _labelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.ValueEnableCustomFontSize = _valueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.ValueEnableCustomFontColor = _valueEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private async void OnEnableComponentToggleChanged()
    {
        if (EnableComponentCard.IsOn)
        {
            if (Settings.EnableComponent)
            {
                return;
            }
            await StartEnableFlow();
        }
        else
        {
            Settings.EnableComponent = false;
        }
    }

    private async Task<bool> ShowEpilepsyWarningDialogAsync()
    {
        var topLevel = Window.GetWindow(this);
        if (topLevel == null) return false;

        var contentPanel = new StackPanel();

        var warningTextBlock = new TextBlock
        {
            Text = "有极少数的人在观看一些视觉影像时可能会突然癫痫发作，这些影像包括快速改变的数字或图形。在使用此组件时，这些人可能会出现癫痫症状。甚至连不具有癫痫史的人，也可能在查看此组件时出现类似癫痫症状。\n\n" +
                   "如果您或您的家人有癫痫史，请在添加此组件之前先与医生咨询。如果您在使用此组件时出现以下症状，包括眼睛疼痛、视觉异常、偏头痛、痉挛或意识障碍（诸如昏迷）等，请立即中止使用，并且请您于再次使用此组件之前咨询您的医生。\n\n" +
                   "除上述症状外，当您感到头痛、头晕眼花、恶心想吐或类似晕车症状时，以及当身体的某些部位感到不舒服或疼痛时，请立即中止使用。若在中止使用后，症状仍没有减退，请立即寻求医生的诊疗。",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(12)
        };
        contentPanel.Children.Add(warningTextBlock);

        var countDownTextBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 8, 0, 0),
            Text = "请阅读以上内容，确定按钮将在15秒后可用..."
        };
        contentPanel.Children.Add(countDownTextBlock);

        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "警告：使用前详阅");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", contentPanel);
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定（15）");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "CloseButtonText", "取消");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "IsPrimaryButtonEnabled", false);

        _ = Task.Run(async () =>
        {
            for (int i = 15; i >= 0; i--)
            {
                await Task.Delay(1000);
                await UIThread.InvokeAsync(() =>
                {
                    if (i > 0)
                    {
                        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", $"确定（{i}）");
                    }
                    else
                    {
                        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
                        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "IsPrimaryButtonEnabled", true);
                    }
                });
            }
        });

        var result = await FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, topLevel);
        return FluentAvaloniaCompatibilityHelper.IsContentDialogResultPrimary(result);
    }

    private async Task<bool> ShowDebugWarningDialogAsync()
    {
        var topLevel = Window.GetWindow(this);
        if (topLevel == null) return false;

        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "警告");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", new TextBlock
        {
            Text = "此组件仅供调试，严禁用于教学环境！！！",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(12)
        });
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "CloseButtonText", "取消");

        var result = await FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, topLevel);
        return FluentAvaloniaCompatibilityHelper.IsContentDialogResultPrimary(result);
    }

    private async Task StartEnableFlow()
    {
        if (_isInDialogFlow) return;
        _isInDialogFlow = true;

        try
        {
            bool epilepsyAccepted = await ShowEpilepsyWarningDialogAsync();
            if (!epilepsyAccepted)
            {
                Settings.EnableComponent = false;
                EnableComponentCard.IsOn = false;
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bool debugAccepted = await ShowDebugWarningDialogAsync();
                if (!debugAccepted)
                {
                    Settings.EnableComponent = false;
                    EnableComponentCard.IsOn = false;
                    return;
                }
            }

            Settings.EnableComponent = true;
        }
        finally
        {
            _isInDialogFlow = false;
        }
    }

    private void UpdateControlsEnabled()
    {
        _labelColorPicker.IsEnabled = Settings.LabelEnableCustomFontColor;
        _labelFontSizeNumericUpDown.IsEnabled = Settings.LabelEnableCustomFontSize;
        _valueColorPicker.IsEnabled = Settings.ValueEnableCustomFontColor;
        _valueFontSizeNumericUpDown.IsEnabled = Settings.ValueEnableCustomFontSize;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        EnableComponentCard.IsOn = Settings.EnableComponent;
        _labelEnableCustomFontSizeToggle.IsChecked = Settings.LabelEnableCustomFontSize;
        _labelEnableCustomFontColorToggle.IsChecked = Settings.LabelEnableCustomFontColor;
        _valueEnableCustomFontSizeToggle.IsChecked = Settings.ValueEnableCustomFontSize;
        _valueEnableCustomFontColorToggle.IsChecked = Settings.ValueEnableCustomFontColor;
        UpdateControlsEnabled();
        _labelColorPicker.Color = ParseColor(Settings.LabelFontColor);
        _labelFontSizeNumericUpDown.Value = (decimal)Settings.LabelFontSize;
        _valueColorPicker.Color = ParseColor(Settings.ValueFontColor);
        _valueFontSizeNumericUpDown.Value = (decimal)Settings.ValueFontSize;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
    }

    private Color ParseColor(string colorString)
    {
        try
        {
            return ThemeHelper.ParseColor(colorString);
        }
        catch
        {
            return Colors.White;
        }
    }

    private void OnLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_labelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.LabelFontSize = (double)_labelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_valueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.ValueFontSize = (double)_valueFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.LabelFontColor = _labelColorPicker.Color.ToString();
    }

    private void OnValueColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ValueFontColor = _valueColorPicker.Color.ToString();
    }
}
