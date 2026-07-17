using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class FestivalRuleSettingsControl : RuleSettingsControlBase<FestivalRuleSettings>
{
    private ComboBox _festivalComboBox;

    public FestivalRuleSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        LoadSettingsToUi();
    }

    private void InitializeComponent()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        var label = new TextBlock { Text = "选择节日：" };
        panel.Children.Add(label);

        _festivalComboBox = new ComboBox
        {
            ItemsSource = GetFestivalList()
        };
        _festivalComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        panel.Children.Add(_festivalComboBox);

        Content = panel;
    }

    private List<string> GetFestivalList()
    {
        var festivals = new List<string>();
        
        // 国际节日
        festivals.Add("元旦 (1月1日)");
        festivals.Add("妇女节 (3月8日)");
        festivals.Add("植树节 (3月12日)");
        festivals.Add("劳动节 (5月1日)");
        festivals.Add("儿童节 (6月1日)");
        festivals.Add("教师节 (9月10日)");
        festivals.Add("清明节 (公历4月4-6日)");
        festivals.Add("冬至 (公历12月21-23日)");

        // 中国传统节日（农历）
        festivals.Add("春节 (农历正月初一)");
        festivals.Add("元宵节 (农历正月十五)");
        festivals.Add("寒食节 (清明前一日)");
        festivals.Add("端午节 (农历五月初五)");
        festivals.Add("七夕节 (农历七月初七)");
        festivals.Add("中元节 (农历七月十五)");
        festivals.Add("中秋节 (农历八月十五)");
        festivals.Add("重阳节 (农历九月初九)");
        festivals.Add("腊八节 (农历十二月初八)");
        festivals.Add("小年 (农历十二月二十三)");
        festivals.Add("除夕 (农历十二月三十)");

        // 红色节日
        festivals.Add("二七纪念日 (2月7日)");
        festivals.Add("学雷锋纪念日 (3月5日)");
        festivals.Add("五四青年节 (5月4日)");
        festivals.Add("七一建党节 (7月1日)");
        festivals.Add("八一建军节 (8月1日)");
        festivals.Add("中国人民抗日战争胜利纪念日 (9月3日)");
        festivals.Add("九一八事变纪念日 (9月18日)");
        festivals.Add("烈士纪念日 (9月30日)");
        festivals.Add("十一国庆节 (10月1日)");
        festivals.Add("中国工农红军长征胜利纪念日 (10月22日)");
        festivals.Add("南京大屠杀死难者国家公祭日 (12月13日)");

        return festivals;
    }

    private void LoadSettingsToUi()
    {
        if (Settings == null) return;
        _festivalComboBox.SelectedItem = Settings.TargetFestival;
    }

    private void UpdateSettingsValue()
    {
        if (Settings == null) return;
        Settings.TargetFestival = _festivalComboBox.SelectedItem as string ?? string.Empty;
    }
}