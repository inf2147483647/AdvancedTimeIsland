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

public class TomorrowYiJiSettingsControl : ComponentBase<TomorrowYiJiSettings>
{
    private TextBox _yiLabelFontSizeTextBox;
    private TextBox _yiLabelColorTextBox;
    private TextBox _yiValueFontSizeTextBox;
    private TextBox _jiLabelFontSizeTextBox;
    private TextBox _jiLabelColorTextBox;
    private TextBox _jiValueFontSizeTextBox;

    public TomorrowYiJiSettingsControl() { InitializeComponent(); }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var yiLabelTitle = new TextBlock { Text = "宜标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(yiLabelTitle);

        var yiLabelColorRow = new Grid();
        yiLabelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yiLabelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var yiLabelColorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(yiLabelColorLabel, 0);
        yiLabelColorRow.Children.Add(yiLabelColorLabel);
        _yiLabelColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_yiLabelColorTextBox, 1);
        _yiLabelColorTextBox.LostFocus += OnYiLabelColorLostFocus;
        yiLabelColorRow.Children.Add(_yiLabelColorTextBox);
        sp.Children.Add(yiLabelColorRow);

        var yiLabelFontSizeRow = new Grid();
        yiLabelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yiLabelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var yiLabelFontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(yiLabelFontSizeLabel, 0);
        yiLabelFontSizeRow.Children.Add(yiLabelFontSizeLabel);
        _yiLabelFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_yiLabelFontSizeTextBox, 1);
        _yiLabelFontSizeTextBox.LostFocus += OnYiLabelFontSizeLostFocus;
        yiLabelFontSizeRow.Children.Add(_yiLabelFontSizeTextBox);
        sp.Children.Add(yiLabelFontSizeRow);

        var yiValueTitle = new TextBlock { Text = "宜内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(yiValueTitle);

        var yiValueColorNote = new TextBlock { Text = "颜色：绿色（固定）", FontSize = 12, Foreground = Brushes.Gray, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(yiValueColorNote);

        var yiValueFontSizeRow = new Grid();
        yiValueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yiValueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var yiValueFontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(yiValueFontSizeLabel, 0);
        yiValueFontSizeRow.Children.Add(yiValueFontSizeLabel);
        _yiValueFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_yiValueFontSizeTextBox, 1);
        _yiValueFontSizeTextBox.LostFocus += OnYiValueFontSizeLostFocus;
        yiValueFontSizeRow.Children.Add(_yiValueFontSizeTextBox);
        sp.Children.Add(yiValueFontSizeRow);

        var jiLabelTitle = new TextBlock { Text = "忌标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(jiLabelTitle);

        var jiLabelColorRow = new Grid();
        jiLabelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        jiLabelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var jiLabelColorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(jiLabelColorLabel, 0);
        jiLabelColorRow.Children.Add(jiLabelColorLabel);
        _jiLabelColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_jiLabelColorTextBox, 1);
        _jiLabelColorTextBox.LostFocus += OnJiLabelColorLostFocus;
        jiLabelColorRow.Children.Add(_jiLabelColorTextBox);
        sp.Children.Add(jiLabelColorRow);

        var jiLabelFontSizeRow = new Grid();
        jiLabelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        jiLabelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var jiLabelFontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(jiLabelFontSizeLabel, 0);
        jiLabelFontSizeRow.Children.Add(jiLabelFontSizeLabel);
        _jiLabelFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_jiLabelFontSizeTextBox, 1);
        _jiLabelFontSizeTextBox.LostFocus += OnJiLabelFontSizeLostFocus;
        jiLabelFontSizeRow.Children.Add(_jiLabelFontSizeTextBox);
        sp.Children.Add(jiLabelFontSizeRow);

        var jiValueTitle = new TextBlock { Text = "忌内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(jiValueTitle);

        var jiValueColorNote = new TextBlock { Text = "颜色：红色（固定）", FontSize = 12, Foreground = Brushes.Gray, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(jiValueColorNote);

        var jiValueFontSizeRow = new Grid();
        jiValueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        jiValueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var jiValueFontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(jiValueFontSizeLabel, 0);
        jiValueFontSizeRow.Children.Add(jiValueFontSizeLabel);
        _jiValueFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_jiValueFontSizeTextBox, 1);
        _jiValueFontSizeTextBox.LostFocus += OnJiValueFontSizeLostFocus;
        jiValueFontSizeRow.Children.Add(_jiValueFontSizeTextBox);
        sp.Children.Add(jiValueFontSizeRow);

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
        _yiLabelColorTextBox.Text = Settings.YiLabelFontColor;
        _yiLabelFontSizeTextBox.Text = Settings.YiLabelFontSize.ToString(CultureInfo.InvariantCulture);
        _yiValueFontSizeTextBox.Text = Settings.YiValueFontSize.ToString(CultureInfo.InvariantCulture);
        _jiLabelColorTextBox.Text = Settings.JiLabelFontColor;
        _jiLabelFontSizeTextBox.Text = Settings.JiLabelFontSize.ToString(CultureInfo.InvariantCulture);
        _jiValueFontSizeTextBox.Text = Settings.JiValueFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnYiLabelColorLostFocus(object? sender, EventArgs e)
    {
        var color = _yiLabelColorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try { Avalonia.Media.Color.Parse(color); Settings.YiLabelFontColor = color; }
            catch { _yiLabelColorTextBox.Text = Settings.YiLabelFontColor; }
        }
        else { _yiLabelColorTextBox.Text = Settings.YiLabelFontColor; }
    }

    private void OnYiLabelFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_yiLabelFontSizeTextBox.Text, out double size)) { Settings.YiLabelFontSize = size; }
        _yiLabelFontSizeTextBox.Text = Settings.YiLabelFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnYiValueFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_yiValueFontSizeTextBox.Text, out double size)) { Settings.YiValueFontSize = size; }
        _yiValueFontSizeTextBox.Text = Settings.YiValueFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnJiLabelColorLostFocus(object? sender, EventArgs e)
    {
        var color = _jiLabelColorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try { Avalonia.Media.Color.Parse(color); Settings.JiLabelFontColor = color; }
            catch { _jiLabelColorTextBox.Text = Settings.JiLabelFontColor; }
        }
        else { _jiLabelColorTextBox.Text = Settings.JiLabelFontColor; }
    }

    private void OnJiLabelFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_jiLabelFontSizeTextBox.Text, out double size)) { Settings.JiLabelFontSize = size; }
        _jiLabelFontSizeTextBox.Text = Settings.JiLabelFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnJiValueFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_jiValueFontSizeTextBox.Text, out double size)) { Settings.JiValueFontSize = size; }
        _jiValueFontSizeTextBox.Text = Settings.JiValueFontSize.ToString(CultureInfo.InvariantCulture);
    }
}
