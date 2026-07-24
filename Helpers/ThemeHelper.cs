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
        return IsDarkTheme() ? Brushes.LightGray : new SolidColorBrush(Color.Parse("#222222"));
    }

    public static string GetSubTextColorHex()
    {
        return IsDarkTheme() ? "#D3D3D3" : "#222222";
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

    public static IBrush GetCardBackgroundBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(Color.Parse("#2D2D30")) : new SolidColorBrush(Color.Parse("#F5F5F5"));
    }

    public static string GetCardBackgroundColorHex()
    {
        return IsDarkTheme() ? "#2D2D30" : "#F5F5F5";
    }

    public static IBrush GetSeparatorBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(Color.Parse("#555555")) : new SolidColorBrush(Color.Parse("#CCCCCC"));
    }

    public static string GetSeparatorColorHex()
    {
        return IsDarkTheme() ? "#555555" : "#CCCCCC";
    }

    public static IBrush GetProgressRingBackgroundBrush()
    {
        if (Application.Current?.Styles.TryGetResource("ControlStrongStrokeColorDisabledBrush", Application.Current.ActualThemeVariant, out var resource) == true && resource is IBrush brush)
        {
            return brush;
        }
        return IsDarkTheme() ? new SolidColorBrush(Color.Parse("#404040")) : new SolidColorBrush(Color.Parse("#E8E8E8"));
    }

    public static string GetProgressRingBackgroundColorHex()
    {
        return IsDarkTheme() ? "#404040" : "#E8E8E8";
    }

    public static IBrush GetQuoteBackgroundBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(Color.Parse("#252528")) : new SolidColorBrush(Color.Parse("#F0F0F0"));
    }

    public static string GetQuoteBackgroundColorHex()
    {
        return IsDarkTheme() ? "#252528" : "#F0F0F0";
    }

    public static IBrush GetHanfuBackgroundBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(Color.Parse("#1E1E1E")) : new SolidColorBrush(Color.Parse("#FFFFFF"));
    }

    public static string GetHanfuBackgroundColorHex()
    {
        return IsDarkTheme() ? "#1E1E1E" : "#FFFFFF";
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
                // 使用 GetSmartContrastColor 确保颜色在当前主题下可见
                var contrastColor = GetSmartContrastColor(colorStr);
                var color = Color.Parse(contrastColor);
                return new SolidColorBrush(color);
            }
            catch { }
        }
        return GetTextBrush();
    }

    public static IBrush GetColorBrush(string? colorStr, bool enableCustom)
    {
        if (enableCustom)
        {
            return ParseColorOrThemeDefault(colorStr);
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

    public static string GetContrastColor(string colorHex)
    {
        try
        {
            var color = Color.Parse(colorHex);
            var brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            if (brightness > 0.5)
            {
                return "#000000";
            }
            return "#FFFFFF";
        }
        catch
        {
            return IsDarkTheme() ? "#FFFFFF" : "#000000";
        }
    }

    public static string GetThemeAwareTextColor()
    {
        return IsDarkTheme() ? "#FFFFFF" : "#000000";
    }

    public static bool IsLightColor(string colorHex)
    {
        try
        {
            var color = Color.Parse(colorHex);
            var brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return brightness > 0.5;
        }
        catch
        {
            return true;
        }
    }

    public static string GetSmartContrastColor(string colorHex)
    {
        var isDark = IsDarkTheme();
        var isLight = IsLightColor(colorHex);
        
        if (isDark)
        {
            if (isLight)
            {
                return colorHex;
            }
            return "#FFFFFF";
        }
        else
        {
            if (!isLight)
            {
                return colorHex;
            }
            return "#000000";
        }
    }
}
