using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Automation.Rules;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Automation.Triggers;

public class MinutelyTimeTriggerSettingsControl : TriggerSettingsControlBase<MinutelyTimeRangeRuleSettings>
{
    private TextBox _startSecondBox = null!;

    public MinutelyTimeTriggerSettingsControl()
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

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        mainPanel.Children.Add(CreateInputGroup("触发秒数:"));

        mainPanel.Children.Add(new TextBlock
        {
            Text = "在每分钟的指定秒触发",
            FontSize = 12,
            Foreground = Brushes.Gray
        });

        Content = new ScrollViewer
        {
            Content = mainPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private StackPanel CreateInputGroup(string label)
    {
        var groupPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        groupPanel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        });

        _startSecondBox = new TextBox
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _startSecondBox.TextChanged += (s, e) => UpdateSettingsValue();

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_startSecondBox, (s, e) => ValidateAndFormatTextBox(_startSecondBox));

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _startSecondBox
        };

        groupPanel.Children.Add(scrollViewer);

        return groupPanel;
    }

    private bool _isLoading;

    private void LoadSettingsToUi()
    {
        _isLoading = true;
        try
        {
            if (Settings == null) return;

            var initialValue = Settings.StartSecond;
            if (int.TryParse(initialValue, out int second))
            {
                _startSecondBox.Text = second.ToString("D2");
            }
            else
            {
                _startSecondBox.Text = "00";
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ValidateAndFormatTextBox(TextBox textBox)
    {
        var text = textBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            textBox.Text = "00";
            return;
        }

        if (double.TryParse(text, out double value))
        {
            int rounded = (int)Math.Round(value);
            int clamped = Math.Clamp(rounded, 0, 59);
            textBox.Text = clamped.ToString("D2");
        }
        else
        {
            textBox.Text = "00";
        }
    }

    private void UpdateSettingsValue()
    {
        if (_isLoading) return;
        if (Settings == null) return;

        int startSecond = ParseSecond(_startSecondBox.Text);
        Settings.StartSecond = $"{startSecond:D2}";
    }

    private int ParseSecond(string text)
    {
        if (int.TryParse(text, out int value))
            return Math.Clamp(value, 0, 59);
        return 0;
    }
}
