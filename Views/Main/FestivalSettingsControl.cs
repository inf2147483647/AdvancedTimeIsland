using System;
using System.Globalization;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class FestivalSettingsControl : ComponentBase<FestivalSettings>
{
    private TextBox _labelColorTextBox;
    private TextBox _labelFontSizeTextBox;
    private TextBox _valueColorTextBox;
    private TextBox _valueFontSizeTextBox;

    public FestivalSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var labelTitle = new TextBlock { Text = "标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(labelTitle);

        var labelColorRow = new Grid();
        labelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelColorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(labelColorLabel, 0);
        labelColorRow.Children.Add(labelColorLabel);

        _labelColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_labelColorTextBox, 1);
        _labelColorTextBox.LostFocus += OnLabelColorLostFocus;
        labelColorRow.Children.Add(_labelColorTextBox);
        sp.Children.Add(labelColorRow);

        var labelFontSizeRow = new Grid();
        labelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelFontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(labelFontSizeLabel, 0);
        labelFontSizeRow.Children.Add(labelFontSizeLabel);

        _labelFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_labelFontSizeTextBox, 1);
        _labelFontSizeTextBox.LostFocus += OnLabelFontSizeLostFocus;
        labelFontSizeRow.Children.Add(_labelFontSizeTextBox);
        sp.Children.Add(labelFontSizeRow);

        var valueTitle = new TextBlock { Text = "值样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(valueTitle);

        var valueColorRow = new Grid();
        valueColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var valueColorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(valueColorLabel, 0);
        valueColorRow.Children.Add(valueColorLabel);

        _valueColorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_valueColorTextBox, 1);
        _valueColorTextBox.LostFocus += OnValueColorLostFocus;
        valueColorRow.Children.Add(_valueColorTextBox);
        sp.Children.Add(valueColorRow);

        var valueFontSizeRow = new Grid();
        valueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var valueFontSizeLabel = new TextBlock { Text = "字体大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(valueFontSizeLabel, 0);
        valueFontSizeRow.Children.Add(valueFontSizeLabel);

        _valueFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_valueFontSizeTextBox, 1);
        _valueFontSizeTextBox.LostFocus += OnValueFontSizeLostFocus;
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

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _labelColorTextBox.Text = Settings.LabelFontColor;
        _labelFontSizeTextBox.Text = Settings.LabelFontSize.ToString(CultureInfo.InvariantCulture);
        _valueColorTextBox.Text = Settings.ValueFontColor;
        _valueFontSizeTextBox.Text = Settings.ValueFontSize.ToString(CultureInfo.InvariantCulture);
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
