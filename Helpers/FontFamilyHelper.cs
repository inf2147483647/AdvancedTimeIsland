using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AdvancedTimeIsland.Helpers;

public static class FontFamilyHelper
{
    private static List<string>? _cachedSystemFontFamilies;

    public static event EventHandler? BodyFontSizeChanged;

    public static List<string> GetSystemFontFamilies()
    {
        if (_cachedSystemFontFamilies != null)
            return _cachedSystemFontFamilies;

        _cachedSystemFontFamilies = FontManager.Current.SystemFonts
            .Select(f => f.Name)
            .OrderBy(f => f)
            .ToList();
        return _cachedSystemFontFamilies;
    }
    
    public static FontFamily GetFontFamilyOrDefault(string fontFamilyName)
    {
        if (string.IsNullOrEmpty(fontFamilyName))
            return FontFamily.Default;
        
        try
        {
            return new FontFamily(fontFamilyName);
        }
        catch
        {
            return FontFamily.Default;
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
            "Thin" => FontWeight.Thin,
            "ExtraLight" => FontWeight.ExtraLight,
            "Light" => FontWeight.Light,
            "Medium" => FontWeight.Medium,
            "DemiBold" => FontWeight.DemiBold,
            "Bold" => FontWeight.Bold,
            "ExtraBold" => FontWeight.ExtraBold,
            "Black" => FontWeight.Black,
            "ExtraBlack" => FontWeight.ExtraBlack,
            _ => FontWeight.Normal
        };
    }

    public static double GetBodyFontSize(Control control)
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