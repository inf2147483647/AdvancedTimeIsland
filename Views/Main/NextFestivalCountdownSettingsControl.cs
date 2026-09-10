using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class NextFestivalCountdownSettingsControl : ComponentBase<NextFestivalCountdownSettings>
{
    private TextBox? _formatTextBox;
    private NumericUpDown? _text1FontSizeNumericUpDown;
    private ColorPicker? _text1FontColorPicker;
    private NumericUpDown? _nameFontSizeNumericUpDown;
    private ColorPicker? _nameFontColorPicker;
    private NumericUpDown? _text3FontSizeNumericUpDown;
    private ColorPicker? _text3FontColorPicker;
    private NumericUpDown? _timeFontSizeNumericUpDown;
    private ColorPicker? _timeFontColorPicker;
    private ToggleSwitch? _internationalToggle;
    private ToggleSwitch? _traditionalToggle;
    private ToggleSwitch? _redToggle;
    private TextBlock? _appearanceGroupHeader;
    private ToggleSwitch? _simpleModeToggle;
    private TextBlock? _simpleModeDesc;

    private CheckBox? _text1EnableCustomFontSizeToggle;
    private CheckBox? _text1EnableCustomFontColorToggle;
    private CheckBox? _nameEnableCustomFontSizeToggle;
    private CheckBox? _nameEnableCustomFontColorToggle;
    private CheckBox? _text3EnableCustomFontSizeToggle;
    private CheckBox? _text3EnableCustomFontColorToggle;
    private CheckBox? _timeEnableCustomFontSizeToggle;
    private CheckBox? _timeEnableCustomFontColorToggle;

    private TextBlock? _formatTitle;
    private TextBlock? _formatLabel;
    private TextBlock? _formatHelpText;
    private TextBlock? _festivalTypeTitle;

    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();

    public NextFestivalCountdownSettingsControl() { InitializeComponent(); }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _formatTitle = new TextBlock { Text = "时间格式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_formatTitle);

        var formatRow = new Grid();
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _formatLabel = new TextBlock { Text = "格式化文本:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_formatLabel, 0);
        formatRow.Children.Add(_formatLabel);

        _formatTextBox = new TextBox { Width = 200, Watermark = "%d天" };
        Grid.SetColumn(_formatTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_formatTextBox, OnFormatLostFocus);
        formatRow.Children.Add(_formatTextBox);
        sp.Children.Add(formatRow);

        _formatHelpText = new TextBlock
        {
            Text = "格式化说明：%d天数，%h小时数，%m分钟数，%s秒数，%x毫秒数，%H总小时数，%M总分钟数，%S总秒数，%X总毫秒数",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };
        sp.Children.Add(_formatHelpText);

        _festivalTypeTitle = new TextBlock { Text = "节日类型", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_festivalTypeTitle);

        _internationalToggle = new ToggleSwitch { Content = "国际节日", OffContent = "", OnContent = "", HorizontalAlignment = HorizontalAlignment.Left };
        _internationalToggle.IsCheckedChanged += OnInternationalToggled;
        sp.Children.Add(_internationalToggle);

        _traditionalToggle = new ToggleSwitch { Content = "中国传统节日", OffContent = "", OnContent = "", HorizontalAlignment = HorizontalAlignment.Left };
        _traditionalToggle.IsCheckedChanged += OnTraditionalToggled;
        sp.Children.Add(_traditionalToggle);

        _redToggle = new ToggleSwitch { Content = "红色节日", OffContent = "", OnContent = "", HorizontalAlignment = HorizontalAlignment.Left };
        _redToggle.IsCheckedChanged += OnRedToggled;
        sp.Children.Add(_redToggle);

        // ==================== 外观设置 ====================
        _appearanceGroupHeader = new TextBlock { Text = "外观设置" };
        var appearanceGroup = new Expander { Header = _appearanceGroupHeader, IsExpanded = true };
        var appearancePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        _simpleModeToggle = new ToggleSwitch
        {
            Content = "简化模式",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _simpleModeToggle.IsCheckedChanged += OnSimpleModeToggleChanged;
        appearancePanel.Children.Add(_simpleModeToggle);

        _simpleModeDesc = new TextBlock
        {
            Text = "开启后，文案只显示名称与时间（文案1、文案3不再显示）。默认关闭。",
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        appearancePanel.Children.Add(_simpleModeDesc);

        appearanceGroup.Content = appearancePanel;
        sp.Children.Add(appearanceGroup);

        var styleTitle = new TextBlock { Text = "文案样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        _dynamicTextBlocks.Add(styleTitle);
        sp.Children.Add(styleTitle);

        sp.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateTextTable()
        });

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    // ==================== 文案样式表格 ====================

    private Grid CreateTextTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (int i = 0; i < 5; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "文案");
        AddTableHeader(grid, 0, 1, "自定义大小");
        AddTableHeader(grid, 0, 2, "自定义颜色");

        AddTableRowLabel(grid, 1, "文本1");
        AddTableCell(grid, 1, 1, CreateSizeCell(out _text1FontSizeNumericUpDown, out _text1EnableCustomFontSizeToggle, OnText1EnableCustomFontSizeChanged));
        AddTableCell(grid, 1, 2, CreateColorCell(out _text1FontColorPicker, out _text1EnableCustomFontColorToggle, OnText1EnableCustomFontColorChanged));

        AddTableRowLabel(grid, 2, "节日名");
        AddTableCell(grid, 2, 1, CreateSizeCell(out _nameFontSizeNumericUpDown, out _nameEnableCustomFontSizeToggle, OnNameEnableCustomFontSizeChanged));
        AddTableCell(grid, 2, 2, CreateColorCell(out _nameFontColorPicker, out _nameEnableCustomFontColorToggle, OnNameEnableCustomFontColorChanged));

        AddTableRowLabel(grid, 3, "文本3");
        AddTableCell(grid, 3, 1, CreateSizeCell(out _text3FontSizeNumericUpDown, out _text3EnableCustomFontSizeToggle, OnText3EnableCustomFontSizeChanged));
        AddTableCell(grid, 3, 2, CreateColorCell(out _text3FontColorPicker, out _text3EnableCustomFontColorToggle, OnText3EnableCustomFontColorChanged));

        AddTableRowLabel(grid, 4, "时间");
        AddTableCell(grid, 4, 1, CreateSizeCell(out _timeFontSizeNumericUpDown, out _timeEnableCustomFontSizeToggle, OnTimeEnableCustomFontSizeChanged));
        AddTableCell(grid, 4, 2, CreateColorCell(out _timeFontColorPicker, out _timeEnableCustomFontColorToggle, OnTimeEnableCustomFontColorChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 5);
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

    private StackPanel CreateSizeCell(out NumericUpDown? numericUpDown, out CheckBox? toggle, EventHandler<RoutedEventArgs> toggleHandler)
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
        numericUpDown.ValueChanged += OnSizeCellValueChanged;
        panel.Children.Add(numericUpDown);
        return panel;
    }

    private static StackPanel CreateColorCell(out ColorPicker? colorPicker, out CheckBox? toggle, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        colorPicker = new ColorPicker { Width = 120, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(colorPicker);
        return panel;
    }

    private void OnSizeCellValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        switch (sender)
        {
            case NumericUpDown nud when nud.Value.HasValue:
                if (nud == _text1FontSizeNumericUpDown) Settings.Text1FontSize = (double)nud.Value.Value;
                else if (nud == _nameFontSizeNumericUpDown) Settings.NameFontSize = (double)nud.Value.Value;
                else if (nud == _text3FontSizeNumericUpDown) Settings.Text3FontSize = (double)nud.Value.Value;
                else if (nud == _timeFontSizeNumericUpDown) Settings.TimeFontSize = (double)nud.Value.Value;
                break;
        }
    }

    private void UpdateThemeColors()
    {
        if (_formatTitle != null) _formatTitle.Foreground = ThemeHelper.GetTextBrush();
        if (_formatLabel != null) _formatLabel.Foreground = ThemeHelper.GetTextBrush();
        if (_formatHelpText != null) _formatHelpText.Foreground = ThemeHelper.GetGrayBrush();
        if (_festivalTypeTitle != null) _festivalTypeTitle.Foreground = ThemeHelper.GetTextBrush();
        if (_internationalToggle != null) _internationalToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_traditionalToggle != null) _traditionalToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_redToggle != null) _redToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_appearanceGroupHeader != null) _appearanceGroupHeader.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeToggle != null) _simpleModeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_simpleModeDesc != null) _simpleModeDesc.Foreground = ThemeHelper.GetGrayBrush();

        if (_text1EnableCustomFontSizeToggle != null) _text1EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text1EnableCustomFontColorToggle != null) _text1EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_nameEnableCustomFontSizeToggle != null) _nameEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_nameEnableCustomFontColorToggle != null) _nameEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text3EnableCustomFontSizeToggle != null) _text3EnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_text3EnableCustomFontColorToggle != null) _text3EnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeEnableCustomFontSizeToggle != null) _timeEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        if (_timeEnableCustomFontColorToggle != null) _timeEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();

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

    private void OnText1EnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.Text1EnableCustomFontSize = _text1EnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.Text1EnableCustomFontColor = _text1EnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.NameEnableCustomFontSize = _nameEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.NameEnableCustomFontColor = _nameEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.Text3EnableCustomFontSize = _text3EnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.Text3EnableCustomFontColor = _text3EnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.TimeEnableCustomFontSize = _timeEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.TimeEnableCustomFontColor = _timeEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        if (_text1FontSizeNumericUpDown != null) _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        if (_text1FontColorPicker != null) _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        if (_nameFontSizeNumericUpDown != null) _nameFontSizeNumericUpDown.IsEnabled = Settings.NameEnableCustomFontSize;
        if (_nameFontColorPicker != null) _nameFontColorPicker.IsEnabled = Settings.NameEnableCustomFontColor;
        if (_text3FontSizeNumericUpDown != null) _text3FontSizeNumericUpDown.IsEnabled = Settings.Text3EnableCustomFontSize;
        if (_text3FontColorPicker != null) _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        if (_timeFontSizeNumericUpDown != null) _timeFontSizeNumericUpDown.IsEnabled = Settings.TimeEnableCustomFontSize;
        if (_timeFontColorPicker != null) _timeFontColorPicker.IsEnabled = Settings.TimeEnableCustomFontColor;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        if (_formatTextBox != null) _formatTextBox.Text = Settings.TimeFormat;

        if (_text1FontSizeNumericUpDown != null) _text1FontSizeNumericUpDown.Value = (decimal)Settings.Text1FontSize;
        if (_text1FontColorPicker != null) _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        if (_nameFontSizeNumericUpDown != null) _nameFontSizeNumericUpDown.Value = (decimal)Settings.NameFontSize;
        if (_nameFontColorPicker != null) _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        if (_text3FontSizeNumericUpDown != null) _text3FontSizeNumericUpDown.Value = (decimal)Settings.Text3FontSize;
        if (_text3FontColorPicker != null) _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        if (_timeFontSizeNumericUpDown != null) _timeFontSizeNumericUpDown.Value = (decimal)Settings.TimeFontSize;
        if (_timeFontColorPicker != null) _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);

        if (_internationalToggle != null) _internationalToggle.IsChecked = Settings.EnableInternationalFestivals;
        if (_traditionalToggle != null) _traditionalToggle.IsChecked = Settings.EnableChineseTraditionalFestivals;
        if (_redToggle != null) _redToggle.IsChecked = Settings.EnableRedFestivals;
        if (_simpleModeToggle != null) _simpleModeToggle.IsChecked = Settings.EnableSimpleMode;

        if (_text1EnableCustomFontSizeToggle != null) _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        if (_text1EnableCustomFontColorToggle != null) _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        if (_nameEnableCustomFontSizeToggle != null) _nameEnableCustomFontSizeToggle.IsChecked = Settings.NameEnableCustomFontSize;
        if (_nameEnableCustomFontColorToggle != null) _nameEnableCustomFontColorToggle.IsChecked = Settings.NameEnableCustomFontColor;
        if (_text3EnableCustomFontSizeToggle != null) _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        if (_text3EnableCustomFontColorToggle != null) _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        if (_timeEnableCustomFontSizeToggle != null) _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        if (_timeEnableCustomFontColorToggle != null) _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;

        UpdateControlsEnabled();

        if (_text1FontColorPicker != null) _text1FontColorPicker.ColorChanged += OnText1ColorChanged;
        if (_nameFontColorPicker != null) _nameFontColorPicker.ColorChanged += OnNameColorChanged;
        if (_text3FontColorPicker != null) _text3FontColorPicker.ColorChanged += OnText3ColorChanged;
        if (_timeFontColorPicker != null) _timeFontColorPicker.ColorChanged += OnTimeColorChanged;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
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

    private void OnFormatLostFocus(object? sender, RoutedEventArgs e) { Settings.TimeFormat = _formatTextBox?.Text ?? "%d天"; }

    private void OnText1ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_text1FontColorPicker != null) Settings.Text1FontColor = _text1FontColorPicker.Color.ToString();
    }

    private void OnNameColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_nameFontColorPicker != null) Settings.NameFontColor = _nameFontColorPicker.Color.ToString();
    }

    private void OnText3ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_text3FontColorPicker != null) Settings.Text3FontColor = _text3FontColorPicker.Color.ToString();
    }

    private void OnTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_timeFontColorPicker != null) Settings.TimeFontColor = _timeFontColorPicker.Color.ToString();
    }

    private void OnInternationalToggled(object? sender, EventArgs e) => Settings.EnableInternationalFestivals = _internationalToggle?.IsChecked ?? true;

    private void OnTraditionalToggled(object? sender, EventArgs e) => Settings.EnableChineseTraditionalFestivals = _traditionalToggle?.IsChecked ?? true;

    private void OnRedToggled(object? sender, EventArgs e) => Settings.EnableRedFestivals = _redToggle?.IsChecked ?? true;

    private void OnSimpleModeToggleChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableSimpleMode = _simpleModeToggle?.IsChecked ?? false;
    }
}
