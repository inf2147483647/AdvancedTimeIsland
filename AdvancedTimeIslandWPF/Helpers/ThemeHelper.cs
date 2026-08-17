using System;
using System.Windows;
using System.Windows.Media;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Helpers;

public static class ThemeHelper
{
    /// <summary>
    /// 当前是否为深色主题。优先使用 ClassIsland 的 <see cref="IThemeService"/>，
    /// 服务不可用时回退为 Windows 系统应用主题。
    /// </summary>
    public static bool IsDarkTheme()
    {
        try
        {
            var themeService = IAppHost.TryGetService<IThemeService>();
            if (themeService != null)
            {
                return themeService.CurrentRealThemeMode == 1;
            }
        }
        catch
        {
        }

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int appsUseLightTheme)
            {
                return appsUseLightTheme == 0;
            }
        }
        catch
        {
        }

        return true;
    }

    public static Brush GetTextBrush()
    {
        return IsDarkTheme() ? Brushes.White : Brushes.Black;
    }

    public static string GetTextColorHex()
    {
        return IsDarkTheme() ? "#FFFFFF" : "#000000";
    }

    public static Brush GetSubTextBrush()
    {
        return IsDarkTheme() ? Brushes.LightGray : new SolidColorBrush(ParseColor("#222222"));
    }

    public static string GetSubTextColorHex()
    {
        return IsDarkTheme() ? "#D3D3D3" : "#222222";
    }

    public static Brush GetGrayBrush()
    {
        return IsDarkTheme() ? Brushes.Gray : Brushes.DimGray;
    }

    public static string GetGrayColorHex()
    {
        return IsDarkTheme() ? "#808080" : "#696969";
    }

    public static Brush GetLightBlueBrush()
    {
        return IsDarkTheme() ? Brushes.LightBlue : Brushes.RoyalBlue;
    }

    public static string GetLightBlueColorHex()
    {
        return IsDarkTheme() ? "#ADD8E6" : "#4169E1";
    }

    public static Brush GetYellowBrush()
    {
        return IsDarkTheme() ? Brushes.Yellow : Brushes.Goldenrod;
    }

    public static string GetYellowColorHex()
    {
        return IsDarkTheme() ? "#FFFF00" : "#DAA520";
    }

    public static Brush GetOrangeBrush()
    {
        return IsDarkTheme() ? Brushes.Orange : Brushes.DarkOrange;
    }

    public static string GetOrangeColorHex()
    {
        return IsDarkTheme() ? "#FFA500" : "#FF8C00";
    }

    public static Brush GetCardBackgroundBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(ParseColor("#2D2D30")) : new SolidColorBrush(ParseColor("#F5F5F5"));
    }

    public static string GetCardBackgroundColorHex()
    {
        return IsDarkTheme() ? "#2D2D30" : "#F5F5F5";
    }

    public static Brush GetSeparatorBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(ParseColor("#555555")) : new SolidColorBrush(ParseColor("#CCCCCC"));
    }

    public static string GetSeparatorColorHex()
    {
        return IsDarkTheme() ? "#555555" : "#CCCCCC";
    }

    public static Brush GetProgressRingBackgroundBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(ParseColor("#404040")) : new SolidColorBrush(ParseColor("#E8E8E8"));
    }

    public static string GetProgressRingBackgroundColorHex()
    {
        return IsDarkTheme() ? "#404040" : "#E8E8E8";
    }

    public static Brush GetQuoteBackgroundBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(ParseColor("#252528")) : new SolidColorBrush(ParseColor("#F0F0F0"));
    }

    public static string GetQuoteBackgroundColorHex()
    {
        return IsDarkTheme() ? "#252528" : "#F0F0F0";
    }

    public static Brush GetHanfuBackgroundBrush()
    {
        return IsDarkTheme() ? new SolidColorBrush(ParseColor("#1E1E1E")) : new SolidColorBrush(ParseColor("#FFFFFF"));
    }

    public static string GetHanfuBackgroundColorHex()
    {
        return IsDarkTheme() ? "#1E1E1E" : "#FFFFFF";
    }

    public static Brush GetYiBrush()
    {
        return IsDarkTheme() ? Brushes.LightGreen : Brushes.DarkGreen;
    }

    public static string GetYiColorHex()
    {
        return IsDarkTheme() ? "#90EE90" : "#006400";
    }

    public static Brush GetJiBrush()
    {
        return Brushes.Red;
    }

    public static string GetJiColorHex()
    {
        return "#FF0000";
    }

    public static Brush ParseColorOrThemeDefault(string? colorStr)
    {
        if (!string.IsNullOrEmpty(colorStr) && colorStr.StartsWith("#") && (colorStr.Length == 7 || colorStr.Length == 9))
        {
            try
            {
                return new SolidColorBrush(ParseColor(colorStr));
            }
            catch { }
        }
        return GetTextBrush();
    }

    public static Brush GetColorBrush(string? colorStr, bool enableCustom)
    {
        if (enableCustom)
        {
            return ParseColorOrThemeDefault(colorStr);
        }
        return GetTextBrush();
    }

    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// 订阅 ClassIsland 主题变更事件。插件初始化时调用一次。
    /// </summary>
    public static void Initialize()
    {
        try
        {
            var themeService = IAppHost.TryGetService<IThemeService>();
            if (themeService != null)
            {
                themeService.ThemeUpdated += OnThemeUpdated;
            }
        }
        catch
        {
        }
    }

    private static void OnThemeUpdated(object? sender, ClassIsland.Core.Models.Theming.ThemeUpdatedEventArgs e)
    {
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string GetContrastColor(string colorHex)
    {
        try
        {
            var color = ParseColor(colorHex);
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
            var color = ParseColor(colorHex);
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

    /// <summary>
    /// 将十六进制颜色字符串解析为 <see cref="Color"/>，兼容 6 位（#RRGGBB）与 8 位（#AARRGGBB）格式。
    /// </summary>
    public static Color ParseColor(string colorHex)
    {
        if (string.IsNullOrEmpty(colorHex))
        {
            return Colors.Black;
        }

        // 兼容 Avalonia 风格的无 # 前缀输入
        var hex = colorHex.TrimStart('#');
        if (hex.Length == 6)
        {
            hex = "FF" + hex;
        }
        else if (hex.Length == 8)
        {
            // 保持原样（AARRGGBB 顺序与 WPF 一致）
        }
        else
        {
            throw new FormatException($"无法解析颜色字符串: {colorHex}");
        }

        return (Color)ColorConverter.ConvertFromString("#" + hex);
    }
}
