namespace AdvancedTimeIsland.Helpers;

public static class LongitudeConverter
{
    public static bool TryParseDecimal(string input, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;
        
        if (double.TryParse(input.Trim(), out double value))
        {
            result = Math.Max(-180, Math.Min(180, value));
            return true;
        }
        return false;
    }

    public static bool TryParseDms(string input, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim().ToUpper();

        int sign = 1;
        if (input.EndsWith("W"))
        {
            sign = -1;
            input = input[..^1].Trim();
        }
        else if (input.EndsWith("E"))
        {
            input = input[..^1].Trim();
        }
        else if (input.StartsWith("-"))
        {
            sign = -1;
            input = input[1..].Trim();
        }

        var parts = input.Split(new[] { '°', '°', 'd', 'D', '\'', '\"', '"' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1 || parts.Length > 3)
            return false;

        if (!int.TryParse(parts[0].Trim(), out int degrees))
            return false;
        
        double minutes = 0;
        if (parts.Length > 1 && !double.TryParse(parts[1].Trim(), out minutes))
            return false;
        
        double seconds = 0;
        if (parts.Length > 2 && !double.TryParse(parts[2].Trim(), out seconds))
            return false;

        if (degrees < 0 || degrees > 180)
            return false;
        if (minutes < 0 || minutes >= 60)
            return false;
        if (seconds < 0 || seconds >= 60)
            return false;

        result = sign * (degrees + minutes / 60.0 + seconds / 3600.0);
        result = Math.Max(-180, Math.Min(180, result));
        return true;
    }

    public static bool TryParseDms(int degrees, int minutes, double seconds, bool isEast, out double result)
    {
        result = 0;
        if (!ValidateDms(degrees, minutes, seconds))
            return false;

        var sign = isEast ? 1 : -1;
        result = sign * (degrees + minutes / 60.0 + seconds / 3600.0);
        result = Math.Max(-180, Math.Min(180, result));
        return true;
    }

    public static string ToDecimalString(double longitude)
    {
        longitude = Math.Max(-180, Math.Min(180, longitude));
        return longitude.ToString("F4");
    }

    public static string ToDmsString(double longitude)
    {
        longitude = Math.Max(-180, Math.Min(180, longitude));
        
        var sign = longitude >= 0 ? "E" : "W";
        var absLongitude = Math.Abs(longitude);
        
        var degrees = (int)Math.Floor(absLongitude);
        var remaining = absLongitude - degrees;
        
        var minutes = (int)Math.Floor(remaining * 60);
        remaining -= minutes / 60.0;
        
        var seconds = remaining * 3600;
        
        return $"{degrees}°{minutes}'{seconds:F2}\"{sign}";
    }

    public static void DecomposeDms(double longitude, out int degrees, out int minutes, out double seconds, out bool isEast)
    {
        longitude = Math.Max(-180, Math.Min(180, longitude));
        
        isEast = longitude >= 0;
        var absLongitude = Math.Abs(longitude);
        
        degrees = (int)Math.Floor(absLongitude);
        var remaining = absLongitude - degrees;
        
        minutes = (int)Math.Floor(remaining * 60);
        remaining -= minutes / 60.0;
        
        seconds = Math.Round(remaining * 3600, 2);
        
        if (seconds >= 60)
        {
            seconds = 0;
            minutes++;
        }
        if (minutes >= 60)
        {
            minutes = 0;
            degrees++;
        }
    }

    public static bool ValidateDms(int degrees, int minutes, double seconds)
    {
        return degrees >= 0 && degrees <= 180 &&
               minutes >= 0 && minutes < 60 &&
               seconds >= 0 && seconds < 60;
    }
}