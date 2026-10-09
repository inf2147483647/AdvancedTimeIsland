using System;
using System.IO;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ExpressiveLoadingIndicator = AdvancedTimeIsland.Views.Controls.ExpressiveLoadingIndicator;

namespace AdvancedTimeIsland.Views.Settings;

/// <summary>
/// AdvancedTimeIsland 调试页（axaml 版，写法对齐 ClassIsland 官方 DebugPage）。
/// 主面板使用官方 <c>settings-container animated-intro</c> 样式类：宿主“动画效果”设为“华丽”时，
/// 警告条 / 标题 / 各测试入口 / 卡片 / 折叠栏依次淡入上滑；嵌套 Expander 展开动画走 FluentAvalonia 官方样式。
/// 顶部警告条使用真实 FA InfoBar（FA2 为 InfoBar、FA3 为 FAInfoBar），经兼容 Helper 按运行时版本创建。
/// </summary>
[SettingsPageInfo("AdvancedTimeIslandDebug", "AdvancedTimeIsland 调试", SettingsPageCategory.Debug)]
public partial class DebugPage : SettingsPageBase
{
    public DebugPage()
    {
        InitializeComponent();
        WireUI();
    }

    private void WireUI()
    {
        // 顶部错误警告条：FA2/FA3 类型名不同，用 Helper 创建真实官方控件后注入宿主
        var warningBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityError());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Message", "仅供调试，除非你能知道您在做什么，请不要使用以下按钮。");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsClosable", false);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Margin", new Thickness(0, 0, 0, 8));
        WarningBarHost.Content = warningBar;

        // “女装”为隐藏入口：启用实验性功能后才在调试页可见
        WomenswearSection.IsVisible = Plugin.Instance?.Settings.EnableExperimentalFeatures ?? false;

        // 内存泄漏测试初始化
        MemoryLeakRateTextBox.Text = MemoryLeakTestService.Instance.LeakRate.ToString();
        var unitIndex = Array.IndexOf(new[] { "Byte", "KiB", "MiB" }, MemoryLeakTestService.Instance.LeakUnit);
        MemoryLeakUnitComboBox.SelectedIndex = unitIndex >= 0 ? unitIndex : 1;
        UpdateMemoryLeakUI();
    }

    // ==================== 危险操作入口 ====================
    private void ButtonCrash_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test.");
    }

    private async void ButtonForceCrash_OnClick(object? sender, RoutedEventArgs e)
    {
        // async void 事件处理器：任何异常都会冒泡到 TaskScheduler 并被宿主判定为"插件异常"而自动禁用插件，
        // 故此处统一兜底（对话框内部逻辑不应影响插件整体可用性）。
        try { await ShowForceCrashDialog(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ButtonForceCrash_OnClick 异常：{ex}"); }
    }

    private async void ButtonSelfDestruct_OnClick(object? sender, RoutedEventArgs e)
    {
        try { await ShowSelfDestructDialog(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ButtonSelfDestruct_OnClick 异常：{ex}"); }
    }

    private void ButtonHanfuTemplate_OnClick(object? sender, RoutedEventArgs e)
    {
        FluentAvaloniaCompatibilityHelper.NavigateToSettingsPage(this, "AdvancedTimeIslandHanfuTemplate");
    }

    private void ButtonWomenswear_OnClick(object? sender, RoutedEventArgs e)
    {
        FluentAvaloniaCompatibilityHelper.NavigateToSettingsPage(this, "AdvancedTimeIslandWomenswear");
    }

    private async void ButtonShowFestivalList_OnClick(object? sender, RoutedEventArgs e)
    {
        try { await ShowFestivalListDialogAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ButtonShowFestivalList_OnClick 异常：{ex}"); }
    }

    private async void ButtonEmpty_OnClick(object? sender, RoutedEventArgs e)
    {
        try { await ShowEmptyDialogAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ButtonEmpty_OnClick 异常：{ex}"); }
    }

    private async void ButtonLoading_OnClick(object? sender, RoutedEventArgs e)
    {
        try { await ShowLoadingDialogAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ButtonLoading_OnClick 异常：{ex}"); }
    }

    /// <summary>
    /// 获取当前主题的强调色画刷（加载占位符颜色与应用强调色保持一致）。
    /// </summary>
    private static SolidColorBrush GetAccentBrush()
    {
        if (Application.Current?.TryFindResource("SystemAccentColor", out var colorObj) == true && colorObj is Color accentColor)
        {
            return new SolidColorBrush(accentColor);
        }
        if (Application.Current?.TryFindResource("AccentColor", out var accentObj) == true && accentObj is Color accentColor2)
        {
            return new SolidColorBrush(accentColor2);
        }
        return new SolidColorBrush(Colors.DodgerBlue);
    }

    // ==================== 展示类对话框（官方空状态 / 加载动画 / 节日列表） ====================
    /// <summary>
    /// 展示 ClassIsland 官方样式的“啥都没有”空白占位符（ClassIsland.Core.Controls.Empty）。
    /// </summary>
    private async Task ShowEmptyDialogAsync()
    {
        var dialog = new Window
        {
            Title = "啥都没有",
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var empty = new Empty
        {
            MinHeight = 160,
            Margin = new Thickness(24, 24, 24, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        var closeButton = new Button
        {
            Content = "关闭",
            Padding = new Thickness(16, 8, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        closeButton.Click += (s, e) => dialog.Close();

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        stack.Children.Add(empty);
        stack.Children.Add(closeButton);

        dialog.Content = stack;

        await FluentAvaloniaCompatibilityHelper.ShowDialogSafeAsync(dialog, this);
    }

    /// <summary>
    /// 展示插件内置的“加载中”占位符（<see cref="ExpressiveLoadingIndicator"/>，
    /// Material 3 Expressive 风格：正方形 → 三角形 → 五边形循环弹簧形变）。
    /// 该控件复制自 ClassIsland.Core 2.1.x，使 net8（Avalonia 11）与 net10（Avalonia 12）宿主下均可显示。
    /// </summary>
    private async Task ShowLoadingDialogAsync()
    {
        var dialog = new Window
        {
            Title = "加载中",
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var loadingIndicator = new ExpressiveLoadingIndicator
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = GetAccentBrush(),
            Margin = new Thickness(24, 40, 24, 0)
        };

        var closeButton = new Button
        {
            Content = "关闭",
            Padding = new Thickness(16, 8, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        closeButton.Click += (s, e) => dialog.Close();

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16)
        };
        stack.Children.Add(loadingIndicator);
        stack.Children.Add(closeButton);

        dialog.Content = stack;

        await FluentAvaloniaCompatibilityHelper.ShowDialogSafeAsync(dialog, this);
    }

    private async Task ShowFestivalListDialogAsync()
    {
        var now = TimeBaseService.Instance?.GetCurrentTime() ?? DateTime.Now;
        var year = now.Year;

        var dialog = new Window
        {
            Title = "节日列表",
            Width = 520,
            Height = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true
        };

        var dialogGrid = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        var titleBlock = new TextBlock
        {
            Text = "插件已注册的全部节日",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        Grid.SetRow(titleBlock, 0);
        dialogGrid.Children.Add(titleBlock);

        var previousYearButton = new Button
        {
            Content = "上一年",
            Padding = new Thickness(12, 4, 12, 4)
        };
        var yearBlock = new TextBlock
        {
            Text = $"{year} 年",
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ThemeHelper.GetTextBrush()
        };
        var nextYearButton = new Button
        {
            Content = "下一年",
            Padding = new Thickness(12, 4, 12, 4)
        };
        var backToCurrentYearButton = new Button
        {
            Content = "回到今年",
            Padding = new Thickness(12, 4, 12, 4)
        };
        var yearJumpTextBox = new TextBox
        {
            Width = 90,
            Watermark = "1-9999"
        };
        var jumpYearButton = new Button
        {
            Content = "快速跳转",
            Padding = new Thickness(12, 4, 12, 4)
        };
        var yearNavPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var jumpPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
        jumpPanel.Children.Add(new TextBlock
        {
            Text = "年份",
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ThemeHelper.GetSubTextBrush()
        });
        jumpPanel.Children.Add(yearJumpTextBox);
        jumpPanel.Children.Add(jumpYearButton);

        yearNavPanel.Children.Add(previousYearButton);
        yearNavPanel.Children.Add(yearBlock);
        yearNavPanel.Children.Add(nextYearButton);
        yearNavPanel.Children.Add(backToCurrentYearButton);

        var yearNavContainer = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        yearNavContainer.Children.Add(yearNavPanel);
        yearNavContainer.Children.Add(jumpPanel);
        Grid.SetRow(yearNavContainer, 1);
        dialogGrid.Children.Add(yearNavContainer);

        var subtitleBlock = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeHelper.GetSubTextBrush(),
            Margin = new Thickness(0, 8, 0, 8)
        };
        Grid.SetRow(subtitleBlock, 2);
        dialogGrid.Children.Add(subtitleBlock);

        var listPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4
        };

        var scrollViewer = new ScrollViewer
        {
            Content = listPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetRow(scrollViewer, 3);
        dialogGrid.Children.Add(scrollViewer);

        var closeButton = new Button
        {
            Content = "关闭",
            Padding = new Thickness(16, 8, 16, 8),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        closeButton.Click += (s, e) => dialog.Close();
        Grid.SetRow(closeButton, 4);
        dialogGrid.Children.Add(closeButton);

        var isLoading = false;

        async Task LoadYearAsync(int targetYear)
        {
            if (isLoading)
                return;

            isLoading = true;
            previousYearButton.IsEnabled = false;
            nextYearButton.IsEnabled = false;
            yearBlock.Text = $"{targetYear} 年";

            try
            {
                var festivals = await Task.Run(() => FestivalCatalog.GetAllFestivalsOfYear(targetYear));

                listPanel.Children.Clear();
                subtitleBlock.Text = $"共 {festivals.Count} 个。列表不受“管理启用的功能”影响，仅展示“主界面-节日”组件的国际、中国传统、红色、实验性 4 类节日（节日库已与“下个节日倒计时”对齐），同义节日名已合并，且不展示早于其起源年份的节日。";

                if (festivals.Count == 0)
                {
                    listPanel.Children.Add(new TextBlock
                    {
                        Text = "无",
                        FontSize = 14,
                        Foreground = ThemeHelper.GetTextBrush()
                    });
                }
                else
                {
                    foreach (var (name, date) in festivals)
                    {
                        var row = new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition { Width = new GridLength(96) },
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                            }
                        };

                        var dateBlock = new TextBlock
                        {
                            Text = $"{date.Month}月{date.Day}日",
                            FontSize = 14,
                            Foreground = ThemeHelper.GetSubTextBrush(),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(dateBlock, 0);
                        row.Children.Add(dateBlock);

                        var nameBlock = new TextBlock
                        {
                            Text = name,
                            FontSize = 14,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = ThemeHelper.GetTextBrush(),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(nameBlock, 1);
                        row.Children.Add(nameBlock);

                        listPanel.Children.Add(row);
                    }
                }
            }
            finally
            {
                previousYearButton.IsEnabled = year > 1;
                nextYearButton.IsEnabled = year < 9999;
                jumpYearButton.IsEnabled = true;
                backToCurrentYearButton.IsEnabled = true;
                isLoading = false;
            }
        }

        // 跳转到指定年份（限 1-9999）
        async Task JumpToYearAsync(int targetYear)
        {
            if (isLoading)
                return;

            if (targetYear < 1 || targetYear > 9999)
            {
                yearJumpTextBox.Text = string.Empty;
                yearJumpTextBox.Watermark = "请输入 1-9999";
                return;
            }

            yearJumpTextBox.Text = string.Empty;
            year = targetYear;
            await LoadYearAsync(year);
        }

        previousYearButton.Click += async (s, e) =>
        {
            if (year <= 1)
                return;

            year--;
            await LoadYearAsync(year);
        };

        nextYearButton.Click += async (s, e) =>
        {
            if (year >= 9999)
                return;

            year++;
            await LoadYearAsync(year);
        };

        jumpYearButton.Click += async (s, e) =>
        {
            if (int.TryParse(yearJumpTextBox.Text?.Trim(), out var targetYear))
            {
                await JumpToYearAsync(targetYear);
            }
            else
            {
                yearJumpTextBox.Text = string.Empty;
                yearJumpTextBox.Watermark = "请输入 1-9999";
            }
        };

        yearJumpTextBox.KeyDown += async (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (int.TryParse(yearJumpTextBox.Text?.Trim(), out var targetYear))
                {
                    await JumpToYearAsync(targetYear);
                }
                else
                {
                    yearJumpTextBox.Text = string.Empty;
                    yearJumpTextBox.Watermark = "请输入 1-9999";
                }
            }
        };

        backToCurrentYearButton.Click += async (s, e) =>
        {
            var currentYear = TimeBaseService.Instance?.GetCurrentTime().Year ?? DateTime.Now.Year;
            if (currentYear < 1 || currentYear > 9999 || year == currentYear)
                return;

            year = currentYear;
            await LoadYearAsync(year);
        };

        dialog.Content = dialogGrid;

        await LoadYearAsync(year);
        await FluentAvaloniaCompatibilityHelper.ShowDialogSafeAsync(dialog, this);
    }

    // ==================== 崩溃 / 自毁确认对话框 ====================
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

        var remaining = 5;
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

        var confirmed = false;
        okButton.Click += (s, e) => { confirmed = true; dialog.Close(); };
        cancelButton.Click += (s, e) => { confirmed = false; dialog.Close(); };

        await FluentAvaloniaCompatibilityHelper.ShowDialogSafeAsync(dialog, this);

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

        var confirmed = false;
        okButton.Click += (s, e) => { confirmed = true; dialog.Close(); };
        cancelButton.Click += (s, e) => { confirmed = false; dialog.Close(); };

        await FluentAvaloniaCompatibilityHelper.ShowDialogSafeAsync(dialog, this);

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

        await FluentAvaloniaCompatibilityHelper.ShowDialogSafeAsync(dialog, this);
    }

    // ==================== 内存泄漏测试 ====================
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

        // axaml 中下拉项是 ComboBoxItem，真实单位字符串取其 Content（旧纯代码版直接存的字符串）
        var unit = (MemoryLeakUnitComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "KiB";
        MemoryLeakTestService.Instance.SetLeakRate(rate, unit);
    }

    // ==================== 主题自适应 / 生命周期 ====================
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        MemoryLeakTestService.Instance.LeakUpdated += OnLeakUpdated;
        UpdateMemoryLeakUI();
        ApplyThemeColors();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        MemoryLeakTestService.Instance.LeakUpdated -= OnLeakUpdated;
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e) => ApplyThemeColors();

    /// <summary>
    /// 标题 / 内存泄漏卡片底色随深浅主题刷新（行标题与正文继承宿主前景，已自动适配）。
    /// </summary>
    private void ApplyThemeColors()
    {
        TitleText.Foreground = ThemeHelper.GetTextBrush();
        MemoryLeakTitleText.Foreground = ThemeHelper.GetTextBrush();
        MemoryLeakAmountTextBlock.Foreground = ThemeHelper.GetTextBrush();
        MemoryLeakCard.Background = ThemeHelper.GetCardBackgroundBrush();
    }
}
