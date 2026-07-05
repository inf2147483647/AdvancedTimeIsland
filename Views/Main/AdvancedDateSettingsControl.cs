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

public class AdvancedDateSettingsControl : ComponentBase<AdvancedDateSettings>
{
    private ToggleSwitch _showWeekDayToggle;
    private TextBox _colorTextBox;
    private TextBox _fontSizeTextBox;

    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;
    private TextBlock _labelTextBlock;
    private TextBlock _styleTitleTextBlock;
    private TextBlock _colorLabelTextBlock;
    private TextBlock _fontSizeLabelTextBlock;

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

        _styleTitleTextBlock = new TextBlock { Text = "字体样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Avalonia.Thickness(0, 10, 0, 0) };
        sp.Children.Add(_styleTitleTextBlock);

        var colorRow = new Grid();
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _colorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_colorLabelTextBlock, 0);
        colorRow.Children.Add(_colorLabelTextBlock);

        _colorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_colorTextBox, 1);
        _colorTextBox.LostFocus += OnColorLostFocus;
        colorRow.Children.Add(_colorTextBox);
        sp.Children.Add(colorRow);

        var fontSizeRow = new Grid();
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _fontSizeLabelTextBlock = new TextBlock { Text = "日期大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_fontSizeLabelTextBlock, 0);
        fontSizeRow.Children.Add(_fontSizeLabelTextBlock);

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

    private void UpdateThemeColors()
    {
        _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _labelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _styleTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _colorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _fontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        _showWeekDayToggle.IsChecked = Settings.ShowWeekDay;
        _colorTextBox.Text = Settings.FontColor;
        _fontSizeTextBox.Text = Settings.DateFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
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


