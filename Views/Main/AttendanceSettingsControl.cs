using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 总在校时间统计（ATI）组件的设置面板：只包含本组件实例的显示选项；
/// 学期区间、节假日与寒暑假等统计口径由插件设置页中的「在校时间统计」统一管理。
/// </summary>
public partial class AttendanceSettingsControl : ComponentBase<AttendanceSettings>
{
    /// <summary>字体样式表格中一行所对应的段落与其控件集合。</summary>
    private sealed class TextStyleRow
    {
        public AttendanceTextSegment Segment;
        public TextBlock Label = null!;
        public WpfNumericUpDown FontSize = null!;
        public CheckBox EnableFontSize = null!;
        public WpfColorPicker Color = null!;
        public CheckBox EnableColor = null!;
        public ComboBox FontFamily = null!;
        public CheckBox EnableFontFamily = null!;
        public ComboBox FontWeight = null!;
        public CheckBox EnableFontWeight = null!;
        public TextStyleSettings? Style;
    }

    /// <summary>字体样式表格的行顺序，与组件中从左到右的文案顺序一致。</summary>
    private static readonly (AttendanceTextSegment Segment, string Label)[] TextStyleRows =
    {
        (AttendanceTextSegment.Summary, "概要文案"),
        (AttendanceTextSegment.Hours, "累计时长"),
        (AttendanceTextSegment.Progress, "进度百分比"),
        (AttendanceTextSegment.RemainingDays, "剩余天数"),
        (AttendanceTextSegment.RemainingHours, "剩余时长")
    };

    private static readonly object[] ProgressDisplayModeItems =
    {
        "不显示", "进度条", "进度环", "进度条 + 进度环"
    };

    private static readonly object[] TimeBaseItems =
    {
        "插件偏移后的服务器时间", "原始服务器时间", "ClassIsland时间"
    };

    private ComboBox _progressDisplayModeComboBox = null!;
    private ComboBox _timeBaseComboBox = null!;
    private CheckBox _showSegmentDividerCheckBox = null!;
    private CheckBox _showSummaryTextCheckBox = null!;
    private CheckBox _showHoursTextCheckBox = null!;
    private CheckBox _showRemainingTextCheckBox = null!;
    private CheckBox _showPercentTextCheckBox = null!;
    private CheckBox _decimalizeHoursCheckBox = null!;

    private readonly List<TextStyleRow> _textStyleRows = new();

    /// <summary>程序化回填控件时置位，避免控件事件把设置改回控件中的旧值。</summary>
    private bool _isUpdatingControls;

    private bool _initCompleted;

    public AttendanceSettingsControl()
    {
        InitializeComponent();
        InitializeControlReferences();
        BuildFontStyleTable();
    }

    /// <summary>
    /// 从 XAML 的 SettingsControl.Switcher 中提取控件引用。
    /// （SettingsControl 模板内元素无法使用 x:Name，需通过 Switcher 属性访问）
    /// </summary>
    private void InitializeControlReferences()
    {
        _progressDisplayModeComboBox = (ComboBox)ProgressDisplayModeItem.Switcher;
        _showSegmentDividerCheckBox = (CheckBox)ShowSegmentDividerItem.Switcher;
        _showSummaryTextCheckBox = (CheckBox)ShowSummaryTextItem.Switcher;
        _showHoursTextCheckBox = (CheckBox)ShowHoursTextItem.Switcher;
        _showRemainingTextCheckBox = (CheckBox)ShowRemainingTextItem.Switcher;
        _showPercentTextCheckBox = (CheckBox)ShowPercentTextItem.Switcher;
        _decimalizeHoursCheckBox = (CheckBox)DecimalizeHoursItem.Switcher;
        _timeBaseComboBox = (ComboBox)TimeBaseItem.Switcher;

        foreach (var item in ProgressDisplayModeItems)
        {
            _progressDisplayModeComboBox.Items.Add(item);
        }
        foreach (var item in TimeBaseItems)
        {
            _timeBaseComboBox.Items.Add(item);
        }
    }

    // ==================== 字体样式表格 ====================

    /// <summary>
    /// 构建 5 段 × 4 项（大小 / 颜色 / 字体 / 字重）的字体样式表格。
    /// 单元格与表头采用宿主主题资源，随主题深浅自动切换。
    /// </summary>
    private void BuildFontStyleTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        for (var i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }
        for (var i = 0; i <= TextStyleRows.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "样式");
        AddTableHeader(grid, 0, 1, "自定义大小");
        AddTableHeader(grid, 0, 2, "自定义颜色");
        AddTableHeader(grid, 0, 3, "自定义字体");
        AddTableHeader(grid, 0, 4, "自定义字重");

        for (var i = 0; i < TextStyleRows.Length; i++)
        {
            var gridRow = i + 1;
            var row = new TextStyleRow { Segment = TextStyleRows[i].Segment };
            _textStyleRows.Add(row);

            AddTableRowLabel(grid, gridRow, TextStyleRows[i].Label, out var label);
            row.Label = label;

            AddTableCell(grid, gridRow, 1, CreateSizeCell(row));
            AddTableCell(grid, gridRow, 2, CreateColorCell(row));
            AddTableCell(grid, gridRow, 3, CreateFamilyCell(row));
            AddTableCell(grid, gridRow, 4, CreateWeightCell(row));
        }

        // 外层边框负责上/左边线，单元格负责右/下边线，拼合为完整网格。
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            IsHitTestVisible = false
        };
        outerBorder.SetResourceReference(Border.BorderBrushProperty, "MaterialDesignDivider");
        Grid.SetRowSpan(outerBorder, TextStyleRows.Length + 1);
        Grid.SetColumnSpan(outerBorder, 5);
        grid.Children.Add(outerBorder);

        FontStyleTableHost.Children.Add(grid);
    }

    private static Border CreateCellBorder(UIElement child)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(6, 3, 6, 3),
            Child = child
        };
        border.SetResourceReference(Border.BorderBrushProperty, "MaterialDesignDivider");
        return border;
    }

    private static void AddTableHeader(Grid grid, int row, int col, string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");

        var border = CreateCellBorder(textBlock);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private static void AddTableRowLabel(Grid grid, int row, string text, out TextBlock textBlock)
    {
        textBlock = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");

        var border = CreateCellBorder(textBlock);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, 0);
        grid.Children.Add(border);
    }

    private static void AddTableCell(Grid grid, int row, int col, UIElement control)
    {
        var border = CreateCellBorder(control);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private static StackPanel CreateCellPanel()
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private StackPanel CreateSizeCell(TextStyleRow row)
    {
        var panel = CreateCellPanel();

        row.EnableFontSize = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontSize.Checked += (_, _) => OnEnableCustomFontSizeChanged(row);
        row.EnableFontSize.Unchecked += (_, _) => OnEnableCustomFontSizeChanged(row);
        panel.Children.Add(row.EnableFontSize);

        row.FontSize = new WpfNumericUpDown
        {
            Width = 120,
            Minimum = 1,
            Maximum = 72,
            Increment = 1,
            FormatString = "0.00",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        row.FontSize.ValueChanged += (_, _) => OnFontSizeChanged(row);
        panel.Children.Add(row.FontSize);
        return panel;
    }

    private StackPanel CreateColorCell(TextStyleRow row)
    {
        var panel = CreateCellPanel();

        row.EnableColor = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        row.EnableColor.Checked += (_, _) => OnEnableCustomFontColorChanged(row);
        row.EnableColor.Unchecked += (_, _) => OnEnableCustomFontColorChanged(row);
        panel.Children.Add(row.EnableColor);

        row.Color = new WpfColorPicker
        {
            Width = 120,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        row.Color.ColorChanged += (_, _) => OnColorChanged(row);
        panel.Children.Add(row.Color);
        return panel;
    }

    private StackPanel CreateFamilyCell(TextStyleRow row)
    {
        var panel = CreateCellPanel();

        row.EnableFontFamily = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontFamily.Checked += (_, _) => OnEnableCustomFontFamilyChanged(row);
        row.EnableFontFamily.Unchecked += (_, _) => OnEnableCustomFontFamilyChanged(row);
        panel.Children.Add(row.EnableFontFamily);

        row.FontFamily = new ComboBox
        {
            Width = 150,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            row.FontFamily.Items.Add(font);
        }
        row.FontFamily.SelectionChanged += (_, _) => OnFontFamilyChanged(row);
        panel.Children.Add(row.FontFamily);
        return panel;
    }

    private StackPanel CreateWeightCell(TextStyleRow row)
    {
        var panel = CreateCellPanel();

        row.EnableFontWeight = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontWeight.Checked += (_, _) => OnEnableCustomFontWeightChanged(row);
        row.EnableFontWeight.Unchecked += (_, _) => OnEnableCustomFontWeightChanged(row);
        panel.Children.Add(row.EnableFontWeight);

        row.FontWeight = new ComboBox
        {
            Width = 110,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            row.FontWeight.Items.Add(weight);
        }
        row.FontWeight.SelectionChanged += (_, _) => OnFontWeightChanged(row);
        panel.Children.Add(row.FontWeight);
        return panel;
    }

    // ==================== 事件处理 ====================

    private void OnProgressDisplayModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.ProgressDisplayMode = _progressDisplayModeComboBox.SelectedIndex switch
        {
            0 => ProgressDisplayMode.None,
            1 => ProgressDisplayMode.Bar,
            2 => ProgressDisplayMode.Ring,
            3 => ProgressDisplayMode.Both,
            _ => ProgressDisplayMode.Ring
        };
    }

    private void OnTimeBaseChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.TimeBaseType = _timeBaseComboBox.SelectedIndex switch
        {
            0 => TimeBaseType.PluginOffsetServerTime,
            1 => TimeBaseType.RawServerTime,
            2 => TimeBaseType.ClassIslandTime,
            _ => TimeBaseType.PluginOffsetServerTime
        };
    }

    private void OnDisplayOptionChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingControls)
        {
            return;
        }

        Settings.ShowSegmentDivider = _showSegmentDividerCheckBox.IsChecked ?? false;
        Settings.ShowSummaryText = _showSummaryTextCheckBox.IsChecked ?? false;
        Settings.ShowHoursText = _showHoursTextCheckBox.IsChecked ?? false;
        Settings.ShowRemainingText = _showRemainingTextCheckBox.IsChecked ?? false;
        Settings.ShowPercentText = _showPercentTextCheckBox.IsChecked ?? false;
        Settings.DecimalizeHours = _decimalizeHoursCheckBox.IsChecked ?? false;
    }

    private void OnEnableCustomFontSizeChanged(TextStyleRow row)
    {
        if (_isUpdatingControls || row.Style == null)
        {
            return;
        }

        row.Style.EnableCustomFontSize = row.EnableFontSize.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorChanged(TextStyleRow row)
    {
        if (_isUpdatingControls || row.Style == null)
        {
            return;
        }

        row.Style.EnableCustomFontColor = row.EnableColor.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontFamilyChanged(TextStyleRow row)
    {
        if (_isUpdatingControls || row.Style == null)
        {
            return;
        }

        row.Style.EnableCustomFontFamily = row.EnableFontFamily.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontWeightChanged(TextStyleRow row)
    {
        if (_isUpdatingControls || row.Style == null)
        {
            return;
        }

        row.Style.EnableCustomFontWeight = row.EnableFontWeight.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnColorChanged(TextStyleRow row)
    {
        if (_isUpdatingControls || row.Style == null)
        {
            return;
        }

        row.Style.FontColor = row.Color.Color.ToString();
    }

    private void OnFontSizeChanged(TextStyleRow row)
    {
        if (_isUpdatingControls || row.Style == null)
        {
            return;
        }

        if (row.FontSize.Value.HasValue)
        {
            row.Style.FontSize = (double)row.FontSize.Value.Value;
        }
    }

    private void OnFontFamilyChanged(TextStyleRow row)
    {
        if (_isUpdatingControls || row.Style == null)
        {
            return;
        }

        if (row.FontFamily.SelectedItem != null)
        {
            row.Style.FontFamily = row.FontFamily.SelectedItem.ToString() ?? string.Empty;
        }
    }

    private void OnFontWeightChanged(TextStyleRow row)
    {
        if (_isUpdatingControls || row.Style == null)
        {
            return;
        }

        if (row.FontWeight.SelectedItem != null)
        {
            row.Style.FontWeight = row.FontWeight.SelectedItem.ToString() ?? string.Empty;
        }
    }

    private void UpdateControlsEnabled()
    {
        foreach (var row in _textStyleRows)
        {
            if (row.Style == null)
            {
                continue;
            }

            row.Color.IsEnabled = row.Style.EnableCustomFontColor;
            row.FontSize.IsEnabled = row.Style.EnableCustomFontSize;
            row.FontFamily.IsEnabled = row.Style.EnableCustomFontFamily;
            row.FontWeight.IsEnabled = row.Style.EnableCustomFontWeight;
        }
    }

    // ==================== 生命周期 ====================

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    /// <summary>
    /// 用设置值回填控件。OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，
    /// 需延迟到 Loaded 后再回填。
    /// </summary>
    private void RunInitWhenReady()
    {
        if (_initCompleted) return;
        if (Settings == null)
        {
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }

        _initCompleted = true;
        Loaded += OnLoaded;

        // 控件建好即可回填一次，避免首次进入面板时显示为空。
        UpdateControlsFromSettings();
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        UpdateControlsFromSettings();
    }

    private void UpdateControlsFromSettings()
    {
        if (Settings == null)
        {
            return;
        }

        _isUpdatingControls = true;
        try
        {
            _progressDisplayModeComboBox.SelectedIndex = Settings.ProgressDisplayMode switch
            {
                ProgressDisplayMode.None => 0,
                ProgressDisplayMode.Bar => 1,
                ProgressDisplayMode.Ring => 2,
                ProgressDisplayMode.Both => 3,
                _ => 2
            };
            _timeBaseComboBox.SelectedIndex = Settings.TimeBaseType switch
            {
                TimeBaseType.PluginOffsetServerTime => 0,
                TimeBaseType.RawServerTime => 1,
                TimeBaseType.ClassIslandTime => 2,
                _ => 0
            };
            _showSegmentDividerCheckBox.IsChecked = Settings.ShowSegmentDivider;
            _showSummaryTextCheckBox.IsChecked = Settings.ShowSummaryText;
            _showHoursTextCheckBox.IsChecked = Settings.ShowHoursText;
            _showRemainingTextCheckBox.IsChecked = Settings.ShowRemainingText;
            _showPercentTextCheckBox.IsChecked = Settings.ShowPercentText;
            _decimalizeHoursCheckBox.IsChecked = Settings.DecimalizeHours;

            foreach (var row in _textStyleRows)
            {
                var style = Settings.GetStyle(row.Segment);
                row.Style = style;
                row.EnableFontSize.IsChecked = style.EnableCustomFontSize;
                row.EnableColor.IsChecked = style.EnableCustomFontColor;
                row.EnableFontFamily.IsChecked = style.EnableCustomFontFamily;
                row.EnableFontWeight.IsChecked = style.EnableCustomFontWeight;
                row.Color.Color = ParseColor(style.FontColor);
                row.FontSize.Value = (decimal)style.FontSize;
                row.FontFamily.SelectedItem = style.FontFamily;
                row.FontWeight.SelectedItem = style.FontWeight;
            }
        }
        finally
        {
            _isUpdatingControls = false;
        }

        UpdateControlsEnabled();
    }

    private static Color ParseColor(string colorString)
    {
        try
        {
            return ThemeHelper.ParseColor(colorString);
        }
        catch
        {
            return ThemeHelper.ParseColor(ThemeHelper.GetTextColorHex());
        }
    }
}
