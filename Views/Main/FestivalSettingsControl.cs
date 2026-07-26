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

public class FestivalSettingsControl : ComponentBase<FestivalSettings>
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

    public FestivalSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _labelTitleTextBlock = new TextBlock { Text = "标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_labelTitleTextBlock);

        sp.Children.Add(CreateFontSizeRow("文本大小", out _labelFontSizeLabelTextBlock, out _labelFontSizeTextBox, out _labelEnableCustomFontSizeToggle, OnLabelFontSizeLostFocus, OnLabelEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _labelColorLabelTextBlock, out _labelColorTextBox, out _labelEnableCustomFontColorToggle, OnLabelColorLostFocus, OnLabelEnableCustomFontColorChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _labelFontFamilyComboBox, out _labelEnableCustomFontFamilyToggle, OnLabelEnableCustomFontFamilyChanged, OnLabelFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _labelFontWeightComboBox, out _labelEnableCustomFontWeightToggle, OnLabelEnableCustomFontWeightChanged, OnLabelFontWeightChanged));
        sp.Children.Add(CreateFontWeightHintTextBlock());

        _valueTitleTextBlock = new TextBlock { Text = "值样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_valueTitleTextBlock);

        sp.Children.Add(CreateFontSizeRow("文本大小", out _valueFontSizeLabelTextBlock, out _valueFontSizeTextBox, out _valueEnableCustomFontSizeToggle, OnValueFontSizeLostFocus, OnValueEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _valueColorLabelTextBlock, out _valueColorTextBox, out _valueEnableCustomFontColorToggle, OnValueColorLostFocus, OnValueEnableCustomFontColorChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _valueFontFamilyComboBox, out _valueEnableCustomFontFamilyToggle, OnValueEnableCustomFontFamilyChanged, OnValueFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _valueFontWeightComboBox, out _valueEnableCustomFontWeightToggle, OnValueEnableCustomFontWeightChanged, OnValueFontWeightChanged));
        sp.Children.Add(CreateFontWeightHintTextBlock());

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
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        textBox = new TextBox { Width = 80, Watermark = "14", HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(textBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, lostFocusHandler);
        row.Children.Add(textBox);

        toggle = new ToggleSwitch { Content = "启用自定义文本大小" };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateColorRow(string labelText, out TextBlock label, out TextBox textBox, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> lostFocusHandler, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        textBox = new TextBox { Width = 120, Watermark = ThemeHelper.GetTextColorHex(), HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(textBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(textBox, lostFocusHandler);
        row.Children.Add(textBox);

        toggle = new ToggleSwitch { Content = "启用自定义文本颜色" };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateFontFamilyRow(string labelText, out ComboBox comboBox, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        comboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            comboBox.Items.Add(font);
        }
        comboBox.SelectionChanged += selectionChangedHandler;
        Grid.SetColumn(comboBox, 1);
        row.Children.Add(comboBox);

        toggle = new ToggleSwitch { Content = "启用自定义字体样式" };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateFontWeightRow(string labelText, out ComboBox comboBox, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        comboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            comboBox.Items.Add(weight);
        }
        comboBox.SelectionChanged += selectionChangedHandler;
        Grid.SetColumn(comboBox, 1);
        row.Children.Add(comboBox);

        toggle = new ToggleSwitch { Content = "启用自定义字重" };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private TextBlock CreateFontWeightHintTextBlock()
    {
        return new TextBlock
        {
            Text = "需要对应字体支持所选字重",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.Orange,
            Margin = new Thickness(0, 2, 0, 0)
        };
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

    private void OnLabelColorLostFocus(object? sender, RoutedEventArgs e)
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

    private void OnLabelFontSizeLostFocus(object? sender, RoutedEventArgs e)
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

    private void OnValueColorLostFocus(object? sender, RoutedEventArgs e)
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

    private void OnValueFontSizeLostFocus(object? sender, RoutedEventArgs e)
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
