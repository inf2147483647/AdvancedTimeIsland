using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 颜色选择控件事件参数（兼容原 Avalonia 版 ColorPicker.ColorChanged 事件签名）。
/// </summary>
public class ColorChangedEventArgs : RoutedEventArgs
{
    public ColorChangedEventArgs(Color oldColor, Color newColor)
    {
        OldColor = oldColor;
        NewColor = newColor;
    }

    public Color OldColor { get; }

    public Color NewColor { get; }
}

/// <summary>
/// WPF 颜色选择控件。封装 <see cref="MaterialDesignThemes.Wpf.ColorPicker"/>，
/// 对外暴露与原 Avalonia 版 ColorPicker 兼容的 <c>Color</c> 属性与 <c>ColorChanged</c> 事件，
/// 以便最小化从 Avalonia 迁移的改动。
/// </summary>
public class WpfColorPicker : UserControl
{
    private readonly MaterialDesignThemes.Wpf.ColorPicker _colorPicker;

    public WpfColorPicker()
    {
        _colorPicker = new MaterialDesignThemes.Wpf.ColorPicker
        {
            HueSliderPosition = Dock.Right
        };
        _colorPicker.ColorChanged += (sender, args) =>
        {
            ColorChanged?.Invoke(this, new ColorChangedEventArgs(args.OldValue, args.NewValue));
        };
        Content = _colorPicker;
    }

    /// <summary>
    /// 当前选中的颜色。
    /// </summary>
    public Color Color
    {
        get => _colorPicker.Color;
        set => _colorPicker.Color = value;
    }

    /// <summary>
    /// 颜色变更事件。
    /// </summary>
    public event EventHandler<ColorChangedEventArgs>? ColorChanged;
}
