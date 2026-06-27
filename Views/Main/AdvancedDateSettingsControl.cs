using System;
using AdvancedTimeIsland.Models;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class AdvancedDateSettingsControl : ComponentBase<AdvancedDateSettings>
{
    private ToggleSwitch _showWeekDayToggle;
    private TextBox _colorTextBox;
    private TextBox _fontSizeTextBox;

    public AdvancedDateSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        // 日期设置
        var title = new TextBlock { Text = "日期设置", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
        sp.Children.Add(title);

        var desc = new TextBlock { Text = "配置日期显示选项", FontSize = 12, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(desc);

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        var label = new TextBlock { Text = "显示星期", FontSize = 12, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        _showWeekDayToggle = new ToggleSwitch();
        _showWeekDayToggle.IsCheckedChanged += OnShowWeekDayChanged;
        Grid.SetColumn(_showWeekDayToggle, 1);
        row.Children.Add(_showWeekDayToggle);

        sp.Children.Add(row);

        // 字体样式设置
        var styleTitle = new TextBlock { Text = "字体样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Avalonia.Thickness(0, 10, 0, 0) };
        sp.Children.Add(styleTitle);

        // 字体颜色设置
        var colorRow = new Grid();
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var colorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(colorLabel, 0);
        colorRow.Children.Add(colorLabel);

        _colorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_colorTextBox, 1);
        _colorTextBox.LostFocus += OnColorLostFocus;
        colorRow.Children.Add(_colorTextBox);
        sp.Children.Add(colorRow);

        // 字体大小设置
        var fontSizeRow = new Grid();
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var fontSizeLabel = new TextBlock { Text = "日期大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(fontSizeLabel, 0);
        fontSizeRow.Children.Add(fontSizeLabel);

        _fontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_fontSizeTextBox, 1);
        _fontSizeTextBox.LostFocus += OnFontSizeLostFocus;
        fontSizeRow.Children.Add(_fontSizeTextBox);
        sp.Children.Add(fontSizeRow);

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
        _showWeekDayToggle.IsChecked = Settings.ShowWeekDay;
        _colorTextBox.Text = Settings.FontColor;
        _fontSizeTextBox.Text = Settings.DateFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void OnShowWeekDayChanged(object? sender, EventArgs e)
    {
        Settings.ShowWeekDay = _showWeekDayToggle.IsChecked == true;
    }

    private void OnColorLostFocus(object? sender, EventArgs e)
    {
        var color = _colorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try
            {
                Avalonia.Media.Color.Parse(color);
                Settings.FontColor = color;
            }
            catch
            {
                _colorTextBox.Text = Settings.FontColor;
            }
        }
        else
        {
            _colorTextBox.Text = Settings.FontColor;
        }
    }

    private void OnFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_fontSizeTextBox.Text, out double size))
        {
            Settings.DateFontSize = size;
            _fontSizeTextBox.Text = Settings.DateFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            _fontSizeTextBox.Text = Settings.DateFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
