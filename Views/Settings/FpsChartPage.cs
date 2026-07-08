using System;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using AdvancedTimeIsland.ViewModels.Settings;
using AdvancedTimeIsland.Views.Controls;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandFpsChart", "帧率折线图", SettingsPageCategory.Debug)]
public class FpsChartPage : SettingsPageBase
{
    private FpsChartViewModel? _viewModel;
    private FpsChartControl? _chartControl;
    private TextBlock? _titleTextBlock;
    private TextBlock? _descriptionTextBlock;
    private TextBlock? _statusTextBlock;
    private Button? _exportButton;
    private DispatcherTimer? _statusTimer;

    public FpsChartPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(16),
            Spacing = 12
        };

        

        _titleTextBlock = new TextBlock
        {
            Text = "帧率折线图",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        mainPanel.Children.Add(_titleTextBlock);

        _descriptionTextBlock = new TextBlock
        {
            Text = "采样频率与主界面帧率组件一致。横轴为时间，纵轴为帧率（从0开始动态扩展）。\n" +
                   "fps: 当前帧率 | max: 10秒内最高帧率 | avg: 10秒内平均帧率 | min: 10秒内最低帧率 | 1%low: 10秒内最低1%帧率平均值\n" +
                   "支持左右滚动查看历史数据，数据保留至应用重启。",
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            TextWrapping = TextWrapping.Wrap
        };
        mainPanel.Children.Add(_descriptionTextBlock);

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        _exportButton = new Button
        {
            Content = "导出到 CSV",
            Padding = new Thickness(16, 8, 16, 8)
        };
        _exportButton.Click += OnExportClick;
        toolbar.Children.Add(_exportButton);

        var clearButton = new Button
        {
            Content = "清空折线图",
            Padding = new Thickness(16, 8, 16, 8)
        };
        clearButton.Click += OnClearClick;
        toolbar.Children.Add(clearButton);

        _statusTextBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        toolbar.Children.Add(_statusTextBlock);

        mainPanel.Children.Add(toolbar);

        _chartControl = new FpsChartControl();
        mainPanel.Children.Add(_chartControl.GetContent());

        var scrollViewer = new ScrollViewer
        {
            Content = mainPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var warningBar = new InfoBar
        {
            Severity = InfoBarSeverity.Error,
            Message = "帧率折线视图不是炒股，与任何股票及其内容无任何关联，也不代表与反映任何股票的涨跌情况；仅供调试，请勿用于教学环境！！！",
            IsOpen = true,
            IsClosable = false,
            Margin = new Thickness(16, 16, 16, 0)
        };
        Grid.SetRow(warningBar, 0);
        rootGrid.Children.Add(warningBar);

        Grid.SetRow(scrollViewer, 1);
        rootGrid.Children.Add(scrollViewer);

        Content = rootGrid;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _viewModel = new FpsChartViewModel();
        DataContext = _viewModel;

        UpdateStatus();

        _statusTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1),
            DispatcherPriority.Background,
            (s, e) => UpdateStatus());
        _statusTimer.Start();

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        UpdateThemeColors();
    }

    private void UpdateStatus()
    {
        if (_statusTextBlock != null && _viewModel != null)
        {
            _statusTextBlock.Text = $"已采样 {_viewModel.RecordCount} 条记录";
        }
    }

    private async void OnClearClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "确认清空",
            Content = "确定要清空折线图数据吗？此操作不可撤销，清空后将从头开始采集数据。",
            PrimaryButtonText = "确定",
            CloseButtonText = "取消"
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            FpsSampler.Clear();
            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = "已清空折线图数据，开始重新采集";
            }
        }
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出帧率数据到 CSV",
            DefaultExtension = "csv",
            SuggestedFileName = $"fps_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV 文件")
                {
                    Patterns = new[] { "*.csv" }
                }
            }
        });

        if (file == null) return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            var bom = Encoding.UTF8.GetPreamble();
            await stream.WriteAsync(bom);
            _viewModel.WriteCsvData(stream);

            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = $"已导出 {_viewModel.RecordCount} 条记录到 {file.Name}";
            }
        }
        catch (Exception ex)
        {
            if (_statusTextBlock != null)
            {
                _statusTextBlock.Text = $"导出失败: {ex.Message}";
            }
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _statusTimer?.Stop();
        _statusTimer = null;

        _chartControl?.Dispose();
        (_viewModel as IDisposable)?.Dispose();

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void UpdateThemeColors()
    {
        if (_titleTextBlock != null)
            _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();

        if (_descriptionTextBlock != null)
            _descriptionTextBlock.Foreground = ThemeHelper.GetSubTextBrush();

        if (_statusTextBlock != null)
            _statusTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
    }
}
