﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

public enum LongitudeDisplayMode
{
    Decimal,
    Dms
}

/// <summary>
/// 悬浮窗层级
/// </summary>
public enum FloatingScheduleWindowLayer
{
    /// <summary>
    /// 置底
    /// </summary>
    Bottom = 0,
    /// <summary>
    /// 置顶（不推荐）
    /// </summary>
    Topmost = 1
}

/// <summary>
/// 悬浮窗层级重设频率（参考 ClassIsland.WindowSettingsPage.WindowTopmostRecheckMode 索引语义）。
/// </summary>
public enum FloatingTopmostRefreshMode
{
    /// <summary>
    /// 0 - 窗口层级变化时（默认）：Win32 WM_WINDOWPOSCHANGED &amp; !SWP_NOZORDER 触发；非 Win 退化为前台窗口变化。
    /// </summary>
    OnWindowZOrderChanged = 0,
    /// <summary>
    /// 1 - 前台窗口变化时：IWindowPlatformService.ForegroundWindowChanged 事件触发。
    /// </summary>
    OnForegroundWindowChanged = 1,
    /// <summary>
    /// 2 - 每 50ms：DispatcherTimer 周期性重设。
    /// </summary>
    Every50Ms = 2,
    /// <summary>
    /// 3 - 每 1ms：DispatcherTimer 极高频率周期性重设（注意性能占用与闪烁风险）。
    /// </summary>
    Every1Ms = 3,
    /// <summary>
    /// 4 - 每 2s：DispatcherTimer 低频周期性重设（对改层级即时性要求低时，最省资源）。
    /// </summary>
    Every2s = 4
}

/// <summary>
/// 悬浮窗隐藏模式（时间表悬浮窗"隐藏悬浮窗"功能）。
/// </summary>
public enum FloatingScheduleHideMode
{
    /// <summary>
    /// 0 - 跟随 ClassIsland 主界面隐藏规则（默认）：主窗口隐藏时悬浮窗同步隐藏。
    /// </summary>
    FollowHost = 0,
    /// <summary>
    /// 1 - 基础模式：复用 ClassIsland 的"上课时隐藏 / 前台窗口最大化时隐藏 / 前台窗口全屏时隐藏"三个开关独立判定。
    /// </summary>
    Basic = 1,
    /// <summary>
    /// 2 - 高级模式（规则集）：复用 ClassIsland 的隐藏规则集 HideRules，经宿主 IRulesetService.IsRulesetSatisfied 判定。
    /// </summary>
    Advanced = 2,
    /// <summary>
    /// 3 - 从不隐藏：悬浮窗不受任何隐藏规则影响。
    /// </summary>
    Never = 3
}

/// <summary>
/// 插件全局设置
/// </summary>
public class PluginSettings : INotifyPropertyChanged
{
    private bool _isLunarInstalled = false;
    private double _timeOffsetSeconds = 0;
    private double _longitude = 114.2628;
    private string _timeZoneId = "China Standard Time";
    private bool _enableCountdownNotification = true;
    private int _countdownAlertSeconds = 60;
    private LongitudeDisplayMode _longitudeDisplayMode = LongitudeDisplayMode.Decimal;
    private bool _enableEasterEgg = false;
    private bool _disclaimerAccepted = false;
    private bool _easterEggDisclaimerAccepted = false;
    private bool _easterEggInfoAccepted = false;
    private string _ntpServer = "ntp.aliyun.com";
    private int _ntpSyncIntervalMinutes = 5;
    private DateTime? _lastSyncTime;
    private string? _lastSyncStatus;
    private string? _lastSyncSource;
    private bool _enableExperimentalFeatures = false;
    private bool _enableLunarCalendar = true;
    private bool _enableLocalSolarTime = true;
    private bool _enableTimeZoneTime = true;
    private bool _enableXingZuo = true;
    private bool _enableJieQi = true;
    private bool _enableDayYiJi = true;
    private bool _enableShengXiao = true;
    private bool _enableFestival = true;
    private string? _cachedVersion;
    private bool _enableFloatingSchedule = true;
    private FloatingScheduleWindowLayer _floatingScheduleWindowLayer = FloatingScheduleWindowLayer.Bottom;
    private int _floatingSchedulePositionX = 100;
    private int _floatingSchedulePositionY = 100;
    private bool _floatingScheduleClickThrough = false;
    private bool _floatingScheduleHoverFade = false;
    private bool _floatingScheduleHoverFadeReverse = false;
    private double _floatingScheduleOpacity = 0.85;
    private double _floatingScheduleFontScale = 18.0;
    private bool _floatingScheduleEnableFullTeacherName = false;
    private bool _floatingScheduleShowTeacher = true;
    private bool _floatingScheduleEdgeHide = false;
    private double _floatingScheduleEdgeHideDelay = 3.0;
    private FloatingTopmostRefreshMode _floatingScheduleTopmostRefreshMode = FloatingTopmostRefreshMode.Every50Ms;
    private FloatingScheduleHideMode _floatingScheduleHideMode = FloatingScheduleHideMode.FollowHost;
    private bool _floatingScheduleRandomTitle = false;
    private bool _floatingScheduleRandomTitleEnhanced = false;
    private bool _floatingSchedulePreventCapture = false;

    public string? CachedVersion
    {
        get => _cachedVersion;
        set
        {
            if (_cachedVersion != value)
            {
                _cachedVersion = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用农历功能
    /// </summary>
    public bool EnableLunarCalendar
    {
        get => _enableLunarCalendar;
        set
        {
            if (_enableLunarCalendar != value)
            {
                _enableLunarCalendar = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用地方时功能
    /// </summary>
    public bool EnableLocalSolarTime
    {
        get => _enableLocalSolarTime;
        set
        {
            if (_enableLocalSolarTime != value)
            {
                _enableLocalSolarTime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用区时功能
    /// </summary>
    public bool EnableTimeZoneTime
    {
        get => _enableTimeZoneTime;
        set
        {
            if (_enableTimeZoneTime != value)
            {
                _enableTimeZoneTime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用星座功能
    /// </summary>
    public bool EnableXingZuo
    {
        get => _enableXingZuo;
        set
        {
            if (_enableXingZuo != value)
            {
                _enableXingZuo = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用节气功能
    /// </summary>
    public bool EnableJieQi
    {
        get => _enableJieQi;
        set
        {
            if (_enableJieQi != value)
            {
                _enableJieQi = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用宜忌功能
    /// </summary>
    public bool EnableDayYiJi
    {
        get => _enableDayYiJi;
        set
        {
            if (_enableDayYiJi != value)
            {
                _enableDayYiJi = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用生肖功能
    /// </summary>
    public bool EnableShengXiao
    {
        get => _enableShengXiao;
        set
        {
            if (_enableShengXiao != value)
            {
                _enableShengXiao = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用节日功能
    /// </summary>
    public bool EnableFestival
    {
        get => _enableFestival;
        set
        {
            if (_enableFestival != value)
            {
                _enableFestival = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已安装农历组件
    /// </summary>
    public bool IsLunarInstalled
    {
        get => _isLunarInstalled;
        set
        {
            if (_isLunarInstalled != value)
            {
                _isLunarInstalled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 时间偏移（秒），与ClassIsland时间独立
    /// 增大偏移抵消铃声滞后，减小偏移抵消铃声提前
    /// </summary>
    public double TimeOffsetSeconds
    {
        get => _timeOffsetSeconds;
        set
        {
            if (Math.Abs(_timeOffsetSeconds - value) > 0.001)
            {
                _timeOffsetSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 经度（用于地方时计算）
    /// </summary>
    public double Longitude
    {
        get => _longitude;
        set
        {
            if (Math.Abs(_longitude - value) > 0.0001)
            {
                _longitude = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 时区ID
    /// </summary>
    public string TimeZoneId
    {
        get => _timeZoneId;
        set
        {
            if (_timeZoneId != value)
            {
                _timeZoneId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用倒计时通知
    /// </summary>
    public bool EnableCountdownNotification
    {
        get => _enableCountdownNotification;
        set
        {
            if (_enableCountdownNotification != value)
            {
                _enableCountdownNotification = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 倒计时提醒时间（秒）
    /// </summary>
    public int CountdownAlertSeconds
    {
        get => _countdownAlertSeconds;
        set
        {
            if (_countdownAlertSeconds != value)
            {
                _countdownAlertSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 经度显示方式（小数/度分秒）
    /// </summary>
    public LongitudeDisplayMode LongitudeDisplayMode
    {
        get => _longitudeDisplayMode;
        set
        {
            if (_longitudeDisplayMode != value)
            {
                _longitudeDisplayMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用女装彩蛋
    /// </summary>
    public bool EnableEasterEgg
    {
        get => _enableEasterEgg;
        set
        {
            if (_enableEasterEgg != value)
            {
                _enableEasterEgg = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已接受免责声明
    /// </summary>
    public bool DisclaimerAccepted
    {
        get => _disclaimerAccepted;
        set
        {
            if (_disclaimerAccepted != value)
            {
                _disclaimerAccepted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已接受女装彩蛋免责声明
    /// </summary>
    public bool EasterEggDisclaimerAccepted
    {
        get => _easterEggDisclaimerAccepted;
        set
        {
            if (_easterEggDisclaimerAccepted != value)
            {
                _easterEggDisclaimerAccepted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否已接受女装彩蛋关闭方式信息
    /// </summary>
    public bool EasterEggInfoAccepted
    {
        get => _easterEggInfoAccepted;
        set
        {
            if (_easterEggInfoAccepted != value)
            {
                _easterEggInfoAccepted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// NTP时间服务器地址
    /// </summary>
    public string NtpServer
    {
        get => _ntpServer;
        set
        {
            if (_ntpServer != value)
            {
                _ntpServer = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// NTP同步周期（分钟）
    /// </summary>
    public int NtpSyncIntervalMinutes
    {
        get => _ntpSyncIntervalMinutes;
        set
        {
            if (_ntpSyncIntervalMinutes != value)
            {
                _ntpSyncIntervalMinutes = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 上次同步时间
    /// </summary>
    public DateTime? LastSyncTime
    {
        get => _lastSyncTime;
        set
        {
            if (_lastSyncTime != value)
            {
                _lastSyncTime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 上次同步状态（Success/Failed）
    /// </summary>
    public string? LastSyncStatus
    {
        get => _lastSyncStatus;
        set
        {
            if (_lastSyncStatus != value)
            {
                _lastSyncStatus = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 上次同步来源（NTP服务器地址或ClassIsland）
    /// </summary>
    public string? LastSyncSource
    {
        get => _lastSyncSource;
        set
        {
            if (_lastSyncSource != value)
            {
                _lastSyncSource = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用实验性功能
    /// </summary>
    public bool EnableExperimentalFeatures
    {
        get => _enableExperimentalFeatures;
        set
        {
            if (_enableExperimentalFeatures != value)
            {
                _enableExperimentalFeatures = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用悬浮时间表
    /// </summary>
    public bool EnableFloatingSchedule
    {
        get => _enableFloatingSchedule;
        set
        {
            if (_enableFloatingSchedule != value)
            {
                _enableFloatingSchedule = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮时间表窗口层级（置底/置顶）
    /// </summary>
    public FloatingScheduleWindowLayer FloatingScheduleWindowLayer
    {
        get => _floatingScheduleWindowLayer;
        set
        {
            if (_floatingScheduleWindowLayer != value)
            {
                _floatingScheduleWindowLayer = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮时间表窗口位置X
    /// </summary>
    public int FloatingSchedulePositionX
    {
        get => _floatingSchedulePositionX;
        set
        {
            if (_floatingSchedulePositionX != value)
            {
                _floatingSchedulePositionX = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮时间表窗口位置Y
    /// </summary>
    public int FloatingSchedulePositionY
    {
        get => _floatingSchedulePositionY;
        set
        {
            if (_floatingSchedulePositionY != value)
            {
                _floatingSchedulePositionY = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮背景不透明度（0完全透明 ~ 1完全不透明；仅作用卡片背景，不影响文字与进度条透明度）
    /// </summary>
    public double FloatingScheduleOpacity
    {
        get => _floatingScheduleOpacity;
        set
        {
            if (Math.Abs(_floatingScheduleOpacity - value) > 0.001)
            {
                _floatingScheduleOpacity = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮时间表课程名字号（单位 pt，范围 8~32，默认 18；其他文字字号会按相对差值自动计算）
    /// </summary>
    public double FloatingScheduleFontScale
    {
        get => _floatingScheduleFontScale;
        set
        {
            if (Math.Abs(_floatingScheduleFontScale - value) > 0.001)
            {
                _floatingScheduleFontScale = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 启用教师全名（不推荐，默认false）：关闭显示"X老师"，开启直接显示 TeacherName 全名（例：张三 而非 张老师）。
    /// 当科目 TeacherName 为空/空白时，不显示教师名。
    /// </summary>
    public bool FloatingScheduleEnableFullTeacherName
    {
        get => _floatingScheduleEnableFullTeacherName;
        set
        {
            if (_floatingScheduleEnableFullTeacherName != value)
            {
                _floatingScheduleEnableFullTeacherName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮窗是否显示教师名（默认 true）。关闭后悬浮窗不显示任何教师信息，
    /// 此时 <see cref="FloatingScheduleEnableFullTeacherName"/> 不生效（保存值保留，重新开启后恢复）。
    /// </summary>
    public bool FloatingScheduleShowTeacher
    {
        get => _floatingScheduleShowTeacher;
        set
        {
            if (_floatingScheduleShowTeacher != value)
            {
                _floatingScheduleShowTeacher = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 贴边自动隐藏：开启后，悬浮时间表被拖到（或初始位于）贴近屏幕边缘（&lt;8px）时，
    /// 会沿该边滑出屏幕、只保留约 6px 可见条；鼠标指针移入可见条区域时滑回原位，
    /// 指针离开后再次滑回隐藏。关闭时恢复贴边前位置并停止该逻辑。
    /// </summary>
    public bool FloatingScheduleEdgeHide
    {
        get => _floatingScheduleEdgeHide;
        set
        {
            if (_floatingScheduleEdgeHide != value)
            {
                _floatingScheduleEdgeHide = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 贴边隐藏延迟时间（秒）：判定窗口贴边且光标离开后，等待本时长再滑出隐藏。
    /// 范围 0 ~ 60，步长 1，精确到 0.1，默认 3 秒。0 表示立即隐藏。仅在 FloatingScheduleEdgeHide 开启时生效。
    /// </summary>
    public double FloatingScheduleEdgeHideDelay
    {
        get => _floatingScheduleEdgeHideDelay;
        set
        {
            var v = Math.Clamp(value, 0.0, 60.0);
            if (Math.Abs(_floatingScheduleEdgeHideDelay - v) > 0.001)
            {
                _floatingScheduleEdgeHideDelay = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮窗启用"点击穿透"：开启后鼠标事件会直接穿透到下方窗口，同时关闭悬浮窗拖拽交互。
    /// 通过 GWL_EXSTYLE 加 WS_EX_TRANSPARENT | WS_EX_LAYERED 实现。
    /// </summary>
    public bool FloatingScheduleClickThrough
    {
        get => _floatingScheduleClickThrough;
        set
        {
            if (_floatingScheduleClickThrough != value)
            {
                _floatingScheduleClickThrough = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮窗启用"指针移入淡化"：参考 ClassIsland 主窗体淡化实现，开启后指针位于悬浮窗区域时（与 Reverse 取异或）
    /// 容器 Opacity 降到 0.05，离开后恢复为用户设置的 FloatingScheduleOpacity。
    /// </summary>
    public bool FloatingScheduleHoverFade
    {
        get => _floatingScheduleHoverFade;
        set
        {
            if (_floatingScheduleHoverFade != value)
            {
                _floatingScheduleHoverFade = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮窗"指针移入淡化"反转：开启后变为"指针在窗口外时淡化，移入时恢复不透明"。
    /// 与 ClassIsland Settings.IsMouseInFadingReversed 同义。
    /// </summary>
    public bool FloatingScheduleHoverFadeReverse
    {
        get => _floatingScheduleHoverFadeReverse;
        set
        {
            if (_floatingScheduleHoverFadeReverse != value)
            {
                _floatingScheduleHoverFadeReverse = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮窗层级重设频率（窗口层级变化时 / 前台窗口变化时 / 每 50ms / 每 1ms）。
    /// 参考 ClassIsland 原生 WindowTopmostRecheckMode 4 档索引，用于防止其他置顶窗口盖住悬浮窗。
    /// </summary>
    public FloatingTopmostRefreshMode FloatingScheduleTopmostRefreshMode
    {
        get => _floatingScheduleTopmostRefreshMode;
        set
        {
            if (_floatingScheduleTopmostRefreshMode != value)
            {
                _floatingScheduleTopmostRefreshMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 悬浮窗隐藏模式（跟随 ClassIsland 主界面隐藏规则[默认] / 基础模式 / 高级模式·规则集 / 从不隐藏）。
    /// 基础与高级模式均复用 ClassIsland 主界面的隐藏开关与隐藏规则集，插件端不单独配置条件。
    /// </summary>
    public FloatingScheduleHideMode FloatingScheduleHideMode
    {
        get => _floatingScheduleHideMode;
        set
        {
            if (_floatingScheduleHideMode != value)
            {
                _floatingScheduleHideMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 随机窗口名（默认 false）：开启后悬浮窗窗口标题改为随机字符串，
    /// 防止学校弹窗拦截工具按标题识别并拦截时间表悬浮窗。
    /// </summary>
    public bool FloatingScheduleRandomTitle
    {
        get => _floatingScheduleRandomTitle;
        set
        {
            if (_floatingScheduleRandomTitle != value)
            {
                _floatingScheduleRandomTitle = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 增强随机模式（默认 false，仅在 <see cref="FloatingScheduleRandomTitle"/> 开启时生效）：
    /// 开启后每 1 秒重新设置一次随机窗口标题。
    /// </summary>
    public bool FloatingScheduleRandomTitleEnhanced
    {
        get => _floatingScheduleRandomTitleEnhanced;
        set
        {
            if (_floatingScheduleRandomTitleEnhanced != value)
            {
                _floatingScheduleRandomTitleEnhanced = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 防止截图（默认 false）：开启后其他应用（截屏/录屏工具）无法捕获悬浮窗内容，
    /// 捕获结果中悬浮窗不显示、透出下方内容（Win32 SetWindowDisplayAffinity WDA_EXCLUDEFROMCAPTURE；
    /// 低于 Win10 2004 回退 WDA_MONITOR——捕获中悬浮窗区域显示为黑色）。
    /// </summary>
    public bool FloatingSchedulePreventCapture
    {
        get => _floatingSchedulePreventCapture;
        set
        {
            if (_floatingSchedulePreventCapture != value)
            {
                _floatingSchedulePreventCapture = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}



