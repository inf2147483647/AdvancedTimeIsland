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

public class XingZuoSettingsControl : ComponentBase<XingZuoSettings>
{
    private TextBox _labelColorTextBox;
    private TextBox _labelFontSizeTextBox;
    private TextBox _valueColorTextBox;
    private TextBox _valueFontSizeTextBox;
    private ToggleSwitch _labelEnableCustomFontSizeToggle;
    private ToggleSwitch _labelEnableCustomFontColorToggle;
    private ToggleSwitch _valueEnableCustomFontSizeToggle;
    private ToggleSwitch _valueEnableCustomFontColorToggle;
    private ToggleSwitch _labelEnableCustomFontFamilyToggle;
    private ToggleSwitch _valueEnableCustomFontFamilyToggle;
    private ToggleSwitch _labelEnableCustomFontWeightToggle;
    private ToggleSwitch _valueEnableCustomFontWeightToggle;
    private ComboBox _labelFontFamilyComboBox;
    private ComboBox _valueFontFamilyComboBox;
    private ComboBox _labelFontWeightComboBox;
    private ComboBox _valueFontWeightComboBox;

    private TextBlock _labelTitleTextBlock;
    private TextBlock _labelColorLabelTextBlock;
    private TextBlock _labelFontSizeLabelTextBlock;
    private TextBlock _valueTitleTextBlock;
    private TextBlock _valueColorLabelTextBlock;
    private TextBlock _valueFontSizeLabelTextBlock;

    public XingZuoSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _labelTitleTextBlock = new TextBlock { Text = "标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_labelTitleTextBlock);

        var labelColorRow = new Grid();
        labelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _labelColorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_labelColorLabelTextBlock, 0);
        labelColorRow.Children.Add(_labelColorLabelTextBlock);

        _labelColorTextBox = new TextBox { Width = 120, Watermark = ThemeHelper.GetTextColorHex() };
        Grid.SetColumn(_labelColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_labelColorTextBox, OnLabelColorLostFocus);
        labelColorRow.Children.Add(_labelColorTextBox);
        sp.Children.Add(labelColorRow);

        _labelEnableCustomFontColorToggle = new ToggleSwitch { Content = "使用自定义颜色", HorizontalAlignment = HorizontalAlignment.Left };
        _labelEnableCustomFontColorToggle.IsCheckedChanged += OnLabelEnableCustomFontColorChanged;
        sp.Children.Add(_labelEnableCustomFontColorToggle);

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

        _labelEnableCustomFontSizeToggle = new ToggleSwitch { Content = "使用自定义大小", HorizontalAlignment = HorizontalAlignment.Left };
        _labelEnableCustomFontSizeToggle.IsCheckedChanged += OnLabelEnableCustomFontSizeChanged;
        sp.Children.Add(_labelEnableCustomFontSizeToggle);

        var labelFontFamilyRow = new Grid();
        labelFontFamilyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelFontFamilyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelFontFamilyLabel = new TextBlock { Text = "字体:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(labelFontFamilyLabel, 0);
        labelFontFamilyRow.Children.Add(labelFontFamilyLabel);

        _labelFontFamilyComboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            _labelFontFamilyComboBox.Items.Add(font);
        }
        _labelFontFamilyComboBox.SelectionChanged += OnLabelFontFamilyChanged;
        Grid.SetColumn(_labelFontFamilyComboBox, 1);
        labelFontFamilyRow.Children.Add(_labelFontFamilyComboBox);
        sp.Children.Add(labelFontFamilyRow);

        var labelFontWeightRow = new Grid();
        labelFontWeightRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelFontWeightRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelFontWeightRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelFontWeightRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelFontWeightLabel = new TextBlock { Text = "字重:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(labelFontWeightLabel, 0);
        labelFontWeightRow.Children.Add(labelFontWeightLabel);

        _labelFontWeightComboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _labelFontWeightComboBox.Items.Add(weight);
        }
        _labelFontWeightComboBox.SelectionChanged += OnLabelFontWeightChanged;
        Grid.SetColumn(_labelFontWeightComboBox, 1);
        labelFontWeightRow.Children.Add(_labelFontWeightComboBox);

        _labelEnableCustomFontWeightToggle = new ToggleSwitch { Content = "启用自定义字重" };
        _labelEnableCustomFontWeightToggle.IsCheckedChanged += OnLabelEnableCustomFontWeightChanged;
        Grid.SetColumn(_labelEnableCustomFontWeightToggle, 2);
        labelFontWeightRow.Children.Add(_labelEnableCustomFontWeightToggle);
        sp.Children.Add(labelFontWeightRow);

        sp.Children.Add(new TextBlock
        {
            Text = "需要对应字体支持所选字重",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.Orange,
            Margin = new Thickness(0, 2, 0, 0)
        });

        _labelEnableCustomFontFamilyToggle = new ToggleSwitch { Content = "启用自定义字体", HorizontalAlignment = HorizontalAlignment.Left };
        _labelEnableCustomFontFamilyToggle.IsCheckedChanged += OnLabelEnableCustomFontFamilyChanged;
        sp.Children.Add(_labelEnableCustomFontFamilyToggle);

        _valueTitleTextBlock = new TextBlock { Text = "值样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_valueTitleTextBlock);

        var valueColorRow = new Grid();
        valueColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _valueColorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_valueColorLabelTextBlock, 0);
        valueColorRow.Children.Add(_valueColorLabelTextBlock);

        _valueColorTextBox = new TextBox { Width = 120, Watermark = ThemeHelper.GetTextColorHex() };
        Grid.SetColumn(_valueColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_valueColorTextBox, OnValueColorLostFocus);
        valueColorRow.Children.Add(_valueColorTextBox);
        sp.Children.Add(valueColorRow);

        _valueEnableCustomFontColorToggle = new ToggleSwitch { Content = "使用自定义颜色", HorizontalAlignment = HorizontalAlignment.Left };
        _valueEnableCustomFontColorToggle.IsCheckedChanged += OnValueEnableCustomFontColorChanged;
        sp.Children.Add(_valueEnableCustomFontColorToggle);

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

        _valueEnableCustomFontSizeToggle = new ToggleSwitch { Content = "使用自定义大小", HorizontalAlignment = HorizontalAlignment.Left };
        _valueEnableCustomFontSizeToggle.IsCheckedChanged += OnValueEnableCustomFontSizeChanged;
        sp.Children.Add(_valueEnableCustomFontSizeToggle);

        var valueFontFamilyRow = new Grid();
        valueFontFamilyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueFontFamilyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var valueFontFamilyLabel = new TextBlock { Text = "字体:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(valueFontFamilyLabel, 0);
        valueFontFamilyRow.Children.Add(valueFontFamilyLabel);

        _valueFontFamilyComboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            _valueFontFamilyComboBox.Items.Add(font);
        }
        _valueFontFamilyComboBox.SelectionChanged += OnValueFontFamilyChanged;
        Grid.SetColumn(_valueFontFamilyComboBox, 1);
        valueFontFamilyRow.Children.Add(_valueFontFamilyComboBox);
        sp.Children.Add(valueFontFamilyRow);

        var valueFontWeightRow = new Grid();
        valueFontWeightRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueFontWeightRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueFontWeightRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueFontWeightRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var valueFontWeightLabel = new TextBlock { Text = "字重:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(valueFontWeightLabel, 0);
        valueFontWeightRow.Children.Add(valueFontWeightLabel);

        _valueFontWeightComboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _valueFontWeightComboBox.Items.Add(weight);
        }
        _valueFontWeightComboBox.SelectionChanged += OnValueFontWeightChanged;
        Grid.SetColumn(_valueFontWeightComboBox, 1);
        valueFontWeightRow.Children.Add(_valueFontWeightComboBox);

        _valueEnableCustomFontWeightToggle = new ToggleSwitch { Content = "启用自定义字重" };
        _valueEnableCustomFontWeightToggle.IsCheckedChanged += OnValueEnableCustomFontWeightChanged;
        Grid.SetColumn(_valueEnableCustomFontWeightToggle, 2);
        valueFontWeightRow.Children.Add(_valueEnableCustomFontWeightToggle);
        sp.Children.Add(valueFontWeightRow);

        sp.Children.Add(new TextBlock
        {
            Text = "需要对应字体支持所选字重",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.Orange,
            Margin = new Thickness(0, 2, 0, 0)
        });

        _valueEnableCustomFontFamilyToggle = new ToggleSwitch { Content = "启用自定义字体", HorizontalAlignment = HorizontalAlignment.Left };
        _valueEnableCustomFontFamilyToggle.IsCheckedChanged += OnValueEnableCustomFontFamilyChanged;
        sp.Children.Add(_valueEnableCustomFontFamilyToggle);

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
        _labelEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
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
    }

    private void OnLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontSize = _labelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontColor = _labelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontSize = _valueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontColor = _valueEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontFamily = _labelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontFamily = _valueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontWeight = _labelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontWeight = _valueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_labelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.LabelFontFamily = _labelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnLabelFontWeightChanged(object? sender, EventArgs e)
    {
        if (_labelFontWeightComboBox.SelectedItem != null)
        {
            Settings.LabelFontWeight = _labelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnValueFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_valueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.ValueFontFamily = _valueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnValueFontWeightChanged(object? sender, EventArgs e)
    {
        if (_valueFontWeightComboBox.SelectedItem != null)
        {
            Settings.ValueFontWeight = _valueFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        _labelColorTextBox.IsEnabled = Settings.LabelEnableCustomFontColor;
        _labelFontSizeTextBox.IsEnabled = Settings.LabelEnableCustomFontSize;
        _labelFontFamilyComboBox.IsEnabled = Settings.LabelEnableCustomFontFamily;
        _labelFontWeightComboBox.IsEnabled = Settings.LabelEnableCustomFontWeight;
        _valueColorTextBox.IsEnabled = Settings.ValueEnableCustomFontColor;
        _valueFontSizeTextBox.IsEnabled = Settings.ValueEnableCustomFontSize;
        _valueFontFamilyComboBox.IsEnabled = Settings.ValueEnableCustomFontFamily;
        _valueFontWeightComboBox.IsEnabled = Settings.ValueEnableCustomFontWeight;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        _labelEnableCustomFontSizeToggle.IsChecked = Settings.LabelEnableCustomFontSize;
        _labelEnableCustomFontColorToggle.IsChecked = Settings.LabelEnableCustomFontColor;
        _valueEnableCustomFontSizeToggle.IsChecked = Settings.ValueEnableCustomFontSize;
        _valueEnableCustomFontColorToggle.IsChecked = Settings.ValueEnableCustomFontColor;
        _labelEnableCustomFontFamilyToggle.IsChecked = Settings.LabelEnableCustomFontFamily;
        _valueEnableCustomFontFamilyToggle.IsChecked = Settings.ValueEnableCustomFontFamily;
        _labelEnableCustomFontWeightToggle.IsChecked = Settings.LabelEnableCustomFontWeight;
        _valueEnableCustomFontWeightToggle.IsChecked = Settings.ValueEnableCustomFontWeight;
        UpdateControlsEnabled();
        _labelColorTextBox.Text = Settings.LabelFontColor;
        _labelFontSizeTextBox.Text = Settings.LabelFontSize.ToString(CultureInfo.InvariantCulture);
        _valueColorTextBox.Text = Settings.ValueFontColor;
        _valueFontSizeTextBox.Text = Settings.ValueFontSize.ToString(CultureInfo.InvariantCulture);
        _labelFontFamilyComboBox.SelectedItem = Settings.LabelFontFamily;
        _valueFontFamilyComboBox.SelectedItem = Settings.ValueFontFamily;
        _labelFontWeightComboBox.SelectedItem = Settings.LabelFontWeight;
        _valueFontWeightComboBox.SelectedItem = Settings.ValueFontWeight;
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
        var color = _labelColorTextBox.Text ?? ThemeHelper.GetTextColorHex();
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
        var color = _valueColorTextBox.Text ?? ThemeHelper.GetTextColorHex();
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
