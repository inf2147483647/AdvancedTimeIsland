using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Controls;

/// <summary>
/// 「女装」图片版权注意事项的整页覆盖层，供「主设置 → 女装」标签页（<see cref="Views.Settings.EasterEggPage"/>）
/// 与「调试 → 女装」页（<see cref="Views.Settings.WomenswearPage"/>）共用。
/// 两处共用同一份设置标记 <see cref="PluginSettings.WomenswearNoticeAccepted"/>——任一入口同意后，
/// 另一入口也不再展示；点击「同意并查看」会立即原子落盘并触发 <see cref="Agreed"/>，由宿主页面装配内容。
/// 外观对齐宿主「插件 / 自动化使用须知」：ATI 图标 + Fluent 加号 + Fluent 插件图标（&#xE4DA;）、
/// 标题、警示色说明块与强调色同意按钮，颜色随深浅主题自适应。
/// </summary>
public partial class WomenswearNoticeOverlay : UserControl
{
    private PluginSettings? _settings;

    /// <summary>用户点击「同意并查看」后触发（此时设置已写入并落盘、覆盖层已隐藏）。</summary>
    public event EventHandler? Agreed;

    public WomenswearNoticeOverlay()
    {
        InitializeComponent();

        AppIcon.Source = PluginIconHelper.LoadIconBitmap();
        PlusIcon.FontFamily = ClassIsland.Core.AppBase.FluentIconsFontFamily;
        PlugIcon.FontFamily = ClassIsland.Core.AppBase.FluentIconsFontFamily;
        WarnIcon.FontFamily = ClassIsland.Core.AppBase.FluentIconsFontFamily;
        CheckIcon.FontFamily = ClassIsland.Core.AppBase.FluentIconsFontFamily;

        ApplyThemeColors();
    }

    /// <summary>
    /// 绑定设置并按「是否已同意」决定覆盖层显隐；调用方可据 <see cref="Visual.IsVisible"/>
    /// 判断是否需要跳过内容装配（未同意时不应加载图片）。
    /// </summary>
    public void Bind(PluginSettings? settings)
    {
        _settings = settings ?? Plugin.Instance?.Settings;
        IsVisible = _settings?.WomenswearNoticeAccepted != true;
    }

    private void AgreeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_settings != null)
        {
            _settings.WomenswearNoticeAccepted = true;
        }

        // 立即原子落盘：避免停留于 500ms 防抖窗口内时宿主退出导致「已同意」标记未保存
        Plugin.Instance?.SaveSettingsNow();

        IsVisible = false;
        Agreed?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        ApplyThemeColors();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        ApplyThemeColors();
    }

    /// <summary>
    /// 文字与底色随深浅主题刷新；覆盖层与警示块底色优先取宿主（FA2/FA3）当前主题变体的画刷，
    /// 取不到时回退到插件自带的等价配色，保证覆盖层始终不透明、不会露出下方内容。
    /// </summary>
    private void ApplyThemeColors()
    {
        TitleText.Foreground = ThemeHelper.GetTextBrush();
        PlusIcon.Foreground = ThemeHelper.GetTextBrush();
        PlugIcon.Foreground = ThemeHelper.GetTextBrush();
        WarnIcon.Foreground = ThemeHelper.GetTextBrush();
        BodyText.Foreground = ThemeHelper.GetTextBrush();

        RootBorder.Background = TryFindBrush("SolidBackgroundFillColorBaseBrush")
            ?? new SolidColorBrush(ThemeHelper.IsDarkTheme() ? Color.Parse("#202020") : Color.Parse("#F9F9F9"));
        WarnBorder.Background = TryFindBrush("SystemFillColorCautionBackgroundBrush")
            ?? new SolidColorBrush(ThemeHelper.IsDarkTheme() ? Color.Parse("#43360F") : Color.Parse("#FFF4CE"));
    }

    /// <summary>
    /// 按资源键查找宿主主题画刷。必须带当前主题变体查询——不带变体的查询拿到的是
    /// 默认（浅色）资源，会在深色主题下返回浅色底，与按深浅主题计算的文字色（白色）叠加后看不清。
    /// 宿主资源缺失时返回 null，由调用方用插件自带的深浅配色兜底。
    /// </summary>
    private static IBrush? TryFindBrush(string resourceKey)
    {
        var app = Application.Current;
        if (app == null)
        {
            return null;
        }

        var variant = app.ActualThemeVariant;
        if (app.TryFindResource(resourceKey, variant, out var resource) == true && resource is IBrush brush)
        {
            return brush;
        }

        if (app.Styles.TryGetResource(resourceKey, variant, out var styleResource) == true && styleResource is IBrush styleBrush)
        {
            return styleBrush;
        }

        return null;
    }
}
