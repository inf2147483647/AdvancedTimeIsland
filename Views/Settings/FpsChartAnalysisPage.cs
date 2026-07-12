using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using AdvancedTimeIsland.Views.Controls;
using AdvancedTimeIsland.Helpers;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;

namespace AdvancedTimeIsland.Views.Settings;

[SettingsPageInfo("AdvancedTimeIslandFpsChartAnalysis", "帧率折线图分析", SettingsPageCategory.Debug)]
public class FpsChartAnalysisPage : SettingsPageBase
{
    private FpsChartAnalysisControl? _chartControl;
    private TextBlock? _titleTextBlock;
    private TextBlock? _descriptionTextBlock;
    private TextBlock? _statusTextBlock;
    private Button? _importButton;
    private Button? _clearButton;

    public FpsChartAnalysisPage()
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
            Text = "帧率折线图分析",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = ThemeHelper.GetTextBrush()
        };
        mainPanel.Children.Add(_titleTextBlock);

        _descriptionTextBlock = new TextBlock
        {
            Text = "导入CSV文件生成帧率折线图进行分析。\n" +
                   "CSV格式：Time,fps,max,avg,min,1%min\n" +
                   "支持左右滚动查看数据，支持缩放。",
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

        _importButton = new Button
        {
            Content = "导入CSV",
            Padding = new Thickness(16, 8, 16, 8)
        };
        _importButton.Click += OnImportClick;
        toolbar.Children.Add(_importButton);

        _clearButton = new Button
        {
            Content = "清空图表",
            Padding = new Thickness(16, 8, 16, 8)
        };
        _clearButton.Click += OnClearClick;
        toolbar.Children.Add(_clearButton);

        _statusTextBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = ThemeHelper.GetSubTextBrush(),
            VerticalAlignment = VerticalAlignment.Center
        };
        toolbar.Children.Add(_statusTextBlock);

        mainPanel.Children.Add(toolbar);

        _chartControl = new FpsChartAnalysisControl();
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

        var warningBar = FluentAvaloniaCompatibilityHelper.CreateInfoBar();
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Severity", FluentAvaloniaCompatibilityHelper.GetInfoBarSeverityWarning());
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Message", "帧率折线图分析仅供调试使用，用于分析历史帧率数据。");
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsOpen", true);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "IsClosable", false);
        FluentAvaloniaCompatibilityHelper.SetInfoBarProperty(warningBar, "Margin", new Thickness(16, 16, 16, 0));
        Grid.SetRow(warningBar, 0);
        rootGrid.Children.Add(warningBar);

        Grid.SetRow(scrollViewer, 1);
        rootGrid.Children.Add(scrollViewer);

        Content = rootGrid;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (_statusTextBlock != null) _statusTextBlock.Text = "请导入CSV文件";

        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }

        UpdateThemeColors();
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择CSV文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("CSV 文件")
                {
                    Patterns = new[] { "*.csv" }
                },
                new FilePickerFileType("所有文件")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files == null || files.Count == 0) return;
        var file = files[0];

        try
        {
            var (records, errorDetail) = await ParseCsvFile(file);
            _chartControl?.SetData(records);
            if (_statusTextBlock != null)
            {
                if (records.Count > 0)
                    _statusTextBlock.Text = $"已导入 {records.Count} 条记录";
                else
                    _statusTextBlock.Text = $"导入0条记录。{errorDetail}";
            }
        }
        catch (Exception ex)
        {
            if (_statusTextBlock != null) _statusTextBlock.Text = $"导入失败: {ex.Message}";
        }
    }

    private async Task<(List<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)> Records, string ErrorDetail)> ParseCsvFile(IStorageFile file)
    {
        var records = new List<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)>();

        using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

        var invariantCulture = System.Globalization.CultureInfo.InvariantCulture;
        var dateFormats = new[] { 
            "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss", 
            "yyyy/MM/dd HH:mm:ss.fff", "yyyy/MM/dd HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss.fff", "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss.fff", "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff", "yyyy-MM-ddTHH:mm:ss",
            "HH:mm:ss.fff", "HH:mm:ss", "HH:mm"
        };

        string? firstDataLine = null;
        int totalLines = 0;
        int skippedColumns = 0;
        int skippedDate = 0;
        int skippedNumber = 0;
        string? failReason = null;

        string? line;
        bool isFirstLine = true;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            totalLines++;

            if (isFirstLine)
            {
                isFirstLine = false;
                var lowerLine = line.ToLower();
                if (lowerLine.Contains("time") || lowerLine.Contains("fps") || lowerLine.Contains("header"))
                    continue;
            }

            if (firstDataLine == null)
                firstDataLine = line;

            // 自动检测分隔符
            char delimiter = ',';
            if (!line.Contains(',') && line.Contains(';'))
                delimiter = ';';
            else if (!line.Contains(',') && line.Contains('\t'))
                delimiter = '\t';

            var parts = line.Split(delimiter);
            if (parts.Length < 6)
            {
                skippedColumns++;
                if (failReason == null)
                    failReason = $"列数不足({parts.Length}列，需要6列): {line.Substring(0, Math.Min(80, line.Length))}";
                continue;
            }

            var timeStr = parts[0].Trim().Trim('"');
            DateTime time;
            if (!DateTime.TryParseExact(timeStr, dateFormats, invariantCulture, System.Globalization.DateTimeStyles.None, out time) &&
                !DateTime.TryParse(timeStr, invariantCulture, System.Globalization.DateTimeStyles.None, out time) &&
                !DateTime.TryParse(timeStr, out time))
            {
                skippedDate++;
                if (failReason == null)
                    failReason = $"日期解析失败: '{parts[0].Trim()}'";
                continue;
            }

            if (time.Year < 1970)
            {
                var today = DateTime.Today;
                time = new DateTime(today.Year, today.Month, today.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
            }

            bool numberParseFailed = false;
            double fps = 0, max = 0, avg = 0, min = 0, low1 = 0;
            var values = new double[5];
            for (int i = 0; i < 5; i++)
            {
                var str = parts[i + 1].Trim().Trim('"');
                if (!double.TryParse(str, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, invariantCulture, out values[i]))
                {
                    // 尝试当前文化
                    if (!double.TryParse(str, out values[i]))
                    {
                        numberParseFailed = true;
                        if (failReason == null)
                            failReason = $"数值解析失败: '{str}' (第{i + 2}列)";
                        break;
                    }
                }
            }

            if (numberParseFailed)
            {
                skippedNumber++;
                continue;
            }

            fps = values[0]; max = values[1]; avg = values[2]; min = values[3]; low1 = values[4];

            records.Add((time, fps, max, avg, min, low1));
        }

        string errorDetail = "";
        if (records.Count == 0 && totalLines > 0)
        {
            errorDetail = $"共{totalLines}行，列数不足{skippedColumns}行，日期失败{skippedDate}行，数值失败{skippedNumber}行。";
            if (failReason != null)
                errorDetail += $" 首个失败原因: {failReason}";
            if (firstDataLine != null)
                errorDetail += $" 首行数据: {firstDataLine.Substring(0, Math.Min(100, firstDataLine.Length))}";
        }

        return (records, errorDetail);
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        _chartControl?.SetData(new List<(DateTime Time, double Fps, double Max, double Avg, double Min, double Low1)>());
        if (_statusTextBlock != null) _statusTextBlock.Text = "已清空图表";
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _chartControl?.Dispose();

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
