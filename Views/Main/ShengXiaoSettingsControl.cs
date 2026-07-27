using System;
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

public class ShengXiaoSettingsControl : ComponentBase<ShengXiaoSettings>
{
    private ColorPicker _labelColorPicker;
    private NumericUpDown _labelFontSizeNumericUpDown;
    private ColorPicker _valueColorPicker;
    private NumericUpDown _valueFontSizeNumericUpDown;
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

    public ShengXiaoSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _labelTitleTextBlock = new TextBlock { Text = "标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_labelTitleTextBlock);

        sp.Children.Add(CreateFontSizeRow("文本大小", out _labelFontSizeLabelTextBlock, out _labelFontSizeNumericUpDown, out _labelEnableCustomFontSizeToggle, OnLabelFontSizeChanged, OnLabelEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _labelColorLabelTextBlock, out _labelColorPicker, out _labelEnableCustomFontColorToggle, OnLabelColorChanged, OnLabelEnableCustomFontColorChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _labelFontFamilyComboBox, out _labelEnableCustomFontFamilyToggle, OnLabelEnableCustomFontFamilyChanged, OnLabelFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _labelFontWeightComboBox, out _labelEnableCustomFontWeightToggle, OnLabelEnableCustomFontWeightChanged, OnLabelFontWeightChanged));
        sp.Children.Add(CreateFontWeightHintTextBlock());

        _valueTitleTextBlock = new TextBlock { Text = "值样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_valueTitleTextBlock);

        sp.Children.Add(CreateFontSizeRow("文本大小", out _valueFontSizeLabelTextBlock, out _valueFontSizeNumericUpDown, out _valueEnableCustomFontSizeToggle, OnValueFontSizeChanged, OnValueEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _valueColorLabelTextBlock, out _valueColorPicker, out _valueEnableCustomFontColorToggle, OnValueColorChanged, OnValueEnableCustomFontColorChanged));
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

    private Grid CreateFontSizeRow(string labelText, out TextBlock label, out NumericUpDown numericUpDown, out ToggleSwitch toggle,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        numericUpDown = new NumericUpDown
        {
            Width = 155,
            Minimum = 1,
            Maximum = 100,
            Increment = 1m,
            FormatString = "0.00",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        Grid.SetColumn(numericUpDown, 1);
        row.Children.Add(numericUpDown);

        toggle = new ToggleSwitch { Content = "启用自定义文本大小", Margin = new Thickness(30, 0, 0, 0) };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    private Grid CreateColorRow(string labelText, out TextBlock label, out ColorPicker colorPicker, out ToggleSwitch toggle,
        EventHandler<ColorChangedEventArgs> colorChangedHandler, EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        colorPicker = new ColorPicker { Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        colorPicker.ColorChanged += colorChangedHandler;
        Grid.SetColumn(colorPicker, 1);
        row.Children.Add(colorPicker);

        toggle = new ToggleSwitch { Content = "启用自定义文本颜色", Margin = new Thickness(30, 0, 0, 0) };
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

        toggle = new ToggleSwitch { Content = "启用自定义字体样式", Margin = new Thickness(30, 0, 0, 0) };
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

        toggle = new ToggleSwitch { Content = "启用自定义字重", Margin = new Thickness(30, 0, 0, 0) };
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
        _labelColorPicker.IsEnabled = Settings.LabelEnableCustomFontColor;
        _labelFontSizeNumericUpDown.IsEnabled = Settings.LabelEnableCustomFontSize;
        _labelFontFamilyComboBox.IsEnabled = Settings.LabelEnableCustomFontFamily;
        _labelFontWeightComboBox.IsEnabled = Settings.LabelEnableCustomFontWeight;
        _valueColorPicker.IsEnabled = Settings.ValueEnableCustomFontColor;
        _valueFontSizeNumericUpDown.IsEnabled = Settings.ValueEnableCustomFontSize;
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
        _labelColorPicker.Color = Avalonia.Media.Color.Parse(Settings.LabelFontColor);
        _labelFontSizeNumericUpDown.Value = (decimal)Settings.LabelFontSize;
        _valueColorPicker.Color = Avalonia.Media.Color.Parse(Settings.ValueFontColor);
        _valueFontSizeNumericUpDown.Value = (decimal)Settings.ValueFontSize;
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

    private void OnLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.LabelFontColor = _labelColorPicker.Color.ToString();
    }

    private void OnLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_labelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.LabelFontSize = (double)_labelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnValueColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.ValueFontColor = _valueColorPicker.Color.ToString();
    }

    private void OnValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_valueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.ValueFontSize = (double)_valueFontSizeNumericUpDown.Value.Value;
        }
    }
}