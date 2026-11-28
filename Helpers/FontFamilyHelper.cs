using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AdvancedTimeIsland.Helpers;

public static class FontFamilyHelper
{
    private static List<string>? _cachedSystemFontFamilies;

    public static event EventHandler? BodyFontSizeChanged;

    public static List<string> GetSystemFontFamilies()
    {
        if (_cachedSystemFontFamilies != null)
            return _cachedSystemFontFamilies;

        try
        {
            _cachedSystemFontFamilies = Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .Where(name => !string.IsNullOrEmpty(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Distinct()
                .ToList();
        }
        catch
        {
            _cachedSystemFontFamilies = new List<string>();
        }
        return _cachedSystemFontFamilies;
    }

    public static FontFamily GetFontFamilyOrDefault(string fontFamilyName)
    {
        if (string.IsNullOrEmpty(fontFamilyName))
            return new FontFamily("Segoe UI");

        try
        {
            return new FontFamily(fontFamilyName);
        }
        catch
        {
            return new FontFamily("Segoe UI");
        }
    }

    public static List<string> GetFontWeights()
    {
        return new List<string>
        {
            "Thin",
            "ExtraLight",
            "Light",
            "Normal",
            "Medium",
            "DemiBold",
            "Bold",
            "ExtraBold",
            "Black",
            "ExtraBlack"
        };
    }

    public static FontWeight GetFontWeightFromString(string fontWeightName)
    {
        return fontWeightName switch
        {
            "Thin" => FontWeights.Thin,
            "ExtraLight" => FontWeights.ExtraLight,
            "Light" => FontWeights.Light,
            "Medium" => FontWeights.Medium,
            "DemiBold" => FontWeights.DemiBold,
            "Bold" => FontWeights.Bold,
            "ExtraBold" => FontWeights.ExtraBold,
            "Black" => FontWeights.Black,
            "ExtraBlack" => FontWeights.ExtraBlack,
            _ => FontWeights.Normal
        };
    }

    public static double GetBodyFontSize(FrameworkElement control)
    {
        var cached = Services.FontSizeSyncService.LastKnownBodyFontSize;
        if (cached > 0)
            return cached;

        try
        {
            var result = control.FindResource("MainWindowBodyFontSize");
            if (result is double fontSize)
                return fontSize;
        }
        catch { }
        return 16;
    }

    public static double GetCachedBodyFontSize()
    {
        return Services.FontSizeSyncService.LastKnownBodyFontSize;
    }

    public static void RaiseBodyFontSizeChanged()
    {
        BodyFontSizeChanged?.Invoke(null, EventArgs.Empty);
    }
}
