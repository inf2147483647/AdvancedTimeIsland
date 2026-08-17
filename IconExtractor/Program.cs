using System;
using System.Reflection;
using System.Linq;
using System.IO;

class Program
{
    static void Main()
    {
        string dllPath = @"c:\Users\Administrator\.nuget\packages\materialdesignthemes\4.8.0\lib\net7.0\MaterialDesignThemes.Wpf.dll";
        
        var assembly = Assembly.LoadFrom(dllPath);
        var enumType = assembly.GetType("MaterialDesignThemes.Wpf.PackIconKind");
        
        if (enumType == null)
        {
            Console.WriteLine("PackIconKind type not found. Listing all types in assembly...");
            foreach (var t in assembly.GetTypes().OrderBy(t => t.FullName))
            {
                Console.WriteLine(t.FullName);
            }
            return;
        }
        
        var names = Enum.GetNames(enumType).OrderBy(n => n).ToArray();
        Console.WriteLine($"Found {names.Length} PackIconKind values.");
        Console.WriteLine();
        
        string outputPath = Path.Combine(AppContext.BaseDirectory, "packiconkind_values.txt");
        File.WriteAllLines(outputPath, names);
        Console.WriteLine($"Written to: {outputPath}");
        Console.WriteLine();
        
        string[] checkValues = {
            "SwapHorizontal", "ClockTimeEightOutline", "ClockStart", "Numeric",
            "CalendarStarFourPoints", "CalendarRangeOutline", "Zodiac",
            "CalendarMonthOutline", "CalendarTodayOutline", "MapMarkerRadiusOutline",
            "WeatherSunnyAlert", "BookOpenVariant", "BugOutline", "Memory",
            "Earth", "Web", "TimerOutline", "ContentCopy",
            "CalendarOutline", "ClockOutline", "MapMarkerOutline"
        };
        
        Console.WriteLine("=== Value Check ===");
        foreach (var v in checkValues)
        {
            bool exists = names.Contains(v);
            Console.WriteLine($"{v}: {(exists ? "VALID" : "INVALID")}");
        }
    }
}