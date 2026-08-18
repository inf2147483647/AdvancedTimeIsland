using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class PeriodicCountdownSettingsControl : ComponentBase<PeriodicCountdownSettings>
{
    private TextBox _text1TextBox = null!;
    private bool _initCompleted;
    private TextBox _text3TextBox = null!;
    private TextBox _text4TextBox = null!;
    private TextBox _timeFormatTextBox = null!;
    private CheckBox _text1EnableCustomFontSizeToggle = null!;
    private CheckBox _text1EnableCustomFontColorToggle = null!;
    private CheckBox _text2EnableCustomFontSizeToggle = null!;
    private CheckBox _text2EnableCustomFontColorToggle = null!;
    private CheckBox _text3EnableCustomFontSizeToggle = null!;
    private CheckBox _text3EnableCustomFontColorToggle = null!;
    private CheckBox _timeEnableCustomFontSizeToggle = null!;
    private CheckBox _timeEnableCustomFontColorToggle = null!;
    private CheckBox _text4EnableCustomFontSizeToggle = null!;
    private CheckBox _text4EnableCustomFontColorToggle = null!;
    private ComboBox _timeBaseComboBox = null!;
    private ListBox _countdownListBox = null!;

    private TextBlock _selectionHintTextBlock = null!;
    private System.Timers.Timer? _hintTimer;

    private TextBox _text1FontSizeTextBox = null!;
    private WpfColorPicker _text1FontColorPicker = null!;
    private TextBox _text2FontSizeTextBox = null!;
    private WpfColorPicker _text2FontColorPicker = null!;
    private TextBox _text3FontSizeTextBox = null!;
    private WpfColorPicker _text3FontColorPicker = null!;
    private TextBox _timeFontSizeTextBox = null!;
    private WpfColorPicker _timeFontColorPicker = null!;
    private TextBox _text4FontSizeTextBox = null!;
    private WpfColorPicker _text4FontColorPicker = null!;

    private ComboBox _progressDisplayModeComboBox = null!;
    private WpfColorPicker _progressBarColorPicker = null!;
    private WpfColorPicker _progressRingColorPicker = null!;

    public PeriodicCountdownSettingsControl()
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
        _text1TextBox = (TextBox)((StackPanel)Text1Item.Switcher).Children[0];
        _text3TextBox = (TextBox)((StackPanel)Text3Item.Switcher).Children[0];
        _text4TextBox = (TextBox)((StackPanel)Text4Item.Switcher).Children[0];
        _timeFormatTextBox = (TextBox)((StackPanel)TimeFormatItem.Switcher).Children[0];
        _timeBaseComboBox = (ComboBox)((StackPanel)TimeBaseItem.Switcher).Children[0];

        _progressDisplayModeComboBox = (ComboBox)((StackPanel)ProgressDisplayModeItem.Switcher).Children[0];
        _progressBarColorPicker = (WpfColorPicker)((StackPanel)ProgressBarColorItem.Switcher).Children[0];
        _progressRingColorPicker = (WpfColorPicker)((StackPanel)ProgressRingColorItem.Switcher).Children[0];

        _text1FontSizeTextBox = (TextBox)((StackPanel)Text1FontSizeItem.Switcher).Children[0];
        _text1EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text1FontSizeItem.Switcher).Children[1];
        _text1FontColorPicker = (WpfColorPicker)((StackPanel)Text1ColorItem.Switcher).Children[0];
        _text1EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text1ColorItem.Switcher).Children[1];

        _text2FontSizeTextBox = (TextBox)((StackPanel)Text2FontSizeItem.Switcher).Children[0];
        _text2EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text2FontSizeItem.Switcher).Children[1];
        _text2FontColorPicker = (WpfColorPicker)((StackPanel)Text2ColorItem.Switcher).Children[0];
        _text2EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text2ColorItem.Switcher).Children[1];

        _text3FontSizeTextBox = (TextBox)((StackPanel)Text3FontSizeItem.Switcher).Children[0];
        _text3EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text3FontSizeItem.Switcher).Children[1];
        _text3FontColorPicker = (WpfColorPicker)((StackPanel)Text3ColorItem.Switcher).Children[0];
        _text3EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text3ColorItem.Switcher).Children[1];

        _timeFontSizeTextBox = (TextBox)((StackPanel)TimeFontSizeItem.Switcher).Children[0];
        _timeEnableCustomFontSizeToggle = (CheckBox)((StackPanel)TimeFontSizeItem.Switcher).Children[1];
        _timeFontColorPicker = (WpfColorPicker)((StackPanel)TimeColorItem.Switcher).Children[0];
        _timeEnableCustomFontColorToggle = (CheckBox)((StackPanel)TimeColorItem.Switcher).Children[1];

        _text4FontSizeTextBox = (TextBox)((StackPanel)Text4FontSizeItem.Switcher).Children[0];
        _text4EnableCustomFontSizeToggle = (CheckBox)((StackPanel)Text4FontSizeItem.Switcher).Children[1];
        _text4FontColorPicker = (WpfColorPicker)((StackPanel)Text4ColorItem.Switcher).Children[0];
        _text4EnableCustomFontColorToggle = (CheckBox)((StackPanel)Text4ColorItem.Switcher).Children[1];

        _timeBaseComboBox.Items.Add("插件偏移后的服务器时间");
        _timeBaseComboBox.Items.Add("插件偏移后的系统时间");
        _timeBaseComboBox.Items.Add("原始服务器时间");
        _timeBaseComboBox.Items.Add("原始系统时间");

        _progressDisplayModeComboBox.Items.Add("不显示");
        _progressDisplayModeComboBox.Items.Add("进度条");
        _progressDisplayModeComboBox.Items.Add("进度环");
        _progressDisplayModeComboBox.Items.Add("进度条和进度环");

        // SettingsCard 无内置事件，通过 IsOn 依赖属性变化订阅开关切换
        DependencyPropertyDescriptor.FromProperty(SettingsCard.IsOnProperty, typeof(SettingsCard))
            .AddValueChanged(TimeCorrectionCard, (s, e) =>
            {
                Settings.EnableTimeCorrection = TimeCorrectionCard.IsOn;
            });
        DependencyPropertyDescriptor.FromProperty(SettingsCard.IsOnProperty, typeof(SettingsCard))
            .AddValueChanged(EnableCustomProgressColorCard, (s, e) =>
            {
                Settings.EnableCustomProgressColor = EnableCustomProgressColorCard.IsOn;
                UpdateProgressColorControlsEnabled();
            });
    }

    private void OnText2ButtonClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            var item = Settings.CountdownItems[_countdownListBox.SelectedIndex];
            ShowEditDialog(item, _countdownListBox.SelectedIndex + 1);
        }
        else
        {
            _countdownListBox.BringIntoView();
            ShowHint();
        }
    }

    private void OnText1EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text1EnableCustomFontSize = _text1EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text1EnableCustomFontColor = _text1EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText2EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text2EnableCustomFontSize = _text2EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText2EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text2EnableCustomFontColor = _text2EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text3EnableCustomFontSize = _text3EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text3EnableCustomFontColor = _text3EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.TimeEnableCustomFontSize = _timeEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.TimeEnableCustomFontColor = _timeEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text4EnableCustomFontSize = _text4EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText4EnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Text4EnableCustomFontColor = _text4EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        _text1FontSizeTextBox.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _text2FontSizeTextBox.IsEnabled = Settings.Text2EnableCustomFontSize;
        _text2FontColorPicker.IsEnabled = Settings.Text2EnableCustomFontColor;
        _text3FontSizeTextBox.IsEnabled = Settings.Text3EnableCustomFontSize;
        _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        _timeFontSizeTextBox.IsEnabled = Settings.TimeEnableCustomFontSize;
        _timeFontColorPicker.IsEnabled = Settings.TimeEnableCustomFontColor;
        _text4FontSizeTextBox.IsEnabled = Settings.Text4EnableCustomFontSize;
        _text4FontColorPicker.IsEnabled = Settings.Text4EnableCustomFontColor;
    }

    private void UpdateProgressColorControlsEnabled()
    {
        var isCustomEnabled = Settings.EnableCustomProgressColor;
        var showProgressBar = Settings.ProgressDisplayMode == ProgressDisplayMode.Bar ||
                              Settings.ProgressDisplayMode == ProgressDisplayMode.Both;

        _progressBarColorPicker.IsEnabled = isCustomEnabled && showProgressBar;
        _progressRingColorPicker.IsEnabled = isCustomEnabled;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    private void RunInitWhenReady()
    {
        if (_initCompleted) return;
        if (Settings == null)
        {
            // OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，延迟到 Loaded 后再初始化
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }
        _initCompleted = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (!Settings.WarningAccepted)
        {
            var warningInfoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "Title", "作者提示");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "Message", "不推荐使用此功能实现放学倒计时或回家倒计时，否则使用者挨老师批，作者概不负责。关闭提示则代表同意此提示。");
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "IsOpen", true);
            FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningInfoBar, "IsClosable", true);
            warningInfoBar.Margin = new Thickness(0, 0, 0, 8);
            FluentAvaloniaCompatibilityHelper.AddInfoBarClosedHandler(warningInfoBar, (s, e2) =>
            {
                Settings.WarningAccepted = true;
            });
            MainPanel.Children.Insert(0, warningInfoBar);
        }

        _text1TextBox.Text = Settings.Text1;
        _text3TextBox.Text = Settings.Text3;
        _text4TextBox.Text = Settings.Text4;
        _timeFormatTextBox.Text = Settings.TimeFormat;
        TimeCorrectionCard.IsOn = Settings.EnableTimeCorrection;

        _timeBaseComboBox.SelectedIndex = (int)Settings.TimeBaseType;

        _progressDisplayModeComboBox.SelectedIndex = (int)Settings.ProgressDisplayMode;

        _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(CultureInfo.InvariantCulture);
        _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        _text2FontSizeTextBox.Text = Settings.Text2FontSize.ToString(CultureInfo.InvariantCulture);
        _text2FontColorPicker.Color = ParseColor(Settings.Text2FontColor);
        _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(CultureInfo.InvariantCulture);
        _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(CultureInfo.InvariantCulture);
        _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);
        _text4FontSizeTextBox.Text = Settings.Text4FontSize.ToString(CultureInfo.InvariantCulture);
        _text4FontColorPicker.Color = ParseColor(Settings.Text4FontColor);

        UpdateCountdownList();

        AttachEventHandlers();

        _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        _text2EnableCustomFontSizeToggle.IsChecked = Settings.Text2EnableCustomFontSize;
        _text2EnableCustomFontColorToggle.IsChecked = Settings.Text2EnableCustomFontColor;
        _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;
        _text4EnableCustomFontSizeToggle.IsChecked = Settings.Text4EnableCustomFontSize;
        _text4EnableCustomFontColorToggle.IsChecked = Settings.Text4EnableCustomFontColor;
        UpdateControlsEnabled();

        EnableCustomProgressColorCard.IsOn = Settings.EnableCustomProgressColor;
        UpdateProgressColorControlsEnabled();
        _progressBarColorPicker.Color = ParseColor(Settings.ProgressBarColor);
        _progressRingColorPicker.Color = ParseColor(Settings.ProgressRingColor);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _hintTimer?.Stop();
        _hintTimer?.Dispose();
    }

    private void AttachEventHandlers()
    {
        AttachTextHandler(_text1TextBox, v => Settings.Text1 = v ?? "距离");
        AttachTextHandler(_text3TextBox, v => Settings.Text3 = v ?? "还有");
        AttachTextHandler(_text4TextBox, v => Settings.Text4 = v ?? "");
        AttachTextHandler(_timeFormatTextBox, v => Settings.TimeFormat = v ?? "%d天%h小时%m分钟%s秒");
        AttachFontSizeHandler(_text1FontSizeTextBox, v => Settings.Text1FontSize = v);
        AttachFontSizeHandler(_text2FontSizeTextBox, v => Settings.Text2FontSize = v);
        AttachFontSizeHandler(_text3FontSizeTextBox, v => Settings.Text3FontSize = v);
        AttachFontSizeHandler(_timeFontSizeTextBox, v => Settings.TimeFontSize = v);
        AttachFontSizeHandler(_text4FontSizeTextBox, v => Settings.Text4FontSize = v);
    }

    private void AttachTextHandler(TextBox textBox, Action<string?> setter)
    {
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => setter(textBox.Text));
    }

    private void AttachFontSizeHandler(TextBox textBox, Action<double> setter)
    {
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, (s, e) => ParseAndSetFontSize(textBox, setter));
    }

    private void OnTimeBaseChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_timeBaseComboBox.SelectedIndex >= 0)
        {
            Settings.TimeBaseType = (TimeBaseType)_timeBaseComboBox.SelectedIndex;
        }
    }

    private void OnProgressDisplayModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_progressDisplayModeComboBox.SelectedIndex >= 0)
        {
            Settings.ProgressDisplayMode = (ProgressDisplayMode)_progressDisplayModeComboBox.SelectedIndex;
            UpdateProgressColorControlsEnabled();
        }
    }

    private void OnProgressBarColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ProgressBarColor = _progressBarColorPicker.Color.ToString();
    }

    private void OnProgressRingColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ProgressRingColor = _progressRingColorPicker.Color.ToString();
    }

    private void OnCountdownListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_countdownListBox.SelectedIndex >= 0)
        {
            HideHint();
        }
    }

    private void OnText1ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text1FontColor = _text1FontColorPicker.Color.ToString();
    }

    private void OnText2ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text2FontColor = _text2FontColorPicker.Color.ToString();
    }

    private void OnText3ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text3FontColor = _text3FontColorPicker.Color.ToString();
    }

    private void OnTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.TimeFontColor = _timeFontColorPicker.Color.ToString();
    }

    private void OnText4ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text4FontColor = _text4FontColorPicker.Color.ToString();
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

    private void ParseAndSetFontSize(TextBox textBox, Action<double> setter)
    {
        if (double.TryParse(textBox.Text, out double size))
        {
            setter(size);
            textBox.Text = size.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            textBox.Text = "14";
        }
    }

    private void UpdateCountdownList()
    {
        _countdownListBox.Items.Clear();
        if (Settings.CountdownItems != null)
        {
            foreach (var item in Settings.CountdownItems)
            {
                var container = new Grid();
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                container.Tag = item;

                var textBlock = new TextBlock
                {
                    Text = item.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetTextBrush(),
                    Padding = new Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(textBlock, 0);
                container.Children.Add(textBlock);

                var periodTextBlock = new TextBlock
                {
                    Text = GetPeriodTypeName(item.PeriodType),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = ThemeHelper.GetSubTextBrush(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(0, 4, 0, 4)
                };
                Grid.SetColumn(periodTextBlock, 1);
                container.Children.Add(periodTextBlock);

                var notifyPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                var notifySwitch = new CheckBox
                {
                    IsChecked = item.EnableNotification,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                var currentItem = item;
                notifySwitch.Checked += (s, e) =>
                {
                    currentItem.EnableNotification = notifySwitch.IsChecked == true;
                };
                notifySwitch.Unchecked += (s, e) =>
                {
                    currentItem.EnableNotification = notifySwitch.IsChecked == true;
                };
                notifyPanel.Children.Add(notifySwitch);
                Grid.SetColumn(notifyPanel, 2);
                container.Children.Add(notifyPanel);

                var listBoxItem = new ListBoxItem
                {
                    Content = container,
                    Tag = item
                };

                _countdownListBox.Items.Add(listBoxItem);
            }
        }
    }

    private string GetPeriodTypeName(PeriodType periodType)
    {
        return periodType switch
        {
            PeriodType.Hourly => "每小时",
            PeriodType.Daily => "每天",
            PeriodType.Weekly => "每周",
            PeriodType.Monthly => "每月",
            PeriodType.Yearly => "每年",
            _ => "未知"
        };
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        HideHint();
        if (Settings.CountdownItems == null)
        {
            Settings.CountdownItems = new List<PeriodicCountdownItem>();
        }
        Settings.CountdownItems.Add(PeriodicCountdownItem.CreateDefault());
        UpdateCountdownList();
    }

    private void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            Settings.CountdownItems.RemoveAt(_countdownListBox.SelectedIndex);
            UpdateCountdownList();
            HideHint();
        }
        else
        {
            ShowHint();
        }
    }

    private void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (Settings.CountdownItems != null && _countdownListBox.SelectedIndex >= 0)
        {
            var item = Settings.CountdownItems[_countdownListBox.SelectedIndex];
            ShowEditDialog(item, _countdownListBox.SelectedIndex + 1);
            HideHint();
        }
        else
        {
            ShowHint();
        }
    }

    private void ShowHint()
    {
        _selectionHintTextBlock.Visibility = Visibility.Visible;
        _hintTimer?.Stop();
        _hintTimer = new System.Timers.Timer(5000);
        _hintTimer.Elapsed += (s, e) =>
        {
            UIThread.Post(HideHint);
        };
        _hintTimer.AutoReset = false;
        _hintTimer.Start();
    }

    private void HideHint()
    {
        _selectionHintTextBlock.Visibility = Visibility.Collapsed;
        _hintTimer?.Stop();
    }

    private void ShowEditDialog(PeriodicCountdownItem item, int order = 0)
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", order > 0 ? $"正在编辑第{order}个倒计时" : "编辑倒计时");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "SecondaryButtonText", "取消");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());

        var contentPanel = new StackPanel { Orientation = Orientation.Vertical };

        var nameLabel = new TextBlock { Text = "名称:", Foreground = ThemeHelper.GetTextBrush() };
        var nameTextBox = new TextBox { Text = item.Name };
        contentPanel.Children.Add(nameLabel);
        contentPanel.Children.Add(nameTextBox);

        var periodLabel = new TextBlock { Text = "周期:", Foreground = ThemeHelper.GetTextBrush() };
        contentPanel.Children.Add(periodLabel);

        var periodComboBox = new ComboBox();
        periodComboBox.Items.Add("每小时");
        periodComboBox.Items.Add("每天");
        periodComboBox.Items.Add("每周");
        periodComboBox.Items.Add("每月");
        periodComboBox.Items.Add("每年");
        periodComboBox.SelectedIndex = (int)item.PeriodType;
        contentPanel.Children.Add(periodComboBox);

        var timeLabel = new TextBlock { Text = "时间:", Foreground = ThemeHelper.GetTextBrush() };
        contentPanel.Children.Add(timeLabel);

        var timePanel = new StackPanel { Orientation = Orientation.Horizontal };
        var hourComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 24; i++) hourComboBox.Items.Add(i.ToString("D2"));
        hourComboBox.SelectedIndex = item.Hour;
        var minuteComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) minuteComboBox.Items.Add(i.ToString("D2"));
        minuteComboBox.SelectedIndex = item.Minute;
        var secondComboBox = new ComboBox { Width = 80 };
        for (int i = 0; i < 60; i++) secondComboBox.Items.Add(i.ToString("D2"));
        secondComboBox.SelectedIndex = item.Second;

        timePanel.Children.Add(hourComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(minuteComboBox);
        timePanel.Children.Add(new TextBlock { Text = ":", FontSize = 16, Foreground = ThemeHelper.GetTextBrush(), VerticalAlignment = VerticalAlignment.Center });
        timePanel.Children.Add(secondComboBox);
        contentPanel.Children.Add(timePanel);

        var extraPanel = new StackPanel { Orientation = Orientation.Vertical };

        var dayOfWeekRow = new Grid();
        dayOfWeekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dayOfWeekRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var dayOfWeekLabel = new TextBlock { Text = "星期:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(dayOfWeekLabel, 0);
        dayOfWeekRow.Children.Add(dayOfWeekLabel);

        var dayOfWeekComboBox = new ComboBox();
        dayOfWeekComboBox.Items.Add("星期日");
        dayOfWeekComboBox.Items.Add("星期一");
        dayOfWeekComboBox.Items.Add("星期二");
        dayOfWeekComboBox.Items.Add("星期三");
        dayOfWeekComboBox.Items.Add("星期四");
        dayOfWeekComboBox.Items.Add("星期五");
        dayOfWeekComboBox.Items.Add("星期六");
        dayOfWeekComboBox.SelectedIndex = item.DayOfWeek;
        Grid.SetColumn(dayOfWeekComboBox, 1);
        dayOfWeekRow.Children.Add(dayOfWeekComboBox);
        extraPanel.Children.Add(dayOfWeekRow);

        var dayOfMonthRow = new Grid();
        dayOfMonthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dayOfMonthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var dayOfMonthLabel = new TextBlock { Text = "日期:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(dayOfMonthLabel, 0);
        dayOfMonthRow.Children.Add(dayOfMonthLabel);

        var dayOfMonthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 31; i++) dayOfMonthComboBox.Items.Add($"{i}日");
        dayOfMonthComboBox.SelectedItem = $"{item.DayOfMonth}日";
        Grid.SetColumn(dayOfMonthComboBox, 1);
        dayOfMonthRow.Children.Add(dayOfMonthComboBox);
        extraPanel.Children.Add(dayOfMonthRow);

        var monthRow = new Grid();
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        monthRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var monthLabel = new TextBlock { Text = "月份:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(monthLabel, 0);
        monthRow.Children.Add(monthLabel);

        var monthComboBox = new ComboBox { Width = 80 };
        for (int i = 1; i <= 12; i++) monthComboBox.Items.Add($"{i}月");
        monthComboBox.SelectedIndex = item.Month - 1;
        Grid.SetColumn(monthComboBox, 1);
        monthRow.Children.Add(monthComboBox);
        extraPanel.Children.Add(monthRow);

        void UpdateExtraVisibility()
        {
            dayOfWeekRow.Visibility = periodComboBox.SelectedIndex == (int)PeriodType.Weekly ? Visibility.Visible : Visibility.Collapsed;
            dayOfMonthRow.Visibility = (periodComboBox.SelectedIndex == (int)PeriodType.Monthly || periodComboBox.SelectedIndex == (int)PeriodType.Yearly) ? Visibility.Visible : Visibility.Collapsed;
            monthRow.Visibility = periodComboBox.SelectedIndex == (int)PeriodType.Yearly ? Visibility.Visible : Visibility.Collapsed;
        }

        periodComboBox.SelectionChanged += (s, e) => UpdateExtraVisibility();
        UpdateExtraVisibility();

        contentPanel.Children.Add(extraPanel);

        var notifyToggle = new CheckBox { Content = "启用通知", IsChecked = item.EnableNotification };
        contentPanel.Children.Add(notifyToggle);

        var notifyTitleLabel = new TextBlock { Text = "通知标题:", Foreground = ThemeHelper.GetTextBrush() };
        var notifyTitleTextBox = new TextBox { Text = item.NotificationTitle };
        contentPanel.Children.Add(notifyTitleLabel);
        contentPanel.Children.Add(notifyTitleTextBox);

        var notifyContentLabel = new TextBlock { Text = "通知内容:", Foreground = ThemeHelper.GetTextBrush() };
        var notifyContentTextBox = new TextBox { Text = item.NotificationContent };
        contentPanel.Children.Add(notifyContentLabel);
        contentPanel.Children.Add(notifyContentTextBox);

        var maskDurationLabel = new TextBlock { Text = "通知标题时长(秒):", Foreground = ThemeHelper.GetTextBrush() };
        var maskDurationTextBox = new TextBox { Text = item.NotificationMaskDurationSeconds.ToString() };
        contentPanel.Children.Add(maskDurationLabel);
        contentPanel.Children.Add(maskDurationTextBox);

        var overlayDurationLabel = new TextBlock { Text = "通知内容时长(秒):", Foreground = ThemeHelper.GetTextBrush() };
        var overlayDurationTextBox = new TextBox { Text = item.NotificationOverlayDurationSeconds.ToString() };
        contentPanel.Children.Add(overlayDurationLabel);
        contentPanel.Children.Add(overlayDurationTextBox);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = contentPanel,
            Margin = new Thickness(12, 12, 12, 0)
        };

        var dialogMainPanel = new Grid();
        dialogMainPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(scrollViewer, 0);
        dialogMainPanel.Children.Add(scrollViewer);

        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", dialogMainPanel);

        FluentAvaloniaCompatibilityHelper.AddContentDialogButtonClickHandler(dialog, "PrimaryButtonClick", (s, e) =>
        {
            item.Name = nameTextBox.Text ?? "新周期性倒计时";
            item.PeriodType = (PeriodType)(periodComboBox.SelectedIndex >= 0 ? periodComboBox.SelectedIndex : 1);

            if (hourComboBox.SelectedIndex >= 0)
                item.Hour = hourComboBox.SelectedIndex;
            if (minuteComboBox.SelectedIndex >= 0)
                item.Minute = minuteComboBox.SelectedIndex;
            if (secondComboBox.SelectedIndex >= 0)
                item.Second = secondComboBox.SelectedIndex;

            if (dayOfWeekComboBox.SelectedIndex >= 0)
                item.DayOfWeek = dayOfWeekComboBox.SelectedIndex;

            if (dayOfMonthComboBox.SelectedItem != null && int.TryParse(dayOfMonthComboBox.SelectedItem.ToString()?.Replace("日", ""), out var dayOfMonth))
                item.DayOfMonth = dayOfMonth;

            if (monthComboBox.SelectedIndex >= 0)
                item.Month = monthComboBox.SelectedIndex + 1;

            item.EnableNotification = notifyToggle.IsChecked == true;
            item.NotificationTitle = notifyTitleTextBox.Text ?? "周期性倒计时到达";
            item.NotificationContent = notifyContentTextBox.Text ?? "目标时间已到达！";

            if (int.TryParse(maskDurationTextBox.Text, out int maskDuration))
            {
                item.NotificationMaskDurationSeconds = maskDuration;
            }

            if (int.TryParse(overlayDurationTextBox.Text, out int overlayDuration))
            {
                item.NotificationOverlayDurationSeconds = overlayDuration;
            }

            item.IsCompleted = false;
            UpdateCountdownList();
        });

        _ = FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, Window.GetWindow(this));
    }
}
