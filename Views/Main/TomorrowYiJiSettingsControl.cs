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
    private ToggleSwitch _yiLabelEnableCustomFontFamilyToggle;
    private ToggleSwitch _yiValueEnableCustomFontSizeToggle;
    private ToggleSwitch _yiValueEnableCustomFontFamilyToggle;
    private ToggleSwitch _jiLabelEnableCustomFontSizeToggle;
    private ToggleSwitch _jiLabelEnableCustomFontColorToggle;
    private ToggleSwitch _jiLabelEnableCustomFontFamilyToggle;
    private ToggleSwitch _jiValueEnableCustomFontSizeToggle;
    private ToggleSwitch _jiValueEnableCustomFontFamilyToggle;
    private ToggleSwitch _yiLabelEnableCustomFontWeightToggle;
    private ToggleSwitch _yiValueEnableCustomFontWeightToggle;
    private ToggleSwitch _jiLabelEnableCustomFontWeightToggle;
    private ToggleSwitch _jiValueEnableCustomFontWeightToggle;
    private ComboBox _yiLabelFontFamilyComboBox;
    private ComboBox _yiValueFontFamilyComboBox;
    private ComboBox _jiLabelFontFamilyComboBox;
    private ComboBox _jiValueFontFamilyComboBox;
    private ComboBox _yiLabelFontWeightComboBox;
    private ComboBox _yiValueFontWeightComboBox;
    private ComboBox _jiLabelFontWeightComboBox;
    private ComboBox _jiValueFontWeightComboBox;

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
        sp.Children.Add(CreateFontFamilyRow("字体:", out _yiLabelFontFamilyComboBox, out _yiLabelEnableCustomFontFamilyToggle, OnYiLabelEnableCustomFontFamilyChanged, OnYiLabelFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重:", out _yiLabelFontWeightComboBox, out _yiLabelEnableCustomFontWeightToggle, OnYiLabelEnableCustomFontWeightChanged, OnYiLabelFontWeightChanged));
        sp.Children.Add(CreateFontWeightHintTextBlock());

        _yiValueTitle = new TextBlock { Text = "宜内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_yiValueTitle);

        _yiValueColorNote = new TextBlock { Text = "颜色：绿色（固定）", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(_yiValueColorNote);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _yiValueFontSizeLabel, out _yiValueFontSizeTextBox, out _yiValueEnableCustomFontSizeToggle, OnYiValueFontSizeLostFocus, OnYiValueEnableCustomFontSizeChanged));
        sp.Children.Add(CreateFontFamilyRow("字体:", out _yiValueFontFamilyComboBox, out _yiValueEnableCustomFontFamilyToggle, OnYiValueEnableCustomFontFamilyChanged, OnYiValueFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重:", out _yiValueFontWeightComboBox, out _yiValueEnableCustomFontWeightToggle, OnYiValueEnableCustomFontWeightChanged, OnYiValueFontWeightChanged));
        sp.Children.Add(CreateFontWeightHintTextBlock());

        _jiLabelTitle = new TextBlock { Text = "忌标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_jiLabelTitle);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _jiLabelFontSizeLabel, out _jiLabelFontSizeTextBox, out _jiLabelEnableCustomFontSizeToggle, OnJiLabelFontSizeLostFocus, OnJiLabelEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色:", out _jiLabelColorLabel, out _jiLabelFontColorPicker, out _jiLabelEnableCustomFontColorToggle, OnJiLabelEnableCustomFontColorChanged));
        sp.Children.Add(CreateFontFamilyRow("字体:", out _jiLabelFontFamilyComboBox, out _jiLabelEnableCustomFontFamilyToggle, OnJiLabelEnableCustomFontFamilyChanged, OnJiLabelFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重:", out _jiLabelFontWeightComboBox, out _jiLabelEnableCustomFontWeightToggle, OnJiLabelEnableCustomFontWeightChanged, OnJiLabelFontWeightChanged));
        sp.Children.Add(CreateFontWeightHintTextBlock());

        _jiValueTitle = new TextBlock { Text = "忌内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_jiValueTitle);

        _jiValueColorNote = new TextBlock { Text = "颜色：红色（固定）", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(_jiValueColorNote);
        sp.Children.Add(CreateFontSizeRow("字体大小:", out _jiValueFontSizeLabel, out _jiValueFontSizeTextBox, out _jiValueEnableCustomFontSizeToggle, OnJiValueFontSizeLostFocus, OnJiValueEnableCustomFontSizeChanged));
        sp.Children.Add(CreateFontFamilyRow("字体:", out _jiValueFontFamilyComboBox, out _jiValueEnableCustomFontFamilyToggle, OnJiValueEnableCustomFontFamilyChanged, OnJiValueFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重:", out _jiValueFontWeightComboBox, out _jiValueEnableCustomFontWeightToggle, OnJiValueEnableCustomFontWeightChanged, OnJiValueFontWeightChanged));
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
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

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

        toggle = new ToggleSwitch { Content = "使用自定义字体" };
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

        toggle = new ToggleSwitch { Content = "使用自定义字重" };
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
        _yiLabelEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _yiValueEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
        _jiValueEnableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();
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

    private void OnYiLabelEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.YiLabelEnableCustomFontFamily = _yiLabelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.YiLabelEnableCustomFontWeight = _yiLabelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiLabelFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiLabelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.YiLabelFontFamily = _yiLabelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiLabelFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiLabelFontWeightComboBox.SelectedItem != null)
        {
            Settings.YiLabelFontWeight = _yiLabelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiValueEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.YiValueEnableCustomFontFamily = _yiValueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.YiValueEnableCustomFontWeight = _yiValueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnYiValueFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiValueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.YiValueFontFamily = _yiValueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnYiValueFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_yiValueFontWeightComboBox.SelectedItem != null)
        {
            Settings.YiValueFontWeight = _yiValueFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiLabelEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.JiLabelEnableCustomFontFamily = _jiLabelEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.JiLabelEnableCustomFontWeight = _jiLabelEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiLabelFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiLabelFontFamilyComboBox.SelectedItem != null)
        {
            Settings.JiLabelFontFamily = _jiLabelFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiLabelFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiLabelFontWeightComboBox.SelectedItem != null)
        {
            Settings.JiLabelFontWeight = _jiLabelFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiValueEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.JiValueEnableCustomFontFamily = _jiValueEnableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.JiValueEnableCustomFontWeight = _jiValueEnableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnJiValueFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiValueFontFamilyComboBox.SelectedItem != null)
        {
            Settings.JiValueFontFamily = _jiValueFontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnJiValueFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_jiValueFontWeightComboBox.SelectedItem != null)
        {
            Settings.JiValueFontWeight = _jiValueFontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        _yiLabelFontSizeTextBox.IsEnabled = Settings.YiLabelEnableCustomFontSize;
        _yiLabelFontColorPicker.IsEnabled = Settings.YiLabelEnableCustomFontColor;
        _yiLabelFontFamilyComboBox.IsEnabled = Settings.YiLabelEnableCustomFontFamily;
        _yiLabelFontWeightComboBox.IsEnabled = Settings.YiLabelEnableCustomFontWeight;
        _yiValueFontSizeTextBox.IsEnabled = Settings.YiValueEnableCustomFontSize;
        _yiValueFontFamilyComboBox.IsEnabled = Settings.YiValueEnableCustomFontFamily;
        _yiValueFontWeightComboBox.IsEnabled = Settings.YiValueEnableCustomFontWeight;
        _jiLabelFontSizeTextBox.IsEnabled = Settings.JiLabelEnableCustomFontSize;
        _jiLabelFontColorPicker.IsEnabled = Settings.JiLabelEnableCustomFontColor;
        _jiLabelFontFamilyComboBox.IsEnabled = Settings.JiLabelEnableCustomFontFamily;
        _jiLabelFontWeightComboBox.IsEnabled = Settings.JiLabelEnableCustomFontWeight;
        _jiValueFontSizeTextBox.IsEnabled = Settings.JiValueEnableCustomFontSize;
        _jiValueFontFamilyComboBox.IsEnabled = Settings.JiValueEnableCustomFontFamily;
        _jiValueFontWeightComboBox.IsEnabled = Settings.JiValueEnableCustomFontWeight;
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

        _yiLabelFontFamilyComboBox.SelectedItem = Settings.YiLabelFontFamily;
        _yiValueFontFamilyComboBox.SelectedItem = Settings.YiValueFontFamily;
        _jiLabelFontFamilyComboBox.SelectedItem = Settings.JiLabelFontFamily;
        _jiValueFontFamilyComboBox.SelectedItem = Settings.JiValueFontFamily;
        _yiLabelFontWeightComboBox.SelectedItem = Settings.YiLabelFontWeight;
        _yiValueFontWeightComboBox.SelectedItem = Settings.YiValueFontWeight;
        _jiLabelFontWeightComboBox.SelectedItem = Settings.JiLabelFontWeight;
        _jiValueFontWeightComboBox.SelectedItem = Settings.JiValueFontWeight;

        _yiLabelEnableCustomFontSizeToggle.IsChecked = Settings.YiLabelEnableCustomFontSize;
        _yiLabelEnableCustomFontColorToggle.IsChecked = Settings.YiLabelEnableCustomFontColor;
        _yiLabelEnableCustomFontFamilyToggle.IsChecked = Settings.YiLabelEnableCustomFontFamily;
        _yiLabelEnableCustomFontWeightToggle.IsChecked = Settings.YiLabelEnableCustomFontWeight;
        _yiValueEnableCustomFontSizeToggle.IsChecked = Settings.YiValueEnableCustomFontSize;
        _yiValueEnableCustomFontFamilyToggle.IsChecked = Settings.YiValueEnableCustomFontFamily;
        _yiValueEnableCustomFontWeightToggle.IsChecked = Settings.YiValueEnableCustomFontWeight;
        _jiLabelEnableCustomFontSizeToggle.IsChecked = Settings.JiLabelEnableCustomFontSize;
        _jiLabelEnableCustomFontColorToggle.IsChecked = Settings.JiLabelEnableCustomFontColor;
        _jiLabelEnableCustomFontFamilyToggle.IsChecked = Settings.JiLabelEnableCustomFontFamily;
        _jiLabelEnableCustomFontWeightToggle.IsChecked = Settings.JiLabelEnableCustomFontWeight;
        _jiValueEnableCustomFontSizeToggle.IsChecked = Settings.JiValueEnableCustomFontSize;
        _jiValueEnableCustomFontFamilyToggle.IsChecked = Settings.JiValueEnableCustomFontFamily;
        _jiValueEnableCustomFontWeightToggle.IsChecked = Settings.JiValueEnableCustomFontWeight;

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
