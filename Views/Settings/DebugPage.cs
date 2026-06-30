using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using FluentAvalonia.UI.Controls;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandDebug", "AdvancedTimeIsland 调试", SettingsPageCategory.Debug)]
public class DebugPage : SettingsPageBase
{
    public DebugPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 16
        };

        var warningBar = new InfoBar
        {
            Severity = InfoBarSeverity.Error,
            Message = "仅供调试，除非你能知道您在做什么，请不要使用以下按钮。",
            IsOpen = true,
            IsClosable = false,
            Margin = new Thickness(0, 0, 0, 8)
        };
        mainPanel.Children.Add(warningBar);

        var title = new TextBlock
        {
            Text = "AdvancedTimeIsland 调试",
            FontSize = 24,
            FontWeight = FontWeight.Bold
        };
        mainPanel.Children.Add(title);

        // 标签 1: 抛出异常测试
        var tab1Panel = CreateSimpleTestPanel(
            "抛出异常测试",
            "开始",
            ButtonCrash_OnClick);

        // 标签 2: 强制崩溃测试
        var tab2Panel = CreateSimpleTestPanel(
            "强制崩溃测试",
            "开始",
            ButtonForceCrash_OnClick);

        // 标签 3: 自毁测试
        var tab3Panel = CreateSimpleTestPanel(
            "自毁测试",
            "开始",
            ButtonSelfDestruct_OnClick);

        mainPanel.Children.Add(tab1Panel);
        mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        mainPanel.Children.Add(tab2Panel);
        mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        mainPanel.Children.Add(tab3Panel);

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
            VerticalAlignment = VerticalAlignment.Center
        };
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
            TextWrapping = TextWrapping.Wrap
        });
        stack.Children.Add(okButton);

        dialog.Content = stack;

        okButton.Click += (s, e) => dialog.Close();

        await dialog.ShowDialog((Window)VisualRoot!);
    }
}



