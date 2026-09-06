using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

/// <summary>
/// 从 ClassIsland 获取并缓存"学期开始日"的服务。
/// ClassIsland 使用 Settings.SingleWeekStartTime（单周开始时间，通常即学期开始日）。
/// </summary>
public class SemesterStartService : IHostedService
{
    private readonly ILogger<SemesterStartService> _logger;
    private object? _settingsServiceInstance;
    private object? _settingsInstance;
    private INotifyPropertyChanged? _settingsNpc;
    private PropertyChangedEventHandler? _handler;
    private CancellationTokenSource? _retryCts;

    /// <summary>学期开始日（未获取到时为 null）。</summary>
    public static DateTime? CurrentSemesterStartDate { get; private set; }

    /// <summary>学期开始日变化事件。</summary>
    public static event EventHandler? SemesterStartDateChanged;

    public SemesterStartService(ILogger<SemesterStartService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _retryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = TryInitializeAsync(_retryCts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _retryCts?.Cancel();
            if (_settingsNpc != null && _handler != null)
            {
                _settingsNpc.PropertyChanged -= _handler;
            }
        }
        catch { }
        return Task.CompletedTask;
    }

    private async Task TryInitializeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (cancellationToken.IsCancellationRequested) return;

            if (IAppHost.Host != null && TrySubscribe())
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        _logger.LogWarning("SemesterStartService: 初始化超时，未能获取 ClassIsland 学期开始日");
    }

    private bool TrySubscribe()
    {
        try
        {
            var settingsServiceType = ResolveSettingsServiceType();
            if (settingsServiceType == null)
            {
                _logger.LogWarning("SemesterStartService: 无法解析 SettingsService 类型");
                return false;
            }

            _settingsServiceInstance = IAppHost.Host!.Services.GetService(settingsServiceType);
            if (_settingsServiceInstance == null)
            {
                _logger.LogWarning("SemesterStartService: 无法获取 SettingsService 实例");
                return false;
            }

            var settingsProperty = settingsServiceType.GetProperty("Settings");
            if (settingsProperty == null)
            {
                _logger.LogWarning("SemesterStartService: SettingsService 上未找到 Settings 属性");
                return false;
            }

            _settingsInstance = settingsProperty.GetValue(_settingsServiceInstance);
            if (_settingsInstance == null)
            {
                _logger.LogWarning("SemesterStartService: Settings 为 null");
                return false;
            }

            var singleWeekProp = _settingsInstance.GetType().GetProperty("SingleWeekStartTime");
            if (singleWeekProp == null)
            {
                _logger.LogWarning("SemesterStartService: Settings 上未找到 SingleWeekStartTime 属性");
                return false;
            }

            if (_settingsInstance is not INotifyPropertyChanged npc)
            {
                _logger.LogWarning("SemesterStartService: Settings 未实现 INotifyPropertyChanged");
                return false;
            }

            _settingsNpc = npc;
            _handler = (sender, args) =>
            {
                if (args.PropertyName == "SingleWeekStartTime")
                {
                    UpdateSemesterStartDate(singleWeekProp);
                }
            };
            npc.PropertyChanged += _handler;

            UpdateSemesterStartDate(singleWeekProp);
            _logger.LogInformation("SemesterStartService: 已获取 ClassIsland 学期开始日");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SemesterStartService: 订阅失败");
            return false;
        }
    }

    private void UpdateSemesterStartDate(System.Reflection.PropertyInfo singleWeekProp)
    {
        try
        {
            if (singleWeekProp.GetValue(_settingsInstance) is DateTime start)
            {
                CurrentSemesterStartDate = start;
                Dispatcher.UIThread.Post(() => SemesterStartDateChanged?.Invoke(null, EventArgs.Empty));
            }
        }
        catch { }
    }

    private static Type? ResolveSettingsServiceType()
    {
        const string settingsServiceTypeName = "ClassIsland.Services.SettingsService";
        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "ClassIsland")
                {
                    var type = asm.GetType(settingsServiceTypeName);
                    if (type != null) return type;
                }
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(settingsServiceTypeName);
                if (type != null) return type;
            }
        }
        catch { }
        return null;
    }
}