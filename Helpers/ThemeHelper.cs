using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace AdvancedTimeIsland.Helpers;

public static class ThemeHelper
{
    public static bool IsDarkTheme()
    {
        var themeVariant = Application.Current?.ActualThemeVariant;
        if (themeVariant == ThemeVariant.Dark) return true;
        if (themeVariant == ThemeVariant.Light) return false;
        return true;
    }

    public static IBrush GetTextBrush()
    {
        return IsDarkTheme() ? Brushes.White : Brushes.Black;
    }

    public static string GetTextColorHex()
    {
        return IsDarkTheme() ? "#FFFFFF" : "#000000";
    }

    public static IBrush GetSubTextBrush()
    {
        return IsDarkTheme() ? Brushes.LightGray : Brushes.DarkGray;
    }

    public static string GetSubTextColorHex()
    {
        return IsDarkTheme() ? "#D3D3D3" : "#A9A9A9";
    }

    public static IBrush GetGrayBrush()
    {
        return IsDarkTheme() ? Brushes.Gray : Brushes.DimGray;
    }

    public static string GetGrayColorHex()
    {
        return IsDarkTheme() ? "#808080" : "#696969";
    }

    public static IBrush GetLightBlueBrush()
    {
        return IsDarkTheme() ? Brushes.LightBlue : Brushes.RoyalBlue;
    }

    public static string GetLightBlueColorHex()
    {
        return IsDarkTheme() ? "#ADD8E6" : "#4169E1";
    }

    public static IBrush GetYellowBrush()
    {
        return IsDarkTheme() ? Brushes.Yellow : Brushes.Goldenrod;
    }

    public static string GetYellowColorHex()
    {
        return IsDarkTheme() ? "#FFFF00" : "#DAA520";
    }

    public static IBrush GetOrangeBrush()
    {
        return IsDarkTheme() ? Brushes.Orange : Brushes.DarkOrange;
    }

    public static string GetOrangeColorHex()
    {
        return IsDarkTheme() ? "#FFA500" : "#FF8C00";
    }

    public static IBrush GetYiBrush()
    {
        return IsDarkTheme() ? Brushes.LightGreen : Brushes.DarkGreen;
    }

    public static string GetYiColorHex()
    {
        return IsDarkTheme() ? "#90EE90" : "#006400";
    }

    public static IBrush GetJiBrush()
    {
        return Brushes.Red;
    }

    public static string GetJiColorHex()
    {
        return "#FF0000";
    }

    public static IBrush ParseColorOrThemeDefault(string? colorStr)
    {
        if (!string.IsNullOrEmpty(colorStr) && colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
        {
            try
            {
                var color = Color.Parse(colorStr);
                return new SolidColorBrush(color);
            }
            catch { }
        }
        return GetTextBrush();
    }

    public static event EventHandler? ThemeChanged;

    public static void Initialize()
    {
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
    }

    private static void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }
}
