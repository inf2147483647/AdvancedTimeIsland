using System;
using System.Globalization;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
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
    private ToggleSwitch _enableCustomFontSizeToggle;
    private ToggleSwitch _enableCustomFontColorToggle;

    private TextBlock _yiLabelTitle;
    private TextBlock _yiLabelColorLabel;
    private TextBlock _yiLabelFontSizeLabel;
    private TextBlock _yiValueTitle;
    private TextBlock _yiValueColorNote;
    private TextBlock _yiValueFontSizeLabel;
    private TextBlock _jiLabelTitle;
    private TextBlock _jiLabelColorLabel;
    private TextBlock _jiLabelFontSizeLabel;
    private TextBlock _jiValueTitle;
    private TextBlock _jiValueColorNote;
    private TextBlock _jiValueFontSizeLabel;

    public TomorrowYiJiSettingsControl() { InitializeComponent(); }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _yiLabelTitle = new TextBlock { Text = "宜标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_yiLabelTitle);

        var yiLabelColorRow = new Grid();
        yiLabelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yiLabelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _yiLabelColorLabel = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_yiLabelColorLabel, 0);
        yiLabelColorRow.Children.Add(_yiLabelColorLabel);
        _yiLabelColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_yiLabelColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_yiLabelColorTextBox, OnYiLabelColorLostFocus);
        yiLabelColorRow.Children.Add(_yiLabelColorTextBox);
        sp.Children.Add(yiLabelColorRow);

        var yiLabelFontSizeRow = new Grid();
        yiLabelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yiLabelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _yiLabelFontSizeLabel = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_yiLabelFontSizeLabel, 0);
        yiLabelFontSizeRow.Children.Add(_yiLabelFontSizeLabel);
        _yiLabelFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_yiLabelFontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_yiLabelFontSizeTextBox, OnYiLabelFontSizeLostFocus);
        yiLabelFontSizeRow.Children.Add(_yiLabelFontSizeTextBox);
        sp.Children.Add(yiLabelFontSizeRow);

        _yiValueTitle = new TextBlock { Text = "宜内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_yiValueTitle);

        _yiValueColorNote = new TextBlock { Text = "颜色：绿色（固定）", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(_yiValueColorNote);

        var yiValueFontSizeRow = new Grid();
        yiValueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        yiValueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _yiValueFontSizeLabel = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_yiValueFontSizeLabel, 0);
        yiValueFontSizeRow.Children.Add(_yiValueFontSizeLabel);
        _yiValueFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_yiValueFontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_yiValueFontSizeTextBox, OnYiValueFontSizeLostFocus);
        yiValueFontSizeRow.Children.Add(_yiValueFontSizeTextBox);
        sp.Children.Add(yiValueFontSizeRow);

        _jiLabelTitle = new TextBlock { Text = "忌标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_jiLabelTitle);

        var jiLabelColorRow = new Grid();
        jiLabelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        jiLabelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _jiLabelColorLabel = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_jiLabelColorLabel, 0);
        jiLabelColorRow.Children.Add(_jiLabelColorLabel);
        _jiLabelColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_jiLabelColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_jiLabelColorTextBox, OnJiLabelColorLostFocus);
        jiLabelColorRow.Children.Add(_jiLabelColorTextBox);
        sp.Children.Add(jiLabelColorRow);

        var jiLabelFontSizeRow = new Grid();
        jiLabelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        jiLabelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _jiLabelFontSizeLabel = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_jiLabelFontSizeLabel, 0);
        jiLabelFontSizeRow.Children.Add(_jiLabelFontSizeLabel);
        _jiLabelFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_jiLabelFontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_jiLabelFontSizeTextBox, OnJiLabelFontSizeLostFocus);
        jiLabelFontSizeRow.Children.Add(_jiLabelFontSizeTextBox);
        sp.Children.Add(jiLabelFontSizeRow);

        _jiValueTitle = new TextBlock { Text = "忌内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_jiValueTitle);

        _jiValueColorNote = new TextBlock { Text = "颜色：红色（固定）", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(_jiValueColorNote);

        var jiValueFontSizeRow = new Grid();
        jiValueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        jiValueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _jiValueFontSizeLabel = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_jiValueFontSizeLabel, 0);
        jiValueFontSizeRow.Children.Add(_jiValueFontSizeLabel);
        _jiValueFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_jiValueFontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_jiValueFontSizeTextBox, OnJiValueFontSizeLostFocus);
        jiValueFontSizeRow.Children.Add(_jiValueFontSizeTextBox);
        sp.Children.Add(jiValueFontSizeRow);

        _enableCustomFontSizeToggle = new ToggleSwitch { Content = "启用自定义字体大小", Margin = new Thickness(0, 10, 0, 0) };
        _enableCustomFontSizeToggle.IsCheckedChanged += OnEnableCustomFontSizeToggleChanged;
        sp.Children.Add(_enableCustomFontSizeToggle);

        _enableCustomFontColorToggle = new ToggleSwitch { Content = "启用自定义字体颜色", Margin = new Thickness(0, 4, 0, 0) };
        _enableCustomFontColorToggle.IsCheckedChanged += OnEnableCustomFontColorToggleChanged;
        sp.Children.Add(_enableCustomFontColorToggle);

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
        _yiLabelTitle.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelColorLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiValueTitle.Foreground = ThemeHelper.GetTextBrush();
        _yiValueColorNote.Foreground = ThemeHelper.GetGrayBrush();
        _yiValueFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelTitle.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelColorLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiValueTitle.Foreground = ThemeHelper.GetTextBrush();
        _jiValueColorNote.Foreground = ThemeHelper.GetGrayBrush();
        _jiValueFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
        if (!Settings.EnableCustomFontColor)
        {
            UpdateFontColorsForTheme();
        }
    }

    private void UpdateFontColorsForTheme()
    {
        var newYiLabelColor = ThemeHelper.GetSmartContrastColor(Settings.YiLabelFontColor);
        Settings.YiLabelFontColor = newYiLabelColor;
        _yiLabelColorTextBox.Text = newYiLabelColor;

        var newJiLabelColor = ThemeHelper.GetSmartContrastColor(Settings.JiLabelFontColor);
        Settings.JiLabelFontColor = newJiLabelColor;
        _jiLabelColorTextBox.Text = newJiLabelColor;
    }

    private void OnEnableCustomFontSizeToggleChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontSize = _enableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorToggleChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontColor = _enableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
        if (!Settings.EnableCustomFontColor)
        {
            UpdateFontColorsForTheme();
        }
    }

    private void UpdateControlsEnabled()
    {
        var fontSizeEnabled = Settings.EnableCustomFontSize;
        var fontColorEnabled = Settings.EnableCustomFontColor;
        _yiLabelColorTextBox.IsEnabled = fontColorEnabled;
        _yiLabelFontSizeTextBox.IsEnabled = fontSizeEnabled;
        _yiValueFontSizeTextBox.IsEnabled = fontSizeEnabled;
        _jiLabelColorTextBox.IsEnabled = fontColorEnabled;
        _jiLabelFontSizeTextBox.IsEnabled = fontSizeEnabled;
        _jiValueFontSizeTextBox.IsEnabled = fontSizeEnabled;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        _yiLabelColorTextBox.Text = Settings.YiLabelFontColor;
        _yiLabelFontSizeTextBox.Text = Settings.YiLabelFontSize.ToString(CultureInfo.InvariantCulture);
        _yiValueFontSizeTextBox.Text = Settings.YiValueFontSize.ToString(CultureInfo.InvariantCulture);
        _jiLabelColorTextBox.Text = Settings.JiLabelFontColor;
        _jiLabelFontSizeTextBox.Text = Settings.JiLabelFontSize.ToString(CultureInfo.InvariantCulture);
        _jiValueFontSizeTextBox.Text = Settings.JiValueFontSize.ToString(CultureInfo.InvariantCulture);
        _enableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        _enableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
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
