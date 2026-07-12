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

public class ShengXiaoSettingsControl : ComponentBase<ShengXiaoSettings>
{
    private TextBox _labelColorTextBox;
    private TextBox _labelFontSizeTextBox;
    private TextBox _valueColorTextBox;
    private TextBox _valueFontSizeTextBox;
    private ToggleSwitch _enableCustomToggle;

    private TextBlock _labelTitleTextBlock;
    private TextBlock _labelColorLabelTextBlock;
    private TextBlock _labelFontSizeLabelTextBlock;
    private TextBlock _valueTitleTextBlock;
    private TextBlock _valueColorLabelTextBlock;
    private TextBlock _valueFontSizeLabelTextBlock;

    public ShengXiaoSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _enableCustomToggle = new ToggleSwitch { Content = "启用自定义颜色与字体", Margin = new Thickness(0, 10, 0, 0) };
        _enableCustomToggle.IsCheckedChanged += OnEnableCustomToggleChanged;
        sp.Children.Add(_enableCustomToggle);

        _labelTitleTextBlock = new TextBlock { Text = "标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_labelTitleTextBlock);

        var labelColorRow = new Grid();
        labelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _labelColorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_labelColorLabelTextBlock, 0);
        labelColorRow.Children.Add(_labelColorLabelTextBlock);

        _labelColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_labelColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_labelColorTextBox, OnLabelColorLostFocus);
        labelColorRow.Children.Add(_labelColorTextBox);
        sp.Children.Add(labelColorRow);

        var labelFontSizeRow = new Grid();
        labelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _labelFontSizeLabelTextBlock = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_labelFontSizeLabelTextBlock, 0);
        labelFontSizeRow.Children.Add(_labelFontSizeLabelTextBlock);

        _labelFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_labelFontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_labelFontSizeTextBox, OnLabelFontSizeLostFocus);
        labelFontSizeRow.Children.Add(_labelFontSizeTextBox);
        sp.Children.Add(labelFontSizeRow);

        _valueTitleTextBlock = new TextBlock { Text = "值样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_valueTitleTextBlock);

        var valueColorRow = new Grid();
        valueColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _valueColorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_valueColorLabelTextBlock, 0);
        valueColorRow.Children.Add(_valueColorLabelTextBlock);

        _valueColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_valueColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_valueColorTextBox, OnValueColorLostFocus);
        valueColorRow.Children.Add(_valueColorTextBox);
        sp.Children.Add(valueColorRow);

        var valueFontSizeRow = new Grid();
        valueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _valueFontSizeLabelTextBlock = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_valueFontSizeLabelTextBlock, 0);
        valueFontSizeRow.Children.Add(_valueFontSizeLabelTextBlock);

        _valueFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_valueFontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_valueFontSizeTextBox, OnValueFontSizeLostFocus);
        valueFontSizeRow.Children.Add(_valueFontSizeTextBox);
        sp.Children.Add(valueFontSizeRow);

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
        _enableCustomToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _labelColorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _labelFontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueColorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueFontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
        if (Settings.EnableCustomColorAndFont)
        {
            UpdateFontColorsForTheme();
        }
    }

    private void UpdateFontColorsForTheme()
    {
        var newLabelColor = ThemeHelper.GetSmartContrastColor(Settings.LabelFontColor);
        Settings.LabelFontColor = newLabelColor;
        _labelColorTextBox.Text = newLabelColor;

        var newValueColor = ThemeHelper.GetSmartContrastColor(Settings.ValueFontColor);
        Settings.ValueFontColor = newValueColor;
        _valueColorTextBox.Text = newValueColor;
    }

    private void OnEnableCustomToggleChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomColorAndFont = _enableCustomToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        var enabled = Settings.EnableCustomColorAndFont;
        _labelColorTextBox.IsEnabled = enabled;
        _labelFontSizeTextBox.IsEnabled = enabled;
        _valueColorTextBox.IsEnabled = enabled;
        _valueFontSizeTextBox.IsEnabled = enabled;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        _enableCustomToggle.IsChecked = Settings.EnableCustomColorAndFont;
        UpdateControlsEnabled();
        _labelColorTextBox.Text = Settings.LabelFontColor;
        _labelFontSizeTextBox.Text = Settings.LabelFontSize.ToString(CultureInfo.InvariantCulture);
        _valueColorTextBox.Text = Settings.ValueFontColor;
        _valueFontSizeTextBox.Text = Settings.ValueFontSize.ToString(CultureInfo.InvariantCulture);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnLabelColorLostFocus(object? sender, EventArgs e)
    {
        var color = _labelColorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try
            {
                Avalonia.Media.Color.Parse(color);
                Settings.LabelFontColor = color;
            }
            catch
            {
                _labelColorTextBox.Text = Settings.LabelFontColor;
            }
        }
        else
        {
            _labelColorTextBox.Text = Settings.LabelFontColor;
        }
    }

    private void OnLabelFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_labelFontSizeTextBox.Text, out double size))
        {
            Settings.LabelFontSize = size;
            _labelFontSizeTextBox.Text = Settings.LabelFontSize.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            _labelFontSizeTextBox.Text = Settings.LabelFontSize.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void OnValueColorLostFocus(object? sender, EventArgs e)
    {
        var color = _valueColorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try
            {
                Avalonia.Media.Color.Parse(color);
                Settings.ValueFontColor = color;
            }
            catch
            {
                _valueColorTextBox.Text = Settings.ValueFontColor;
            }
        }
        else
        {
            _valueColorTextBox.Text = Settings.ValueFontColor;
        }
    }

    private void OnValueFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_valueFontSizeTextBox.Text, out double size))
        {
            Settings.ValueFontSize = size;
            _valueFontSizeTextBox.Text = Settings.ValueFontSize.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            _valueFontSizeTextBox.Text = Settings.ValueFontSize.ToString(CultureInfo.InvariantCulture);
        }
    }
}
