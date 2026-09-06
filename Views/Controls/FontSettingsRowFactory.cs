using System;
using System.Collections.Generic;
using AdvancedTimeIsland.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Controls;

/// <summary>
/// 复用的字体设置行控件工厂，供各主界面组件设置面板使用。
/// </summary>
public static class FontSettingsRowFactory
{
    /// <summary>
    /// 创建设置行的基础网格：标签列 | 控件列 | 开关列 | 剩余空间列。
    /// </summary>
    private static Grid CreateBaseRow()
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return row;
    }

    public static Grid CreateFontSizeRow(string labelText,
        out TextBlock label, out NumericUpDown numericUpDown, out ToggleSwitch toggle,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler,
        EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = CreateBaseRow();

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        numericUpDown = new NumericUpDown
        {
            Width = 155,
            Minimum = 1,
            Maximum = 72,
            Increment = 1m,
            FormatString = "0.00",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        Grid.SetColumn(numericUpDown, 1);
        row.Children.Add(numericUpDown);

        toggle = new ToggleSwitch { Content = "启用自定义文本大小", Margin = new Thickness(30, 0, 0, 0) };
        toggle.IsCheckedChanged += toggleHandler;
        Grid.SetColumn(toggle, 2);
        row.Children.Add(toggle);

        return row;
    }

    public static Grid CreateColorRow(string labelText,
        out TextBlock label, out ColorPicker colorPicker, out ToggleSwitch toggle,
        EventHandler<ColorChangedEventArgs> colorChangedHandler,
        EventHandler<RoutedEventArgs> toggleHandler)
    {
        var row = CreateBaseRow();

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

    public static Grid CreateFontFamilyRow(string labelText,
        out TextBlock label, out ComboBox comboBox, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> toggleHandler,
        EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var row = CreateBaseRow();

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

        toggle = new ToggleSwitch { Content = "启用自定义字体样式", Margin = new Thickness(30, 0, 0, 0) };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    public static Grid CreateFontWeightRow(string labelText,
        out TextBlock label, out ComboBox comboBox, out ToggleSwitch toggle,
        EventHandler<RoutedEventArgs> toggleHandler,
        EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var row = CreateBaseRow();

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

        toggle = new ToggleSwitch { Content = "启用自定义字重", Margin = new Thickness(30, 0, 0, 0) };
        Grid.SetColumn(toggle, 2);
        toggle.IsCheckedChanged += toggleHandler;
        row.Children.Add(toggle);

        return row;
    }

    /// <summary>
    /// 创建一个通用的下拉选择行（标签 | ComboBox | 可选开关 | 剩余空间）。
    /// </summary>
    public static Grid CreateComboBoxRow(string labelText, IEnumerable<object> items,
        out TextBlock label, out ComboBox comboBox, out ToggleSwitch? toggle,
        string? toggleContent = null,
        EventHandler<SelectionChangedEventArgs>? selectionChangedHandler = null,
        EventHandler<RoutedEventArgs>? toggleHandler = null)
    {
        var row = CreateBaseRow();

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        comboBox = new ComboBox { Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var item in items)
        {
            comboBox.Items.Add(item);
        }
        if (selectionChangedHandler != null)
        {
            comboBox.SelectionChanged += selectionChangedHandler;
        }
        Grid.SetColumn(comboBox, 1);
        row.Children.Add(comboBox);

        if (!string.IsNullOrEmpty(toggleContent))
        {
            toggle = new ToggleSwitch { Content = toggleContent, Margin = new Thickness(30, 0, 0, 0) };
            if (toggleHandler != null)
            {
                toggle.IsCheckedChanged += toggleHandler;
            }
            Grid.SetColumn(toggle, 2);
            row.Children.Add(toggle);
        }
        else
        {
            toggle = null;
        }

        return row;
    }

    public static TextBlock CreateFontWeightHintTextBlock()
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

    /// <summary>
    /// 为主题自适应统一刷新一组标签/切换开关的前景色。
    /// </summary>
    public static void ApplyTheme(IEnumerable<TextBlock> labels, IEnumerable<ToggleSwitch> toggles)
    {
        var brush = ThemeHelper.GetTextBrush();
        foreach (var label in labels)
        {
            if (label != null) label.Foreground = brush;
        }
        foreach (var toggle in toggles)
        {
            if (toggle != null) toggle.Foreground = brush;
        }
    }
}