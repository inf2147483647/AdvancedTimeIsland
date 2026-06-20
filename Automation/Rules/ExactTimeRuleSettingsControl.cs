using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Rules;

public class ExactTimeRuleSettingsControl : RuleSettingsControlBase<ExactTimeRuleSettings>
{
    public ExactTimeRuleSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        mainPanel.Children.Add(CreateDateTimePickerGroup("精确时间:", Settings?.TargetTime ?? "", time =>
        {
            if (Settings != null)
                Settings.TargetTime = time;
        }));

        Content = mainPanel;
    }

    private StackPanel CreateDateTimePickerGroup(string label, string initialValue, Action<string> onValueChanged)
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        var pickerRow = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        ParseDateTimeString(initialValue, out int year, out int month, out int day, out int hour, out int minute, out int second);

        var yearCombo = new ComboBox
        {
            Width = 90,
            SelectedIndex = -1,
            Margin = new Thickness(0, 0, 6, 4)
        };
        for (int i = 1900; i <= 2100; i++)
        {
            yearCombo.Items.Add($"{i}年");
        }
        if (year >= 1900 && year <= 2100)
            yearCombo.SelectedIndex = year - 1900;
        pickerRow.Children.Add(yearCombo);

        var monthCombo = new ComboBox
        {
            Width = 70,
            SelectedIndex = -1,
            Margin = new Thickness(0, 0, 6, 4)
        };
        for (int i = 1; i <= 12; i++)
        {
            monthCombo.Items.Add($"{i}月");
        }
        if (month >= 1 && month <= 12)
            monthCombo.SelectedIndex = month - 1;
        pickerRow.Children.Add(monthCombo);

        var dayCombo = new ComboBox
        {
            Width = 70,
            SelectedIndex = -1,
            Margin = new Thickness(0, 0, 6, 4)
        };
        for (int i = 1; i <= 31; i++)
        {
            dayCombo.Items.Add($"{i}日");
        }
        if (day >= 1 && day <= 31)
            dayCombo.SelectedIndex = day - 1;
        pickerRow.Children.Add(dayCombo);

        var hourCombo = new ComboBox
        {
            Width = 60,
            SelectedIndex = -1,
            Margin = new Thickness(0, 0, 6, 4)
        };
        for (int i = 0; i < 24; i++)
        {
            hourCombo.Items.Add($"{i:D2}时");
        }
        if (hour >= 0 && hour < 24)
            hourCombo.SelectedIndex = hour;
        pickerRow.Children.Add(hourCombo);

        var minuteCombo = new ComboBox
        {
            Width = 60,
            SelectedIndex = -1,
            Margin = new Thickness(0, 0, 6, 4)
        };
        for (int i = 0; i < 60; i++)
        {
            minuteCombo.Items.Add($"{i:D2}分");
        }
        if (minute >= 0 && minute < 60)
            minuteCombo.SelectedIndex = minute;
        pickerRow.Children.Add(minuteCombo);

        var secondCombo = new ComboBox
        {
            Width = 60,
            SelectedIndex = -1,
            Margin = new Thickness(0, 0, 6, 4)
        };
        for (int i = 0; i < 60; i++)
        {
            secondCombo.Items.Add($"{i:D2}秒");
        }
        if (second >= 0 && second < 60)
            secondCombo.SelectedIndex = second;
        pickerRow.Children.Add(secondCombo);

        void OnSelectionChanged(object? s, EventArgs e)
        {
            var y = yearCombo.SelectedIndex >= 0 ? yearCombo.SelectedIndex + 1900 : 0;
            var mo = monthCombo.SelectedIndex >= 0 ? monthCombo.SelectedIndex + 1 : 0;
            var d = dayCombo.SelectedIndex >= 0 ? dayCombo.SelectedIndex + 1 : 0;
            var h = hourCombo.SelectedIndex >= 0 ? hourCombo.SelectedIndex : 0;
            var mi = minuteCombo.SelectedIndex >= 0 ? minuteCombo.SelectedIndex : 0;
            var s2 = secondCombo.SelectedIndex >= 0 ? secondCombo.SelectedIndex : 0;
            onValueChanged($"{y:D4}-{mo:D2}-{d:D2}-{h:D2}-{mi:D2}-{s2:D2}");
        }

        yearCombo.SelectionChanged += OnSelectionChanged;
        monthCombo.SelectionChanged += OnSelectionChanged;
        dayCombo.SelectionChanged += OnSelectionChanged;
        hourCombo.SelectionChanged += OnSelectionChanged;
        minuteCombo.SelectionChanged += OnSelectionChanged;
        secondCombo.SelectionChanged += OnSelectionChanged;

        groupPanel.Children.Add(pickerRow);

        return groupPanel;
    }

    private void ParseDateTimeString(string value, out int year, out int month, out int day, out int hour, out int minute, out int second)
    {
        year = 0; month = 0; day = 0; hour = 0; minute = 0; second = 0;

        if (string.IsNullOrWhiteSpace(value))
            return;

        var parts = value.Split('-');
        if (parts.Length >= 1 && int.TryParse(parts[0], out int y)) year = y;
        if (parts.Length >= 2 && int.TryParse(parts[1], out int m)) month = m;
        if (parts.Length >= 3 && int.TryParse(parts[2], out int d)) day = d;
        if (parts.Length >= 4 && int.TryParse(parts[3], out int h)) hour = h;
        if (parts.Length >= 5 && int.TryParse(parts[4], out int mi)) minute = mi;
        if (parts.Length >= 6 && int.TryParse(parts[5], out int s)) second = s;
    }
}