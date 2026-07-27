using System;
using System.Globalization;
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
    private TextBox _formatTextBox;
    private NumericUpDown _text1FontSizeNumericUpDown;
    private ColorPicker _text1FontColorPicker;
    private NumericUpDown _nameFontSizeNumericUpDown;
    private ColorPicker _nameFontColorPicker;
    private NumericUpDown _text3FontSizeNumericUpDown;
    private ColorPicker _text3FontColorPicker;
    private NumericUpDown _timeFontSizeNumericUpDown;
    private ColorPicker _timeFontColorPicker;
    private ToggleSwitch _internationalToggle;
    private ToggleSwitch _traditionalToggle;
    private ToggleSwitch _redToggle;

    private ToggleSwitch? _text1EnableCustomFontSizeToggle;
    private ToggleSwitch? _text1EnableCustomFontColorToggle;
    private ToggleSwitch? _nameEnableCustomFontSizeToggle;
    private ToggleSwitch? _nameEnableCustomFontColorToggle;
    private ToggleSwitch? _text3EnableCustomFontSizeToggle;
    private ToggleSwitch? _text3EnableCustomFontColorToggle;
    private ToggleSwitch? _timeEnableCustomFontSizeToggle;
    private ToggleSwitch? _timeEnableCustomFontColorToggle;

    private TextBlock _formatTitle;
    private TextBlock _formatLabel;
    private TextBlock _formatHelpText;
    private TextBlock _festivalTypeTitle;
    private TextBlock _text1Title;
    private TextBlock _text1ColorLabel;
    private TextBlock _text1FontSizeLabel;
    private TextBlock _nameTitle;
    private TextBlock _nameColorLabel;
    private TextBlock _nameFontSizeLabel;
    private TextBlock _text3Title;
    private TextBlock _text3ColorLabel;
    private TextBlock _text3FontSizeLabel;
    private TextBlock _timeTitle;
    private TextBlock _timeColorLabel;
    private TextBlock _timeFontSizeLabel;

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

        _text1Title = new TextBlock { Text = "文本1样式", FontSize = 14, FontWeight = FontWeight.Bold };
        var text1TitleRow = CreateTitleRow(_text1Title, out _text1EnableCustomFontSizeToggle, out _text1EnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnText1EnableCustomFontSizeChanged, OnText1EnableCustomFontColorChanged, null, null);
        text1TitleRow.Margin = new Thickness(0, 10, 0, 0);
        sp.Children.Add(text1TitleRow);
        sp.Children.Add(CreateFontSizeRow("文本大小", out _text1FontSizeLabel, out _text1FontSizeNumericUpDown, OnText1FontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _text1ColorLabel, out _text1FontColorPicker, OnText1ColorChanged));

        _nameTitle = new TextBlock { Text = "节日名样式", FontSize = 14, FontWeight = FontWeight.Bold };
        var nameTitleRow = CreateTitleRow(_nameTitle, out _nameEnableCustomFontSizeToggle, out _nameEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnNameEnableCustomFontSizeChanged, OnNameEnableCustomFontColorChanged, null, null);
        nameTitleRow.Margin = new Thickness(0, 10, 0, 0);
        sp.Children.Add(nameTitleRow);
        sp.Children.Add(CreateFontSizeRow("文本大小", out _nameFontSizeLabel, out _nameFontSizeNumericUpDown, OnNameFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _nameColorLabel, out _nameFontColorPicker, OnNameColorChanged));

        _text3Title = new TextBlock { Text = "文本3样式", FontSize = 14, FontWeight = FontWeight.Bold };
        var text3TitleRow = CreateTitleRow(_text3Title, out _text3EnableCustomFontSizeToggle, out _text3EnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnText3EnableCustomFontSizeChanged, OnText3EnableCustomFontColorChanged, null, null);
        text3TitleRow.Margin = new Thickness(0, 10, 0, 0);
        sp.Children.Add(text3TitleRow);
        sp.Children.Add(CreateFontSizeRow("文本大小", out _text3FontSizeLabel, out _text3FontSizeNumericUpDown, OnText3FontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _text3ColorLabel, out _text3FontColorPicker, OnText3ColorChanged));

        _timeTitle = new TextBlock { Text = "时间样式", FontSize = 14, FontWeight = FontWeight.Bold };
        var timeTitleRow = CreateTitleRow(_timeTitle, out _timeEnableCustomFontSizeToggle, out _timeEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnTimeEnableCustomFontSizeChanged, OnTimeEnableCustomFontColorChanged, null, null);
        timeTitleRow.Margin = new Thickness(0, 10, 0, 0);
        sp.Children.Add(timeTitleRow);
        sp.Children.Add(CreateFontSizeRow("文本大小", out _timeFontSizeLabel, out _timeFontSizeNumericUpDown, OnTimeFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _timeColorLabel, out _timeFontColorPicker, OnTimeColorChanged));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
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

    private void UpdateThemeColors()
    {
        _formatTitle.Foreground = ThemeHelper.GetTextBrush();
        _formatLabel.Foreground = ThemeHelper.GetTextBrush();
        _formatHelpText.Foreground = ThemeHelper.GetGrayBrush();
        _festivalTypeTitle.Foreground = ThemeHelper.GetTextBrush();
        _text1Title.Foreground = ThemeHelper.GetTextBrush();
        _text1ColorLabel.Foreground = ThemeHelper.GetTextBrush();
        _text1FontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _nameTitle.Foreground = ThemeHelper.GetTextBrush();
        _nameColorLabel.Foreground = ThemeHelper.GetTextBrush();
        _nameFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _text3Title.Foreground = ThemeHelper.GetTextBrush();
        _text3ColorLabel.Foreground = ThemeHelper.GetTextBrush();
        _text3FontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _timeTitle.Foreground = ThemeHelper.GetTextBrush();
        _timeColorLabel.Foreground = ThemeHelper.GetTextBrush();
        _timeFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
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
        _text1FontSizeNumericUpDown.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _nameFontSizeNumericUpDown.IsEnabled = Settings.NameEnableCustomFontSize;
        _nameFontColorPicker.IsEnabled = Settings.NameEnableCustomFontColor;
        _text3FontSizeNumericUpDown.IsEnabled = Settings.Text3EnableCustomFontSize;
        _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        _timeFontSizeNumericUpDown.IsEnabled = Settings.TimeEnableCustomFontSize;
        _timeFontColorPicker.IsEnabled = Settings.TimeEnableCustomFontColor;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        _formatTextBox.Text = Settings.TimeFormat;

        _text1FontSizeNumericUpDown.Value = (decimal)Settings.Text1FontSize;
        _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        _nameFontSizeNumericUpDown.Value = (decimal)Settings.NameFontSize;
        _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        _text3FontSizeNumericUpDown.Value = (decimal)Settings.Text3FontSize;
        _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        _timeFontSizeNumericUpDown.Value = (decimal)Settings.TimeFontSize;
        _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);

        _internationalToggle.IsChecked = Settings.EnableInternationalFestivals;
        _traditionalToggle.IsChecked = Settings.EnableChineseTraditionalFestivals;
        _redToggle.IsChecked = Settings.EnableRedFestivals;

        _text1EnableCustomFontSizeToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.Text1EnableCustomFontSize);
        _text1EnableCustomFontColorToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.Text1EnableCustomFontColor);
        _nameEnableCustomFontSizeToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.NameEnableCustomFontSize);
        _nameEnableCustomFontColorToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.NameEnableCustomFontColor);
        _text3EnableCustomFontSizeToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.Text3EnableCustomFontSize);
        _text3EnableCustomFontColorToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.Text3EnableCustomFontColor);
        _timeEnableCustomFontSizeToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.TimeEnableCustomFontSize);
        _timeEnableCustomFontColorToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.TimeEnableCustomFontColor);

        UpdateControlsEnabled();
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

    private void OnFormatLostFocus(object? sender, RoutedEventArgs e) { Settings.TimeFormat = _formatTextBox.Text ?? "%d天"; }

    private void OnText1FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text1FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text1FontSize = (double)_text1FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnNameFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_nameFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.NameFontSize = (double)_nameFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText3FontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_text3FontSizeNumericUpDown.Value.HasValue)
        {
            Settings.Text3FontSize = (double)_text3FontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnTimeFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_timeFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.TimeFontSize = (double)_timeFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnText1ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text1FontColor = _text1FontColorPicker.Color.ToString();
    }

    private void OnNameColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.NameFontColor = _nameFontColorPicker.Color.ToString();
    }

    private void OnText3ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.Text3FontColor = _text3FontColorPicker.Color.ToString();
    }

    private void OnTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.TimeFontColor = _timeFontColorPicker.Color.ToString();
    }

    private void OnInternationalToggled(object? sender, EventArgs e) => Settings.EnableInternationalFestivals = _internationalToggle.IsChecked ?? true;

    private void OnTraditionalToggled(object? sender, EventArgs e) => Settings.EnableChineseTraditionalFestivals = _traditionalToggle.IsChecked ?? true;

    private void OnRedToggled(object? sender, EventArgs e) => Settings.EnableRedFestivals = _redToggle.IsChecked ?? true;
}
