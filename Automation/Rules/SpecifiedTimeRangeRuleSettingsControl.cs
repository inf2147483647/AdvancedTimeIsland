using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using AdvancedTimeIsland.Helpers;

namespace AdvancedTimeIsland.Automation.Rules;

/// <summary>
/// 在指定时间范围内 规则设置控件。
/// 在 [周期] 的 [单位] 范围内：输入 例如 "1,3-5,7~11"。
/// </summary>
public class SpecifiedTimeRangeRuleSettingsControl : RuleSettingsControlBase<SpecifiedTimeRangeRuleSettings>
{
    private ComboBox _periodComboBox = null!;
    private ComboBox _unitComboBox = null!;
    private TextBox _expressionTextBox = null!;
    private TextBlock _hintTextBlock = null!;

    private bool _isLoading;

    public SpecifiedTimeRangeRuleSettingsControl()
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
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // 在 [周期] 的 [单位] 范围内
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        headerPanel.Children.Add(CreateText("在"));

        _periodComboBox = new ComboBox
        {
            ItemsSource = SpecifiedTimeRangeHelper.PeriodNames,
            MinWidth = 110,
            VerticalAlignment = VerticalAlignment.Center
        };
        _periodComboBox.SelectionChanged += (s, e) =>
        {
            if (_isLoading) return;
            ApplyPeriodToUnitComboBox();
            UpdateSettingsValue();
        };
        headerPanel.Children.Add(_periodComboBox);

        headerPanel.Children.Add(CreateText("的"));

        _unitComboBox = new ComboBox
        {
            MinWidth = 110,
            VerticalAlignment = VerticalAlignment.Center
        };
        _unitComboBox.SelectionChanged += (s, e) => UpdateSettingsValue();
        headerPanel.Children.Add(_unitComboBox);

        headerPanel.Children.Add(CreateText("范围内"));

        panel.Children.Add(headerPanel);

        _expressionTextBox = new TextBox
        {
            MinWidth = 320,
            Watermark = "例如：1,3-5,7~11",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _expressionTextBox.TextChanged += (s, e) => UpdateSettingsValue();
        panel.Children.Add(_expressionTextBox);

        _hintTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 420,
            Foreground = ThemeHelper.GetSubTextBrush()
        };
        panel.Children.Add(_hintTextBlock);

        Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private static TextBlock CreateText(string text) => new()
    {
        Text = text,
        VerticalAlignment = VerticalAlignment.Center,
        Foreground = ThemeHelper.GetTextBrush()
    };

    private void LoadSettingsToUi()
    {
        _isLoading = true;
        try
        {
            if (Settings == null) return;

            _periodComboBox.SelectedIndex = SpecifiedTimeRangeHelper.ClampPeriod(Settings.Period);
            ApplyPeriodToUnitComboBox();
            _unitComboBox.SelectedIndex = 0;
            _expressionTextBox.Text = Settings.Expression;
        }
        finally
        {
            _isLoading = false;
        }

        UpdateSettingsValue();
    }

    /// <summary>按当前周期刷新单位下拉的候选项。</summary>
    private void ApplyPeriodToUnitComboBox()
    {
        var period = SpecifiedTimeRangeHelper.ClampPeriod(_periodComboBox.SelectedIndex);
        _unitComboBox.ItemsSource = SpecifiedTimeRangeHelper.UnitNames[period];
        _unitComboBox.SelectedIndex = 0;
    }

    private void UpdateSettingsValue()
    {
        if (_isLoading) return;
        if (Settings == null) return;

        Settings.Period = SpecifiedTimeRangeHelper.ClampPeriod(_periodComboBox.SelectedIndex);
        Settings.Unit = _unitComboBox.SelectedIndex < 0 ? 0 : _unitComboBox.SelectedIndex;
        Settings.Expression = _expressionTextBox.Text ?? string.Empty;

        UpdateHint();
    }

    private void UpdateHint()
    {
        var period = SpecifiedTimeRangeHelper.ClampPeriod(_periodComboBox.SelectedIndex);
        var example = SpecifiedTimeRangeHelper.GetExample(period);

        if (SpecifiedTimeRangeHelper.TryParse(_expressionTextBox.Text, period, out _, out var error))
        {
            _hintTextBlock.Text = $"{example}；多个范围用半角逗号分隔，分隔符可用减号(-)和波浪号(~)";
            _hintTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
            return;
        }

        _hintTextBlock.Text = $"⚠ {error}。{example}";
        _hintTextBlock.Foreground = ThemeHelper.GetOrangeBrush();
    }
}
