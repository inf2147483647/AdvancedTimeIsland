using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace AdvancedTimeIsland.Helpers;

public static class FontFamilyHelper
{
    public static List<string> GetSystemFontFamilies()
    {
        return FontManager.Current.SystemFonts
            .Select(f => f.Name)
            .OrderBy(f => f)
            .ToList();
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
}