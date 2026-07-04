using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using Avalonia.Threading;

namespace AdvancedTimeIsland.ViewModels.Main;

public class NextFestivalCountdownViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TimeBaseService _timeBaseService;
    private readonly NextFestivalCountdownSettings _settings;
    private readonly System.Timers.Timer _updateTimer;
    private readonly Action<string> _updateText1FontColor;
    private readonly Action<double> _updateText1FontSize;
    private readonly Action<string> _updateNameFontColor;
    private readonly Action<double> _updateNameFontSize;
    private readonly Action<string> _updateText3FontColor;
    private readonly Action<double> _updateText3FontSize;
    private readonly Action<string> _updateTimeFontColor;
    private readonly Action<double> _updateTimeFontSize;
    private string _text1Display = string.Empty;
    private string _nameDisplay = string.Empty;
    private string _text3Display = string.Empty;
    private string _timeDisplay = string.Empty;
    private bool _isDisposed;

    public string Text1Display { get => _text1Display; private set { if (_text1Display != value) { _text1Display = value; OnPropertyChanged(); } } }
    public string NameDisplay { get => _nameDisplay; private set { if (_nameDisplay != value) { _nameDisplay = value; OnPropertyChanged(); } } }
    public string Text3Display { get => _text3Display; private set { if (_text3Display != value) { _text3Display = value; OnPropertyChanged(); } } }
    public string TimeDisplay { get => _timeDisplay; private set { if (_timeDisplay != value) { _timeDisplay = value; OnPropertyChanged(); } } }

    public NextFestivalCountdownViewModel(TimeBaseService timeBaseService, NextFestivalCountdownSettings settings,
        Action<string> updateText1FontColor = null, Action<double> updateText1FontSize = null,
        Action<string> updateNameFontColor = null, Action<double> updateNameFontSize = null,
        Action<string> updateText3FontColor = null, Action<double> updateText3FontSize = null,
        Action<string> updateTimeFontColor = null, Action<double> updateTimeFontSize = null)
    {
        _timeBaseService = timeBaseService;
        _settings = settings;
        _updateText1FontColor = updateText1FontColor;
        _updateText1FontSize = updateText1FontSize;
        _updateNameFontColor = updateNameFontColor;
        _updateNameFontSize = updateNameFontSize;
        _updateText3FontColor = updateText3FontColor;
        _updateText3FontSize = updateText3FontSize;
        _updateTimeFontColor = updateTimeFontColor;
        _updateTimeFontSize = updateTimeFontSize;
        
        _settings.PropertyChanged += OnSettingsChanged;
        UpdateDisplay();
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += OnTimerElapsed;
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NextFestivalCountdownSettings.Text1FontColor)) _updateText1FontColor?.Invoke(_settings.Text1FontColor);
        else if (e.PropertyName == nameof(NextFestivalCountdownSettings.Text1FontSize)) _updateText1FontSize?.Invoke(_settings.Text1FontSize);
        else if (e.PropertyName == nameof(NextFestivalCountdownSettings.NameFontColor)) _updateNameFontColor?.Invoke(_settings.NameFontColor);
        else if (e.PropertyName == nameof(NextFestivalCountdownSettings.NameFontSize)) _updateNameFontSize?.Invoke(_settings.NameFontSize);
        else if (e.PropertyName == nameof(NextFestivalCountdownSettings.Text3FontColor)) _updateText3FontColor?.Invoke(_settings.Text3FontColor);
        else if (e.PropertyName == nameof(NextFestivalCountdownSettings.Text3FontSize)) _updateText3FontSize?.Invoke(_settings.Text3FontSize);
        else if (e.PropertyName == nameof(NextFestivalCountdownSettings.TimeFontColor)) _updateTimeFontColor?.Invoke(_settings.TimeFontColor);
        else if (e.PropertyName == nameof(NextFestivalCountdownSettings.TimeFontSize)) _updateTimeFontSize?.Invoke(_settings.TimeFontSize);
        else if (e.PropertyName == nameof(NextFestivalCountdownSettings.TimeFormat) || e.PropertyName == nameof(NextFestivalCountdownSettings.Text1) || e.PropertyName == nameof(NextFestivalCountdownSettings.Text3) ||
                 e.PropertyName == nameof(NextFestivalCountdownSettings.EnableInternationalFestivals) || e.PropertyName == nameof(NextFestivalCountdownSettings.EnableChineseTraditionalFestivals) || e.PropertyName == nameof(NextFestivalCountdownSettings.EnableRedFestivals))
        {
            UpdateDisplay();
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e) => _ = UpdateDisplayAsync();

    private void UpdateDisplay()
    {
        try
        {
            var now = _timeBaseService.GetCurrentTime();
            var nextFestival = GetNextFestival(now);
            var targetTime = new DateTime(nextFestival.Year, nextFestival.Month, nextFestival.Day, 0, 0, 0);
            var timeLeft = targetTime - now;
            Text1Display = _settings.Text1;
            NameDisplay = nextFestival.Name;
            Text3Display = _settings.Text3;
            TimeDisplay = FormatTime(timeLeft);
        }
        catch { }
    }

    private async System.Threading.Tasks.Task UpdateDisplayAsync()
    {
        try
        {
            var now = await _timeBaseService.GetCurrentTimeAsync().ConfigureAwait(false);
            var nextFestival = GetNextFestival(now);
            var targetTime = new DateTime(nextFestival.Year, nextFestival.Month, nextFestival.Day, 0, 0, 0);
            var timeLeft = targetTime - now;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Text1Display = _settings.Text1;
                NameDisplay = nextFestival.Name;
                Text3Display = _settings.Text3;
                TimeDisplay = FormatTime(timeLeft);
            });
        }
        catch { }
    }

    private (string Name, int Year, int Month, int Day) GetNextFestival(DateTime date)
    {
        var festivals = new List<(string Name, DateTime Date)>();

        if (_settings.EnableInternationalFestivals)
        {
            AddInternationalFestivals(festivals, date);
        }

        if (_settings.EnableChineseTraditionalFestivals)
        {
            AddChineseTraditionalFestivals(festivals, date);
        }

        if (_settings.EnableRedFestivals)
        {
            AddRedFestivals(festivals, date);
        }

        var nextFestival = festivals.Where(f => f.Date > date).OrderBy(f => f.Date).FirstOrDefault();
        if (nextFestival.Date == default)
        {
            if (_settings.EnableInternationalFestivals)
            {
                AddInternationalFestivals(festivals, date.AddYears(1));
            }

            if (_settings.EnableChineseTraditionalFestivals)
            {
                AddChineseTraditionalFestivals(festivals, date.AddYears(1));
            }

            if (_settings.EnableRedFestivals)
            {
                AddRedFestivals(festivals, date.AddYears(1));
            }

            nextFestival = festivals.Where(f => f.Date > date).OrderBy(f => f.Date).FirstOrDefault();
        }

        return (nextFestival.Name, nextFestival.Date.Year, nextFestival.Date.Month, nextFestival.Date.Day);
    }

    private void AddInternationalFestivals(List<(string Name, DateTime Date)> festivals, DateTime date)
    {
        festivals.Add(("元旦", new DateTime(date.Year, 1, 1)));
        festivals.Add(("妇女节", new DateTime(date.Year, 3, 8)));
        festivals.Add(("植树节", new DateTime(date.Year, 3, 12)));
        festivals.Add(("劳动节", new DateTime(date.Year, 5, 1)));
        festivals.Add(("儿童节", new DateTime(date.Year, 6, 1)));
        festivals.Add(("教师节", new DateTime(date.Year, 9, 10)));
        festivals.Add(("清明节", GetQingMingDate(date.Year)));
        festivals.Add(("冬至", GetDongZhiDate(date.Year)));
    }

    private void AddChineseTraditionalFestivals(List<(string Name, DateTime Date)> festivals, DateTime date)
    {
        var solar = Lunar.Solar.FromDate(date);
        var lunarYear = solar.Lunar.Year;

        festivals.Add(("春节", LunarToSolar(lunarYear, 1, 1)));
        festivals.Add(("元宵节", LunarToSolar(lunarYear, 1, 15)));
        festivals.Add(("寒食节", GetQingMingDate(date.Year).AddDays(-1)));
        festivals.Add(("清明节", GetQingMingDate(date.Year)));
        festivals.Add(("端午节", LunarToSolar(lunarYear, 5, 5)));
        festivals.Add(("七夕节", LunarToSolar(lunarYear, 7, 7)));
        festivals.Add(("中元节", LunarToSolar(lunarYear, 7, 15)));
        festivals.Add(("中秋节", LunarToSolar(lunarYear, 8, 15)));
        festivals.Add(("重阳节", LunarToSolar(lunarYear, 9, 9)));
        festivals.Add(("冬至", GetDongZhiDate(date.Year)));
        festivals.Add(("腊八节", LunarToSolar(lunarYear, 12, 8)));
        festivals.Add(("小年", LunarToSolar(lunarYear, 12, 23)));
        festivals.Add(("除夕", GetChuXiDate(lunarYear)));
    }

    private void AddRedFestivals(List<(string Name, DateTime Date)> festivals, DateTime date)
    {
        festivals.Add(("二七纪念日", new DateTime(date.Year, 2, 7)));
        festivals.Add(("学雷锋纪念日", new DateTime(date.Year, 3, 5)));
        festivals.Add(("五四青年节", new DateTime(date.Year, 5, 4)));
        festivals.Add(("七一建党节", new DateTime(date.Year, 7, 1)));
        festivals.Add(("八一建军节", new DateTime(date.Year, 8, 1)));
        festivals.Add(("中国人民抗日战争胜利纪念日", new DateTime(date.Year, 9, 3)));
        festivals.Add(("九一八事变纪念日", new DateTime(date.Year, 9, 18)));
        festivals.Add(("烈士纪念日", new DateTime(date.Year, 9, 30)));
        festivals.Add(("十一国庆节", new DateTime(date.Year, 10, 1)));
        festivals.Add(("中国工农红军长征胜利纪念日", new DateTime(date.Year, 10, 22)));
        festivals.Add(("南京大屠杀死难者国家公祭日", new DateTime(date.Year, 12, 13)));
    }

    private DateTime LunarToSolar(int lunarYear, int lunarMonth, int lunarDay)
    {
        try
        {
            var lunar = Lunar.Lunar.FromYmdHms(lunarYear, lunarMonth, lunarDay);
            var solar = lunar.Solar;
            return new DateTime(solar.Year, solar.Month, solar.Day);
        }
        catch
        {
            return DateTime.MaxValue;
        }
    }

    private DateTime GetQingMingDate(int year)
    {
        var solar = Lunar.Solar.FromYmdHms(year, 4, 4);
        var jieQi = solar.Lunar.JieQi;
        if (jieQi == "清明") return new DateTime(year, 4, 4);
        return new DateTime(year, 4, 5);
    }

    private DateTime GetDongZhiDate(int year)
    {
        var solar = Lunar.Solar.FromYmdHms(year, 12, 21);
        var jieQi = solar.Lunar.JieQi;
        if (jieQi == "冬至") return new DateTime(year, 12, 21);
        solar = Lunar.Solar.FromYmdHms(year, 12, 22);
        jieQi = solar.Lunar.JieQi;
        if (jieQi == "冬至") return new DateTime(year, 12, 22);
        return new DateTime(year, 12, 23);
    }

    private DateTime GetChuXiDate(int lunarYear)
    {
        try
        {
            var nextYearLunar = Lunar.Lunar.FromYmdHms(lunarYear + 1, 1, 1);
            var nextYearSolar = nextYearLunar.Solar;
            var nextYearDate = new DateTime(nextYearSolar.Year, nextYearSolar.Month, nextYearSolar.Day);
            return nextYearDate.AddDays(-1);
        }
        catch
        {
            return DateTime.MaxValue;
        }
    }

    private string FormatTime(TimeSpan timeLeft)
    {
        if (timeLeft.TotalSeconds < 0) return "0天";
        var totalSeconds = (long)timeLeft.TotalSeconds;
        var totalMilliseconds = timeLeft.TotalMilliseconds;
        var days = (int)(totalSeconds / 86400);
        var remainingSeconds = totalSeconds % 86400;
        var hours = (int)(remainingSeconds / 3600);
        remainingSeconds %= 3600;
        var minutes = (int)(remainingSeconds / 60);
        var seconds = (int)(remainingSeconds % 60);
        var milliseconds = (int)(totalMilliseconds % 1000);
        var format = string.IsNullOrEmpty(_settings.TimeFormat) ? "%d天" : _settings.TimeFormat;
        return format
            .Replace("%d", days.ToString())
            .Replace("%h", hours.ToString())
            .Replace("%m", minutes.ToString("D2"))
            .Replace("%s", seconds.ToString("D2"))
            .Replace("%x", milliseconds.ToString("D3"))
            .Replace("%H", ((int)timeLeft.TotalHours).ToString())
            .Replace("%M", ((int)timeLeft.TotalMinutes).ToString())
            .Replace("%S", totalSeconds.ToString())
            .Replace("%X", ((int)totalMilliseconds).ToString());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _settings.PropertyChanged -= OnSettingsChanged;
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
