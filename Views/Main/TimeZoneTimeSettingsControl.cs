using System;
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

public class TimeZoneTimeSettingsControl : ComponentBase<TimeZoneTimeSettings>
{
    private ComboBox _timeZoneComboBox;
    private ToggleSwitch _enableCustomFontSizeToggle;
    private ToggleSwitch _enableCustomFontColorToggle;
    private TextBox _colorTextBox;
    private TextBox _fontSizeTextBox;

    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;
    private TextBlock _styleTitleTextBlock;
    private TextBlock _colorLabelTextBlock;
    private TextBlock _fontSizeLabelTextBlock;

    public TimeZoneTimeSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "时区设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "选择时区", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_descTextBlock);

        _timeZoneComboBox = new ComboBox { Width = 250 };
        var tzs = TimeZoneInfo.GetSystemTimeZones();
        foreach (var tz in tzs) _timeZoneComboBox.Items.Add(tz);
        _timeZoneComboBox.SelectionChanged += OnTimeZoneSelectionChanged;
        sp.Children.Add(_timeZoneComboBox);

        _enableCustomFontSizeToggle = new ToggleSwitch { Content = "启用自定义字体大小", Margin = new Thickness(0, 10, 0, 0) };
        _enableCustomFontSizeToggle.IsCheckedChanged += OnEnableCustomFontSizeChanged;
        sp.Children.Add(_enableCustomFontSizeToggle);

        _enableCustomFontColorToggle = new ToggleSwitch { Content = "启用自定义字体颜色", Margin = new Thickness(0, 4, 0, 0) };
        _enableCustomFontColorToggle.IsCheckedChanged += OnEnableCustomFontColorChanged;
        sp.Children.Add(_enableCustomFontColorToggle);

        _styleTitleTextBlock = new TextBlock { Text = "字体样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) };
        sp.Children.Add(_styleTitleTextBlock);

        var colorRow = new Grid();
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _colorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_colorLabelTextBlock, 0);
        colorRow.Children.Add(_colorLabelTextBlock);

        _colorTextBox = new TextBox { Width = 120, Watermark = ThemeHelper.GetTextColorHex() };
        Grid.SetColumn(_colorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_colorTextBox, OnColorLostFocus);
        colorRow.Children.Add(_colorTextBox);
        sp.Children.Add(colorRow);

        var fontSizeRow = new Grid();
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _fontSizeLabelTextBlock = new TextBlock { Text = "文本大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_fontSizeLabelTextBlock, 0);
        fontSizeRow.Children.Add(_fontSizeLabelTextBlock);

        _fontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_fontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_fontSizeTextBox, OnFontSizeLostFocus);
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

    private void UpdateThemeColors()
    {
        _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _enableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _enableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _styleTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _colorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _fontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void OnEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontSize = _enableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontColor = _enableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        var fontSizeEnabled = Settings.EnableCustomFontSize;
        var fontColorEnabled = Settings.EnableCustomFontColor;
        _colorTextBox.IsEnabled = fontColorEnabled;
        _fontSizeTextBox.IsEnabled = fontSizeEnabled;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        foreach (var item in _timeZoneComboBox.Items)
        {
            if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZoneId)
            {
                _timeZoneComboBox.SelectedItem = item;
                break;
            }
        }
        _enableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        _enableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
        UpdateControlsEnabled();
        _colorTextBox.Text = Settings.FontColor;
        _fontSizeTextBox.Text = Settings.TextFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnTimeZoneSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_timeZoneComboBox.SelectedItem is TimeZoneInfo tz) Settings.TimeZoneId = tz.Id;
    }

    private void OnColorLostFocus(object? sender, EventArgs e)
    {
        var color = _colorTextBox.Text ?? ThemeHelper.GetTextColorHex();
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
            Settings.TextFontSize = size;
            _fontSizeTextBox.Text = Settings.TextFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            _fontSizeTextBox.Text = Settings.TextFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
