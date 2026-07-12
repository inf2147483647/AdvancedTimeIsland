using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Styling;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Services;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;


namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDebug", "AdvancedTimeIsland 调试", SettingsPageCategory.Debug)]
public class DebugPage : SettingsPageBase
{
    private TextBlock? _titleTextBlock;
    private List<TextBlock>? _testPanelTitleTextBlocks;

    private TextBlock? _memoryLeakTitleTextBlock;
    private Button? _memoryLeakStartButton;
    private Button? _memoryLeakClearButton;
    private TextBox? _memoryLeakRateTextBox;
    private ComboBox? _memoryLeakUnitComboBox;
    private TextBlock? _memoryLeakAmountTextBlock;

    public DebugPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _testPanelTitleTextBlocks = new List<TextBlock>();

        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 16
        };

        var warningBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityError());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Message", "仅供调试，除非你能知道您在做什么，请不要使用以下按钮。");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsClosable", false);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Margin", new Thickness(0, 0, 0, 8));
        mainPanel.Children.Add(warningBar);

        _titleTextBlock = new TextBlock
        {
            Text = "AdvancedTimeIsland 调试",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        mainPanel.Children.Add(_titleTextBlock);

        var tab1Panel = CreateSimpleTestPanel(
            "抛出异常测试",
            "开始",
            ButtonCrash_OnClick);

        var tab2Panel = CreateSimpleTestPanel(
            "强制崩溃测试",
            "开始",
            ButtonForceCrash_OnClick);

        var tab3Panel = CreateSimpleTestPanel(
            "自毁测试",
            "开始",
            ButtonSelfDestruct_OnClick);

        mainPanel.Children.Add(tab1Panel);
        mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        mainPanel.Children.Add(tab2Panel);
        mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        mainPanel.Children.Add(tab3Panel);
        mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        mainPanel.Children.Add(CreateMemoryLeakTestPanel());

        var scrollViewer = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        Content = scrollViewer;
    }

    private Grid CreateSimpleTestPanel(
        string titleText,
        string buttonText,
        EventHandler<RoutedEventArgs> clickHandler)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var titleBlock = new TextBlock
        {
            Text = titleText,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        _testPanelTitleTextBlocks?.Add(titleBlock);
        Grid.SetColumn(titleBlock, 0);
        grid.Children.Add(titleBlock);

        var button = new Button
        {
            Content = buttonText,
            Padding = new Thickness(16, 8, 16, 8)
        };
        button.Click += clickHandler;
        Grid.SetColumn(button, 1);
        grid.Children.Add(button);

        return grid;
    }

    private void ButtonCrash_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test.");
    }

    private async void ButtonForceCrash_OnClick(object? sender, RoutedEventArgs e)
    {
        await ShowForceCrashDialog();
    }

    private async void ButtonSelfDestruct_OnClick(object? sender, RoutedEventArgs e)
    {
        await ShowSelfDestructDialog();
    }

    private async Task ShowForceCrashDialog()
    {
        var dialog = new Window
        {
            Title = "提示",
            Width = 400,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var okButton = new Button
        {
            Content = "确定（5秒）",
            IsEnabled = false,
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(8, 0, 8, 0)
        };
        var cancelButton = new Button
        {
            Content = "取消",
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(8, 0, 8, 0)
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        stack.Children.Add(new TextBlock
        {
            Text = "当前操作危险，是否继续执行？",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        stack.Children.Add(buttonPanel);

        dialog.Content = stack;

        int remaining = 5;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) =>
        {
            remaining--;
            if (remaining > 0)
            {
                okButton.Content = $"确定（{remaining}秒）";
            }
            else
            {
                okButton.Content = "确定";
                okButton.IsEnabled = true;
                timer.Stop();
            }
        };
        timer.Start();

        bool confirmed = false;
        okButton.Click += (s, e) => { confirmed = true; dialog.Close(); };
        cancelButton.Click += (s, e) => { confirmed = false; dialog.Close(); };

        await dialog.ShowDialog((Window)VisualRoot!);

        if (confirmed)
        {
            Environment.FailFast("调试");
        }
    }

    private async Task ShowSelfDestructDialog()
    {
        const string requiredText = "我已知悉此操作存在极高风险，我已认真考虑并同意进行自毁测试。";

        var dialog = new Window
        {
            Title = "提示",
            Width = 1000,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var textBox = new TextBox
        {
            Watermark = "请输入确认文本",
            Margin = new Thickness(0, 8, 0, 8)
        };

        var okButton = new Button
        {
            Content = "确定",
            IsEnabled = true,
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(8, 0, 8, 0)
        };
        var cancelButton = new Button
        {
            Content = "取消",
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(8, 0, 8, 0)
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        stack.Children.Add(new TextBlock
        {
            Text = "当前操作存在不可逆的致命危险，是否继续执行？\n请在下方输入\"" + requiredText + "\"",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });
        stack.Children.Add(textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        stack.Children.Add(buttonPanel);

        dialog.Content = stack;

        bool confirmed = false;
        okButton.Click += (s, e) => { confirmed = true; dialog.Close(); };
        cancelButton.Click += (s, e) => { confirmed = false; dialog.Close(); };

        await dialog.ShowDialog((Window)VisualRoot!);

        if (confirmed)
        {
            if (textBox.Text == requiredText)
            {
                try
                {
                    var pluginDir = Plugin.Instance?.PluginConfigFolder
                        ?? Path.Combine("..", "data", "plugin", "AdvancedTimeIsland");
                    var uninstallFile = Path.Combine(pluginDir, ".uninstall");
                    File.WriteAllText(uninstallFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                catch
                {
                }

                await Task.Delay(1000);
                Environment.FailFast("调试");
            }
            else
            {
                await ShowMessageAsync("输入的文本不正确，操作已取消。");
            }
        }
    }

    private async Task ShowMessageAsync(string message)
    {
        var dialog = new Window
        {
            Title = "提示",
            Width = 380,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var okButton = new Button
        {
            Content = "确定",
            Padding = new Thickness(16, 8, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        stack.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeHelper.GetTextBrush()
        });
        stack.Children.Add(okButton);

        dialog.Content = stack;

        okButton.Click += (s, e) => dialog.Close();

        await dialog.ShowDialog((Window)VisualRoot!);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        MemoryLeakTestService.Instance.LeakUpdated += OnLeakUpdated;
        UpdateMemoryLeakUI();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        MemoryLeakTestService.Instance.LeakUpdated -= OnLeakUpdated;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void UpdateThemeColors()
    {
        if (_titleTextBlock != null)
            _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();

        if (_testPanelTitleTextBlocks != null)
        {
            foreach (var tb in _testPanelTitleTextBlocks)
            {
                tb.Foreground = ThemeHelper.GetTextBrush();
            }
        }

        if (_memoryLeakTitleTextBlock != null)
            _memoryLeakTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();

        if (_memoryLeakAmountTextBlock != null)
            _memoryLeakAmountTextBlock.Foreground = ThemeHelper.GetTextBrush();
    }

    private Border CreateMemoryLeakTestPanel()
    {
        var panel = new Border
        {
            Background = ThemeHelper.GetCardBackgroundBrush(),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };

        _memoryLeakTitleTextBlock = new TextBlock
        {
            Text = "内存泄漏测试",
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        content.Children.Add(_memoryLeakTitleTextBlock);

        var buttonPanel = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };

        _memoryLeakStartButton = new Button
        {
            Content = "开始",
            Padding = new Thickness(16, 8, 16, 8)
        };
        _memoryLeakStartButton.Click += MemoryLeakStartButton_OnClick;
        Grid.SetColumn(_memoryLeakStartButton, 1);
        buttonPanel.Children.Add(_memoryLeakStartButton);

        _memoryLeakClearButton = new Button
        {
            Content = "清除",
            Padding = new Thickness(16, 8, 16, 8)
        };
        _memoryLeakClearButton.Click += MemoryLeakClearButton_OnClick;
        Grid.SetColumn(_memoryLeakClearButton, 2);
        buttonPanel.Children.Add(_memoryLeakClearButton);

        content.Children.Add(buttonPanel);

        var ratePanel = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(100) },
                new ColumnDefinition { Width = new GridLength(80) }
            },
            ColumnSpacing = 8
        };

        var rateLabel = new TextBlock
        {
            Text = "每秒泄漏量",
            FontSize = 14,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(rateLabel, 0);
        ratePanel.Children.Add(rateLabel);

        _memoryLeakRateTextBox = new TextBox
        {
            Text = MemoryLeakTestService.Instance.LeakRate.ToString(),
            FontSize = 14,
            Padding = new Thickness(8, 4, 8, 4)
        };
        Grid.SetColumn(_memoryLeakRateTextBox, 1);
        ratePanel.Children.Add(_memoryLeakRateTextBox);

        _memoryLeakUnitComboBox = new ComboBox
        {
            FontSize = 14,
            Padding = new Thickness(8, 4, 8, 4)
        };
        _memoryLeakUnitComboBox.Items.Add("Byte");
        _memoryLeakUnitComboBox.Items.Add("KiB");
        _memoryLeakUnitComboBox.Items.Add("MiB");
        var unitIndex = Array.IndexOf(new[] { "Byte", "KiB", "MiB" }, MemoryLeakTestService.Instance.LeakUnit);
        _memoryLeakUnitComboBox.SelectedIndex = unitIndex >= 0 ? unitIndex : 1;
        Grid.SetColumn(_memoryLeakUnitComboBox, 2);
        ratePanel.Children.Add(_memoryLeakUnitComboBox);

        content.Children.Add(ratePanel);

        var amountPanel = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var amountLabel = new TextBlock
        {
            Text = "已泄漏内存：",
            FontSize = 14,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(amountLabel, 0);
        amountPanel.Children.Add(amountLabel);

        _memoryLeakAmountTextBlock = new TextBlock
        {
            Text = MemoryLeakTestService.Instance.FormatMemorySize(MemoryLeakTestService.Instance.LeakedBytes),
            FontSize = 14,
            Foreground = ThemeHelper.GetTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_memoryLeakAmountTextBlock, 1);
        amountPanel.Children.Add(_memoryLeakAmountTextBlock);

        content.Children.Add(amountPanel);

        panel.Child = content;

        return panel;
    }

    private void MemoryLeakStartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var service = MemoryLeakTestService.Instance;

        UpdateLeakRateFromUI();

        if (service.IsRunning)
        {
            if (service.IsPaused)
            {
                service.Start();
                _memoryLeakStartButton!.Content = "暂停";
            }
            else
            {
                service.Pause();
                _memoryLeakStartButton!.Content = "继续";
            }
        }
        else
        {
            service.Start();
            _memoryLeakStartButton!.Content = "暂停";
        }
    }

    private void MemoryLeakClearButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowMemoryLeakClearDialog();
    }

    private void ShowMemoryLeakClearDialog()
    {
        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "需要重启");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", "需要重启以清除内存泄漏。");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "立即重启");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "SecondaryButtonText", "稍后");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "DefaultButton", FluentAvaloniaCompatibilityHelper.GetContentDialogButtonPrimary());
        FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, TopLevel.GetTopLevel(this)).ContinueWith(task =>
        {
            if (FluentAvaloniaCompatibilityHelper.IsContentDialogResultPrimary(task.Result))
            {
                ClassIsland.Core.AppBase.Current.Restart();
            }
        });
    }

    private void OnLeakUpdated(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(UpdateMemoryLeakUI);
    }

    private void UpdateMemoryLeakUI()
    {
        var service = MemoryLeakTestService.Instance;

        if (_memoryLeakStartButton != null)
        {
            if (service.IsRunning)
            {
                _memoryLeakStartButton.Content = service.IsPaused ? "继续" : "暂停";
            }
            else
            {
                _memoryLeakStartButton.Content = "开始";
            }
        }

        if (_memoryLeakAmountTextBlock != null)
        {
            _memoryLeakAmountTextBlock.Text = service.FormatMemorySize(service.LeakedBytes);
        }
    }

    private void UpdateLeakRateFromUI()
    {
        if (!double.TryParse(_memoryLeakRateTextBox?.Text ?? "0", out var rate))
        {
            rate = 0;
        }

        var unit = _memoryLeakUnitComboBox?.SelectedItem?.ToString() ?? "KiB";
        MemoryLeakTestService.Instance.SetLeakRate(rate, unit);
    }
}