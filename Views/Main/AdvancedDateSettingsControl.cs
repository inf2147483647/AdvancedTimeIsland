using System;
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

public class AdvancedDateSettingsControl : ComponentBase<AdvancedDateSettings>
{
    private ToggleSwitch _showWeekDayToggle;
    private ComboBox _contentOrderComboBox;
    private ComboBox _dateSeparatorComboBox;

    private ToggleSwitch _dateEnableCustomFontSizeToggle;
    private ToggleSwitch _dateEnableCustomFontColorToggle;
    private ToggleSwitch _dateEnableCustomFontFamilyToggle;
    private ToggleSwitch _dateEnableCustomFontWeightToggle;
    private ColorPicker _dateColorPicker;
    private NumericUpDown _dateFontSizeNumericUpDown;
    private ComboBox _dateFontFamilyComboBox;
    private ComboBox _dateFontWeightComboBox;

    private ToggleSwitch _weekDayEnableCustomFontSizeToggle;
    private ToggleSwitch _weekDayEnableCustomFontColorToggle;
    private ToggleSwitch _weekDayEnableCustomFontFamilyToggle;
    private ToggleSwitch _weekDayEnableCustomFontWeightToggle;
    private ColorPicker _weekDayColorPicker;
    private NumericUpDown _weekDayFontSizeNumericUpDown;
    private ComboBox _weekDayFontFamilyComboBox;
    private ComboBox _weekDayFontWeightComboBox;

    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;
    private TextBlock _labelTextBlock;
    private TextBlock _contentOrderLabelTextBlock;
    private TextBlock _dateSeparatorLabelTextBlock;
    private TextBlock _dateTitleTextBlock;
    private TextBlock _dateColorLabelTextBlock;
    private TextBlock _dateFontSizeLabelTextBlock;
    private TextBlock _weekDayTitleTextBlock;
    private TextBlock _weekDayColorLabelTextBlock;
    private TextBlock _weekDayFontSizeLabelTextBlock;

    public AdvancedDateSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "日期设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "配置日期显示选项", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_descTextBlock);

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        _labelTextBlock = new TextBlock { Text = "显示星期", FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_labelTextBlock, 0);
        row.Children.Add(_labelTextBlock);

        _showWeekDayToggle = new ToggleSwitch();
        _showWeekDayToggle.IsCheckedChanged += OnShowWeekDayChanged;
        Grid.SetColumn(_showWeekDayToggle, 1);
        row.Children.Add(_showWeekDayToggle);

        sp.Children.Add(row);

        var contentOrderRow = new Grid();
        contentOrderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentOrderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _contentOrderLabelTextBlock = new TextBlock { Text = "内容组合", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_contentOrderLabelTextBlock, 0);
        contentOrderRow.Children.Add(_contentOrderLabelTextBlock);

        _contentOrderComboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 150 };
        _contentOrderComboBox.Items.Add("日期-星期");
        _contentOrderComboBox.Items.Add("星期-日期");
        _contentOrderComboBox.SelectedIndex = 0;
        _contentOrderComboBox.SelectionChanged += OnContentOrderChanged;
        Grid.SetColumn(_contentOrderComboBox, 1);
        contentOrderRow.Children.Add(_contentOrderComboBox);
        sp.Children.Add(contentOrderRow);

        var dateSeparatorRow = new Grid();
        dateSeparatorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dateSeparatorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _dateSeparatorLabelTextBlock = new TextBlock { Text = "日期分隔符", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_dateSeparatorLabelTextBlock, 0);
        dateSeparatorRow.Children.Add(_dateSeparatorLabelTextBlock);

        _dateSeparatorComboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 150 };
        _dateSeparatorComboBox.Items.Add("- (2026-07-29)");
        _dateSeparatorComboBox.Items.Add("/ (2026/07/29)");
        _dateSeparatorComboBox.Items.Add(". (2026.07.29)");
        _dateSeparatorComboBox.Items.Add("纯文本 (2026 年 7 月 29 日)");
        _dateSeparatorComboBox.SelectedIndex = 0;
        _dateSeparatorComboBox.SelectionChanged += OnDateSeparatorChanged;
        Grid.SetColumn(_dateSeparatorComboBox, 1);
        dateSeparatorRow.Children.Add(_dateSeparatorComboBox);
        sp.Children.Add(dateSeparatorRow);

        _dateTitleTextBlock = new TextBlock { Text = "日期样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_dateTitleTextBlock);

        sp.Children.Add(CreateFontSizeRow("文本大小", out _dateFontSizeLabelTextBlock, out _dateFontSizeNumericUpDown, out _dateEnableCustomFontSizeToggle, OnDateFontSizeChanged, OnDateEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _dateColorLabelTextBlock, out _dateColorPicker, out _dateEnableCustomFontColorToggle, OnDateColorChanged, OnDateEnableCustomFontColorChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _dateFontFamilyComboBox, out _dateEnableCustomFontFamilyToggle, OnDateEnableCustomFontFamilyChanged, OnDateFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _dateFontWeightComboBox, out _dateEnableCustomFontWeightToggle, OnDateEnableCustomFontWeightChanged, OnDateFontWeightChanged));
        sp.Children.Add(CreateFontWeightHintTextBlock());

        _weekDayTitleTextBlock = new TextBlock { Text = "星期样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_weekDayTitleTextBlock);

        sp.Children.Add(CreateFontSizeRow("文本大小", out _weekDayFontSizeLabelTextBlock, out _weekDayFontSizeNumericUpDown, out _weekDayEnableCustomFontSizeToggle, OnWeekDayFontSizeChanged, OnWeekDayEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _weekDayColorLabelTextBlock, out _weekDayColorPicker, out _weekDayEnableCustomFontColorToggle, OnWeekDayColorChanged, OnWeekDayEnableCustomFontColorChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _weekDayFontFamilyComboBox, out _weekDayEnableCustomFontFamilyToggle, OnWeekDayEnableCustomFontFamilyChanged, OnWeekDayFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _weekDayFontWeightComboBox, out _weekDayEnableCustomFontWeightToggle, OnWeekDayEnableCustomFontWeightChanged, OnWeekDayFontWeightChanged));
        sp.Children.Add(CreateFontWeightHintTextBlock());

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    private Grid CreateFontSizeRow(string labelText, out TextBlock label, out NumericUpDown numericUpDown, out ToggleSwitch toggle,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
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
            Maximum = 72,
            Increment = 1m,
            FormatString = "0.00",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        Grid.SetColumn(numericUpDown, 1);
        row.Children.Add(numericUpDown);

        toggle = new ToggleSwitch { Content = "启用自定义文本大小", Margin = new Thickness(30, 0, 0, 0) };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateColorRow(string labelText, out TextBlock label, out ColorPicker colorPicker, out ToggleSwitch toggle,
        EventHandler<ColorChangedEventArgs> colorChangedHandler, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
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

        toggle = new ToggleSwitch { Content = "启用自定义文本颜色", Margin = new Thickness(30, 0, 0, 0) };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateFontFamilyRow(string labelText, out ComboBox comboBox, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        comboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            comboBox.Items.Add(font);
        }
        comboBox.SelectionChanged += selectionChangedHandler;
        Grid.SetColumn(comboBox, 1);
        row.Children.Add(comboBox);

        toggle = new ToggleSwitch { Content = "启用自定义字体样式", Margin = new Thickness(30, 0, 0, 0) };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateFontWeightRow(string labelText, out ComboBox comboBox, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        comboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            comboBox.Items.Add(weight);
        }
        comboBox.SelectionChanged += selectionChangedHandler;
        Grid.SetColumn(comboBox, 1);
        row.Children.Add(comboBox);

        toggle = new ToggleSwitch { Content = "启用自定义字重", Margin = new Thickness(30, 0, 0, 0) };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private TextBlock CreateFontWeightHintTextBlock()
    {
        return new TextBlock
        {
            Text = "需要对应字体支持所选字重",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.Orange,
            Margin = new Thickness(0, 2, 0, 0)
        };
    }

    private void UpdateThemeColors()
    {
        _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _labelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _contentOrderLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dateSeparatorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dateEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _dateEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _dateEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _dateTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dateColorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dateFontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _weekDayEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _weekDayEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _weekDayEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _weekDayTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _weekDayColorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _weekDayFontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void OnDateEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontSize = _dateEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontColor = _dateEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontFamily = _dateEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontWeight = _dateEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.WeekDayEnableCustomFontSize = _weekDayEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.WeekDayEnableCustomFontColor = _weekDayEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.WeekDayEnableCustomFontFamily = _weekDayEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnWeekDayEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.WeekDayEnableCustomFontWeight = _weekDayEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnDateFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_dateFontFamilyComboBox.SelectedItem != null)
        {
            Settings.FontFamily = _dateFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnDateFontWeightChanged(object? sender, EventArgs e)
    {
        if (_dateFontWeightComboBox.SelectedItem != null)
        {
            Settings.FontWeight = _dateFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnWeekDayFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_weekDayFontFamilyComboBox.SelectedItem != null)
        {
            Settings.WeekDayFontFamily = _weekDayFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnWeekDayFontWeightChanged(object? sender, EventArgs e)
    {
        if (_weekDayFontWeightComboBox.SelectedItem != null)
        {
            Settings.WeekDayFontWeight = _weekDayFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        var dateFontSizeEnabled = Settings.EnableCustomFontSize;
        var dateFontColorEnabled = Settings.EnableCustomFontColor;
        var dateFontFamilyEnabled = Settings.EnableCustomFontFamily;
        var dateFontWeightEnabled = Settings.EnableCustomFontWeight;
        _dateColorPicker.IsEnabled = dateFontColorEnabled;
        _dateFontSizeNumericUpDown.IsEnabled = dateFontSizeEnabled;
        _dateFontFamilyComboBox.IsEnabled = dateFontFamilyEnabled;
        _dateFontWeightComboBox.IsEnabled = dateFontWeightEnabled;

        var weekDayEnabled = Settings.ShowWeekDay;
        var weekDayFontSizeEnabled = weekDayEnabled && Settings.WeekDayEnableCustomFontSize;
        var weekDayFontColorEnabled = weekDayEnabled && Settings.WeekDayEnableCustomFontColor;
        var weekDayFontFamilyEnabled = weekDayEnabled && Settings.WeekDayEnableCustomFontFamily;
        var weekDayFontWeightEnabled = weekDayEnabled && Settings.WeekDayEnableCustomFontWeight;
        _weekDayColorPicker.IsEnabled = weekDayFontColorEnabled;
        _weekDayFontSizeNumericUpDown.IsEnabled = weekDayFontSizeEnabled;
        _weekDayFontFamilyComboBox.IsEnabled = weekDayFontFamilyEnabled;
        _weekDayFontWeightComboBox.IsEnabled = weekDayFontWeightEnabled;
        _weekDayEnableCustomFontSizeToggle.IsEnabled = weekDayEnabled;
        _weekDayEnableCustomFontColorToggle.IsEnabled = weekDayEnabled;
        _weekDayEnableCustomFontFamilyToggle.IsEnabled = weekDayEnabled;
        _weekDayEnableCustomFontWeightToggle.IsEnabled = weekDayEnabled;
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
        _showWeekDayToggle.IsChecked = Settings.ShowWeekDay;
    _contentOrderComboBox.SelectedIndex = Settings.DateContentOrder;
    _dateSeparatorComboBox.SelectedIndex = Settings.DateSeparator;
    _dateEnableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        _dateEnableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
        _dateEnableCustomFontFamilyToggle.IsChecked = Settings.EnableCustomFontFamily;
        _dateEnableCustomFontWeightToggle.IsChecked = Settings.EnableCustomFontWeight;
        _weekDayEnableCustomFontSizeToggle.IsChecked = Settings.WeekDayEnableCustomFontSize;
        _weekDayEnableCustomFontColorToggle.IsChecked = Settings.WeekDayEnableCustomFontColor;
        _weekDayEnableCustomFontFamilyToggle.IsChecked = Settings.WeekDayEnableCustomFontFamily;
        _weekDayEnableCustomFontWeightToggle.IsChecked = Settings.WeekDayEnableCustomFontWeight;
        UpdateControlsEnabled();
        _dateColorPicker.Color = ParseColor(Settings.FontColor);
        _dateFontSizeNumericUpDown.Value = (decimal)Settings.DateFontSize;
        _dateFontFamilyComboBox.SelectedItem = Settings.FontFamily;
        _dateFontWeightComboBox.SelectedItem = Settings.FontWeight;
        _weekDayColorPicker.Color = ParseColor(Settings.WeekDayFontColor);
        _weekDayFontSizeNumericUpDown.Value = (decimal)Settings.WeekDayFontSize;
        _weekDayFontFamilyComboBox.SelectedItem = Settings.WeekDayFontFamily;
        _weekDayFontWeightComboBox.SelectedItem = Settings.WeekDayFontWeight;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnShowWeekDayChanged(object? sender, EventArgs e)
    {
        Settings.ShowWeekDay = _showWeekDayToggle.IsChecked == true;
        UpdateControlsEnabled();
    }

    private void OnContentOrderChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_contentOrderComboBox.SelectedIndex >= 0)
        {
            Settings.DateContentOrder = _contentOrderComboBox.SelectedIndex;
        }
    }

    private void OnDateSeparatorChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_dateSeparatorComboBox.SelectedIndex >= 0)
        {
            Settings.DateSeparator = _dateSeparatorComboBox.SelectedIndex;
        }
    }

    private void OnDateColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.FontColor = _dateColorPicker.Color.ToString();
    }

    private void OnDateFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_dateFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.DateFontSize = (double)_dateFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnWeekDayColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.WeekDayFontColor = _weekDayColorPicker.Color.ToString();
    }

    private Color ParseColor(string colorString)
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

    private void OnWeekDayFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_weekDayFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.WeekDayFontSize = (double)_weekDayFontSizeNumericUpDown.Value.Value;
        }
    }
}
