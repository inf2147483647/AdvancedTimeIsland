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

public class TomorrowYiJiSettingsControl : ComponentBase<TomorrowYiJiSettings>
{
    private TextBox _yiLabelFontSizeTextBox;
    private ColorPicker _yiLabelFontColorPicker;
    private TextBox _yiValueFontSizeTextBox;
    private TextBox _jiLabelFontSizeTextBox;
    private ColorPicker _jiLabelFontColorPicker;
    private TextBox _jiValueFontSizeTextBox;

    private ToggleSwitch _yiLabelEnableCustomFontSizeToggle;
    private ToggleSwitch _yiLabelEnableCustomFontColorToggle;
    private ToggleSwitch _yiValueEnableCustomFontSizeToggle;
    private ToggleSwitch _jiLabelEnableCustomFontSizeToggle;
    private ToggleSwitch _jiLabelEnableCustomFontColorToggle;
    private ToggleSwitch _jiValueEnableCustomFontSizeToggle;

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
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _yiLabelFontSizeLabel, out _yiLabelFontSizeTextBox, out _yiLabelEnableCustomFontSizeToggle, OnYiLabelFontSizeLostFocus, OnYiLabelEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色:", out _yiLabelColorLabel, out _yiLabelFontColorPicker, out _yiLabelEnableCustomFontColorToggle, OnYiLabelEnableCustomFontColorChanged));

        _yiValueTitle = new TextBlock { Text = "宜内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_yiValueTitle);

        _yiValueColorNote = new TextBlock { Text = "颜色：绿色（固定）", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(_yiValueColorNote);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _yiValueFontSizeLabel, out _yiValueFontSizeTextBox, out _yiValueEnableCustomFontSizeToggle, OnYiValueFontSizeLostFocus, OnYiValueEnableCustomFontSizeChanged));

        _jiLabelTitle = new TextBlock { Text = "忌标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_jiLabelTitle);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _jiLabelFontSizeLabel, out _jiLabelFontSizeTextBox, out _jiLabelEnableCustomFontSizeToggle, OnJiLabelFontSizeLostFocus, OnJiLabelEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色:", out _jiLabelColorLabel, out _jiLabelFontColorPicker, out _jiLabelEnableCustomFontColorToggle, OnJiLabelEnableCustomFontColorChanged));

        _jiValueTitle = new TextBlock { Text = "忌内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_jiValueTitle);

        _jiValueColorNote = new TextBlock { Text = "颜色：红色（固定）", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(_jiValueColorNote);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _jiValueFontSizeLabel, out _jiValueFontSizeTextBox, out _jiValueEnableCustomFontSizeToggle, OnJiValueFontSizeLostFocus, OnJiValueEnableCustomFontSizeChanged));

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
    }

    private void OnYiLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.YiLabelEnableCustomFontSize = _yiLabelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.YiLabelEnableCustomFontColor = _yiLabelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.YiValueEnableCustomFontSize = _yiValueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.JiLabelEnableCustomFontSize = _jiLabelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.JiLabelEnableCustomFontColor = _jiLabelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.JiValueEnableCustomFontSize = _jiValueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        _yiLabelFontSizeTextBox.IsEnabled = Settings.YiLabelEnableCustomFontSize;
        _yiLabelFontColorPicker.IsEnabled = Settings.YiLabelEnableCustomFontColor;
        _yiValueFontSizeTextBox.IsEnabled = Settings.YiValueEnableCustomFontSize;
        _jiLabelFontSizeTextBox.IsEnabled = Settings.JiLabelEnableCustomFontSize;
        _jiLabelFontColorPicker.IsEnabled = Settings.JiLabelEnableCustomFontColor;
        _jiValueFontSizeTextBox.IsEnabled = Settings.JiValueEnableCustomFontSize;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();

        _yiLabelFontSizeTextBox.Text = Settings.YiLabelFontSize.ToString(CultureInfo.InvariantCulture);
        _yiLabelFontColorPicker.Color = ParseColor(Settings.YiLabelFontColor);
        _yiValueFontSizeTextBox.Text = Settings.YiValueFontSize.ToString(CultureInfo.InvariantCulture);
        _jiLabelFontSizeTextBox.Text = Settings.JiLabelFontSize.ToString(CultureInfo.InvariantCulture);
        _jiLabelFontColorPicker.Color = ParseColor(Settings.JiLabelFontColor);
        _jiValueFontSizeTextBox.Text = Settings.JiValueFontSize.ToString(CultureInfo.InvariantCulture);

        _yiLabelEnableCustomFontSizeToggle.IsChecked = Settings.YiLabelEnableCustomFontSize;
        _yiLabelEnableCustomFontColorToggle.IsChecked = Settings.YiLabelEnableCustomFontColor;
        _yiValueEnableCustomFontSizeToggle.IsChecked = Settings.YiValueEnableCustomFontSize;
        _jiLabelEnableCustomFontSizeToggle.IsChecked = Settings.JiLabelEnableCustomFontSize;
        _jiLabelEnableCustomFontColorToggle.IsChecked = Settings.JiLabelEnableCustomFontColor;
        _jiValueEnableCustomFontSizeToggle.IsChecked = Settings.JiValueEnableCustomFontSize;

        UpdateControlsEnabled();

        _yiLabelFontColorPicker.ColorChanged += (s, e) => Settings.YiLabelFontColor = _yiLabelFontColorPicker.Color.ToString();
        _jiLabelFontColorPicker.ColorChanged += (s, e) => Settings.JiLabelFontColor = _jiLabelFontColorPicker.Color.ToString();
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
            return ((SolidColorBrush)ThemeHelper.GetTextBrush()).Color;
        }
    }

    private void OnYiLabelFontSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_yiLabelFontSizeTextBox.Text, out double size)) { Settings.YiLabelFontSize = size; }
        _yiLabelFontSizeTextBox.Text = Settings.YiLabelFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnYiValueFontSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_yiValueFontSizeTextBox.Text, out double size)) { Settings.YiValueFontSize = size; }
        _yiValueFontSizeTextBox.Text = Settings.YiValueFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnJiLabelFontSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_jiLabelFontSizeTextBox.Text, out double size)) { Settings.JiLabelFontSize = size; }
        _jiLabelFontSizeTextBox.Text = Settings.JiLabelFontSize.ToString(CultureInfo.InvariantCulture);
    }

    private void OnJiValueFontSizeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(_jiValueFontSizeTextBox.Text, out double size)) { Settings.JiValueFontSize = size; }
        _jiValueFontSizeTextBox.Text = Settings.JiValueFontSize.ToString(CultureInfo.InvariantCulture);
    }
}
