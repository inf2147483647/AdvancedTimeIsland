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

public class NextXingZuoCountdownSettingsControl : ComponentBase<NextXingZuoCountdownSettings>
{
    private TextBox _formatTextBox;
    private TextBox _text1FontSizeTextBox;
    private ColorPicker _text1FontColorPicker;
    private TextBox _nameFontSizeTextBox;
    private ColorPicker _nameFontColorPicker;
    private TextBox _text3FontSizeTextBox;
    private ColorPicker _text3FontColorPicker;
    private TextBox _timeFontSizeTextBox;
    private ColorPicker _timeFontColorPicker;

    private ToggleSwitch _text1EnableCustomFontSizeToggle;
    private ToggleSwitch _text1EnableCustomFontColorToggle;
    private ToggleSwitch _nameEnableCustomFontSizeToggle;
    private ToggleSwitch _nameEnableCustomFontColorToggle;
    private ToggleSwitch _text3EnableCustomFontSizeToggle;
    private ToggleSwitch _text3EnableCustomFontColorToggle;
    private ToggleSwitch _timeEnableCustomFontSizeToggle;
    private ToggleSwitch _timeEnableCustomFontColorToggle;

    private TextBlock _formatTitle;
    private TextBlock _formatLabel;
    private TextBlock _formatHelpText;
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

    public NextXingZuoCountdownSettingsControl() { InitializeComponent(); }

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

        _text1Title = new TextBlock { Text = "文本1样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_text1Title);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _text1FontSizeLabel, out _text1FontSizeTextBox, out _text1EnableCustomFontSizeToggle, OnText1FontSizeLostFocus, OnText1EnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色:", out _text1ColorLabel, out _text1FontColorPicker, out _text1EnableCustomFontColorToggle, OnText1EnableCustomFontColorChanged));

        _nameTitle = new TextBlock { Text = "星座名样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_nameTitle);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _nameFontSizeLabel, out _nameFontSizeTextBox, out _nameEnableCustomFontSizeToggle, OnNameFontSizeLostFocus, OnNameEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色:", out _nameColorLabel, out _nameFontColorPicker, out _nameEnableCustomFontColorToggle, OnNameEnableCustomFontColorChanged));

        _text3Title = new TextBlock { Text = "文本3样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_text3Title);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _text3FontSizeLabel, out _text3FontSizeTextBox, out _text3EnableCustomFontSizeToggle, OnText3FontSizeLostFocus, OnText3EnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色:", out _text3ColorLabel, out _text3FontColorPicker, out _text3EnableCustomFontColorToggle, OnText3EnableCustomFontColorChanged));

        _timeTitle = new TextBlock { Text = "时间样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_timeTitle);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _timeFontSizeLabel, out _timeFontSizeTextBox, out _timeEnableCustomFontSizeToggle, OnTimeFontSizeLostFocus, OnTimeEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色:", out _timeColorLabel, out _timeFontColorPicker, out _timeEnableCustomFontColorToggle, OnTimeEnableCustomFontColorChanged));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    private Grid CreateFontSizeRow(string labelText, out TextBlock label, out TextBox textBox, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> lostFocusHandler, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        textBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(textBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, lostFocusHandler);
        row.Children.Add(textBox);

        toggle = new ToggleSwitch { Content = "使用自定义大小" };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateColorRow(string labelText, out TextBlock label, out ColorPicker colorPicker, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        colorPicker = new ColorPicker { Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(colorPicker, 1);
        row.Children.Add(colorPicker);

        toggle = new ToggleSwitch { Content = "使用自定义颜色" };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private void UpdateThemeColors()
    {
        _formatTitle.Foreground = ThemeHelper.GetTextBrush();
        _formatLabel.Foreground = ThemeHelper.GetTextBrush();
        _formatHelpText.Foreground = ThemeHelper.GetGrayBrush();
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
        Settings.Text1EnableCustomFontSize = _text1EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText1EnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.Text1EnableCustomFontColor = _text1EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.NameEnableCustomFontSize = _nameEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnNameEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.NameEnableCustomFontColor = _nameEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.Text3EnableCustomFontSize = _text3EnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnText3EnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.Text3EnableCustomFontColor = _text3EnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.TimeEnableCustomFontSize = _timeEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnTimeEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.TimeEnableCustomFontColor = _timeEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        _text1FontSizeTextBox.IsEnabled = Settings.Text1EnableCustomFontSize;
        _text1FontColorPicker.IsEnabled = Settings.Text1EnableCustomFontColor;
        _nameFontSizeTextBox.IsEnabled = Settings.NameEnableCustomFontSize;
        _nameFontColorPicker.IsEnabled = Settings.NameEnableCustomFontColor;
        _text3FontSizeTextBox.IsEnabled = Settings.Text3EnableCustomFontSize;
        _text3FontColorPicker.IsEnabled = Settings.Text3EnableCustomFontColor;
        _timeFontSizeTextBox.IsEnabled = Settings.TimeEnableCustomFontSize;
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

        _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(CultureInfo.InvariantCulture);
        _text1FontColorPicker.Color = ParseColor(Settings.Text1FontColor);
        _nameFontSizeTextBox.Text = Settings.NameFontSize.ToString(CultureInfo.InvariantCulture);
        _nameFontColorPicker.Color = ParseColor(Settings.NameFontColor);
        _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(CultureInfo.InvariantCulture);
        _text3FontColorPicker.Color = ParseColor(Settings.Text3FontColor);
        _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(CultureInfo.InvariantCulture);
        _timeFontColorPicker.Color = ParseColor(Settings.TimeFontColor);

        _text1EnableCustomFontSizeToggle.IsChecked = Settings.Text1EnableCustomFontSize;
        _text1EnableCustomFontColorToggle.IsChecked = Settings.Text1EnableCustomFontColor;
        _nameEnableCustomFontSizeToggle.IsChecked = Settings.NameEnableCustomFontSize;
        _nameEnableCustomFontColorToggle.IsChecked = Settings.NameEnableCustomFontColor;
        _text3EnableCustomFontSizeToggle.IsChecked = Settings.Text3EnableCustomFontSize;
        _text3EnableCustomFontColorToggle.IsChecked = Settings.Text3EnableCustomFontColor;
        _timeEnableCustomFontSizeToggle.IsChecked = Settings.TimeEnableCustomFontSize;
        _timeEnableCustomFontColorToggle.IsChecked = Settings.TimeEnableCustomFontColor;

        UpdateControlsEnabled();

        _text1FontColorPicker.ColorChanged += (s, e) => Settings.Text1FontColor = _text1FontColorPicker.Color.ToString();
        _nameFontColorPicker.ColorChanged += (s, e) => Settings.NameFontColor = _nameFontColorPicker.Color.ToString();
        _text3FontColorPicker.ColorChanged += (s, e) => Settings.Text3FontColor = _text3FontColorPicker.Color.ToString();
        _timeFontColorPicker.ColorChanged += (s, e) => Settings.TimeFontColor = _timeFontColorPicker.Color.ToString();
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

    private void OnText1FontSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_text1FontSizeTextBox.Text, out double size)) { Settings.Text1FontSize = size; }
        _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnNameFontSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_nameFontSizeTextBox.Text, out double size)) { Settings.NameFontSize = size; }
        _nameFontSizeTextBox.Text = Settings.NameFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnText3FontSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_text3FontSizeTextBox.Text, out double size)) { Settings.Text3FontSize = size; }
        _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnTimeFontSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_timeFontSizeTextBox.Text, out double size)) { Settings.TimeFontSize = size; }
        _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(CultureInfo.InvariantCulture);
    }
}
