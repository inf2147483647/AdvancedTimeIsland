using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 数字输入控件事件参数（兼容原 Avalonia 版 NumericUpDown.ValueChanged 事件签名）。
/// </summary>
public class NumericUpDownValueChangedEventArgs : RoutedEventArgs
{
    public NumericUpDownValueChangedEventArgs(decimal? oldValue, decimal? newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public decimal? OldValue { get; }

    public decimal? NewValue { get; }
}

/// <summary>
/// WPF 数字输入控件。对外暴露与原 Avalonia 版 NumericUpDown 兼容的
/// <c>Value / Minimum / Maximum / Increment / FormatString</c> 属性与 <c>ValueChanged</c> 事件，
/// 以便最小化从 Avalonia 迁移的改动。
/// </summary>
public class WpfNumericUpDown : UserControl
{
    private readonly TextBox _textBox;
    private readonly Button _upButton;
    private readonly Button _downButton;

    private bool _isUpdating;

    public WpfNumericUpDown()
    {
        var panel = new DockPanel();

        _downButton = new Button
        {
            Content = "▼",
            Width = 24,
            FontSize = 9,
            Padding = new Thickness(0, 0, 0, 0)
        };
        DockPanel.SetDock(_downButton, Dock.Left);
        panel.Children.Add(_downButton);

        _upButton = new Button
        {
            Content = "▲",
            Width = 24,
            FontSize = 9,
            Padding = new Thickness(0, 0, 0, 0)
        };
        DockPanel.SetDock(_upButton, Dock.Right);
        panel.Children.Add(_upButton);

        _textBox = new TextBox
        {
            TextAlignment = TextAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(4, 2, 4, 2)
        };
        panel.Children.Add(_textBox);

        _upButton.Click += (_, _) => ChangeValue(Increment);
        _downButton.Click += (_, _) => ChangeValue(-Increment);
        _textBox.TextChanged += (_, _) =>
        {
            if (_isUpdating) return;
            if (decimal.TryParse(_textBox.Text, out var parsed))
            {
                Value = Clamp(parsed);
            }
        };
        _textBox.LostFocus += (_, _) => RefreshText();

        Content = panel;
    }

    private decimal Clamp(decimal value)
    {
        if (value < Minimum) return Minimum;
        if (value > Maximum) return Maximum;
        return value;
    }

    private void ChangeValue(decimal delta)
    {
        var current = Value ?? Minimum;
        Value = Clamp(current + delta);
    }

    private void RefreshText()
    {
        _isUpdating = true;
        try
        {
            _textBox.Text = Value.HasValue
                ? string.IsNullOrEmpty(FormatString) ? Value.Value.ToString() : Value.Value.ToString(FormatString)
                : "";
        }
        finally
        {
            _isUpdating = false;
        }
    }

    public decimal? Value
    {
        get => _value;
        set
        {
            var clamped = value.HasValue ? Clamp(value.Value) : (decimal?)null;
            if (_value == clamped) return;
            var oldValue = _value;
            _value = clamped;
            RefreshText();
            ValueChanged?.Invoke(this, new NumericUpDownValueChangedEventArgs(oldValue, clamped));
        }
    }

    public decimal Minimum { get; set; } = 0;

    public decimal Maximum { get; set; } = 100;

    public decimal Increment { get; set; } = 1;

    public string FormatString { get; set; } = "";

    public event EventHandler<NumericUpDownValueChangedEventArgs>? ValueChanged;

    private decimal? _value;

    public new double Width
    {
        get => base.Width;
        set => base.Width = value;
    }
}
