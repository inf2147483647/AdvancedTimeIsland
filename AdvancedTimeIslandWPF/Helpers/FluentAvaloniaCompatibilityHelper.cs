using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 兼容辅助类（WPF 版）。
/// 原 Avalonia 版用于在 FA2/FA3 之间做反射兼容；WPF 版改为直接提供
/// InfoBar / ContentDialog / SettingsExpander 等自绘控件，并保持方法签名不变，
/// 以便调用方代码尽量少改动。
/// </summary>
public static class FluentAvaloniaCompatibilityHelper
{
    // ==================== InfoBar ====================

    public static FrameworkElement CreateInfoBar()
    {
        return new WpfInfoBar();
    }

    public static void SetInfoBarProperty(FrameworkElement infoBar, string propertyName, object value)
    {
        if (infoBar == null) return;
        var property = infoBar.GetType().GetProperty(propertyName);
        if (property != null)
        {
            try
            {
                if (property.PropertyType == typeof(Thickness) && value is Thickness t)
                {
                    property.SetValue(infoBar, t);
                }
                else if (property.PropertyType == typeof(bool) && value is bool b)
                {
                    property.SetValue(infoBar, b);
                }
                else if (property.PropertyType == typeof(int) && value is int i)
                {
                    property.SetValue(infoBar, i);
                }
                else if (property.PropertyType == typeof(string) && value is string s)
                {
                    property.SetValue(infoBar, s);
                }
                else
                {
                    property.SetValue(infoBar, value);
                }
            }
            catch
            {
            }
        }
    }

    public static object GetInfoBarProperty(FrameworkElement infoBar, string propertyName)
    {
        if (infoBar == null) return null!;
        var property = infoBar.GetType().GetProperty(propertyName);
        return property?.GetValue(infoBar) ?? null!;
    }

    public static void AddInfoBarClosedHandler(FrameworkElement infoBar, EventHandler handler)
    {
        if (infoBar is WpfInfoBar wpfInfoBar)
        {
            wpfInfoBar.Closed += handler;
        }
    }

    public static object GetInfoBarSeverityWarning() => WpfInfoBar.SeverityWarning;
    public static object GetInfoBarSeverityInformational() => WpfInfoBar.SeverityInformational;
    public static object GetInfoBarSeverityError() => WpfInfoBar.SeverityError;
    public static object GetInfoBarSeveritySuccess() => WpfInfoBar.SeveritySuccess;

    // ==================== ContentDialog ====================

    public static object CreateContentDialog()
    {
        return new WpfContentDialog();
    }

    public static void SetContentDialogProperty(object dialog, string propertyName, object value)
    {
        if (dialog == null) return;
        var property = dialog.GetType().GetProperty(propertyName);
        if (property != null)
        {
            try
            {
                property.SetValue(dialog, value);
            }
            catch
            {
            }
        }
    }

    public static object GetContentDialogProperty(object dialog, string propertyName)
    {
        if (dialog == null) return null!;
        var property = dialog.GetType().GetProperty(propertyName);
        return property?.GetValue(dialog) ?? null!;
    }

    public static void AddContentDialogButtonClickHandler(object dialog, string eventName, EventHandler handler)
    {
        if (dialog is WpfContentDialog wpfDialog)
        {
            switch (eventName)
            {
                case "PrimaryButtonClick":
                    wpfDialog.PrimaryButtonClick += handler;
                    break;
                case "SecondaryButtonClick":
                    wpfDialog.SecondaryButtonClick += handler;
                    break;
                case "CloseButtonClick":
                    wpfDialog.CloseButtonClick += handler;
                    break;
            }
        }
    }

    public static async System.Threading.Tasks.Task<object> ShowContentDialogAsync(object dialog, object? topLevel)
    {
        if (dialog is not WpfContentDialog wpfDialog)
            return null!;

        var owner = topLevel as Window ?? Application.Current?.MainWindow;
        var result = await wpfDialog.ShowAsync(owner);
        return result!;
    }

    public static bool IsContentDialogResultPrimary(object result)
    {
        return result is WpfContentDialogResult r && r == WpfContentDialogResult.Primary;
    }

    public static bool IsContentDialogResultSecondary(object result)
    {
        return result is WpfContentDialogResult r && r == WpfContentDialogResult.Secondary;
    }

    public static object GetContentDialogResultPrimary() => WpfContentDialogResult.Primary;
    public static object GetContentDialogButtonPrimary() => WpfContentDialogButton.Primary;
    public static object GetContentDialogButtonSecondary() => WpfContentDialogButton.Secondary;
    public static object GetContentDialogButtonClose() => WpfContentDialogButton.Close;

    /// <summary>
    /// 对对话框窗口应用 MaterialDesign 主题样式，使其与 ClassIsland 主界面风格一致，
    /// 避免出现 Windows 95 经典样式。
    /// </summary>
    public static void ApplyDialogTheme(Window dialog)
    {
        var paper = dialog.TryFindResource("MaterialDesignPaper") as Brush
                    ?? Application.Current?.TryFindResource("MaterialDesignPaper") as Brush;
        var body = dialog.TryFindResource("MaterialDesignBody") as Brush
                   ?? Application.Current?.TryFindResource("MaterialDesignBody") as Brush;
        var font = dialog.TryFindResource("HarmonyOsSans") as FontFamily
                   ?? Application.Current?.TryFindResource("HarmonyOsSans") as FontFamily;

        if (paper != null) dialog.Background = paper;
        if (body != null) dialog.Foreground = body;
        if (font != null) dialog.FontFamily = font;
        dialog.FontSize = 14;
        dialog.Focusable = true;

        if (body != null)
            TextElement.SetForeground(dialog, body);
    }

    /// <summary>
    /// 为对话框窗口安装鼠标滚轮兜底路由：若鼠标光标下的子元素存在 ScrollViewer，
    /// 但该 ScrollViewer 自身因焦点问题未收到滚轮事件，就把滚轮转发给它。
    /// 同时在 Loaded 时强制把键盘焦点交给第一个可滚动内容，避免滚轮完全失效。
    /// </summary>
    public static void WireDialogScrollSupport(Window dialog, ScrollViewer? primaryScroll)
    {
        dialog.Focusable = true;
        primaryScroll ??= FindVisualChild<ScrollViewer>(dialog.Content as DependencyObject);

        // 加载时：激活窗口 + 把键盘焦点交给主 ScrollViewer（若它支持聚焦）
        dialog.Loaded += (_, _) =>
        {
            dialog.Activate();
            dialog.Focus();
            if (primaryScroll != null)
            {
                primaryScroll.Focusable = true;
                if (primaryScroll.Content is IInputElement ie)
                {
                    System.Windows.Input.Keyboard.Focus(ie);
                }
                else
                {
                    System.Windows.Input.Keyboard.Focus(primaryScroll);
                }
            }
        };

        // 兜底：在 PreviewMouseWheel 上，找到光标下最近的 ScrollViewer 并执行 LineUp/LineDown
        dialog.PreviewMouseWheel += (sender, e) =>
        {
            if (e.Handled) return;
            var point = e.GetPosition(dialog);
            if (VisualTreeHelper.HitTest(dialog, point) is HitTestResult hit
                && hit.VisualHit is DependencyObject hitDo)
            {
                var target = FindVisualAncestorOrSelf<ScrollViewer>(hitDo) ?? primaryScroll;
                if (target != null && target.ComputedVerticalScrollBarVisibility != Visibility.Collapsed)
                {
                    int count = Math.Max(1, Math.Abs(e.Delta) / 48);
                    for (int i = 0; i < count; i++)
                    {
                        if (e.Delta > 0) target.LineUp();
                        else target.LineDown();
                    }
                    e.Handled = true;
                }
            }
        };
    }

    /// <summary>查找视觉树子元素</summary>
    private static T? FindVisualChild<T>(DependencyObject? obj) where T : DependencyObject
    {
        if (obj == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t) return t;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    /// <summary>查找视觉树祖先或自身</summary>
    private static T? FindVisualAncestorOrSelf<T>(DependencyObject? obj) where T : DependencyObject
    {
        while (obj != null)
        {
            if (obj is T t) return t;
            obj = VisualTreeHelper.GetParent(obj);
        }
        return null;
    }

    // ==================== SettingsExpander ====================

    public static FrameworkElement CreateSettingsExpander()
    {
        return new WpfSettingsExpander();
    }

    public static void SetSettingsExpanderProperty(FrameworkElement expander, string propertyName, object value)
    {
        if (expander == null) return;
        var property = expander.GetType().GetProperty(propertyName);
        if (property != null)
        {
            try
            {
                property.SetValue(expander, value);
            }
            catch
            {
            }
        }
    }

    public static FrameworkElement CreateSettingsExpanderItem()
    {
        return new WpfSettingsExpanderItem();
    }

    public static void SetSettingsExpanderItemProperty(FrameworkElement item, string propertyName, object value)
    {
        if (item == null) return;
        var property = item.GetType().GetProperty(propertyName);
        if (property != null)
        {
            try
            {
                property.SetValue(item, value);
            }
            catch
            {
            }
        }
    }

    public static void AddSettingsExpanderClickHandler(FrameworkElement expander, EventHandler handler)
    {
        if (expander is WpfSettingsExpander wpfExpander)
        {
            wpfExpander.Click += handler;
        }
    }

    public static object CreateSymbolIconSource(string symbolName)
    {
        try
        {
            if (Enum.TryParse<PackIconKind>(symbolName, true, out var kind))
            {
                return new PackIcon { Kind = kind, Width = 24, Height = 24 };
            }
        }
        catch
        {
        }
        return new PackIcon { Kind = PackIconKind.SettingsOutline, Width = 24, Height = 24 };
    }

    // ==================== 导航 ====================

    public static void NavigateBack(FrameworkElement control)
    {
        if (control == null) return;

        try
        {
            var p = VisualTreeHelper.GetParent(control) as DependencyObject;
            while (p != null)
            {
                if (p is Frame frame)
                {
                    if (frame.CanGoBack)
                    {
                        frame.GoBack();
                        return;
                    }
                }
                p = VisualTreeHelper.GetParent(p);
            }
        }
        catch
        {
        }
    }

    // ==================== 事件适配 ====================

    public static void AddLostFocusHandler(FrameworkElement element, RoutedEventHandler handler)
    {
        if (element == null || handler == null) return;
        element.LostFocus += handler;
    }

    public static void AddCheckedHandler(ToggleButton toggleButton, RoutedEventHandler handler)
    {
        if (toggleButton == null || handler == null) return;
        toggleButton.Checked += handler;
    }

    public static void AddUncheckedHandler(ToggleButton toggleButton, RoutedEventHandler handler)
    {
        if (toggleButton == null || handler == null) return;
        toggleButton.Unchecked += handler;
    }
}

// ==================== 自绘兼容控件 ====================

/// <summary>
/// 简单信息条控件（兼容原 FA InfoBar 用法）。
/// </summary>
public class WpfInfoBar : Border
{
    public const int SeverityInformational = 0;
    public const int SeveritySuccess = 1;
    public const int SeverityWarning = 2;
    public const int SeverityError = 3;

    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(nameof(Severity), typeof(int), typeof(WpfInfoBar),
            new PropertyMetadata(SeverityInformational, OnSeverityChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(WpfInfoBar),
            new PropertyMetadata("", OnContentChanged));

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(WpfInfoBar),
            new PropertyMetadata("", OnContentChanged));

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(WpfInfoBar),
            new PropertyMetadata(false, OnIsOpenChanged));

    public static readonly DependencyProperty IsClosableProperty =
        DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(WpfInfoBar),
            new PropertyMetadata(true));

    private readonly Grid _root;
    private readonly TextBlock _titleText;
    private readonly TextBlock _messageText;
    private readonly Button _closeButton;

    public event EventHandler? Closed;

    public int Severity
    {
        get => (int)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public WpfInfoBar()
    {
        Margin = new Thickness(0, 0, 0, 8);
        CornerRadius = new CornerRadius(4);
        Padding = new Thickness(10, 6, 10, 6);
        BorderThickness = new Thickness(1);

        _root = new Grid();
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new PackIcon
        {
            Kind = PackIconKind.InformationOutline,
            Width = 18,
            Height = 18,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        _root.Children.Add(icon);

        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _titleText = new TextBlock { FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap };
        _messageText = new TextBlock { TextWrapping = TextWrapping.Wrap };
        textPanel.Children.Add(_titleText);
        textPanel.Children.Add(_messageText);
        Grid.SetColumn(textPanel, 1);
        _root.Children.Add(textPanel);

        _closeButton = new Button
        {
            Content = "✕",
            Width = 22,
            Height = 22,
            Padding = new Thickness(0, 0, 0, 0),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };
        _closeButton.Click += (_, _) => OnClosed();
        Grid.SetColumn(_closeButton, 2);
        _root.Children.Add(_closeButton);

        Child = _root;
        ApplySeverity(Severity);
        RefreshVisibility();
    }

    private void OnClosed()
    {
        IsOpen = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnContentChanged()
    {
        _titleText.Text = Title;
        _messageText.Text = Message;
    }

    private void OnIsOpenChanged()
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        Visibility = IsOpen ? Visibility.Visible : Visibility.Collapsed;
        if (_closeButton != null)
        {
            _closeButton.Visibility = IsClosable ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ApplySeverity(int severity)
    {
        Brush background;
        Brush border;
        Brush foreground = Brushes.White;
        switch (severity)
        {
            case SeverityWarning:
                background = new SolidColorBrush(ThemeHelper.ParseColor("#FFF4E5"));
                border = new SolidColorBrush(ThemeHelper.ParseColor("#FFC107"));
                foreground = new SolidColorBrush(ThemeHelper.ParseColor("#7A5B00"));
                break;
            case SeverityError:
                background = new SolidColorBrush(ThemeHelper.ParseColor("#FDECEA"));
                border = new SolidColorBrush(ThemeHelper.ParseColor("#F44336"));
                foreground = new SolidColorBrush(ThemeHelper.ParseColor("#8B1A1A"));
                break;
            case SeveritySuccess:
                background = new SolidColorBrush(ThemeHelper.ParseColor("#E8F5E9"));
                border = new SolidColorBrush(ThemeHelper.ParseColor("#4CAF50"));
                foreground = new SolidColorBrush(ThemeHelper.ParseColor("#1B5E20"));
                break;
            default:
                background = new SolidColorBrush(ThemeHelper.ParseColor("#E3F2FD"));
                border = new SolidColorBrush(ThemeHelper.ParseColor("#2196F3"));
                foreground = new SolidColorBrush(ThemeHelper.ParseColor("#0D47A1"));
                break;
        }
        if (ThemeHelper.IsDarkTheme())
        {
            background = new SolidColorBrush(ThemeHelper.ParseColor("#3A3A3A"));
            border = new SolidColorBrush(ThemeHelper.ParseColor("#5A5A5A"));
            foreground = Brushes.White;
        }
        Background = background;
        BorderBrush = border;
        _titleText.Foreground = foreground;
        _messageText.Foreground = foreground;
    }

    private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WpfInfoBar infoBar)
        {
            infoBar.ApplySeverity((int)e.NewValue);
        }
    }

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WpfInfoBar infoBar)
        {
            infoBar.OnContentChanged();
        }
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WpfInfoBar infoBar)
        {
            infoBar.OnIsOpenChanged();
        }
    }
}

/// <summary>
/// 内容对话框结果。
/// </summary>
public enum WpfContentDialogResult
{
    None = 0,
    Primary = 1,
    Secondary = 2,
    Cancel = 3
}

/// <summary>
/// 内容对话框按钮。
/// </summary>
public enum WpfContentDialogButton
{
    None = 0,
    Primary = 1,
    Secondary = 2,
    Close = 3
}

/// <summary>
/// 简单内容对话框控件（兼容原 FA ContentDialog 用法）。
/// </summary>
public class WpfContentDialog
{
    public string Title { get; set; } = "";
    public object? Content { get; set; }
    public string PrimaryButtonText { get; set; } = "";
    public string SecondaryButtonText { get; set; } = "";
    public string CloseButtonText { get; set; } = "";
    public object? DefaultButton { get; set; }

    public event EventHandler? PrimaryButtonClick;
    public event EventHandler? SecondaryButtonClick;
    public event EventHandler? CloseButtonClick;

    private TaskCompletionSource<WpfContentDialogResult>? _tcs;

    public Task<WpfContentDialogResult> ShowAsync(Window? owner)
    {
        _tcs = new TaskCompletionSource<WpfContentDialogResult>();

        var paper = Application.Current?.TryFindResource("MaterialDesignPaper") as Brush;
        var body = Application.Current?.TryFindResource("MaterialDesignBody") as Brush;
        var font = Application.Current?.TryFindResource("HarmonyOsSans") as FontFamily;
        var raisedButtonStyle = Application.Current?.TryFindResource("MaterialDesignRaisedButton") as Style;
        var flatButtonStyle = Application.Current?.TryFindResource("MaterialDesignFlatButton") as Style;

        var window = new Window
        {
            Title = Title,
            Width = 420,
            Height = 280,
            MinWidth = 360,
            MinHeight = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Background = paper,
            FontFamily = font,
            FontSize = 14
        };

        TextElement.SetForeground(window, body ?? Brushes.Black);

        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(20, 16, 20, 16),
            Background = paper
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = Title,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap,
            Foreground = body ?? Brushes.Black
        };
        Grid.SetRow(titleText, 0);
        root.Children.Add(titleText);

        var contentScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = Content
        };
        Grid.SetRow(contentScroll, 1);
        root.Children.Add(contentScroll);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var closeButton = new Button
        {
            Content = string.IsNullOrEmpty(CloseButtonText) ? "关闭" : CloseButtonText,
            Margin = new Thickness(8, 0, 0, 0),
            MinWidth = 72,
            Padding = new Thickness(16, 6, 16, 6),
            Style = raisedButtonStyle
        };
        closeButton.Click += (_, _) =>
        {
            CloseButtonClick?.Invoke(this, EventArgs.Empty);
            window.Close();
            _tcs.TrySetResult(WpfContentDialogResult.Cancel);
        };
        buttonPanel.Children.Add(closeButton);

        if (!string.IsNullOrEmpty(SecondaryButtonText))
        {
            var secondaryButton = new Button
            {
                Content = SecondaryButtonText,
                Margin = new Thickness(8, 0, 0, 0),
                MinWidth = 72,
                Padding = new Thickness(16, 6, 16, 6),
                Style = raisedButtonStyle
            };
            secondaryButton.Click += (_, _) =>
            {
                SecondaryButtonClick?.Invoke(this, EventArgs.Empty);
                window.Close();
                _tcs.TrySetResult(WpfContentDialogResult.Secondary);
            };
            buttonPanel.Children.Add(secondaryButton);
        }

        if (!string.IsNullOrEmpty(PrimaryButtonText))
        {
            var primaryButton = new Button
            {
                Content = PrimaryButtonText,
                Margin = new Thickness(8, 0, 0, 0),
                MinWidth = 72,
                IsDefault = true,
                Padding = new Thickness(16, 6, 16, 6),
                Style = flatButtonStyle
            };
            primaryButton.Click += (_, _) =>
            {
                PrimaryButtonClick?.Invoke(this, EventArgs.Empty);
                window.Close();
                _tcs.TrySetResult(WpfContentDialogResult.Primary);
            };
            buttonPanel.Children.Add(primaryButton);
        }

        Grid.SetRow(buttonPanel, 2);
        root.Children.Add(buttonPanel);

        border.Child = root;
        window.Content = border;
        window.Closed += (_, _) => _tcs.TrySetResult(WpfContentDialogResult.None);

        // 主题 + 鼠标滚轮兜底（焦点 + PreviewMouseWheel 转发给 ScrollViewer）
        FluentAvaloniaCompatibilityHelper.ApplyDialogTheme(window);
        FluentAvaloniaCompatibilityHelper.WireDialogScrollSupport(window, contentScroll);

        if (owner != null)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        window.ShowDialog();

        return _tcs.Task;
    }
}

/// <summary>
/// 设置展开器控件（兼容原 FA SettingsExpander 用法）。
/// </summary>
public class WpfSettingsExpander : Border
{
    private readonly StackPanel _rootPanel;
    private readonly Grid _headerPanel;
    private readonly TextBlock _headerText;
    private readonly TextBlock _descriptionText;
    private readonly ContentControl _iconHost;
    private readonly ItemsControl _itemsControl;
    private readonly ContentControl _footerHost;
    private readonly ToggleButton _expandToggle;

    public event EventHandler? Click;

    public ObservableCollection<object> Items { get; } = new();

    public object? Header
    {
        get => _headerText.Text;
        set => _headerText.Text = value?.ToString() ?? "";
    }

    public object? Description
    {
        get => _descriptionText.Text;
        set => _descriptionText.Text = value?.ToString() ?? "";
    }

    public object? IconSource
    {
        get => _iconHost.Content;
        set => _iconHost.Content = value;
    }

    public object? Footer
    {
        get => _footerHost.Content;
        set => _footerHost.Content = value;
    }

    public bool IsExpanded
    {
        get => _expandToggle.IsChecked == true;
        set => _expandToggle.IsChecked = value;
    }

    public WpfSettingsExpander()
    {
        Margin = new Thickness(0, 0, 0, 12);
        CornerRadius = new CornerRadius(8);
        BorderThickness = new Thickness(0);
        Padding = new Thickness(16, 8, 16, 8);
        SetResourceReference(BackgroundProperty, "MaterialDesignCardBackground");

        _rootPanel = new StackPanel();

        _headerPanel = new Grid();
        _headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _iconHost = new ContentControl { Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_iconHost, 0);
        _headerPanel.Children.Add(_iconHost);

        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _headerText = new TextBlock { FontSize = 14, FontWeight = FontWeights.SemiBold };
        _headerText.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
        _descriptionText = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap };
        _descriptionText.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBodyLight");
        textPanel.Children.Add(_headerText);
        textPanel.Children.Add(_descriptionText);
        Grid.SetColumn(textPanel, 1);
        _headerPanel.Children.Add(textPanel);

        _expandToggle = new ToggleButton
        {
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(4, 0, 4, 0),
            IsChecked = true,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Cursor = Cursors.Hand
        };
        _expandToggle.Content = new PackIcon { Kind = PackIconKind.ChevronDown, Width = 16, Height = 16 };
        ((PackIcon)_expandToggle.Content).SetResourceReference(TextElement.ForegroundProperty, "MaterialDesignBody");
        _expandToggle.Checked += (_, _) => UpdateExpandVisual();
        _expandToggle.Unchecked += (_, _) => UpdateExpandVisual();
        Grid.SetColumn(_expandToggle, 2);
        _headerPanel.Children.Add(_expandToggle);

        _rootPanel.Children.Add(_headerPanel);

        _itemsControl = new ItemsControl { ItemsSource = Items };
        _rootPanel.Children.Add(_itemsControl);

        _footerHost = new ContentControl { HorizontalAlignment = HorizontalAlignment.Right };
        _rootPanel.Children.Add(_footerHost);

        Child = _rootPanel;
    }

    private void UpdateExpandVisual()
    {
        _itemsControl.Visibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
        _footerHost.Visibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
        _expandToggle.Content = new PackIcon
        {
            Kind = IsExpanded ? PackIconKind.ChevronDown : PackIconKind.ChevronRight,
            Width = 16,
            Height = 16
        };
        ((PackIcon)_expandToggle.Content).SetResourceReference(TextElement.ForegroundProperty, "MaterialDesignBody");
    }
}

/// <summary>
/// 设置展开器条目控件（兼容原 FA SettingsExpanderItem 用法）。
/// </summary>
public class WpfSettingsExpanderItem : Border
{
    private readonly TextBlock _contentText;
    private readonly TextBlock _descriptionText;
    private readonly ContentControl _footerHost;

    public object? Content
    {
        get => _contentText.Text;
        set
        {
            if (value is string s)
            {
                _contentText.Text = s;
            }
            else if (value is FrameworkElement fe)
            {
                _contentText.Visibility = Visibility.Collapsed;
                _footerHost.Visibility = Visibility.Collapsed;
            }
        }
    }

    public object? Description
    {
        get => _descriptionText.Text;
        set => _descriptionText.Text = value?.ToString() ?? "";
    }

    public object? Footer
    {
        get => _footerHost.Content;
        set => _footerHost.Content = value;
    }

    public WpfSettingsExpanderItem()
    {
        Margin = new Thickness(0, 4, 0, 4);
        Padding = new Thickness(0, 6, 0, 6);
        BorderBrush = ThemeHelper.GetSeparatorBrush();
        BorderThickness = new Thickness(0, 0, 0, 0.5);
        Background = Brushes.Transparent;

        var panel = new Grid();
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _contentText = new TextBlock { FontSize = 13 };
        _contentText.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
        _descriptionText = new TextBlock { FontSize = 11, TextWrapping = TextWrapping.Wrap };
        _descriptionText.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBodyLight");
        textPanel.Children.Add(_contentText);
        textPanel.Children.Add(_descriptionText);
        Grid.SetColumn(textPanel, 0);
        panel.Children.Add(textPanel);

        _footerHost = new ContentControl { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_footerHost, 1);
        panel.Children.Add(_footerHost);

        Child = panel;
    }
}
