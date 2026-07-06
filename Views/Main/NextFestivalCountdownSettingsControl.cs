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
    private TextBox _text1FontSizeTextBox;
    private TextBox _text1ColorTextBox;
    private TextBox _nameFontSizeTextBox;
    private TextBox _nameColorTextBox;
    private TextBox _text3FontSizeTextBox;
    private TextBox _text3ColorTextBox;
    private TextBox _timeFontSizeTextBox;
    private TextBox _timeColorTextBox;
    private ToggleSwitch _internationalToggle;
    private ToggleSwitch _traditionalToggle;
    private ToggleSwitch _redToggle;
    private ToggleSwitch _enableCustomToggle;

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
        _formatTextBox.LostFocus += OnFormatLostFocus;
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

        _internationalToggle = new ToggleSwitch { Content = "国际节日", OffContent = "", OnContent = "" };
        _internationalToggle.IsCheckedChanged += OnInternationalToggled;
        sp.Children.Add(_internationalToggle);

        _traditionalToggle = new ToggleSwitch { Content = "中国传统节日", OffContent = "", OnContent = "" };
        _traditionalToggle.IsCheckedChanged += OnTraditionalToggled;
        sp.Children.Add(_traditionalToggle);

        _redToggle = new ToggleSwitch { Content = "红色节日", OffContent = "", OnContent = "" };
        _redToggle.IsCheckedChanged += OnRedToggled;
        sp.Children.Add(_redToggle);

        _enableCustomToggle = new ToggleSwitch { Content = "启用自定义颜色与字体", Margin = new Thickness(0, 10, 0, 0) };
        _enableCustomToggle.IsCheckedChanged += OnEnableCustomToggleChanged;
        sp.Children.Add(_enableCustomToggle);

        _text1Title = new TextBlock { Text = "文本1样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_text1Title);

        var text1ColorRow = new Grid();
        text1ColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        text1ColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _text1ColorLabel = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_text1ColorLabel, 0);
        text1ColorRow.Children.Add(_text1ColorLabel);
        _text1ColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_text1ColorTextBox, 1);
        _text1ColorTextBox.LostFocus += OnText1ColorLostFocus;
        text1ColorRow.Children.Add(_text1ColorTextBox);
        sp.Children.Add(text1ColorRow);

        var text1FontSizeRow = new Grid();
        text1FontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        text1FontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _text1FontSizeLabel = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_text1FontSizeLabel, 0);
        text1FontSizeRow.Children.Add(_text1FontSizeLabel);
        _text1FontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_text1FontSizeTextBox, 1);
        _text1FontSizeTextBox.LostFocus += OnText1FontSizeLostFocus;
        text1FontSizeRow.Children.Add(_text1FontSizeTextBox);
        sp.Children.Add(text1FontSizeRow);

        _nameTitle = new TextBlock { Text = "节日名样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_nameTitle);

        var nameColorRow = new Grid();
        nameColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        nameColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _nameColorLabel = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_nameColorLabel, 0);
        nameColorRow.Children.Add(_nameColorLabel);
        _nameColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_nameColorTextBox, 1);
        _nameColorTextBox.LostFocus += OnNameColorLostFocus;
        nameColorRow.Children.Add(_nameColorTextBox);
        sp.Children.Add(nameColorRow);

        var nameFontSizeRow = new Grid();
        nameFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        nameFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _nameFontSizeLabel = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_nameFontSizeLabel, 0);
        nameFontSizeRow.Children.Add(_nameFontSizeLabel);
        _nameFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_nameFontSizeTextBox, 1);
        _nameFontSizeTextBox.LostFocus += OnNameFontSizeLostFocus;
        nameFontSizeRow.Children.Add(_nameFontSizeTextBox);
        sp.Children.Add(nameFontSizeRow);

        _text3Title = new TextBlock { Text = "文本3样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_text3Title);

        var text3ColorRow = new Grid();
        text3ColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        text3ColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _text3ColorLabel = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_text3ColorLabel, 0);
        text3ColorRow.Children.Add(_text3ColorLabel);
        _text3ColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_text3ColorTextBox, 1);
        _text3ColorTextBox.LostFocus += OnText3ColorLostFocus;
        text3ColorRow.Children.Add(_text3ColorTextBox);
        sp.Children.Add(text3ColorRow);

        var text3FontSizeRow = new Grid();
        text3FontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        text3FontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _text3FontSizeLabel = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_text3FontSizeLabel, 0);
        text3FontSizeRow.Children.Add(_text3FontSizeLabel);
        _text3FontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_text3FontSizeTextBox, 1);
        _text3FontSizeTextBox.LostFocus += OnText3FontSizeLostFocus;
        text3FontSizeRow.Children.Add(_text3FontSizeTextBox);
        sp.Children.Add(text3FontSizeRow);

        _timeTitle = new TextBlock { Text = "时间样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_timeTitle);

        var timeColorRow = new Grid();
        timeColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _timeColorLabel = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_timeColorLabel, 0);
        timeColorRow.Children.Add(_timeColorLabel);
        _timeColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_timeColorTextBox, 1);
        _timeColorTextBox.LostFocus += OnTimeColorLostFocus;
        timeColorRow.Children.Add(_timeColorTextBox);
        sp.Children.Add(timeColorRow);

        var timeFontSizeRow = new Grid();
        timeFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _timeFontSizeLabel = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_timeFontSizeLabel, 0);
        timeFontSizeRow.Children.Add(_timeFontSizeLabel);
        _timeFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_timeFontSizeTextBox, 1);
        _timeFontSizeTextBox.LostFocus += OnTimeFontSizeLostFocus;
        timeFontSizeRow.Children.Add(_timeFontSizeTextBox);
        sp.Children.Add(timeFontSizeRow);

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
        if (!Settings.EnableCustomColorAndFont)
        {
            UpdateFontColorsForTheme();
        }
    }

    private void UpdateFontColorsForTheme()
    {
        var newText1Color = ThemeHelper.GetSmartContrastColor(Settings.Text1FontColor);
        Settings.Text1FontColor = newText1Color;
        _text1ColorTextBox.Text = newText1Color;

        var newNameColor = ThemeHelper.GetSmartContrastColor(Settings.NameFontColor);
        Settings.NameFontColor = newNameColor;
        _nameColorTextBox.Text = newNameColor;

        var newText3Color = ThemeHelper.GetSmartContrastColor(Settings.Text3FontColor);
        Settings.Text3FontColor = newText3Color;
        _text3ColorTextBox.Text = newText3Color;

        var newTimeColor = ThemeHelper.GetSmartContrastColor(Settings.TimeFontColor);
        Settings.TimeFontColor = newTimeColor;
        _timeColorTextBox.Text = newTimeColor;
    }

    private void OnEnableCustomToggleChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomColorAndFont = _enableCustomToggle.IsChecked ?? false;
        UpdateControlsEnabled();
        if (!Settings.EnableCustomColorAndFont)
        {
            UpdateFontColorsForTheme();
        }
    }

    private void UpdateControlsEnabled()
    {
        var isEnabled = Settings.EnableCustomColorAndFont;
        _text1ColorTextBox.IsEnabled = isEnabled;
        _text1FontSizeTextBox.IsEnabled = isEnabled;
        _nameColorTextBox.IsEnabled = isEnabled;
        _nameFontSizeTextBox.IsEnabled = isEnabled;
        _text3ColorTextBox.IsEnabled = isEnabled;
        _text3FontSizeTextBox.IsEnabled = isEnabled;
        _timeColorTextBox.IsEnabled = isEnabled;
        _timeFontSizeTextBox.IsEnabled = isEnabled;
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
        _text1ColorTextBox.Text = Settings.Text1FontColor;
        _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(CultureInfo.InvariantCulture);
        _nameColorTextBox.Text = Settings.NameFontColor;
        _nameFontSizeTextBox.Text = Settings.NameFontSize.ToString(CultureInfo.InvariantCulture);
        _text3ColorTextBox.Text = Settings.Text3FontColor;
        _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(CultureInfo.InvariantCulture);
        _timeColorTextBox.Text = Settings.TimeFontColor;
        _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(CultureInfo.InvariantCulture);
        _internationalToggle.IsChecked = Settings.EnableInternationalFestivals;
        _traditionalToggle.IsChecked = Settings.EnableChineseTraditionalFestivals;
        _redToggle.IsChecked = Settings.EnableRedFestivals;
        _enableCustomToggle.IsChecked = Settings.EnableCustomColorAndFont;
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

    private void OnFormatLostFocus(object? sender, EventArgs e) { Settings.TimeFormat = _formatTextBox.Text ?? "%d天"; }

    private void OnText1ColorLostFocus(object? sender, EventArgs e)
    {
        var color = _text1ColorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try { Avalonia.Media.Color.Parse(color); Settings.Text1FontColor = color; }
            catch { _text1ColorTextBox.Text = Settings.Text1FontColor; }
        }
        else { _text1ColorTextBox.Text = Settings.Text1FontColor; }
    }

    private void OnText1FontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_text1FontSizeTextBox.Text, out double size)) { Settings.Text1FontSize = size; }
        _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnNameColorLostFocus(object? sender, EventArgs e)
    {
        var color = _nameColorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try { Avalonia.Media.Color.Parse(color); Settings.NameFontColor = color; }
            catch { _nameColorTextBox.Text = Settings.NameFontColor; }
        }
        else { _nameColorTextBox.Text = Settings.NameFontColor; }
    }

    private void OnNameFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_nameFontSizeTextBox.Text, out double size)) { Settings.NameFontSize = size; }
        _nameFontSizeTextBox.Text = Settings.NameFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnText3ColorLostFocus(object? sender, EventArgs e)
    {
        var color = _text3ColorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try { Avalonia.Media.Color.Parse(color); Settings.Text3FontColor = color; }
            catch { _text3ColorTextBox.Text = Settings.Text3FontColor; }
        }
        else { _text3ColorTextBox.Text = Settings.Text3FontColor; }
    }

    private void OnText3FontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_text3FontSizeTextBox.Text, out double size)) { Settings.Text3FontSize = size; }
        _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnTimeColorLostFocus(object? sender, EventArgs e)
    {
        var color = _timeColorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try { Avalonia.Media.Color.Parse(color); Settings.TimeFontColor = color; }
            catch { _timeColorTextBox.Text = Settings.TimeFontColor; }
        }
        else { _timeColorTextBox.Text = Settings.TimeFontColor; }
    }

    private void OnTimeFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_timeFontSizeTextBox.Text, out double size)) { Settings.TimeFontSize = size; }
        _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnInternationalToggled(object? sender, EventArgs e) => Settings.EnableInternationalFestivals = _internationalToggle.IsChecked ?? true;

    private void OnTraditionalToggled(object? sender, EventArgs e) => Settings.EnableChineseTraditionalFestivals = _traditionalToggle.IsChecked ?? true;

    private void OnRedToggled(object? sender, EventArgs e) => Settings.EnableRedFestivals = _redToggle.IsChecked ?? true;
}
