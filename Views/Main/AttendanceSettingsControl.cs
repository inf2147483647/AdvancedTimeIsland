using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Views.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 总在校时间统计（ATI）组件的设置面板：只包含本组件实例的显示选项；
/// 学期区间、节假日与寒暑假等统计口径由插件设置页中的「在校时间统计」统一管理。
/// </summary>
public class AttendanceSettingsControl : ComponentBase<AttendanceSettings>
{
    /// <summary>字体样式表格中一行所对应的段落与其控件集合。</summary>
    private sealed class TextStyleRow
    {
        public AttendanceTextSegment Segment;
        public TextBlock Label = null!;
        public NumericUpDown FontSize = null!;
        public CheckBox EnableFontSize = null!;
        public ColorPicker Color = null!;
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

    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;
    private TextBlock _scopeHintTextBlock;
    private TextBlock _textStyleHintTextBlock;
    private readonly List<TextBlock> _labels = new();
    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();
    private readonly List<CheckBox> _checkBoxes = new();
    private readonly List<TextStyleRow> _textStyleRows = new();

    private ComboBox _progressDisplayModeComboBox;
    private ComboBox _timeBaseComboBox;
    private CheckBox _showSegmentDividerCheckBox;
    private CheckBox _showSummaryTextCheckBox;
    private CheckBox _showHoursTextCheckBox;
    private CheckBox _showRemainingTextCheckBox;
    private CheckBox _showPercentTextCheckBox;
    private CheckBox _decimalizeHoursCheckBox;

    /// <summary>程序化回填控件时置位，避免控件事件把设置改回控件中的旧值。</summary>
    private bool _isUpdatingControls;

    private static readonly object[] ProgressDisplayModeItems =
    {
        "不显示", "进度条", "进度环", "进度条 + 进度环"
    };

    private static readonly object[] TimeBaseItems =
    {
        "插件偏移后的服务器时间", "原始服务器时间", "ClassIsland时间"
    };

    public AttendanceSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "总在校时间统计（ATI）设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock
        {
            Text = "统计学期内的在校天数与时长，自动排除周末、法定节假日与寒暑假，并展示学期进度与剩余量。",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        sp.Children.Add(_descTextBlock);

        _scopeHintTextBlock = new TextBlock
        {
            Text = "学期起止、每日在校时长、节假日与寒暑假等统计口径，请前往插件设置页的「在校时间统计」中配置。",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        sp.Children.Add(_scopeHintTextBlock);

        // ==================== 显示设置 ====================
        var displayPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var progressModeRow = FontSettingsRowFactory.CreateComboBoxRow("进度显示方式", ProgressDisplayModeItems,
            out var progressModeLabel, out _progressDisplayModeComboBox, out _,
            selectionChangedHandler: OnProgressDisplayModeChanged);
        _labels.Add(progressModeLabel);
        displayPanel.Children.Add(progressModeRow);

        _showSegmentDividerCheckBox = AddCheckBox(displayPanel, "在各段文案之间插入分割线");
        _showSummaryTextCheckBox = AddCheckBox(displayPanel, "显示概要文案（已在校 N 天 / 共 M 天）");
        _showHoursTextCheckBox = AddCheckBox(displayPanel, "显示累计在校时长");
        _showRemainingTextCheckBox = AddCheckBox(displayPanel, "显示剩余在校天数与时长");
        _showPercentTextCheckBox = AddCheckBox(displayPanel, "显示进度百分比");
        _decimalizeHoursCheckBox = AddCheckBox(displayPanel, "时长保留一位小数（关闭则取整）");

        sp.Children.Add(SettingsGroupFactory.Create("显示设置", displayPanel));

        // ==================== 时间设置 ====================
        var timePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var timeBaseRow = FontSettingsRowFactory.CreateComboBoxRow("时间基准", TimeBaseItems,
            out var timeBaseLabel, out _timeBaseComboBox, out _,
            selectionChangedHandler: OnTimeBaseChanged);
        _labels.Add(timeBaseLabel);
        timePanel.Children.Add(timeBaseRow);

        sp.Children.Add(SettingsGroupFactory.Create("时间设置", timePanel));

        // ==================== 文案设置 ====================
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _textStyleHintTextBlock = new TextBlock
        {
            Text = "组件的文案分为五段，可分别设置字体样式；未开启自定义的项随主题与全局字号自适应。",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        textPanel.Children.Add(_textStyleHintTextBlock);

        var tableScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateFontStyleTable()
        };
        textPanel.Children.Add(tableScroll);

        textPanel.Children.Add(FontSettingsRowFactory.CreateFontWeightHintTextBlock());

        sp.Children.Add(SettingsGroupFactory.Create("文案设置", textPanel));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    private CheckBox AddCheckBox(StackPanel panel, string text)
    {
        var checkBox = new CheckBox { Content = text, VerticalAlignment = VerticalAlignment.Center };
        checkBox.IsCheckedChanged += OnDisplayOptionChanged;
        _checkBoxes.Add(checkBox);
        panel.Children.Add(checkBox);
        return checkBox;
    }

    // ==================== 字体样式表格 ====================

    private Grid CreateFontStyleTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }
        for (int i = 0; i <= TextStyleRows.Length; i++)
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

        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, TextStyleRows.Length + 1);
        Grid.SetColumnSpan(outerBorder, 5);
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

    private void AddTableRowLabel(Grid grid, int row, string text, out TextBlock textBlock)
    {
        textBlock = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(textBlock);
        var border = CreateCellBorder(textBlock);
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

    private StackPanel CreateSizeCell(TextStyleRow row)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontSize = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontSize.IsCheckedChanged += (_, _) => OnEnableCustomFontSizeChanged(row);
        _checkBoxes.Add(row.EnableFontSize);
        panel.Children.Add(row.EnableFontSize);

        row.FontSize = new NumericUpDown
        {
            Width = 120,
            Minimum = 1,
            Maximum = 72,
            Increment = 1m,
            FormatString = "0.00",
            VerticalAlignment = VerticalAlignment.Center
        };
        row.FontSize.ValueChanged += (_, _) => OnFontSizeChanged(row);
        panel.Children.Add(row.FontSize);
        return panel;
    }

    private StackPanel CreateColorCell(TextStyleRow row)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        row.EnableColor = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        row.EnableColor.IsCheckedChanged += (_, _) => OnEnableCustomFontColorChanged(row);
        _checkBoxes.Add(row.EnableColor);
        panel.Children.Add(row.EnableColor);

        row.Color = new ColorPicker { Width = 120, VerticalAlignment = VerticalAlignment.Center };
        row.Color.ColorChanged += (_, _) => OnColorChanged(row);
        panel.Children.Add(row.Color);
        return panel;
    }

    private StackPanel CreateFamilyCell(TextStyleRow row)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontFamily = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontFamily.IsCheckedChanged += (_, _) => OnEnableCustomFontFamilyChanged(row);
        _checkBoxes.Add(row.EnableFontFamily);
        panel.Children.Add(row.EnableFontFamily);

        row.FontFamily = new ComboBox { Width = 150, VerticalAlignment = VerticalAlignment.Center };
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
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontWeight = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        row.EnableFontWeight.IsCheckedChanged += (_, _) => OnEnableCustomFontWeightChanged(row);
        _checkBoxes.Add(row.EnableFontWeight);
        panel.Children.Add(row.EnableFontWeight);

        row.FontWeight = new ComboBox { Width = 110, VerticalAlignment = VerticalAlignment.Center };
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            row.FontWeight.Items.Add(weight);
        }
        row.FontWeight.SelectionChanged += (_, _) => OnFontWeightChanged(row);
        panel.Children.Add(row.FontWeight);
        return panel;
    }

    // ==================== 事件处理 ====================

    private void OnProgressDisplayModeChanged(object? sender, SelectionChangedEventArgs e)
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

    private void OnTimeBaseChanged(object? sender, SelectionChangedEventArgs e)
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

    private void OnDisplayOptionChanged(object? sender, RoutedEventArgs e)
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

    private void UpdateThemeColors()
    {
        var textBrush = ThemeHelper.GetTextBrush();
        foreach (var label in _labels)
        {
            label.Foreground = textBrush;
        }
        foreach (var checkBox in _checkBoxes)
        {
            checkBox.Foreground = textBrush;
        }
        foreach (var tb in _dynamicTextBlocks)
        {
            tb.Foreground = textBrush;
        }
        var separatorBrush = ThemeHelper.GetSeparatorBrush();
        foreach (var border in _tableCellBorders)
        {
            border.BorderBrush = separatorBrush;
        }
        _titleTextBlock.Foreground = textBrush;
        _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _scopeHintTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _textStyleHintTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e) => UpdateThemeColors();

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

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private static Color ParseColor(string colorString)
    {
        try
        {
            return Color.Parse(colorString);
        }
        catch
        {
            return Color.Parse(ThemeHelper.GetTextColorHex());
        }
    }
}