﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System.ComponentModel;
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
    /// 0 - 窗口层级变化时（默认）：仅在 Win32 WM_WINDOWPOSCHANGED &amp; !SWP_NOZORDER 时重新设置
    ///        （非 Windows 平台退化为 OnForegroundWindowChanged）。
    /// </summary>
    OnWindowZOrderChanged = 0,
    /// <summary>
    /// 1 - 前台窗口变化时：IWindowPlatformService.ForegroundWindowChanged 事件触发。
    /// </summary>
    OnForegroundWindowChanged = 1,
    /// <summary>
    /// 2 - 每 50ms：DispatcherTimer 高频周期性重设。
    /// </summary>
    Every50Ms = 2,
    /// <summary>
    /// 3 - 每 1ms：DispatcherTimer 极高频率周期性重设（注意性能占用 / 闪烁风险）。
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
/// 悬浮窗"显示明天课表"模式（参考 ClassIsland 课程表组件的 TomorrowScheduleShowMode 四档索引语义）。
/// </summary>
public enum FloatingScheduleTomorrowShowMode
{
    /// <summary>
    /// 0 - 不显示：悬浮窗始终显示当天课表。
    /// </summary>
    Never = 0,

    /// <summary>
    /// 1 - 放学后显示（默认，与 ClassIsland 一致）：当天课程结束（或当天课表未加载）后切换为明天课表。
    /// </summary>
    AfterSchool = 1,

    /// <summary>
    /// 2 - 总是显示：悬浮窗始终显示明天课表。
    /// </summary>
    Always = 2,

    /// <summary>
    /// 3 - 无展示课程时显示：当天没有可展示课程时切换为明天课表。
    /// </summary>
    OnEmpty = 3
}

/// <summary>悬浮窗"高级外观"窗口材质类型。</summary>
public enum FloatingScheduleMaterialType
{
    /// <summary>0 - 半透明（默认）：主题卡片色 × 不透明度纯色。</summary>
    SemiTransparent = 0,
    /// <summary>1 - 磨砂：ExperimentalAcrylicBorder 实时背景模糊。</summary>
    Blur = 1,
    /// <summary>2 - 亚克力：高透明 tint + 噪点。</summary>
    Acrylic = 2,
    /// <summary>3 - 液态玻璃（Beta，实验性）。</summary>
    LiquidGlass = 3,
    /// <summary>4 - 古卷（Alpha，实验性）：竹简样式。</summary>
    AncientScroll = 4,
    /// <summary>5 - 自定义图片（实验性）。</summary>
    CustomImage = 5
}

/// <summary>悬浮窗"高级外观"装饰边框预设。</summary>
public enum FloatingScheduleBorderPreset
{
    PictureFrame = 0,   // 画框
    Blackboard = 1,     // 黑板
    Windows7 = 2,
    WindowsXP = 3,
    Windows10 = 4,
    Windows11 = 5,
    HarmonyOS = 6
}

/// <summary>悬浮窗"高级外观"表格布局方向。</summary>
public enum FloatingScheduleLayoutDir
{
    /// <summary>0 - 纵向排列（默认，现状）。</summary>
    Vertical = 0,
    /// <summary>1 - 横向排列（从左往右）：每列一节课，第1节最左。</summary>
    HorizontalLeftToRight = 1,
    /// <summary>2 - 横向排列（从右往左）：每列一节课，第1节最右（推荐古卷材质）。</summary>
    HorizontalRightToLeft = 2
}

/// <summary>悬浮窗"高级外观"列间距模式。</summary>
public enum FloatingScheduleColumnSpacingMode
{
    /// <summary>0 - 自动适配（默认，现状 ColumnSpacing=14）。</summary>
    Auto = 0,
    /// <summary>1 - 自定义（50~1000px）。</summary>
    Custom = 1
}

/// <summary>悬浮窗"高级外观"主题色模式（4 档）。</summary>
public enum FloatingScheduleThemeMode
{
    /// <summary>0 - 浅色（强制）。</summary>
    Light = 0,
    /// <summary>1 - 深色（强制）。</summary>
    Dark = 1,
    /// <summary>2 - 跟随 ClassIsland（默认）。</summary>
    FollowClassIsland = 2,
    /// <summary>3 - 跟随系统。</summary>
    FollowSystem = 3
}

/// <summary>悬浮窗"液态玻璃"背景捕获方式（2 档）。</summary>
public enum FloatingScheduleGlassCaptureMode
{
    /// <summary>0 - DXGI Desktop Duplication（默认，推荐）：帧到达即取，非阻塞、亚毫秒。</summary>
    Dxgi = 0,
    /// <summary>1 - GDI BitBlt（回退）：单次固定 ~24ms 同步阻塞，仅在 DXGI 不可用时使用。</summary>
    GdiBitBlt = 1
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
    // ===== 「管理启用的功能」新增开关 =====
    private bool _enableFpsChart = false;      // 帧率折线图及其分析页面（默认关闭）
    private bool _enableHanfuGuide = true;     // 汉服指南（170+ 汉服页面 + Markdown 下载，默认开启）
    private bool _enableUtilities = true;      // 小工具（时间格式转换 / 时间计算器 / 专业名词解释，默认开启）
    private string? _cachedVersion;
    private bool _enableFloatingSchedule = true;
    private FloatingScheduleWindowLayer _floatingScheduleWindowLayer = FloatingScheduleWindowLayer.Bottom;
    private int _floatingSchedulePositionX = 100;
    private int _floatingSchedulePositionY = 100;
    private bool _floatingScheduleClickThrough = false;
    private bool _floatingScheduleHoverFade = false;
    private bool _floatingScheduleHoverFadeReverse = false;
    private double _floatingScheduleOpacity = 0.5;
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
    private FloatingScheduleTomorrowShowMode _floatingScheduleTomorrowShowMode = FloatingScheduleTomorrowShowMode.AfterSchool;
    private string _floatingScheduleTomorrowPlaceholderText = "明天没有课程";
    private string _floatingScheduleTodayPlaceholderText = "今天没有课程";
    // ===== 独立进程模式（时间表悬浮窗由 AdvancedTimeIslandFloatSchedule.exe 渲染；仅 Windows）=====
    private bool _floatingScheduleIndependentProcess = false;
    private bool _floatingScheduleRandomProcessName = false;
    private bool _floatingScheduleSingleInstanceProtection = true;
    private bool _floatingScheduleFollowHostLifetime = true;
    // 上次"独立程序已更新"提示时记录的子进程 exe 内容哈希（内部标记：保证同一版本只提示一次）
    private string _floatingScheduleChildExeHash = "";
    // ===== 高级外观（独立编辑器窗口；材质/边框/文本/学科配色/表格/主题/调试）=====
    // 窗口
    private FloatingScheduleMaterialType _floatingScheduleMaterial = FloatingScheduleMaterialType.SemiTransparent;
    private bool _floatingScheduleJellyEnabled = false;
    private double _floatingScheduleCornerRadius = 8.0;      // 用户决议 Q-A：默认 8px，保持现状观感
    private double _floatingScheduleMaterialIntensity = 0.5; // 效果强度 0~1（磨砂/亚克力模糊度、液态玻璃折射度）
    private double _floatingScheduleGlassSampleRateHz = 30.0; // 液态玻璃真折射背景的采样率（Hz，1~60）
    private FloatingScheduleGlassCaptureMode _floatingScheduleGlassCaptureMode = FloatingScheduleGlassCaptureMode.Dxgi; // 液态玻璃背景捕获方式
    private bool _floatingScheduleGlassDragCompensation = true; // 液态玻璃拖动位移补偿（拖动期间背景跟手平移）
    private bool _floatingScheduleGlassAllowCapture = false;     // 允许截图（暂停液态玻璃背景采样；默认关）
    private string _floatingScheduleCustomBackgroundPath = "";
    // 边框
    private bool _floatingScheduleBorderEnabled = false;
    private FloatingScheduleBorderPreset _floatingScheduleBorderPreset = FloatingScheduleBorderPreset.PictureFrame;
    private bool _floatingScheduleBorderColorEnabled = false;
    private uint _floatingScheduleBorderColor = 0xFFB8860B;  // 画框暖金默认（AARRGGBB）
    // 文本
    private bool _floatingScheduleFontEnabled = false;
    private string? _floatingScheduleTextFontFamily = null;
    private bool _floatingScheduleFontWeightEnabled = false;
    private int _floatingScheduleTextFontWeight = 400;        // Normal
    private bool _floatingScheduleTextColorEnabled = false;
    private uint _floatingScheduleTextColor = 0xFFFFFFFF;
    // 学科配色
    private bool _floatingScheduleSubjectColorEnabled = true;
    private Dictionary<string, uint> _floatingScheduleSubjectColorMap = new();
    // 表格
    private FloatingScheduleLayoutDir _floatingScheduleLayoutDirection = FloatingScheduleLayoutDir.Vertical;
    private bool _floatingScheduleZebraStriped = false;       // 间色
    private double _floatingScheduleRowSpacingFactor = 1.0;   // 0.5~2.0
    private FloatingScheduleColumnSpacingMode _floatingScheduleColumnSpacingMode = FloatingScheduleColumnSpacingMode.Auto;
    private double _floatingScheduleColumnSpacing = 300.0;    // 50~1000
    // 四边距=卡片内边距（替换式）；默认 12/10 与现状卡片 Padding 一致，老用户零观感变化（用户决议）
    private double _floatingScheduleMarginTop = 10.0;
    private double _floatingScheduleMarginBottom = 10.0;
    private double _floatingScheduleMarginLeft = 12.0;
    private double _floatingScheduleMarginRight = 12.0;
    // 主题
    private FloatingScheduleThemeMode _floatingScheduleThemeMode = FloatingScheduleThemeMode.FollowClassIsland;
    private bool _floatingScheduleTableColorEnabled = false;
    private uint _floatingScheduleTableColor = 0xFF000000;    // 默认黑
    private bool _floatingScheduleAccentColorEnabled = false;
    private uint _floatingScheduleAccentColor = 0xFF0078D4;   // Win10 蓝
    private bool _floatingScheduleSeparatorColorEnabled = false;
    private uint _floatingScheduleSeparatorColor = 0xCCFFFFFF; // 白色 80% 不透明
    private double _floatingScheduleSeparatorThickness = 2.0;  // 0~10；默认 2.0=现状档案分隔线粗细（保持观感）
    // 调试
    private bool _floatingScheduleShowFps = false;

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
    /// 是否注册帧率折线图及其分析页面（「管理启用的功能」，默认关闭）
    /// </summary>
    public bool EnableFpsChart
    {
        get => _enableFpsChart;
        set
        {
            if (_enableFpsChart != value)
            {
                _enableFpsChart = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用汉服指南（「管理启用的功能」，默认开启）。
    /// 关闭后不再注册 170+ 汉服页面与「汉服指南」标签页，也不再从 AdvancedTimeIslandHanfu 下载 Markdown；
    /// 汉服页面模板（HanfuPageTemplate）始终注册。
    /// </summary>
    public bool EnableHanfuGuide
    {
        get => _enableHanfuGuide;
        set
        {
            if (_enableHanfuGuide != value)
            {
                _enableHanfuGuide = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否显示小工具（「管理启用的功能」，默认开启）。
    /// 关闭后主设置导航栏不再显示「时间格式转换」「时间计算器」「专业名词解释」三个标签页。
    /// </summary>
    public bool EnableUtilities
    {
        get => _enableUtilities;
        set
        {
            if (_enableUtilities != value)
            {
                _enableUtilities = value;
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
    /// 独立进程模式：时间表悬浮窗改由独立进程 AdvancedTimeIslandFloatSchedule.exe 渲染
    /// （防弹窗拦截"超级模式"，且不受 ClassIsland 主程序卡顿影响）。仅 Windows 支持，默认关闭。
    /// </summary>
    public bool FloatingScheduleIndependentProcess
    {
        get => _floatingScheduleIndependentProcess;
        set
        {
            if (_floatingScheduleIndependentProcess != value)
            {
                _floatingScheduleIndependentProcess = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 随机进程名：以随机命名的临时副本启动子进程（进一步规避按进程名拦截；可能触发杀软误报）。默认关闭。
    /// </summary>
    public bool FloatingScheduleRandomProcessName
    {
        get => _floatingScheduleRandomProcessName;
        set
        {
            if (_floatingScheduleRandomProcessName != value)
            {
                _floatingScheduleRandomProcessName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 单实例保护：子进程启动时获取全局 Mutex，已有实例则直接退出并由插件接管连接。默认开启。
    /// </summary>
    public bool FloatingScheduleSingleInstanceProtection
    {
        get => _floatingScheduleSingleInstanceProtection;
        set
        {
            if (_floatingScheduleSingleInstanceProtection != value)
            {
                _floatingScheduleSingleInstanceProtection = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 跟随启停：开启后 ClassIsland 关闭或异常停止后，此程序将关闭；
    /// 关闭后 ClassIsland 退出时悬浮窗冻结显示最后课表，ClassIsland 重启后自动重连恢复推送。默认开启。
    /// </summary>
    public bool FloatingScheduleFollowHostLifetime
    {
        get => _floatingScheduleFollowHostLifetime;
        set
        {
            if (_floatingScheduleFollowHostLifetime != value)
            {
                _floatingScheduleFollowHostLifetime = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 上次"独立程序已更新"提示时记录的子进程 exe 内容哈希（内部标记，非用户设置）。
    /// 用于判断插件升级后随包重新分发的 AdvancedTimeIslandFloatSchedule.exe 是否发生变化，
    /// 从而只在真正更新时发一次系统通知，而不是每次启动都提示。
    /// </summary>
    public string FloatingScheduleChildExeHash
    {
        get => _floatingScheduleChildExeHash;
        set
        {
            if (_floatingScheduleChildExeHash != value)
            {
                _floatingScheduleChildExeHash = value;
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
    /// 贴边自动隐藏：开启后，悬浮时间表被拖到（或初始位于）贴近屏幕边缘（<8px）时，
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
    /// 贴边隐藏延迟时间（秒）：判定窗口贴边后，等待本时长再滑出隐藏；光标离开可见条后同样延迟本时长再滑回隐藏。
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
    /// （通过 WS_EX_TRANSPARENT 系统扩展样式 / Avalonia InputElement.IsHitTestVisible=false 实现）。
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
    /// 悬浮窗"指针移入淡化"反转：开启后变为"指针在窗口外时淡化，移入窗口内恢复不透明"。
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
    /// 捕获结果中悬浮窗不显示、透出下方内容（Win32 SetWindowDisplayAffinity WDA_EXCLUDEFROMCAPTURE，
    /// 需 Windows 10 2004+；低于 2004 回退 WDA_MONITOR——捕获中悬浮窗区域显示为黑色）。
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

    /// <summary>
    /// 显示明天课表模式（不显示 / 放学后显示[默认] / 总是显示 / 无展示课程时显示）。
    /// 参考 ClassIsland 课程表组件的 TomorrowScheduleShowMode：放学后显示 = 当天状态为放学后或当天课表未加载时，
    /// 悬浮窗改为展示明天课表；若明天无课程则显示"明天无课程时的占位符"。
    /// </summary>
    public FloatingScheduleTomorrowShowMode FloatingScheduleTomorrowShowMode
    {
        get => _floatingScheduleTomorrowShowMode;
        set
        {
            if (_floatingScheduleTomorrowShowMode != value)
            {
                _floatingScheduleTomorrowShowMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 显示明天课表且明天没有课程时的占位符文案（默认为"明天没有课程"）。
    /// </summary>
    public string FloatingScheduleTomorrowPlaceholderText
    {
        get => _floatingScheduleTomorrowPlaceholderText;
        set
        {
            if (_floatingScheduleTomorrowPlaceholderText != value)
            {
                _floatingScheduleTomorrowPlaceholderText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 显示当天课表但当天没有课程时的占位符文案（默认为"今天没有课程"）。
    /// </summary>
    public string FloatingScheduleTodayPlaceholderText
    {
        get => _floatingScheduleTodayPlaceholderText;
        set
        {
            if (_floatingScheduleTodayPlaceholderText != value)
            {
                _floatingScheduleTodayPlaceholderText = value;
                OnPropertyChanged();
            }
        }
    }

    // ==================== 高级外观：窗口 ====================

    /// <summary>窗口材质类型（半透明/磨砂/亚克力/液态玻璃/古卷/自定义图片）。</summary>
    public FloatingScheduleMaterialType FloatingScheduleMaterial
    {
        get => _floatingScheduleMaterial;
        set
        {
            if (_floatingScheduleMaterial != value)
            {
                _floatingScheduleMaterial = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>果冻效果（仅液态玻璃材质可用，拖动时 Q 弹形变）。默认关。</summary>
    public bool FloatingScheduleJellyEnabled
    {
        get => _floatingScheduleJellyEnabled;
        set
        {
            if (_floatingScheduleJellyEnabled != value)
            {
                _floatingScheduleJellyEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>圆角程度（0~10px，默认 8；仅半透明/磨砂/亚克力/液态玻璃生效，0=方角）。</summary>
    public double FloatingScheduleCornerRadius
    {
        get => _floatingScheduleCornerRadius;
        set
        {
            var v = Math.Clamp(value, 0, 10);
            if (Math.Abs(_floatingScheduleCornerRadius - v) > 0.001)
            {
                _floatingScheduleCornerRadius = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>效果强度（0~1，默认 0.5；磨砂/亚克力模糊度、液态玻璃折射度）。</summary>
    public double FloatingScheduleMaterialIntensity
    {
        get => _floatingScheduleMaterialIntensity;
        set
        {
            var v = Math.Clamp(value, 0, 1);
            if (Math.Abs(_floatingScheduleMaterialIntensity - v) > 0.001)
            {
                _floatingScheduleMaterialIntensity = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>【液态玻璃】真折射背景的采样率（Hz，1~60，默认 30）：越高越跟手，抓屏 + 折射的 CPU 开销随之线性上升。</summary>
    public double FloatingScheduleGlassSampleRateHz
    {
        get => _floatingScheduleGlassSampleRateHz;
        set
        {
            var v = Math.Clamp(value, 1, 60);
            if (Math.Abs(_floatingScheduleGlassSampleRateHz - v) > 0.001)
            {
                _floatingScheduleGlassSampleRateHz = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>【液态玻璃】背景捕获方式：DXGI Desktop Duplication（默认，推荐，亚毫秒非阻塞）/ GDI BitBlt（回退，单次固定 ~24ms）。
    /// 选 DXGI 时若运行期不可用（被 OBS 等独占 / 初始化失败）会自动回退 GDI 并每 10 秒重试恢复。</summary>
    public FloatingScheduleGlassCaptureMode FloatingScheduleGlassCaptureMode
    {
        get => _floatingScheduleGlassCaptureMode;
        set
        {
            var v = value == FloatingScheduleGlassCaptureMode.GdiBitBlt
                ? FloatingScheduleGlassCaptureMode.GdiBitBlt
                : FloatingScheduleGlassCaptureMode.Dxgi;   // 非法值收敛到默认（枚举以 int 反序列化，需防脏数据）
            if (_floatingScheduleGlassCaptureMode != v)
            {
                _floatingScheduleGlassCaptureMode = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>【液态玻璃】拖动位移补偿（默认开）：拖动窗口期间不再抓帧，改用已抓到的"外扩一圈"的缓冲按窗口位移
    /// 偏移取材并重做折射（~5ms），使拖动期间背景 60fps 跟手；拖出外扩范围或松手后由采样定时器重抓。</summary>
    public bool FloatingScheduleGlassDragCompensation
    {
        get => _floatingScheduleGlassDragCompensation;
        set
        {
            if (_floatingScheduleGlassDragCompensation != value)
            {
                _floatingScheduleGlassDragCompensation = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>【液态玻璃】允许截图（默认关）：开启后暂停液态玻璃的背景采样与重烘焙（背景冻结在当前已折射的那一帧），
    /// 并解除采样期强制的"排除捕获"（WDA_EXCLUDEFROMCAPTURE）——窗口捕获亲和性改为只按用户自己的"防截图"设置，
    /// 用户的防截图关时窗口可出现在截图/录屏里。关闭（默认）时恢复：采样运行 + 采样期强制排除捕获（防止自采样残影）。</summary>
    public bool FloatingScheduleGlassAllowCapture
    {
        get => _floatingScheduleGlassAllowCapture;
        set
        {
            if (_floatingScheduleGlassAllowCapture != value)
            {
                _floatingScheduleGlassAllowCapture = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>自定义图片背景路径（材质=自定义图片时使用；无效路径回退半透明）。</summary>
    public string FloatingScheduleCustomBackgroundPath
    {
        get => _floatingScheduleCustomBackgroundPath;
        set
        {
            if (_floatingScheduleCustomBackgroundPath != value)
            {
                _floatingScheduleCustomBackgroundPath = value;
                OnPropertyChanged();
            }
        }
    }

    // ==================== 高级外观：边框 ====================

    /// <summary>窗口装饰边框开关（液态玻璃/古卷材质下不可用）。默认关。</summary>
    public bool FloatingScheduleBorderEnabled
    {
        get => _floatingScheduleBorderEnabled;
        set
        {
            if (_floatingScheduleBorderEnabled != value)
            {
                _floatingScheduleBorderEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>边框预设样式（画框/黑板/Win7/XP/Win10/Win11/HarmonyOS）。</summary>
    public FloatingScheduleBorderPreset FloatingScheduleBorderPreset
    {
        get => _floatingScheduleBorderPreset;
        set
        {
            if (_floatingScheduleBorderPreset != value)
            {
                _floatingScheduleBorderPreset = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>边框自定义颜色开关（关=用预设内置主色）。</summary>
    public bool FloatingScheduleBorderColorEnabled
    {
        get => _floatingScheduleBorderColorEnabled;
        set
        {
            if (_floatingScheduleBorderColorEnabled != value)
            {
                _floatingScheduleBorderColorEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>边框自定义颜色（AARRGGBB）。</summary>
    public uint FloatingScheduleBorderColor
    {
        get => _floatingScheduleBorderColor;
        set
        {
            if (_floatingScheduleBorderColor != value)
            {
                _floatingScheduleBorderColor = value;
                OnPropertyChanged();
            }
        }
    }

    // ==================== 高级外观：文本 ====================

    /// <summary>自定义字体开关（关=跟随应用主题字体）。</summary>
    public bool FloatingScheduleFontEnabled
    {
        get => _floatingScheduleFontEnabled;
        set
        {
            if (_floatingScheduleFontEnabled != value)
            {
                _floatingScheduleFontEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>自定义字体族名（null=跟随应用；经 FontFamilySource 双模式下发）。</summary>
    public string? FloatingScheduleTextFontFamily
    {
        get => _floatingScheduleTextFontFamily;
        set
        {
            if (_floatingScheduleTextFontFamily != value)
            {
                _floatingScheduleTextFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>自定义字重开关（关=现状：当前课加粗、其余常规）。</summary>
    public bool FloatingScheduleFontWeightEnabled
    {
        get => _floatingScheduleFontWeightEnabled;
        set
        {
            if (_floatingScheduleFontWeightEnabled != value)
            {
                _floatingScheduleFontWeightEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>自定义字重数值（100~900，默认 400 Normal）。</summary>
    public int FloatingScheduleTextFontWeight
    {
        get => _floatingScheduleTextFontWeight;
        set
        {
            var v = Math.Clamp(value, 100, 900);
            if (_floatingScheduleTextFontWeight != v)
            {
                _floatingScheduleTextFontWeight = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>全局字体颜色自定义开关（关=随主题）。</summary>
    public bool FloatingScheduleTextColorEnabled
    {
        get => _floatingScheduleTextColorEnabled;
        set
        {
            if (_floatingScheduleTextColorEnabled != value)
            {
                _floatingScheduleTextColorEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>全局字体颜色（AARRGGBB；次级文字由渲染器按同色降透明度派生）。</summary>
    public uint FloatingScheduleTextColor
    {
        get => _floatingScheduleTextColor;
        set
        {
            if (_floatingScheduleTextColor != value)
            {
                _floatingScheduleTextColor = value;
                OnPropertyChanged();
            }
        }
    }

    // ==================== 高级外观：学科配色 ====================

    /// <summary>按学科配色开关（默认开；关=全部用全局字体颜色/主题色）。</summary>
    public bool FloatingScheduleSubjectColorEnabled
    {
        get => _floatingScheduleSubjectColorEnabled;
        set
        {
            if (_floatingScheduleSubjectColorEnabled != value)
            {
                _floatingScheduleSubjectColorEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>学科配色映射（科目名 → AARRGGBB）。System.Text.Json 直接序列化。</summary>
    public Dictionary<string, uint> FloatingScheduleSubjectColorMap
    {
        get => _floatingScheduleSubjectColorMap;
        set
        {
            _floatingScheduleSubjectColorMap = value ?? new Dictionary<string, uint>();
            OnPropertyChanged();
        }
    }

    // ==================== 高级外观：表格 ====================

    /// <summary>表格布局方向（纵向/横向左起/横向右起）。</summary>
    public FloatingScheduleLayoutDir FloatingScheduleLayoutDirection
    {
        get => _floatingScheduleLayoutDirection;
        set
        {
            if (_floatingScheduleLayoutDirection != value)
            {
                _floatingScheduleLayoutDirection = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>间色（斑马底）开关：奇数行原底色、偶数行加深底色。默认关。</summary>
    public bool FloatingScheduleZebraStriped
    {
        get => _floatingScheduleZebraStriped;
        set
        {
            if (_floatingScheduleZebraStriped != value)
            {
                _floatingScheduleZebraStriped = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>行间距倍率（0.5~2.0，默认 1.0；乘现状行上下 Margin）。</summary>
    public double FloatingScheduleRowSpacingFactor
    {
        get => _floatingScheduleRowSpacingFactor;
        set
        {
            var v = Math.Clamp(value, 0.5, 2.0);
            if (Math.Abs(_floatingScheduleRowSpacingFactor - v) > 0.001)
            {
                _floatingScheduleRowSpacingFactor = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>列间距模式（自动适配/自定义）。</summary>
    public FloatingScheduleColumnSpacingMode FloatingScheduleColumnSpacingMode
    {
        get => _floatingScheduleColumnSpacingMode;
        set
        {
            if (_floatingScheduleColumnSpacingMode != value)
            {
                _floatingScheduleColumnSpacingMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>自定义列间距（50~1000px，默认 300；仅模式=自定义时生效）。</summary>
    public double FloatingScheduleColumnSpacing
    {
        get => _floatingScheduleColumnSpacing;
        set
        {
            var v = Math.Clamp(value, 50, 1000);
            if (Math.Abs(_floatingScheduleColumnSpacing - v) > 0.001)
            {
                _floatingScheduleColumnSpacing = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>表格内容上边距（0~50px，默认 6）。</summary>
    public double FloatingScheduleMarginTop
    {
        get => _floatingScheduleMarginTop;
        set
        {
            var v = Math.Clamp(value, 0, 50);
            if (Math.Abs(_floatingScheduleMarginTop - v) > 0.001)
            {
                _floatingScheduleMarginTop = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>表格内容下边距（0~50px，默认 6）。</summary>
    public double FloatingScheduleMarginBottom
    {
        get => _floatingScheduleMarginBottom;
        set
        {
            var v = Math.Clamp(value, 0, 50);
            if (Math.Abs(_floatingScheduleMarginBottom - v) > 0.001)
            {
                _floatingScheduleMarginBottom = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>表格内容左边距（0~50px，默认 6）。</summary>
    public double FloatingScheduleMarginLeft
    {
        get => _floatingScheduleMarginLeft;
        set
        {
            var v = Math.Clamp(value, 0, 50);
            if (Math.Abs(_floatingScheduleMarginLeft - v) > 0.001)
            {
                _floatingScheduleMarginLeft = v;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>表格内容右边距（0~50px，默认 6）。</summary>
    public double FloatingScheduleMarginRight
    {
        get => _floatingScheduleMarginRight;
        set
        {
            var v = Math.Clamp(value, 0, 50);
            if (Math.Abs(_floatingScheduleMarginRight - v) > 0.001)
            {
                _floatingScheduleMarginRight = v;
                OnPropertyChanged();
            }
        }
    }

    // ==================== 高级外观：主题 ====================

    /// <summary>主题色模式（浅色/深色/跟随 ClassIsland/跟随系统）。</summary>
    public FloatingScheduleThemeMode FloatingScheduleThemeMode
    {
        get => _floatingScheduleThemeMode;
        set
        {
            if (_floatingScheduleThemeMode != value)
            {
                _floatingScheduleThemeMode = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>表格底色自定义开关（关=随材质/主题卡片色）。</summary>
    public bool FloatingScheduleTableColorEnabled
    {
        get => _floatingScheduleTableColorEnabled;
        set
        {
            if (_floatingScheduleTableColorEnabled != value)
            {
                _floatingScheduleTableColorEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>表格底色（AARRGGBB，默认黑）。</summary>
    public uint FloatingScheduleTableColor
    {
        get => _floatingScheduleTableColor;
        set
        {
            if (_floatingScheduleTableColor != value)
            {
                _floatingScheduleTableColor = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>强调色自定义开关（关=跟随 ClassIsland 强调色）。</summary>
    public bool FloatingScheduleAccentColorEnabled
    {
        get => _floatingScheduleAccentColorEnabled;
        set
        {
            if (_floatingScheduleAccentColorEnabled != value)
            {
                _floatingScheduleAccentColorEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>强调色（AARRGGBB，默认 Win10 蓝 #0078D4）。</summary>
    public uint FloatingScheduleAccentColor
    {
        get => _floatingScheduleAccentColor;
        set
        {
            if (_floatingScheduleAccentColor != value)
            {
                _floatingScheduleAccentColor = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>分隔线颜色自定义开关（关=随主题深白/浅黑）。</summary>
    public bool FloatingScheduleSeparatorColorEnabled
    {
        get => _floatingScheduleSeparatorColorEnabled;
        set
        {
            if (_floatingScheduleSeparatorColorEnabled != value)
            {
                _floatingScheduleSeparatorColorEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>分隔线颜色（AARRGGBB；A 通道即分隔线不透明度，默认白 80%）。</summary>
    public uint FloatingScheduleSeparatorColor
    {
        get => _floatingScheduleSeparatorColor;
        set
        {
            if (_floatingScheduleSeparatorColor != value)
            {
                _floatingScheduleSeparatorColor = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>分隔线粗细（0~10px，默认 3.0；0=禁用档案分隔线，古卷材质映射竹缝外缘距）。</summary>
    public double FloatingScheduleSeparatorThickness
    {
        get => _floatingScheduleSeparatorThickness;
        set
        {
            var v = Math.Clamp(value, 0, 10);
            if (Math.Abs(_floatingScheduleSeparatorThickness - v) > 0.001)
            {
                _floatingScheduleSeparatorThickness = v;
                OnPropertyChanged();
            }
        }
    }

    // ==================== 高级外观：调试 ====================

    /// <summary>显示 FPS 角标（左=渲染 FPS，右=无等待 FPS）。默认关。</summary>
    public bool FloatingScheduleShowFps
    {
        get => _floatingScheduleShowFps;
        set
        {
            if (_floatingScheduleShowFps != value)
            {
                _floatingScheduleShowFps = value;
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



