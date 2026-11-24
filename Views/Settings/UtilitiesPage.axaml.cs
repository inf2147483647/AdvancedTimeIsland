using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// 「小工具」设置页：把原先挂在「主设置」里的
/// 时间格式转换 / 时间计算器 / 注释三个工具集中到独立页面。
/// <para>
/// 是否注册本页由「管理启用的功能 → 小工具」开关决定（<see cref="PluginSettings.EnableUtilities"/>），
/// 该开关在插件加载时读取，因此修改后需重启 ClassIsland 才能生效。
/// </para>
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandUtilities", "小工具", "\uE90F", "\uE712")]
[Group("advancedtimeisland.main")]
public partial class UtilitiesPage : SettingsPageBase
{
    private const string TimeConverterTag = "TimeConverter";
    private const string TimeCalculatorTag = "TimeCalculator";
    private const string GlossaryTag = "Glossary";

    private readonly PluginSettings? _settings;

    public UtilitiesPage()
    {
        _settings = Plugin.Instance?.Settings;

        InitializeComponent();

        // 标签页切换的翻页过场动画：与「主设置」导航栏保持一致
        IntroAnimationHelper.EnablePageFlip(RootTabControl);
        RootTabControl.SelectionChanged += OnTabSelectionChanged;

        AddTab("时间格式转换", TimeConverterTag);
        AddTab("时间计算器", TimeCalculatorTag);
        AddTab("注释", GlossaryTag);
    }

    private void AddTab(string header, string tag)
    {
        RootTabControl.Items.Add(new TabItem
        {
            Header = header,
            Content = null,
            Tag = tag
        });
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem { Content: null } tabItem)
        {
            tabItem.Content = CreateTabContent(tabItem.Tag as string);
        }
    }

    private Control CreateTabContent(string? tag)
    {
        return tag switch
        {
            TimeConverterTag => new TimeConverterPage(_settings),
            TimeCalculatorTag => new TimeCalculatorPage(_settings),
            GlossaryTag => new GlossaryPage(_settings),
            _ => new TextBlock { Text = "内容加载中..." }
        };
    }
}
