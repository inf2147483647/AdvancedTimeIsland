using System;
using System.Globalization;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class FpsMonitorSettingsControl : ComponentBase<FpsMonitorSettings>
{
    private ColorPicker _labelColorPicker;
    private NumericUpDown _labelFontSizeNumericUpDown;
    private ColorPicker _valueColorPicker;
    private NumericUpDown _valueFontSizeNumericUpDown;
    private ToggleSwitch? _labelEnableCustomFontSizeToggle;
    private ToggleSwitch? _labelEnableCustomFontColorToggle;
    private ToggleSwitch? _valueEnableCustomFontSizeToggle;
    private ToggleSwitch? _valueEnableCustomFontColorToggle;
    private ToggleSwitch _enableComponentToggle;

    private TextBlock _labelTitleTextBlock;
    private TextBlock _labelColorLabelTextBlock;
    private TextBlock _labelFontSizeLabelTextBlock;
    private TextBlock _valueTitleTextBlock;
    private TextBlock _valueColorNoteTextBlock;
    private TextBlock _valueColorLabelTextBlock;
    private TextBlock _valueFontSizeLabelTextBlock;

    private bool _isInDialogFlow;

    public FpsMonitorSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _enableComponentToggle = new ToggleSwitch { Content = "启用此组件", Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        _enableComponentToggle.IsCheckedChanged += OnEnableComponentToggleChanged;
        sp.Children.Add(_enableComponentToggle);

        _labelTitleTextBlock = new TextBlock { Text = "标签样式", FontSize = 14, FontWeight = FontWeight.Bold };
        var labelTitleRow = CreateTitleRow(_labelTitleTextBlock, out _labelEnableCustomFontSizeToggle, out _labelEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnLabelEnableCustomFontSizeChanged, OnLabelEnableCustomFontColorChanged, null, null);
        labelTitleRow.Margin = new Thickness(0, 10, 0, 0);
        sp.Children.Add(labelTitleRow);

        sp.Children.Add(CreateFontSizeRow("文本大小", out _labelFontSizeLabelTextBlock, out _labelFontSizeNumericUpDown, OnLabelFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _labelColorLabelTextBlock, out _labelColorPicker, OnLabelColorChanged));

        _valueTitleTextBlock = new TextBlock { Text = "值样式", FontSize = 14, FontWeight = FontWeight.Bold };
        var valueTitleRow = CreateTitleRow(_valueTitleTextBlock, out _valueEnableCustomFontSizeToggle, out _valueEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnValueEnableCustomFontSizeChanged, OnValueEnableCustomFontColorChanged, null, null);
        valueTitleRow.Margin = new Thickness(0, 10, 0, 0);
        sp.Children.Add(valueTitleRow);

        _valueColorNoteTextBlock = new TextBlock { Text = "默认颜色根据FPS自动变化（>=30绿色，20-30黄色，<20红色），启用自定义颜色后将使用固定颜色", FontSize = 12, Foreground = ThemeHelper.GetSubTextBrush(), TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_valueColorNoteTextBlock);

        sp.Children.Add(CreateFontSizeRow("文本大小", out _valueFontSizeLabelTextBlock, out _valueFontSizeNumericUpDown, OnValueFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _valueColorLabelTextBlock, out _valueColorPicker, OnValueColorChanged));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    private void UpdateThemeColors()
    {
        _enableComponentToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _labelColorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _labelFontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueColorNoteTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _valueColorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueFontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void OnLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontSize = _labelEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontColor = _labelEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontSize = _valueEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontColor = _valueEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private async void OnEnableComponentToggleChanged(object? sender, EventArgs e)
    {
        if (_enableComponentToggle.IsChecked ?? false)
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
        var topLevel = TopLevel.GetTopLevel(this);
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
                await Dispatcher.UIThread.InvokeAsync(() =>
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
        var topLevel = TopLevel.GetTopLevel(this);
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
                _enableComponentToggle.IsChecked = false;
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bool debugAccepted = await ShowDebugWarningDialogAsync();
                if (!debugAccepted)
                {
                    Settings.EnableComponent = false;
                    _enableComponentToggle.IsChecked = false;
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

    private Grid CreateTitleRow(TextBlock title, out ToggleSwitch? toggle1, out ToggleSwitch? toggle2, out ToggleSwitch? toggle3, out ToggleSwitch? toggle4,
        string? content1, string? content2, string? content3, string? content4,
        EventHandler<RoutedEventArgs>? handler1, EventHandler<RoutedEventArgs>? handler2, EventHandler<RoutedEventArgs>? handler3, EventHandler<RoutedEventArgs>? handler4)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(title, 0);
        row.Children.Add(title);

        int col = 1;

        if (content1 != null)
        {
            toggle1 = new ToggleSwitch { Content = content1, VerticalAlignment = VerticalAlignment.Center };
            if (handler1 != null)
                toggle1.IsCheckedChanged += handler1;
            Grid.SetColumn(toggle1, col++);
            row.Children.Add(toggle1);
        }
        else
        {
            toggle1 = null;
        }

        if (content2 != null)
        {
            toggle2 = new ToggleSwitch { Content = content2, VerticalAlignment = VerticalAlignment.Center };
            if (handler2 != null)
                toggle2.IsCheckedChanged += handler2;
            Grid.SetColumn(toggle2, col++);
            row.Children.Add(toggle2);
        }
        else
        {
            toggle2 = null;
        }

        if (content3 != null)
        {
            toggle3 = new ToggleSwitch { Content = content3, VerticalAlignment = VerticalAlignment.Center };
            if (handler3 != null)
                toggle3.IsCheckedChanged += handler3;
            Grid.SetColumn(toggle3, col++);
            row.Children.Add(toggle3);
        }
        else
        {
            toggle3 = null;
        }

        if (content4 != null)
        {
            toggle4 = new ToggleSwitch { Content = content4, VerticalAlignment = VerticalAlignment.Center };
            if (handler4 != null)
                toggle4.IsCheckedChanged += handler4;
            Grid.SetColumn(toggle4, col);
            row.Children.Add(toggle4);
        }
        else
        {
            toggle4 = null;
        }

        return row;
    }

    private Grid CreateFontSizeRow(string labelText, out TextBlock label, out NumericUpDown numericUpDown,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        numericUpDown = new NumericUpDown
        {
            Width = 155,
            Minimum = 1,
            Maximum = 100,
            Increment = 1m,
            FormatString = "0.00",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        Grid.SetColumn(numericUpDown, 1);
        row.Children.Add(numericUpDown);

        return row;
    }

    private Grid CreateColorRow(string labelText, out TextBlock label, out ColorPicker colorPicker,
        EventHandler<ColorChangedEventArgs> colorChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        colorPicker = new ColorPicker { Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        colorPicker.ColorChanged += colorChangedHandler;
        Grid.SetColumn(colorPicker, 1);
        row.Children.Add(colorPicker);

        return row;
    }

    private void UpdateControlsEnabled()
    {
        _labelColorPicker.IsEnabled = Settings.LabelEnableCustomFontColor;
        _labelFontSizeNumericUpDown.IsEnabled = Settings.LabelEnableCustomFontSize;
        _valueColorPicker.IsEnabled = Settings.ValueEnableCustomFontColor;
        _valueFontSizeNumericUpDown.IsEnabled = Settings.ValueEnableCustomFontSize;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        _enableComponentToggle.IsChecked = Settings.EnableComponent;
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

    private Color ParseColor(string colorString)
    {
        try
        {
            return Color.Parse(colorString);
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

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }
}
