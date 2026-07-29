using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using Avalonia.Threading;
using ClassIsland.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Services;

public class FontSizeSyncService : IHostedService
{
    private readonly ILogger<FontSizeSyncService> _logger;
    private PropertyChangedEventHandler? _settingsPropertyChangedHandler;
    private object? _settingsServiceInstance;
    private object? _settingsInstance;
    private CancellationTokenSource? _retryCts;

    public static event EventHandler<double>? BodyFontSizeChanged;

    public static double LastKnownBodyFontSize { get; private set; } = 16;

    public FontSizeSyncService(ILogger<FontSizeSyncService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _retryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = TryInitializeAsync(_retryCts.Token);
        return Task.CompletedTask;
    }

    private async Task TryInitializeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (cancellationToken.IsCancellationRequested) return;

            if (IAppHost.Host != null)
            {
                if (TrySubscribe())
                {
                    return;
                }
            }

            await Task.Delay(500, cancellationToken);
        }

        _logger.LogWarning("FontSizeSyncService: 初始化超时，未能订阅 ClassIsland 字体大小变更");
    }

    private bool TrySubscribe()
    {
        try
        {
            var settingsServiceType = ResolveSettingsServiceType();
            if (settingsServiceType == null)
            {
                _logger.LogWarning("FontSizeSyncService: 无法解析 SettingsService 类型");
                return false;
            }

            _settingsServiceInstance = IAppHost.Host!.Services.GetService(settingsServiceType);
            if (_settingsServiceInstance == null)
            {
                _logger.LogWarning("FontSizeSyncService: 无法获取 SettingsService 实例");
                return false;
            }

            var settingsProperty = settingsServiceType.GetProperty("Settings");
            if (settingsProperty == null)
            {
                _logger.LogWarning("FontSizeSyncService: SettingsService 上未找到 Settings 属性");
                return false;
            }

            _settingsInstance = settingsProperty.GetValue(_settingsServiceInstance);
            if (_settingsInstance == null)
            {
                _logger.LogWarning("FontSizeSyncService: Settings 为 null");
                return false;
            }

            if (_settingsInstance is not INotifyPropertyChanged npc)
            {
                _logger.LogWarning("FontSizeSyncService: Settings 未实现 INotifyPropertyChanged");
                return false;
            }

            _settingsPropertyChangedHandler = (sender, args) =>
            {
                if (args.PropertyName == "MainWindowBodyFontSize")
                {
                    UpdateCachedFontSize();
                }
            };

            npc.PropertyChanged += _settingsPropertyChangedHandler;

            UpdateCachedFontSize();
            _logger.LogInformation("FontSizeSyncService: 已订阅 ClassIsland 正文字体大小变更");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FontSizeSyncService: 订阅失败");
            return false;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _retryCts?.Cancel();

            if (_settingsInstance is INotifyPropertyChanged npc && _settingsPropertyChangedHandler != null)
            {
                npc.PropertyChanged -= _settingsPropertyChangedHandler;
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    private void UpdateCachedFontSize()
    {
        try
        {
            if (_settingsInstance == null) return;

            var prop = _settingsInstance.GetType().GetProperty("MainWindowBodyFontSize");
            if (prop?.GetValue(_settingsInstance) is double fontSize)
            {
                LastKnownBodyFontSize = fontSize;
                Dispatcher.UIThread.Post(() =>
                {
                    BodyFontSizeChanged?.Invoke(null, fontSize);
                    FontFamilyHelper.RaiseBodyFontSizeChanged();
                });
            }
        }
        catch { }
    }

    private static Type? ResolveSettingsServiceType()
    {
        var settingsServiceAssemblyName = "ClassIsland";
        var settingsServiceTypeName = "ClassIsland.Services.SettingsService";

        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                if (asm.GetName().Name == settingsServiceAssemblyName)
                {
                    var type = asm.GetType(settingsServiceTypeName);
                    if (type != null) return type;
                }
            }

            foreach (var asm in assemblies)
            {
                var type = asm.GetType(settingsServiceTypeName);
                if (type != null) return type;
            }
        }
        catch { }

        return null;
    }
}