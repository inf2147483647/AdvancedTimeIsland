using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Views.Controls;
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
    private CheckBox? _labelEnableCustomFontSizeToggle;
    private CheckBox? _labelEnableCustomFontColorToggle;
    private CheckBox? _valueEnableCustomFontSizeToggle;
    private CheckBox? _valueEnableCustomFontColorToggle;
    private ToggleSwitch _enableComponentToggle;

    private TextBlock _valueColorNoteTextBlock;

    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();

    private bool _isInDialogFlow;

    public FpsMonitorSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        // ==================== 基本设置 ====================
        var basicPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _enableComponentToggle = new ToggleSwitch { Content = "启用此组件", Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        _enableComponentToggle.IsCheckedChanged += OnEnableComponentToggleChanged;
        basicPanel.Children.Add(_enableComponentToggle);

        sp.Children.Add(SettingsGroupFactory.Create("基本设置", basicPanel));

        // ==================== 外观设置 ====================
        var appearancePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _valueColorNoteTextBlock = new TextBlock { Text = "默认颜色根据FPS自动变化（>=30绿色，20-30黄色，<20红色），启用自定义颜色后将使用固定颜色", FontSize = 12, Foreground = ThemeHelper.GetSubTextBrush(), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };
        appearancePanel.Children.Add(_valueColorNoteTextBlock);

        sp.Children.Add(SettingsGroupFactory.Create("外观设置", appearancePanel));

        // ==================== 文案设置 ====================
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var fontStyleTableScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateFontStyleTable()
        };
        textPanel.Children.Add(fontStyleTableScroll);

        sp.Children.Add(SettingsGroupFactory.Create("文案设置", textPanel));

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
        _labelEnableCustomFontSizeToggle!.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontColorToggle!.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontSizeToggle!.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontColorToggle!.Foreground = ThemeHelper.GetTextBrush();
        _valueColorNoteTextBlock.Foreground = ThemeHelper.GetSubTextBrush();

        foreach (var tb in _dynamicTextBlocks)
        {
            tb.Foreground = ThemeHelper.GetTextBrush();
        }

        var separatorBrush = ThemeHelper.GetSeparatorBrush();
        foreach (var border in _tableCellBorders)
        {
            border.BorderBrush = separatorBrush;
        }
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

    // ==================== 字体样式表格 ====================

    private Grid CreateFontStyleTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (int i = 0; i < 3; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "样式");
        AddTableHeader(grid, 0, 1, "自定义大小");
        AddTableHeader(grid, 0, 2, "自定义颜色");

        AddTableRowLabel(grid, 1, "标签");
        AddTableCell(grid, 1, 1, CreateSizeCell(out _labelFontSizeNumericUpDown, out _labelEnableCustomFontSizeToggle, OnLabelEnableCustomFontSizeChanged, OnLabelFontSizeChanged));
        AddTableCell(grid, 1, 2, CreateColorCell(out _labelColorPicker, out _labelEnableCustomFontColorToggle, OnLabelEnableCustomFontColorChanged, OnLabelColorChanged));

        AddTableRowLabel(grid, 2, "值");
        AddTableCell(grid, 2, 1, CreateSizeCell(out _valueFontSizeNumericUpDown, out _valueEnableCustomFontSizeToggle, OnValueEnableCustomFontSizeChanged, OnValueFontSizeChanged));
        AddTableCell(grid, 2, 2, CreateColorCell(out _valueColorPicker, out _valueEnableCustomFontColorToggle, OnValueEnableCustomFontColorChanged, OnValueColorChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 3);
        Grid.SetColumnSpan(outerBorder, 3);
        _tableCellBorders.Add(outerBorder);
        grid.Children.Add(outerBorder);

        return grid;
    }

    private Border CreateCellBorder(Control child)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            Padding = new Thickness(6, 3, 6, 3),
            Child = child
        };
        _tableCellBorders.Add(border);
        return border;
    }

    private void AddTableHeader(Grid grid, int row, int col, string text)
    {
        var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(tb);
        var border = CreateCellBorder(tb);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private void AddTableRowLabel(Grid grid, int row, string text)
    {
        var tb = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(tb);
        var border = CreateCellBorder(tb);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, 0);
        grid.Children.Add(border);
    }

    private void AddTableCell(Grid grid, int row, int col, Control control)
    {
        var border = CreateCellBorder(control);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private static StackPanel CreateSizeCell(out NumericUpDown numericUpDown, out CheckBox? toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        numericUpDown = new NumericUpDown
        {
            Width = 120,
            Minimum = 1,
            Maximum = 72,
            Increment = 1m,
            FormatString = "0.00",
            VerticalAlignment = VerticalAlignment.Center
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        panel.Children.Add(numericUpDown);
        return panel;
    }

    private static StackPanel CreateColorCell(out ColorPicker colorPicker, out CheckBox? toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<ColorChangedEventArgs> colorChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        colorPicker = new ColorPicker { Width = 120, VerticalAlignment = VerticalAlignment.Center };
        colorPicker.ColorChanged += colorChangedHandler;
        panel.Children.Add(colorPicker);
        return panel;
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
