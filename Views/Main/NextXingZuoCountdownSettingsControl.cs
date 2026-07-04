using System;
using System.Globalization;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class NextXingZuoCountdownSettingsControl : ComponentBase<NextXingZuoCountdownSettings>
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

    public NextXingZuoCountdownSettingsControl() { InitializeComponent(); }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var formatTitle = new TextBlock { Text = "时间格式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(formatTitle);

        var formatRow = new Grid();
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        formatRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var formatLabel = new TextBlock { Text = "格式化文本:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(formatLabel, 0);
        formatRow.Children.Add(formatLabel);

        _formatTextBox = new TextBox { Width = 200, Watermark = "%d天" };
        Grid.SetColumn(_formatTextBox, 1);
        _formatTextBox.LostFocus += OnFormatLostFocus;
        formatRow.Children.Add(_formatTextBox);
        sp.Children.Add(formatRow);

        var text1Title = new TextBlock { Text = "文本1样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(text1Title);

        var text1ColorRow = new Grid();
        text1ColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        text1ColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var text1ColorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(text1ColorLabel, 0);
        text1ColorRow.Children.Add(text1ColorLabel);
        _text1ColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_text1ColorTextBox, 1);
        _text1ColorTextBox.LostFocus += OnText1ColorLostFocus;
        text1ColorRow.Children.Add(_text1ColorTextBox);
        sp.Children.Add(text1ColorRow);

        var text1FontSizeRow = new Grid();
        text1FontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        text1FontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var text1FontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(text1FontSizeLabel, 0);
        text1FontSizeRow.Children.Add(text1FontSizeLabel);
        _text1FontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_text1FontSizeTextBox, 1);
        _text1FontSizeTextBox.LostFocus += OnText1FontSizeLostFocus;
        text1FontSizeRow.Children.Add(_text1FontSizeTextBox);
        sp.Children.Add(text1FontSizeRow);

        var nameTitle = new TextBlock { Text = "星座名样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(nameTitle);

        var nameColorRow = new Grid();
        nameColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        nameColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var nameColorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(nameColorLabel, 0);
        nameColorRow.Children.Add(nameColorLabel);
        _nameColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_nameColorTextBox, 1);
        _nameColorTextBox.LostFocus += OnNameColorLostFocus;
        nameColorRow.Children.Add(_nameColorTextBox);
        sp.Children.Add(nameColorRow);

        var nameFontSizeRow = new Grid();
        nameFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        nameFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var nameFontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(nameFontSizeLabel, 0);
        nameFontSizeRow.Children.Add(nameFontSizeLabel);
        _nameFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_nameFontSizeTextBox, 1);
        _nameFontSizeTextBox.LostFocus += OnNameFontSizeLostFocus;
        nameFontSizeRow.Children.Add(_nameFontSizeTextBox);
        sp.Children.Add(nameFontSizeRow);

        var text3Title = new TextBlock { Text = "文本3样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(text3Title);

        var text3ColorRow = new Grid();
        text3ColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        text3ColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var text3ColorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(text3ColorLabel, 0);
        text3ColorRow.Children.Add(text3ColorLabel);
        _text3ColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_text3ColorTextBox, 1);
        _text3ColorTextBox.LostFocus += OnText3ColorLostFocus;
        text3ColorRow.Children.Add(_text3ColorTextBox);
        sp.Children.Add(text3ColorRow);

        var text3FontSizeRow = new Grid();
        text3FontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        text3FontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var text3FontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(text3FontSizeLabel, 0);
        text3FontSizeRow.Children.Add(text3FontSizeLabel);
        _text3FontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_text3FontSizeTextBox, 1);
        _text3FontSizeTextBox.LostFocus += OnText3FontSizeLostFocus;
        text3FontSizeRow.Children.Add(_text3FontSizeTextBox);
        sp.Children.Add(text3FontSizeRow);

        var timeTitle = new TextBlock { Text = "时间样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(timeTitle);

        var timeColorRow = new Grid();
        timeColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var timeColorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(timeColorLabel, 0);
        timeColorRow.Children.Add(timeColorLabel);
        _timeColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_timeColorTextBox, 1);
        _timeColorTextBox.LostFocus += OnTimeColorLostFocus;
        timeColorRow.Children.Add(_timeColorTextBox);
        sp.Children.Add(timeColorRow);

        var timeFontSizeRow = new Grid();
        timeFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var timeFontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(timeFontSizeLabel, 0);
        timeFontSizeRow.Children.Add(timeFontSizeLabel);
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

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _formatTextBox.Text = Settings.TimeFormat;
        _text1ColorTextBox.Text = Settings.Text1FontColor;
        _text1FontSizeTextBox.Text = Settings.Text1FontSize.ToString(CultureInfo.InvariantCulture);
        _nameColorTextBox.Text = Settings.NameFontColor;
        _nameFontSizeTextBox.Text = Settings.NameFontSize.ToString(CultureInfo.InvariantCulture);
        _text3ColorTextBox.Text = Settings.Text3FontColor;
        _text3FontSizeTextBox.Text = Settings.Text3FontSize.ToString(CultureInfo.InvariantCulture);
        _timeColorTextBox.Text = Settings.TimeFontColor;
        _timeFontSizeTextBox.Text = Settings.TimeFontSize.ToString(CultureInfo.InvariantCulture);
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
}
