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

public class TomorrowYiJiSettingsControl : ComponentBase<TomorrowYiJiSettings>
{
    private NumericUpDown _yiLabelFontSizeNumericUpDown;
    private ColorPicker _yiLabelFontColorPicker;
    private NumericUpDown _yiValueFontSizeNumericUpDown;
    private NumericUpDown _jiLabelFontSizeNumericUpDown;
    private ColorPicker _jiLabelFontColorPicker;
    private NumericUpDown _jiValueFontSizeNumericUpDown;
    private ToggleSwitch _yiLabelEnableCustomFontSizeToggle;
    private ToggleSwitch _yiLabelEnableCustomFontColorToggle;
    private ToggleSwitch _yiLabelEnableCustomFontFamilyToggle;
    private ToggleSwitch _yiLabelEnableCustomFontWeightToggle;
    private ToggleSwitch _yiValueEnableCustomFontSizeToggle;
    private ToggleSwitch _yiValueEnableCustomFontFamilyToggle;
    private ToggleSwitch _yiValueEnableCustomFontWeightToggle;
    private ToggleSwitch _jiLabelEnableCustomFontSizeToggle;
    private ToggleSwitch _jiLabelEnableCustomFontColorToggle;
    private ToggleSwitch _jiLabelEnableCustomFontFamilyToggle;
    private ToggleSwitch _jiLabelEnableCustomFontWeightToggle;
    private ToggleSwitch _jiValueEnableCustomFontSizeToggle;
    private ToggleSwitch _jiValueEnableCustomFontFamilyToggle;
    private ToggleSwitch _jiValueEnableCustomFontWeightToggle;
    private ComboBox _yiLabelFontFamilyComboBox;
    private ComboBox _yiValueFontFamilyComboBox;
    private ComboBox _jiLabelFontFamilyComboBox;
    private ComboBox _jiValueFontFamilyComboBox;
    private ComboBox _yiLabelFontWeightComboBox;
    private ComboBox _yiValueFontWeightComboBox;
    private ComboBox _jiLabelFontWeightComboBox;
    private ComboBox _jiValueFontWeightComboBox;

    private ComboBox _displayModeComboBox;
    private TextBlock _displayModeLabel;
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
    private TextBlock _yiLabelFontFamilyLabel;
    private TextBlock _yiLabelFontWeightLabel;
    private TextBlock _yiValueFontFamilyLabel;
    private TextBlock _yiValueFontWeightLabel;
    private TextBlock _jiLabelFontFamilyLabel;
    private TextBlock _jiLabelFontWeightLabel;
    private TextBlock _jiValueFontFamilyLabel;
    private TextBlock _jiValueFontWeightLabel;
    private TextBox _yiLabelTextBox;
    private TextBox _jiLabelTextBox;
    private TextBlock _yiLabelTextLabel;
    private TextBlock _jiLabelTextLabel;

    public TomorrowYiJiSettingsControl() { InitializeComponent(); }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var infoBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityInformational());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Message", "注意：此组件的内容可能非常长，以至于超出屏幕，建议包括在滚动容器中使用。");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "IsClosable", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(infoBar, "Margin", new Thickness(0, 0, 0, 8));
        sp.Children.Add(infoBar);

        sp.Children.Add(CreateDisplayModeRow());

        _yiLabelTitle = new TextBlock { Text = "宜标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_yiLabelTitle);
        sp.Children.Add(CreateLabelTextRow("标签文本", out _yiLabelTextLabel, out _yiLabelTextBox, s => Settings.YiLabel = s));
        sp.Children.Add(CreateFontSizeRow("文本大小", out _yiLabelFontSizeLabel, out _yiLabelFontSizeNumericUpDown, out _yiLabelEnableCustomFontSizeToggle, OnYiLabelFontSizeChanged, OnYiLabelEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _yiLabelColorLabel, out _yiLabelFontColorPicker, out _yiLabelEnableCustomFontColorToggle, OnYiLabelColorChanged, OnYiLabelEnableCustomFontColorChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _yiLabelFontFamilyLabel, out _yiLabelFontFamilyComboBox, out _yiLabelEnableCustomFontFamilyToggle, OnYiLabelFontFamilyChanged, OnYiLabelEnableCustomFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _yiLabelFontWeightLabel, out _yiLabelFontWeightComboBox, out _yiLabelEnableCustomFontWeightToggle, OnYiLabelFontWeightChanged, OnYiLabelEnableCustomFontWeightChanged));

        _yiValueTitle = new TextBlock { Text = "宜内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_yiValueTitle);
        _yiValueColorNote = new TextBlock { Text = "颜色：绿色（固定）", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(_yiValueColorNote);
        sp.Children.Add(CreateFontSizeRow("文本大小", out _yiValueFontSizeLabel, out _yiValueFontSizeNumericUpDown, out _yiValueEnableCustomFontSizeToggle, OnYiValueFontSizeChanged, OnYiValueEnableCustomFontSizeChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _yiValueFontFamilyLabel, out _yiValueFontFamilyComboBox, out _yiValueEnableCustomFontFamilyToggle, OnYiValueFontFamilyChanged, OnYiValueEnableCustomFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _yiValueFontWeightLabel, out _yiValueFontWeightComboBox, out _yiValueEnableCustomFontWeightToggle, OnYiValueFontWeightChanged, OnYiValueEnableCustomFontWeightChanged));

        _jiLabelTitle = new TextBlock { Text = "忌标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_jiLabelTitle);
        sp.Children.Add(CreateLabelTextRow("标签文本", out _jiLabelTextLabel, out _jiLabelTextBox, s => Settings.JiLabel = s));
        sp.Children.Add(CreateFontSizeRow("文本大小", out _jiLabelFontSizeLabel, out _jiLabelFontSizeNumericUpDown, out _jiLabelEnableCustomFontSizeToggle, OnJiLabelFontSizeChanged, OnJiLabelEnableCustomFontSizeChanged));
        sp.Children.Add(CreateColorRow("文本颜色", out _jiLabelColorLabel, out _jiLabelFontColorPicker, out _jiLabelEnableCustomFontColorToggle, OnJiLabelColorChanged, OnJiLabelEnableCustomFontColorChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _jiLabelFontFamilyLabel, out _jiLabelFontFamilyComboBox, out _jiLabelEnableCustomFontFamilyToggle, OnJiLabelFontFamilyChanged, OnJiLabelEnableCustomFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _jiLabelFontWeightLabel, out _jiLabelFontWeightComboBox, out _jiLabelEnableCustomFontWeightToggle, OnJiLabelFontWeightChanged, OnJiLabelEnableCustomFontWeightChanged));

        _jiValueTitle = new TextBlock { Text = "忌内容样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_jiValueTitle);
        _jiValueColorNote = new TextBlock { Text = "颜色：红色（固定）", FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
        sp.Children.Add(_jiValueColorNote);
        sp.Children.Add(CreateFontSizeRow("文本大小", out _jiValueFontSizeLabel, out _jiValueFontSizeNumericUpDown, out _jiValueEnableCustomFontSizeToggle, OnJiValueFontSizeChanged, OnJiValueEnableCustomFontSizeChanged));
        sp.Children.Add(CreateFontFamilyRow("字体样式", out _jiValueFontFamilyLabel, out _jiValueFontFamilyComboBox, out _jiValueEnableCustomFontFamilyToggle, OnJiValueFontFamilyChanged, OnJiValueEnableCustomFontFamilyChanged));
        sp.Children.Add(CreateFontWeightRow("字重", out _jiValueFontWeightLabel, out _jiValueFontWeightComboBox, out _jiValueEnableCustomFontWeightToggle, OnJiValueFontWeightChanged, OnJiValueEnableCustomFontWeightChanged));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    private Grid CreateFontSizeRow(string labelText, out TextBlock label, out NumericUpDown numericUpDown, out ToggleSwitch toggleSwitch,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler, EventHandler<RoutedEventArgs> toggleCheckedHandler)
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

        toggleSwitch = new ToggleSwitch { Content = "启用自定义文本大小", Margin = new Thickness(30, 0, 0, 0) };
        toggleSwitch.IsCheckedChanged += toggleCheckedHandler;
        Grid.SetColumn(toggleSwitch, 2);
        row.Children.Add(toggleSwitch);

        return row;
    }

    private Grid CreateColorRow(string labelText, out TextBlock label, out ColorPicker colorPicker, out ToggleSwitch toggleSwitch,
        EventHandler<ColorChangedEventArgs> colorChangedHandler, EventHandler<RoutedEventArgs> toggleCheckedHandler)
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

        toggleSwitch = new ToggleSwitch { Content = "启用自定义文本颜色", Margin = new Thickness(30, 0, 0, 0) };
        toggleSwitch.IsCheckedChanged += toggleCheckedHandler;
        Grid.SetColumn(toggleSwitch, 2);
        row.Children.Add(toggleSwitch);

        return row;
    }

    private Grid CreateFontFamilyRow(string labelText, out TextBlock label, out ComboBox comboBox, out ToggleSwitch toggleSwitch,
        EventHandler<SelectionChangedEventArgs> selectionChangedHandler, EventHandler<RoutedEventArgs> toggleCheckedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
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

        toggleSwitch = new ToggleSwitch { Content = "启用自定义字体样式", Margin = new Thickness(30, 0, 0, 0) };
        toggleSwitch.IsCheckedChanged += toggleCheckedHandler;
        Grid.SetColumn(toggleSwitch, 2);
        row.Children.Add(toggleSwitch);

        return row;
    }

    private Grid CreateFontWeightRow(string labelText, out TextBlock label, out ComboBox comboBox, out ToggleSwitch toggleSwitch,
        EventHandler<SelectionChangedEventArgs> selectionChangedHandler, EventHandler<RoutedEventArgs> toggleCheckedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
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

        toggleSwitch = new ToggleSwitch { Content = "启用自定义字重", Margin = new Thickness(30, 0, 0, 0) };
        toggleSwitch.IsCheckedChanged += toggleCheckedHandler;
        Grid.SetColumn(toggleSwitch, 2);
        row.Children.Add(toggleSwitch);

        return row;
    }

    private Grid CreateDisplayModeRow()
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _displayModeLabel = new TextBlock { Text = "显示模式", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_displayModeLabel, 0);
        row.Children.Add(_displayModeLabel);

        _displayModeComboBox = new ComboBox { Width = 150, HorizontalAlignment = HorizontalAlignment.Left };
        _displayModeComboBox.Items.Add("单行");
        _displayModeComboBox.Items.Add("双行");
        _displayModeComboBox.SelectionChanged += OnDisplayModeChanged;
        Grid.SetColumn(_displayModeComboBox, 1);
        row.Children.Add(_displayModeComboBox);

        return row;
    }

    private Grid CreateLabelTextRow(string labelText, out TextBlock label, out TextBox textBox, Action<string>? onTextChanged)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        var tb = new TextBox { HorizontalAlignment = HorizontalAlignment.Left, Width = 200 };
        if (onTextChanged != null)
        {
            tb.TextChanged += (sender, e) => onTextChanged(tb.Text ?? "");
        }
        textBox = tb;
        Grid.SetColumn(textBox, 1);
        row.Children.Add(textBox);

        return row;
    }

    private void OnDisplayModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        Settings.DisplayMode = _displayModeComboBox.SelectedIndex;
    }

    private void UpdateThemeColors()
    {
        _displayModeLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelTitle.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelTextLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelColorLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelFontFamilyLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiLabelFontWeightLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiValueTitle.Foreground = ThemeHelper.GetTextBrush();
        _yiValueColorNote.Foreground = ThemeHelper.GetGrayBrush();
        _yiValueFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiValueFontFamilyLabel.Foreground = ThemeHelper.GetTextBrush();
        _yiValueFontWeightLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelTitle.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelTextLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelColorLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelFontFamilyLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiLabelFontWeightLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiValueTitle.Foreground = ThemeHelper.GetTextBrush();
        _jiValueColorNote.Foreground = ThemeHelper.GetGrayBrush();
        _jiValueFontSizeLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiValueFontFamilyLabel.Foreground = ThemeHelper.GetTextBrush();
        _jiValueFontWeightLabel.Foreground = ThemeHelper.GetTextBrush();
        
        _yiLabelEnableCustomFontSizeToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _yiLabelEnableCustomFontColorToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _yiLabelEnableCustomFontFamilyToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _yiLabelEnableCustomFontWeightToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _yiValueEnableCustomFontSizeToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _yiValueEnableCustomFontFamilyToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _yiValueEnableCustomFontWeightToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _jiLabelEnableCustomFontSizeToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _jiLabelEnableCustomFontColorToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _jiLabelEnableCustomFontFamilyToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _jiLabelEnableCustomFontWeightToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _jiValueEnableCustomFontSizeToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _jiValueEnableCustomFontFamilyToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _jiValueEnableCustomFontWeightToggle.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
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
        _yiLabelFontSizeNumericUpDown.IsEnabled = Settings.YiLabelEnableCustomFontSize;
        _yiLabelFontColorPicker.IsEnabled = Settings.YiLabelEnableCustomFontColor;
        _yiLabelFontFamilyComboBox.IsEnabled = Settings.YiLabelEnableCustomFontFamily;
        _yiLabelFontWeightComboBox.IsEnabled = Settings.YiLabelEnableCustomFontWeight;
        _yiValueFontSizeNumericUpDown.IsEnabled = Settings.YiValueEnableCustomFontSize;
        _yiValueFontFamilyComboBox.IsEnabled = Settings.YiValueEnableCustomFontFamily;
        _yiValueFontWeightComboBox.IsEnabled = Settings.YiValueEnableCustomFontWeight;
        _jiLabelFontSizeNumericUpDown.IsEnabled = Settings.JiLabelEnableCustomFontSize;
        _jiLabelFontColorPicker.IsEnabled = Settings.JiLabelEnableCustomFontColor;
        _jiLabelFontFamilyComboBox.IsEnabled = Settings.JiLabelEnableCustomFontFamily;
        _jiLabelFontWeightComboBox.IsEnabled = Settings.JiLabelEnableCustomFontWeight;
        _jiValueFontSizeNumericUpDown.IsEnabled = Settings.JiValueEnableCustomFontSize;
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
        _displayModeComboBox.SelectedIndex = Settings.DisplayMode;

        _yiLabelTextBox.Text = Settings.YiLabel;
        _jiLabelTextBox.Text = Settings.JiLabel;

        _yiLabelFontSizeNumericUpDown.Value = (decimal)Settings.YiLabelFontSize;
        _yiLabelFontColorPicker.Color = ParseColor(Settings.YiLabelFontColor);
        _yiValueFontSizeNumericUpDown.Value = (decimal)Settings.YiValueFontSize;
        _jiLabelFontSizeNumericUpDown.Value = (decimal)Settings.JiLabelFontSize;
        _jiLabelFontColorPicker.Color = ParseColor(Settings.JiLabelFontColor);
        _jiValueFontSizeNumericUpDown.Value = (decimal)Settings.JiValueFontSize;

        _yiLabelFontFamilyComboBox.SelectedItem = Settings.YiLabelFontFamily;
        _yiValueFontFamilyComboBox.SelectedItem = Settings.YiValueFontFamily;
        _jiLabelFontFamilyComboBox.SelectedItem = Settings.JiLabelFontFamily;
        _jiValueFontFamilyComboBox.SelectedItem = Settings.JiValueFontFamily;
        _yiLabelFontWeightComboBox.SelectedItem = Settings.YiLabelFontWeight;
        _yiValueFontWeightComboBox.SelectedItem = Settings.YiValueFontWeight;
        _jiLabelFontWeightComboBox.SelectedItem = Settings.JiLabelFontWeight;
        _jiValueFontWeightComboBox.SelectedItem = Settings.JiValueFontWeight;

        _yiLabelEnableCustomFontSizeToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.YiLabelEnableCustomFontSize);
        _yiLabelEnableCustomFontColorToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.YiLabelEnableCustomFontColor);
        _yiLabelEnableCustomFontFamilyToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.YiLabelEnableCustomFontFamily);
        _yiLabelEnableCustomFontWeightToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.YiLabelEnableCustomFontWeight);
        _yiValueEnableCustomFontSizeToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.YiValueEnableCustomFontSize);
        _yiValueEnableCustomFontFamilyToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.YiValueEnableCustomFontFamily);
        _yiValueEnableCustomFontWeightToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.YiValueEnableCustomFontWeight);
        _jiLabelEnableCustomFontSizeToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.JiLabelEnableCustomFontSize);
        _jiLabelEnableCustomFontColorToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.JiLabelEnableCustomFontColor);
        _jiLabelEnableCustomFontFamilyToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.JiLabelEnableCustomFontFamily);
        _jiLabelEnableCustomFontWeightToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.JiLabelEnableCustomFontWeight);
        _jiValueEnableCustomFontSizeToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.JiValueEnableCustomFontSize);
        _jiValueEnableCustomFontFamilyToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.JiValueEnableCustomFontFamily);
        _jiValueEnableCustomFontWeightToggle.SetValue(ToggleSwitch.IsCheckedProperty, Settings.JiValueEnableCustomFontWeight);

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

    private void OnYiLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_yiLabelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.YiLabelFontSize = (double)_yiLabelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnYiValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_yiValueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.YiValueFontSize = (double)_yiValueFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnJiLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_jiLabelFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.JiLabelFontSize = (double)_jiLabelFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnJiValueFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_jiValueFontSizeNumericUpDown.Value.HasValue)
        {
            Settings.JiValueFontSize = (double)_jiValueFontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnYiLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.YiLabelFontColor = _yiLabelFontColorPicker.Color.ToString();
    }

    private void OnJiLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.JiLabelFontColor = _jiLabelFontColorPicker.Color.ToString();
    }
}
