using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Services;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDebug", "AdvancedTimeIsland 调试", SettingsPageCategory.Debug)]
public partial class DebugPage : SettingsPageBase
{
    public DebugPage()
    {
        InitializeComponent();

        // 初始化内存泄漏测试设置
        MemoryLeakRateTextBox.Text = MemoryLeakTestService.Instance.LeakRate.ToString();
        MemoryLeakUnitComboBox.Items.Add("Byte");
        MemoryLeakUnitComboBox.Items.Add("KiB");
        MemoryLeakUnitComboBox.Items.Add("MiB");
        var unitIndex = Array.IndexOf(new[] { "Byte", "KiB", "MiB" }, MemoryLeakTestService.Instance.LeakUnit);
        MemoryLeakUnitComboBox.SelectedIndex = unitIndex >= 0 ? unitIndex : 1;
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

    private void ButtonHanfuTemplate_OnClick(object? sender, RoutedEventArgs e)
    {
        IAppHost.TryGetService<IUriNavigationService>()?
            .NavigateWrapped(new Uri("classisland://app/settings/AdvancedTimeIslandHanfuTemplate?ci_keepHistory=true"));
    }

    private async Task ShowForceCrashDialog()
    {
        var dialog = new Window
        {
            Title = "提示",
            Width = 400,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };

        FluentAvaloniaCompatibilityHelper.ApplyDialogTheme(dialog);

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
            HorizontalAlignment = HorizontalAlignment.Right
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

        dialog.Owner = Window.GetWindow(this);
        dialog.ShowDialog();

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
            ResizeMode = ResizeMode.NoResize
        };

        FluentAvaloniaCompatibilityHelper.ApplyDialogTheme(dialog);

        var textBox = new TextBox
        {
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
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        stack.Children.Add(buttonPanel);

        dialog.Content = stack;

        bool confirmed = false;
        okButton.Click += (s, e) => { confirmed = true; dialog.Close(); };
        cancelButton.Click += (s, e) => { confirmed = false; dialog.Close(); };

        dialog.Owner = Window.GetWindow(this);
        dialog.ShowDialog();

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
            ResizeMode = ResizeMode.NoResize
        };

        FluentAvaloniaCompatibilityHelper.ApplyDialogTheme(dialog);

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
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(okButton);

        dialog.Content = stack;

        okButton.Click += (s, e) => dialog.Close();

        dialog.Owner = Window.GetWindow(this);
        dialog.ShowDialog();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        MemoryLeakTestService.Instance.LeakUpdated += OnLeakUpdated;
        UpdateMemoryLeakUI();
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        MemoryLeakTestService.Instance.LeakUpdated -= OnLeakUpdated;
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
                MemoryLeakStartButton.Content = "暂停";
            }
            else
            {
                service.Pause();
                MemoryLeakStartButton.Content = "继续";
            }
        }
        else
        {
            service.Start();
            MemoryLeakStartButton.Content = "暂停";
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
        FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, Window.GetWindow(this)).ContinueWith(task =>
        {
            if (FluentAvaloniaCompatibilityHelper.IsContentDialogResultPrimary(task.Result))
            {
                ClassIsland.Core.AppBase.Current.Restart();
            }
        });
    }

    private void OnLeakUpdated(object? sender, EventArgs e)
    {
        UIThread.Post(UpdateMemoryLeakUI);
    }

    private void UpdateMemoryLeakUI()
    {
        var service = MemoryLeakTestService.Instance;

        if (service.IsRunning)
        {
            MemoryLeakStartButton.Content = service.IsPaused ? "继续" : "暂停";
        }
        else
        {
            MemoryLeakStartButton.Content = "开始";
        }

        MemoryLeakAmountTextBlock.Text = service.FormatMemorySize(service.LeakedBytes);
    }

    private void UpdateLeakRateFromUI()
    {
        if (!double.TryParse(MemoryLeakRateTextBox.Text ?? "0", out var rate))
        {
            rate = 0;
        }

        var unit = MemoryLeakUnitComboBox.SelectedItem?.ToString() ?? "KiB";
        MemoryLeakTestService.Instance.SetLeakRate(rate, unit);
    }
}
