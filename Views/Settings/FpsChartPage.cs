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

        var warningBar = new InfoBar
        {
            Severity = InfoBarSeverity.Error,
            Message = "帧率折线视图不是炒股，与任何股票及其内容无任何关联，也不代表与反映任何股票的涨跌情况；仅供调试，请勿用于教学环境！！！",
            IsOpen = true,
            IsClosable = true,
            Margin = new Thickness(0, 0, 0, 8)
        };
        mainPanel.Children.Add(warningBar);

        _titleTextBlock = new TextBlock
        {
            Text = "帧率折线图",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        };
        mainPanel.Children.Add(_titleTextBlock);

        _descriptionTextBlock = new TextBlock
        {
            Text = "采样频率与主界面帧率组件一致。横轴为时间，纵轴为帧率（从0开始动态扩展）。\n" +
                   "fps: 当前帧率 | max: 10秒内最高帧率 | avg: 10秒内平均帧率 | min: 10秒内最低帧率 | 1%low: 10秒内最低1%帧率平均值\n" +
                   "支持左右滚动查看历史数据，数据保留至应用重启。",
            FontSize = 12,
            Foreground = Brushes.Gray,
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

        _statusTextBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = Brushes.Gray,
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

        Content = scrollViewer;
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
            var csvData = _viewModel.GetCsvData();
            await using var stream = await file.OpenWriteAsync();
            var bom = Encoding.UTF8.GetPreamble();
            await stream.WriteAsync(bom);
            var bytes = Encoding.UTF8.GetBytes(csvData);
            await stream.WriteAsync(bytes);

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
            _titleTextBlock.Foreground = Brushes.White;

        if (_descriptionTextBlock != null)
            _descriptionTextBlock.Foreground = Brushes.Gray;

        if (_statusTextBlock != null)
            _statusTextBlock.Foreground = Brushes.Gray;
    }
}
