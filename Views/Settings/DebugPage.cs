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
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;

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
        
        var hanfuTemplatePanel = CreateSimpleTestPanel(
            "汉服页面模板",
            "进入",
            ButtonHanfuTemplate_OnClick);
        mainPanel.Children.Add(hanfuTemplatePanel);
        mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });

        var festivalListPanel = CreateSimpleTestPanel(
            "显示节日列表",
            "查看",
            ButtonShowFestivalList_OnClick);
        mainPanel.Children.Add(festivalListPanel);
        mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        
        mainPanel.Children.Add(CreateMemoryLeakTestPanel());
        mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        mainPanel.Children.Add(CreateNestedExpanderPanel());

        var scrollViewer = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = false
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

    private async void ButtonShowFestivalList_OnClick(object? sender, RoutedEventArgs e)
    {
        try { await ShowFestivalListDialogAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ButtonShowFestivalList_OnClick 异常：{ex}"); }
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
        var yearNavPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };
        yearNavPanel.Children.Add(previousYearButton);
        yearNavPanel.Children.Add(yearBlock);
        yearNavPanel.Children.Add(nextYearButton);
        Grid.SetRow(yearNavPanel, 1);
        dialogGrid.Children.Add(yearNavPanel);

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
                isLoading = false;
            }
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

        dialog.Content = dialogGrid;

        await LoadYearAsync(year);
        await FluentAvaloniaCompatibilityHelper.ShowDialogSafeAsync(dialog, this);
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

        bool confirmed = false;
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

    /// <summary>
    /// 嵌套折叠栏示例：外层折叠栏中再放一层折叠栏，共两层。
    /// </summary>
    private Expander CreateNestedExpanderPanel()
    {
        var outerHeader = new TextBlock
        {
            Text = "嵌套折叠栏",
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _testPanelTitleTextBlocks?.Add(outerHeader);

        var outerContent = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };
        outerContent.Children.Add(new TextBlock
        {
            Text = "第一层折叠栏的内容。",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeHelper.GetTextBrush()
        });

        var innerHeader = new TextBlock
        {
            Text = "第二层折叠栏",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        _testPanelTitleTextBlocks?.Add(innerHeader);

        var innerContent = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };
        innerContent.Children.Add(new TextBlock
        {
            Text = "第二层折叠栏的内容。",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = ThemeHelper.GetTextBrush()
        });

        var innerExpander = new Expander
        {
            Header = innerHeader,
            Content = innerContent,
            IsExpanded = false,
            Margin = new Thickness(16, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        outerContent.Children.Add(innerExpander);

        return new Expander
        {
            Header = outerHeader,
            Content = outerContent,
            IsExpanded = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
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