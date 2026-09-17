using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia.Threading;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 联网更新在校日历数据的结果。
/// </summary>
public sealed class HolidayUpdateResult
{
    /// <summary>成功更新的年份。</summary>
    public List<int> UpdatedYears { get; } = new();

    /// <summary>更新失败的年份及原因。</summary>
    public List<string> FailedYears { get; } = new();

    /// <summary>实际使用的数据来源描述。</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>是否至少有一个年份更新成功。</summary>
    public bool Success => UpdatedYears.Count > 0;

    /// <summary>生成可直接展示给用户的结果文案。</summary>
    public string ToDisplayText()
    {
        var parts = new List<string>();
        if (UpdatedYears.Count > 0)
        {
            parts.Add($"已更新 {string.Join("、", UpdatedYears)} 年数据（来源：{Source}）");
        }
        if (FailedYears.Count > 0)
        {
            parts.Add("失败：" + string.Join("；", FailedYears));
        }
        return parts.Count == 0 ? "没有可更新的年份" : string.Join("；", parts);
    }
}

/// <summary>
/// 在校日历数据服务：负责节假日/调休/寒暑假/自定义日期与统计口径配置的
/// 持久化、内置数据恢复以及联网更新。
/// 数据存放于插件配置目录的 attendance_calendar.json，独立于 settings.json。
/// </summary>
public class AttendanceCalendarService
{
    private const string DataFileName = "attendance_calendar.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// GitHub raw 高速镜像前缀，与汉服内容更新保持一致的回退链。
    /// </summary>
    private static readonly string[] DownloadMirrorPrefixes =
    {
        "https://gh-proxy.com/",
        "https://gh-proxy.at9.net/",
        "https://ghproxy.net/",
        "https://ghfast.top/"
    };

    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly object _saveLock = new();
    private AttendanceCalendarData _data = new();

    /// <summary>当前服务实例（组件与设置页共享同一份数据）。</summary>
    public static AttendanceCalendarService? Instance { get; private set; }

    /// <summary>数据或统计口径发生变化时触发（已切换到 UI 线程）。</summary>
    public static event EventHandler? DataChanged;

    public AttendanceCalendarService()
    {
        // 仅首个实例接管静态 Instance，避免意外构造第二个实例时
        // 覆盖 DI 单例、导致组件与设置页读写不同的数据副本。
        Instance ??= this;
        Load();
    }

    /// <summary>当前在校日历数据（含统计口径配置）。</summary>
    public AttendanceCalendarData Data => _data;

    /// <summary>数据文件路径。</summary>
    public string DataFilePath =>
        Path.Combine(ResolveConfigFolder(), DataFileName);

    /// <summary>
    /// 从磁盘加载数据；文件不存在时用内置数据初始化。
    /// </summary>
    public void Load()
    {
        try
        {
            var filePath = DataFilePath;
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var loaded = JsonSerializer.Deserialize<AttendanceCalendarData>(json, JsonOptions);
                if (loaded != null)
                {
                    _data = loaded;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AttendanceCalendarService] 加载失败：{ex.Message}");
        }

        Normalize();
        _data.Config.PropertyChanged -= OnConfigPropertyChanged;
        _data.Config.PropertyChanged += OnConfigPropertyChanged;
        NotifyDataChanged();
    }

    /// <summary>
    /// 保存数据到磁盘并广播变更。
    /// </summary>
    public void Save()
    {
        lock (_saveLock)
        {
            try
            {
                var filePath = DataFilePath;
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempPath = filePath + ".tmp";
                var json = JsonSerializer.Serialize(_data, JsonOptions);
                File.WriteAllText(tempPath, json);

                if (File.Exists(filePath))
                {
                    File.Replace(tempPath, filePath, null);
                }
                else
                {
                    File.Move(tempPath, filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AttendanceCalendarService] 保存失败：{ex.Message}");
            }
        }

        NotifyDataChanged();
    }

    /// <summary>
    /// 用内置数据恢复节假日与调休表（保留用户手动编辑的条目）。
    /// </summary>
    /// <returns>恢复后的内置条目数。</returns>
    public int RestoreBuiltinData()
    {
        _data.Holidays.RemoveAll(x => x.Source != HolidayEntrySource.Manual);
        _data.MakeupDays.RemoveAll(x => x.Source != HolidayEntrySource.Manual);
        AppendMissing(_data.Holidays, BuiltinHolidayData.CreateHolidays());
        AppendMissing(_data.MakeupDays, BuiltinHolidayData.CreateMakeupDays());
        Save();
        return _data.Holidays.Count + _data.MakeupDays.Count;
    }

    /// <summary>
    /// 从网络拉取指定年份的法定节假日与调休安排并合并到现有数据。
    /// 内置与联网来源的同名年份条目会被替换，用户手动编辑的条目始终保留。
    /// </summary>
    public async Task<HolidayUpdateResult> UpdateFromNetworkAsync(IEnumerable<int> years)
    {
        var result = new HolidayUpdateResult();
        var targetYears = years.Distinct().Where(y => y >= 2000 && y <= 2100).OrderBy(y => y).ToList();

        foreach (var year in targetYears)
        {
            try
            {
                var (source, days) = await FetchYearAsync(year).ConfigureAwait(false);
                if (days.Count == 0)
                {
                    result.FailedYears.Add($"{year} 年：未获取到有效数据");
                    continue;
                }

                MergeYear(year, days);
                result.UpdatedYears.Add(year);
                result.Source = source;
            }
            catch (Exception ex)
            {
                result.FailedYears.Add($"{year} 年：{ex.Message}");
            }
        }

        if (result.Success)
        {
            _data.LastUpdated = DateTime.Now;
            _data.LastUpdateSource = result.Source;
            Save();
        }

        return result;
    }

    /// <summary>
    /// 需要联网更新的年份：覆盖学期区间所在年份，并额外包含前后一年。
    /// </summary>
    public static IEnumerable<int> BuildUpdateYears(DateTime semesterStart, DateTime semesterEnd)
    {
        var from = Math.Min(semesterStart.Year, semesterEnd.Year) - 1;
        var to = Math.Max(semesterStart.Year, semesterEnd.Year) + 1;
        for (var year = from; year <= to; year++)
        {
            yield return year;
        }
    }

    private void Normalize()
    {
        _data.Config ??= new AttendanceStatisticsConfig();
        _data.Holidays ??= new List<HolidayDay>();
        _data.MakeupDays ??= new List<HolidayDay>();
        _data.Vacations ??= new List<VacationRange>();
        _data.CustomIncluded ??= new List<HolidayDay>();
        _data.CustomExcluded ??= new List<HolidayDay>();
        _data.LastUpdateSource ??= string.Empty;

        // 首次运行：用内置数据初始化节假日与调休表。
        if (_data.Holidays.Count == 0 && _data.MakeupDays.Count == 0)
        {
            _data.Holidays.AddRange(BuiltinHolidayData.CreateHolidays());
            _data.MakeupDays.AddRange(BuiltinHolidayData.CreateMakeupDays());
            Save();
        }
    }

    private void OnConfigPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Save();
    }

    /// <summary>合并某年份的联网数据，替换内置/联网来源条目，保留手动条目。</summary>
    private void MergeYear(int year, List<HolidayDay> days)
    {
        _data.Holidays.RemoveAll(x => x.Date.Year == year && x.Source != HolidayEntrySource.Manual);
        _data.MakeupDays.RemoveAll(x => x.Date.Year == year && x.Source != HolidayEntrySource.Manual);

        var manualHolidays = new HashSet<DateTime>(
            _data.Holidays.Where(x => x.Source == HolidayEntrySource.Manual).Select(x => x.Date.Date));
        var manualMakeup = new HashSet<DateTime>(
            _data.MakeupDays.Where(x => x.Source == HolidayEntrySource.Manual).Select(x => x.Date.Date));

        foreach (var day in days)
        {
            if (day.Date.Year != year)
            {
                continue;
            }

            var target = day.IsOffDay
                ? (manualHolidays.Contains(day.Date.Date) ? null : _data.Holidays)
                : (manualMakeup.Contains(day.Date.Date) ? null : _data.MakeupDays);
            target?.Add(day);
        }
    }

    private static void AppendMissing(List<HolidayDay> target, List<HolidayDay> source)
    {
        var existing = new HashSet<DateTime>(target.Select(x => x.Date.Date));
        foreach (var day in source)
        {
            if (existing.Add(day.Date.Date))
            {
                target.Add(day);
            }
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AdvancedTimeIsland-Plugin");
        return client;
    }

    private static string ResolveConfigFolder()
    {
        var folder = Plugin.Instance?.PluginConfigFolder;
        if (!string.IsNullOrEmpty(folder))
        {
            return folder;
        }
        // 兜底：写入用户本地应用数据目录，避免污染 ClassIsland 程序目录。
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AdvancedTimeIsland");
    }

    private static void NotifyDataChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            DataChanged?.Invoke(null, EventArgs.Empty);
        }
        else
        {
            Dispatcher.UIThread.Post(() => DataChanged?.Invoke(null, EventArgs.Empty));
        }
    }

    /// <summary>
    /// 按 GitHub raw 直链 → 高速镜像 → timor.tech 的顺序拉取某年份数据。
    /// </summary>
    private static async Task<(string Source, List<HolidayDay> Days)> FetchYearAsync(int year)
    {
        var errors = new List<string>();

        var rawUrl = $"https://raw.githubusercontent.com/NateScarlet/holiday-cn/master/{year}.json";
        var sources = new List<(string Name, string Url)> { ("GitHub", rawUrl) };
        foreach (var prefix in DownloadMirrorPrefixes)
        {
            sources.Add((new Uri(prefix).Host, prefix + rawUrl));
        }

        foreach (var (name, url) in sources)
        {
            try
            {
                var json = await HttpClient.GetStringAsync(url).ConfigureAwait(false);
                var days = ParseHolidayCn(json);
                if (days.Count > 0)
                {
                    return ($"holiday-cn/{name}", days);
                }
                errors.Add($"{name}：未解析到节假日数据");
            }
            catch (Exception ex)
            {
                errors.Add($"{name}：{ex.Message}");
            }
        }

        try
        {
            var json = await HttpClient
                .GetStringAsync($"https://timor.tech/api/holiday/year/{year}/")
                .ConfigureAwait(false);
            var days = ParseTimorTech(json, year);
            if (days.Count > 0)
            {
                return ("timor.tech", days);
            }
            errors.Add("timor.tech：未解析到节假日数据");
        }
        catch (Exception ex)
        {
            errors.Add($"timor.tech：{ex.Message}");
        }

        throw new InvalidOperationException(string.Join("；", errors));
    }

    /// <summary>
    /// 解析 holiday-cn 数据格式：{"days":[{"name":"元旦","date":"2026-01-01","isOffDay":true}, ...]}
    /// </summary>
    private static List<HolidayDay> ParseHolidayCn(string json)
    {
        var result = new List<HolidayDay>();
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("days", out var days) ||
            days.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in days.EnumerateArray())
        {
            if (!item.TryGetProperty("date", out var dateElement) ||
                !DateTime.TryParse(dateElement.GetString(), out var date))
            {
                continue;
            }

            var name = item.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            var isOffDay = !item.TryGetProperty("isOffDay", out var offElement) ||
                           offElement.ValueKind != JsonValueKind.False;

            result.Add(new HolidayDay
            {
                Date = date.Date,
                Name = name,
                IsOffDay = isOffDay,
                Source = HolidayEntrySource.Network
            });
        }

        return result;
    }

    /// <summary>
    /// 解析 timor.tech 数据格式：
    /// {"holiday":{"01-01":{"holiday":true,"name":"元旦","date":"2026-01-01"}, ...}}
    /// </summary>
    private static List<HolidayDay> ParseTimorTech(string json, int year)
    {
        var result = new List<HolidayDay>();
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("holiday", out var holiday) ||
            holiday.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var item in holiday.EnumerateObject())
        {
            var value = item.Value;
            if (!value.TryGetProperty("date", out var dateElement) ||
                !DateTime.TryParse(dateElement.GetString(), out var date))
            {
                if (!DateTime.TryParse($"{year}-{item.Name}", out date))
                {
                    continue;
                }
            }

            var name = value.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            var isOffDay = !value.TryGetProperty("holiday", out var holidayElement) ||
                           holidayElement.ValueKind != JsonValueKind.False;

            result.Add(new HolidayDay
            {
                Date = date.Date,
                Name = name,
                IsOffDay = isOffDay,
                Source = HolidayEntrySource.Network
            });
        }

        return result;
    }
}