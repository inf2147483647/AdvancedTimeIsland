using System;
using System.ComponentModel;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Views.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class SunriseSunsetSettingsControl : ComponentBase<SunriseSunsetSettings>
{
    private readonly PluginSettings? _pluginSettings;

    private TextBlock _titleTextBlock;
    private TextBlock _descTextBlock;

    private TextBox _longitudeTextBox;
    private TextBox _longitudeDmsDegreesTextBox;
    private TextBox _longitudeDmsMinutesTextBox;
    private TextBox _longitudeDmsSecondsTextBox;
    private ComboBox _longitudeDmsDirectionComboBox;
    private Panel _longitudeDmsPanel;
    private TextBlock _longitudeLabelTextBlock;
    private TextBlock _dmsDegreeSymbol;
    private TextBlock _dmsMinuteSymbol;
    private TextBlock _dmsSecondSymbol;

    private TextBox _latitudeTextBox;
    private TextBox _latitudeDmsDegreesTextBox;
    private TextBox _latitudeDmsMinutesTextBox;
    private TextBox _latitudeDmsSecondsTextBox;
    private ComboBox _latitudeDmsDirectionComboBox;
    private Panel _latitudeDmsPanel;
    private TextBlock _latitudeLabelTextBlock;
    private TextBlock _latDmsDegreeSymbol;
    private TextBlock _latDmsMinuteSymbol;
    private TextBlock _latDmsSecondSymbol;

    private Button _getLocationButton;
    private TextBlock _statusText;

    private ComboBox _timeZoneComboBox;
    private Button _getTimeZoneButton;
    private TextBlock _timeZoneLabelTextBlock;

    private CheckBox? _sunriseLabelEnableCustomFontSizeToggle;
    private CheckBox? _sunriseLabelEnableCustomFontColorToggle;
    private CheckBox? _sunriseTimeEnableCustomFontSizeToggle;
    private CheckBox? _sunriseTimeEnableCustomFontColorToggle;
    private CheckBox? _sunsetLabelEnableCustomFontSizeToggle;
    private CheckBox? _sunsetLabelEnableCustomFontColorToggle;
    private CheckBox? _sunsetTimeEnableCustomFontSizeToggle;
    private CheckBox? _sunsetTimeEnableCustomFontColorToggle;

    private ColorPicker _sunriseLabelColorPicker;
    private NumericUpDown _sunriseLabelSizeNumericUpDown;

    private ColorPicker _sunriseTimeColorPicker;
    private NumericUpDown _sunriseTimeSizeNumericUpDown;

    private ColorPicker _sunsetLabelColorPicker;
    private NumericUpDown _sunsetLabelSizeNumericUpDown;

    private ColorPicker _sunsetTimeColorPicker;
    private NumericUpDown _sunsetTimeSizeNumericUpDown;

    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();

    public SunriseSunsetSettingsControl() : this(null)
    {
    }

    public SunriseSunsetSettingsControl(PluginSettings? pluginSettings = null)
    {
        _pluginSettings = pluginSettings;
        if (_pluginSettings != null)
        {
            _pluginSettings.PropertyChanged += OnPluginSettingsPropertyChanged;
        }
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _titleTextBlock = new TextBlock { Text = "日出日落设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

        _descTextBlock = new TextBlock { Text = "配置日出日落时间显示选项", FontSize = 12, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_descTextBlock);

        // ==================== 经纬度设置 ====================
        var coordPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var longitudeRow = new Grid();
        longitudeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        longitudeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        longitudeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _longitudeLabelTextBlock = new TextBlock { Text = "经度:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_longitudeLabelTextBlock, 0);
        longitudeRow.Children.Add(_longitudeLabelTextBlock);

        var isDms = _pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms;

        _longitudeTextBox = new TextBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Left, IsVisible = !isDms };
        Grid.SetColumn(_longitudeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeTextBox, OnLongitudeLostFocus);
        longitudeRow.Children.Add(_longitudeTextBox);

        _longitudeDmsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Left, IsVisible = isDms };
        Grid.SetColumn(_longitudeDmsPanel, 1);

        _longitudeDmsDegreesTextBox = new TextBox { Width = 50, Watermark = "度" };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsDegreesTextBox, OnLongitudeDmsValueChanged);
        _longitudeDmsPanel.Children.Add(_longitudeDmsDegreesTextBox);
        _dmsDegreeSymbol = new TextBlock { Text = "°", VerticalAlignment = VerticalAlignment.Center };
        _longitudeDmsPanel.Children.Add(_dmsDegreeSymbol);

        _longitudeDmsMinutesTextBox = new TextBox { Width = 45, Watermark = "分" };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsMinutesTextBox, OnLongitudeDmsValueChanged);
        _longitudeDmsPanel.Children.Add(_longitudeDmsMinutesTextBox);
        _dmsMinuteSymbol = new TextBlock { Text = "′", VerticalAlignment = VerticalAlignment.Center };
        _longitudeDmsPanel.Children.Add(_dmsMinuteSymbol);

        _longitudeDmsSecondsTextBox = new TextBox { Width = 45, Watermark = "秒" };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsSecondsTextBox, OnLongitudeDmsValueChanged);
        _longitudeDmsPanel.Children.Add(_longitudeDmsSecondsTextBox);
        _dmsSecondSymbol = new TextBlock { Text = "″", VerticalAlignment = VerticalAlignment.Center };
        _longitudeDmsPanel.Children.Add(_dmsSecondSymbol);

        _longitudeDmsDirectionComboBox = new ComboBox { Width = 90 };
        _longitudeDmsDirectionComboBox.Items.Add("东经");
        _longitudeDmsDirectionComboBox.Items.Add("西经");
        _longitudeDmsDirectionComboBox.SelectedIndex = 0;
        _longitudeDmsDirectionComboBox.SelectionChanged += OnLongitudeDmsValueChanged;
        _longitudeDmsPanel.Children.Add(_longitudeDmsDirectionComboBox);

        longitudeRow.Children.Add(_longitudeDmsPanel);
        coordPanel.Children.Add(longitudeRow);

        var latitudeRow = new Grid();
        latitudeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        latitudeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _latitudeLabelTextBlock = new TextBlock { Text = "纬度:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_latitudeLabelTextBlock, 0);
        latitudeRow.Children.Add(_latitudeLabelTextBlock);

        _latitudeTextBox = new TextBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Left, IsVisible = !isDms };
        Grid.SetColumn(_latitudeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_latitudeTextBox, OnLatitudeLostFocus);
        latitudeRow.Children.Add(_latitudeTextBox);

        _latitudeDmsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Left, IsVisible = isDms };
        Grid.SetColumn(_latitudeDmsPanel, 1);

        _latitudeDmsDegreesTextBox = new TextBox { Width = 50, Watermark = "度" };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_latitudeDmsDegreesTextBox, OnLatitudeDmsValueChanged);
        _latitudeDmsPanel.Children.Add(_latitudeDmsDegreesTextBox);
        _latDmsDegreeSymbol = new TextBlock { Text = "°", VerticalAlignment = VerticalAlignment.Center };
        _latitudeDmsPanel.Children.Add(_latDmsDegreeSymbol);

        _latitudeDmsMinutesTextBox = new TextBox { Width = 45, Watermark = "分" };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_latitudeDmsMinutesTextBox, OnLatitudeDmsValueChanged);
        _latitudeDmsPanel.Children.Add(_latitudeDmsMinutesTextBox);
        _latDmsMinuteSymbol = new TextBlock { Text = "′", VerticalAlignment = VerticalAlignment.Center };
        _latitudeDmsPanel.Children.Add(_latDmsMinuteSymbol);

        _latitudeDmsSecondsTextBox = new TextBox { Width = 45, Watermark = "秒" };
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_latitudeDmsSecondsTextBox, OnLatitudeDmsValueChanged);
        _latitudeDmsPanel.Children.Add(_latitudeDmsSecondsTextBox);
        _latDmsSecondSymbol = new TextBlock { Text = "″", VerticalAlignment = VerticalAlignment.Center };
        _latitudeDmsPanel.Children.Add(_latDmsSecondSymbol);

        _latitudeDmsDirectionComboBox = new ComboBox { Width = 90 };
        _latitudeDmsDirectionComboBox.Items.Add("北纬");
        _latitudeDmsDirectionComboBox.Items.Add("南纬");
        _latitudeDmsDirectionComboBox.SelectedIndex = 0;
        _latitudeDmsDirectionComboBox.SelectionChanged += OnLatitudeDmsValueChanged;
        _latitudeDmsPanel.Children.Add(_latitudeDmsDirectionComboBox);

        latitudeRow.Children.Add(_latitudeDmsPanel);
        coordPanel.Children.Add(latitudeRow);

        _getLocationButton = new Button
        {
            Content = "获取当前位置",
            Padding = new Thickness(8, 4),
            Margin = new Thickness(0, 4, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _getLocationButton.Click += OnGetLocationClick;
        coordPanel.Children.Add(_getLocationButton);

        _statusText = new TextBlock { Text = "", FontSize = 11, TextWrapping = TextWrapping.Wrap };
        coordPanel.Children.Add(_statusText);

        sp.Children.Add(SettingsGroupFactory.Create("经纬度设置", coordPanel));

        // ==================== 时区设置 ====================
        var timeZonePanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var timeZoneRow = new Grid();
        timeZoneRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        timeZoneRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        timeZoneRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _timeZoneLabelTextBlock = new TextBlock { Text = "时区:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_timeZoneLabelTextBlock, 0);
        timeZoneRow.Children.Add(_timeZoneLabelTextBlock);

        _timeZoneComboBox = new ComboBox { Width = 200 };
        _timeZoneComboBox.Items.Add("跟随插件设置");
        foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
        {
            _timeZoneComboBox.Items.Add(tz);
        }
        _timeZoneComboBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, ns) => new TextBlock { Text = item is TimeZoneInfo tz ? tz.DisplayName : item?.ToString() ?? "" });
        _timeZoneComboBox.SelectionChanged += OnTimeZoneChanged;
        Grid.SetColumn(_timeZoneComboBox, 1);
        timeZoneRow.Children.Add(_timeZoneComboBox);

        _getTimeZoneButton = new Button
        {
            Content = "获取时区",
            Padding = new Thickness(6, 3),
            Margin = new Thickness(8, 0, 0, 0)
        };
        _getTimeZoneButton.Click += OnGetTimeZoneClick;
        Grid.SetColumn(_getTimeZoneButton, 2);
        timeZoneRow.Children.Add(_getTimeZoneButton);

        timeZonePanel.Children.Add(timeZoneRow);
        sp.Children.Add(SettingsGroupFactory.Create("时区设置", timeZonePanel));

        // ==================== 文案设置 ====================
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var fontStyleTableScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateFontStyleTable()
        };
        textPanel.Children.Add(fontStyleTableScroll);

        sp.Children.Add(SettingsGroupFactory.Create("文案设置", textPanel));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    private void UpdateThemeColors()
    {
        _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _descTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _longitudeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _latitudeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dmsDegreeSymbol.Foreground = ThemeHelper.GetTextBrush();
        _dmsMinuteSymbol.Foreground = ThemeHelper.GetTextBrush();
        _dmsSecondSymbol.Foreground = ThemeHelper.GetTextBrush();
        _latDmsDegreeSymbol.Foreground = ThemeHelper.GetTextBrush();
        _latDmsMinuteSymbol.Foreground = ThemeHelper.GetTextBrush();
        _latDmsSecondSymbol.Foreground = ThemeHelper.GetTextBrush();
        _statusText.Foreground = ThemeHelper.GetGrayBrush();
        _timeZoneLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _sunriseLabelEnableCustomFontSizeToggle!.Foreground = ThemeHelper.GetTextBrush();
        _sunriseLabelEnableCustomFontColorToggle!.Foreground = ThemeHelper.GetTextBrush();
        _sunriseTimeEnableCustomFontSizeToggle!.Foreground = ThemeHelper.GetTextBrush();
        _sunriseTimeEnableCustomFontColorToggle!.Foreground = ThemeHelper.GetTextBrush();
        _sunsetLabelEnableCustomFontSizeToggle!.Foreground = ThemeHelper.GetTextBrush();
        _sunsetLabelEnableCustomFontColorToggle!.Foreground = ThemeHelper.GetTextBrush();
        _sunsetTimeEnableCustomFontSizeToggle!.Foreground = ThemeHelper.GetTextBrush();
        _sunsetTimeEnableCustomFontColorToggle!.Foreground = ThemeHelper.GetTextBrush();

        foreach (var tb in _dynamicTextBlocks)
        {
            tb.Foreground = ThemeHelper.GetTextBrush();
        }

        var separatorBrush = ThemeHelper.GetSeparatorBrush();
        foreach (var border in _tableCellBorders)
        {
            border.BorderBrush = separatorBrush;
        }
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void OnSunriseLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.SunriseLabelEnableCustomFontSize = _sunriseLabelEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunriseLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.SunriseLabelEnableCustomFontColor = _sunriseLabelEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunriseTimeEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.SunriseTimeEnableCustomFontSize = _sunriseTimeEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunriseTimeEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.SunriseTimeEnableCustomFontColor = _sunriseTimeEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunsetLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.SunsetLabelEnableCustomFontSize = _sunsetLabelEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunsetLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.SunsetLabelEnableCustomFontColor = _sunsetLabelEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunsetTimeEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.SunsetTimeEnableCustomFontSize = _sunsetTimeEnableCustomFontSizeToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunsetTimeEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.SunsetTimeEnableCustomFontColor = _sunsetTimeEnableCustomFontColorToggle?.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    // ==================== 字体样式表格 ====================

    private Grid CreateFontStyleTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (int i = 0; i < 5; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "样式");
        AddTableHeader(grid, 0, 1, "自定义大小");
        AddTableHeader(grid, 0, 2, "自定义颜色");

        AddTableRowLabel(grid, 1, "日出标签");
        AddTableCell(grid, 1, 1, CreateSizeCell(out _sunriseLabelSizeNumericUpDown, out _sunriseLabelEnableCustomFontSizeToggle, OnSunriseLabelEnableCustomFontSizeChanged, OnSunriseLabelFontSizeChanged));
        AddTableCell(grid, 1, 2, CreateColorCell(out _sunriseLabelColorPicker, out _sunriseLabelEnableCustomFontColorToggle, OnSunriseLabelEnableCustomFontColorChanged, OnSunriseLabelColorChanged));

        AddTableRowLabel(grid, 2, "日出时间");
        AddTableCell(grid, 2, 1, CreateSizeCell(out _sunriseTimeSizeNumericUpDown, out _sunriseTimeEnableCustomFontSizeToggle, OnSunriseTimeEnableCustomFontSizeChanged, OnSunriseTimeFontSizeChanged));
        AddTableCell(grid, 2, 2, CreateColorCell(out _sunriseTimeColorPicker, out _sunriseTimeEnableCustomFontColorToggle, OnSunriseTimeEnableCustomFontColorChanged, OnSunriseTimeColorChanged));

        AddTableRowLabel(grid, 3, "日落标签");
        AddTableCell(grid, 3, 1, CreateSizeCell(out _sunsetLabelSizeNumericUpDown, out _sunsetLabelEnableCustomFontSizeToggle, OnSunsetLabelEnableCustomFontSizeChanged, OnSunsetLabelFontSizeChanged));
        AddTableCell(grid, 3, 2, CreateColorCell(out _sunsetLabelColorPicker, out _sunsetLabelEnableCustomFontColorToggle, OnSunsetLabelEnableCustomFontColorChanged, OnSunsetLabelColorChanged));

        AddTableRowLabel(grid, 4, "日落时间");
        AddTableCell(grid, 4, 1, CreateSizeCell(out _sunsetTimeSizeNumericUpDown, out _sunsetTimeEnableCustomFontSizeToggle, OnSunsetTimeEnableCustomFontSizeChanged, OnSunsetTimeFontSizeChanged));
        AddTableCell(grid, 4, 2, CreateColorCell(out _sunsetTimeColorPicker, out _sunsetTimeEnableCustomFontColorToggle, OnSunsetTimeEnableCustomFontColorChanged, OnSunsetTimeColorChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 5);
        Grid.SetColumnSpan(outerBorder, 3);
        _tableCellBorders.Add(outerBorder);
        grid.Children.Add(outerBorder);

        return grid;
    }

    private Border CreateCellBorder(Control child)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            Padding = new Thickness(6, 3, 6, 3),
            Child = child
        };
        _tableCellBorders.Add(border);
        return border;
    }

    private void AddTableHeader(Grid grid, int row, int col, string text)
    {
        var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(tb);
        var border = CreateCellBorder(tb);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private void AddTableRowLabel(Grid grid, int row, string text)
    {
        var tb = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
        _dynamicTextBlocks.Add(tb);
        var border = CreateCellBorder(tb);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, 0);
        grid.Children.Add(border);
    }

    private void AddTableCell(Grid grid, int row, int col, Control control)
    {
        var border = CreateCellBorder(control);
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        grid.Children.Add(border);
    }

    private static StackPanel CreateSizeCell(out NumericUpDown numericUpDown, out CheckBox? toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        numericUpDown = new NumericUpDown
        {
            Width = 120,
            Minimum = 1,
            Maximum = 72,
            Increment = 1m,
            FormatString = "0.00",
            VerticalAlignment = VerticalAlignment.Center
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        panel.Children.Add(numericUpDown);
        return panel;
    }

    private static StackPanel CreateColorCell(out ColorPicker colorPicker, out CheckBox? toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<ColorChangedEventArgs> colorChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        colorPicker = new ColorPicker { Width = 120, VerticalAlignment = VerticalAlignment.Center };
        colorPicker.ColorChanged += colorChangedHandler;
        panel.Children.Add(colorPicker);
        return panel;
    }

    private void UpdateControlsEnabled()
    {
        _sunriseLabelColorPicker.IsEnabled = Settings.SunriseLabelEnableCustomFontColor;
        _sunriseLabelSizeNumericUpDown.IsEnabled = Settings.SunriseLabelEnableCustomFontSize;
        _sunriseTimeColorPicker.IsEnabled = Settings.SunriseTimeEnableCustomFontColor;
        _sunriseTimeSizeNumericUpDown.IsEnabled = Settings.SunriseTimeEnableCustomFontSize;
        _sunsetLabelColorPicker.IsEnabled = Settings.SunsetLabelEnableCustomFontColor;
        _sunsetLabelSizeNumericUpDown.IsEnabled = Settings.SunsetLabelEnableCustomFontSize;
        _sunsetTimeColorPicker.IsEnabled = Settings.SunsetTimeEnableCustomFontColor;
        _sunsetTimeSizeNumericUpDown.IsEnabled = Settings.SunsetTimeEnableCustomFontSize;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();

        _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(Settings.Longitude);
        UpdateLongitudeDmsFromValue();
        _latitudeTextBox.Text = LatitudeConverter.ToDecimalString(Settings.Latitude);
        UpdateLatitudeDmsFromValue();

        var isDms = _pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms;
        _longitudeTextBox.IsVisible = !isDms;
        _longitudeDmsPanel.IsVisible = isDms;
        _latitudeTextBox.IsVisible = !isDms;
        _latitudeDmsPanel.IsVisible = isDms;

        if (string.IsNullOrEmpty(Settings.TimeZoneId))
        {
            _timeZoneComboBox.SelectedIndex = 0;
        }
        else
        {
            foreach (var item in _timeZoneComboBox.Items)
            {
                if (item is TimeZoneInfo tz && tz.Id == Settings.TimeZoneId)
                {
                    _timeZoneComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        _sunriseLabelEnableCustomFontSizeToggle.IsChecked = Settings.SunriseLabelEnableCustomFontSize;
        _sunriseLabelEnableCustomFontColorToggle.IsChecked = Settings.SunriseLabelEnableCustomFontColor;
        _sunriseTimeEnableCustomFontSizeToggle.IsChecked = Settings.SunriseTimeEnableCustomFontSize;
        _sunriseTimeEnableCustomFontColorToggle.IsChecked = Settings.SunriseTimeEnableCustomFontColor;
        _sunsetLabelEnableCustomFontSizeToggle.IsChecked = Settings.SunsetLabelEnableCustomFontSize;
        _sunsetLabelEnableCustomFontColorToggle.IsChecked = Settings.SunsetLabelEnableCustomFontColor;
        _sunsetTimeEnableCustomFontSizeToggle.IsChecked = Settings.SunsetTimeEnableCustomFontSize;
        _sunsetTimeEnableCustomFontColorToggle.IsChecked = Settings.SunsetTimeEnableCustomFontColor;
        UpdateControlsEnabled();

        _sunriseLabelColorPicker.Color = ParseColor(Settings.SunriseLabelFontColor);
        _sunriseLabelSizeNumericUpDown.Value = (decimal)Settings.SunriseLabelFontSize;
        _sunriseTimeColorPicker.Color = ParseColor(Settings.SunriseTimeFontColor);
        _sunriseTimeSizeNumericUpDown.Value = (decimal)Settings.SunriseTimeFontSize;
        _sunsetLabelColorPicker.Color = ParseColor(Settings.SunsetLabelFontColor);
        _sunsetLabelSizeNumericUpDown.Value = (decimal)Settings.SunsetLabelFontSize;
        _sunsetTimeColorPicker.Color = ParseColor(Settings.SunsetTimeFontColor);
        _sunsetTimeSizeNumericUpDown.Value = (decimal)Settings.SunsetTimeFontSize;
    }

    private Color ParseColor(string colorString)
    {
        try
        {
            return Color.Parse(colorString);
        }
        catch
        {
            return Colors.White;
        }
    }

    private void OnSunriseLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_sunriseLabelSizeNumericUpDown.Value.HasValue)
        {
            Settings.SunriseLabelFontSize = (double)_sunriseLabelSizeNumericUpDown.Value.Value;
        }
    }

    private void OnSunriseTimeFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_sunriseTimeSizeNumericUpDown.Value.HasValue)
        {
            Settings.SunriseTimeFontSize = (double)_sunriseTimeSizeNumericUpDown.Value.Value;
        }
    }

    private void OnSunsetLabelFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_sunsetLabelSizeNumericUpDown.Value.HasValue)
        {
            Settings.SunsetLabelFontSize = (double)_sunsetLabelSizeNumericUpDown.Value.Value;
        }
    }

    private void OnSunsetTimeFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_sunsetTimeSizeNumericUpDown.Value.HasValue)
        {
            Settings.SunsetTimeFontSize = (double)_sunsetTimeSizeNumericUpDown.Value.Value;
        }
    }

    private void OnSunriseLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.SunriseLabelFontColor = _sunriseLabelColorPicker.Color.ToString();
    }

    private void OnSunriseTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.SunriseTimeFontColor = _sunriseTimeColorPicker.Color.ToString();
    }

    private void OnSunsetLabelColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.SunsetLabelFontColor = _sunsetLabelColorPicker.Color.ToString();
    }

    private void OnSunsetTimeColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.SunsetTimeFontColor = _sunsetTimeColorPicker.Color.ToString();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        if (_pluginSettings != null)
        {
            _pluginSettings.PropertyChanged -= OnPluginSettingsPropertyChanged;
        }
    }

    private void UpdateLongitudeDmsFromValue()
    {
        LongitudeConverter.DecomposeDms(Settings.Longitude, out int d, out int m, out double s, out bool isEast);
        _longitudeDmsDegreesTextBox.Text = d.ToString();
        _longitudeDmsMinutesTextBox.Text = m.ToString();
        _longitudeDmsSecondsTextBox.Text = s.ToString("F2");
        _longitudeDmsDirectionComboBox.SelectedIndex = isEast ? 0 : 1;
    }

    private void OnLongitudeLostFocus(object? sender, EventArgs e)
    {
        if (LongitudeConverter.TryParseDecimal(_longitudeTextBox.Text, out double lon))
        {
            Settings.Longitude = lon;
            _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(lon);
            UpdateLongitudeDmsFromValue();
        }
        else
        {
            _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(Settings.Longitude);
        }
    }

    private void OnLongitudeDmsValueChanged(object? sender, EventArgs e)
    {
        if (!int.TryParse(_longitudeDmsDegreesTextBox.Text, out int d)) d = 0;
        if (!int.TryParse(_longitudeDmsMinutesTextBox.Text, out int m)) m = 0;
        if (!double.TryParse(_longitudeDmsSecondsTextBox.Text, out double s)) s = 0;
        var isEast = _longitudeDmsDirectionComboBox.SelectedIndex == 0;
        if (LongitudeConverter.TryParseDms(d, m, s, isEast, out double lon))
        {
            Settings.Longitude = lon;
            _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(lon);
        }
        else
        {
            UpdateLongitudeDmsFromValue();
        }
    }

    private void UpdateLatitudeDmsFromValue()
    {
        LatitudeConverter.DecomposeDms(Settings.Latitude, out int d, out int m, out double s, out bool isNorth);
        _latitudeDmsDegreesTextBox.Text = d.ToString();
        _latitudeDmsMinutesTextBox.Text = m.ToString();
        _latitudeDmsSecondsTextBox.Text = s.ToString("F2");
        _latitudeDmsDirectionComboBox.SelectedIndex = isNorth ? 0 : 1;
    }

    private void OnLatitudeLostFocus(object? sender, EventArgs e)
    {
        if (LatitudeConverter.TryParseDecimal(_latitudeTextBox.Text, out double lat))
        {
            Settings.Latitude = lat;
            _latitudeTextBox.Text = LatitudeConverter.ToDecimalString(lat);
            UpdateLatitudeDmsFromValue();
        }
        else
        {
            _latitudeTextBox.Text = LatitudeConverter.ToDecimalString(Settings.Latitude);
        }
    }

    private void OnLatitudeDmsValueChanged(object? sender, EventArgs e)
    {
        if (!int.TryParse(_latitudeDmsDegreesTextBox.Text, out int d)) d = 0;
        if (!int.TryParse(_latitudeDmsMinutesTextBox.Text, out int m)) m = 0;
        if (!double.TryParse(_latitudeDmsSecondsTextBox.Text, out double s)) s = 0;
        var isNorth = _latitudeDmsDirectionComboBox.SelectedIndex == 0;
        if (LatitudeConverter.TryParseDms(d, m, s, isNorth, out double lat))
        {
            Settings.Latitude = lat;
            _latitudeTextBox.Text = LatitudeConverter.ToDecimalString(lat);
        }
        else
        {
            UpdateLatitudeDmsFromValue();
        }
    }

    private async void OnGetLocationClick(object? sender, EventArgs e)
    {
        _getLocationButton.IsEnabled = false;
        _getLocationButton.Content = "获取中...";
        _statusText.Text = "正在获取位置...";
        _statusText.Foreground = ThemeHelper.GetOrangeBrush();

        try
        {
            var location = await GetLocationAsync();
            if (location != null)
            {
                var longitude = Math.Round(location.Value.Longitude, 4);
                var latitude = Math.Round(location.Value.Latitude, 4);
                Settings.Longitude = longitude;
                Settings.Latitude = latitude;

                if (_pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms)
                {
                    UpdateLongitudeDmsFromValue();
                    UpdateLatitudeDmsFromValue();
                    _statusText.Text = $"已获取位置：{LatitudeConverter.ToDmsString(latitude)}, {LongitudeConverter.ToDmsString(longitude)}";
                }
                else
                {
                    _longitudeTextBox.Text = longitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                    _latitudeTextBox.Text = latitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                    _statusText.Text = $"已获取位置：{latitude:F4}°N, {longitude:F4}°E";
                }
                _statusText.Foreground = ThemeHelper.GetYiBrush();
            }
            else
            {
                _statusText.Text = "无法获取位置，请检查定位权限设置";
                _statusText.Foreground = Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            _statusText.Text = $"获取位置失败：{ex.Message}";
            _statusText.Foreground = Brushes.Red;
        }
        finally
        {
            _getLocationButton.IsEnabled = true;
            _getLocationButton.Content = "获取当前位置";
        }
    }

    private async void OnGetTimeZoneClick(object? sender, EventArgs e)
    {
        _getTimeZoneButton.IsEnabled = false;
        _getTimeZoneButton.Content = "获取中...";

        try
        {
            var timeZoneId = await GetTimeZoneByLocationAsync();
            if (!string.IsNullOrEmpty(timeZoneId))
                {
                    Settings.TimeZoneId = timeZoneId;
                    foreach (var item in _timeZoneComboBox.Items)
                    {
                        if (item is TimeZoneInfo tz && tz.Id == timeZoneId)
                        {
                            _timeZoneComboBox.SelectedItem = item;
                            break;
                        }
                    }
                    _statusText.Text = $"已获取时区：{timeZoneId}";
                    _statusText.Foreground = ThemeHelper.GetYiBrush();
                }
            else
            {
                _statusText.Text = "无法获取时区";
                _statusText.Foreground = Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            _statusText.Text = $"获取时区失败：{ex.Message}";
            _statusText.Foreground = Brushes.Red;
        }
        finally
        {
            _getTimeZoneButton.IsEnabled = true;
            _getTimeZoneButton.Content = "获取时区";
        }
    }

    private void OnTimeZoneChanged(object? sender, EventArgs e)
    {
        if (_timeZoneComboBox.SelectedIndex == 0)
        {
            Settings.TimeZoneId = "";
        }
        else if (_timeZoneComboBox.SelectedItem is TimeZoneInfo tz)
        {
            Settings.TimeZoneId = tz.Id;
        }
    }

    private async Task<(double Latitude, double Longitude)?> GetLocationAsync()
    {
        // 直接使用基于 IP 的定位，避免引入 WinRT/Windows SDK 依赖（可显著减小安装包体积）
        // 精度为城市级，对日出日落计算已足够（每 17km 经度差异约 1 分钟）
        return await GetLocationByIpAsync();
    }

    private async Task<(double Latitude, double Longitude)?> GetLocationByIpAsync()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetStringAsync("http://ip-api.com/json/?fields=lat,lon");

            var json = System.Text.Json.JsonDocument.Parse(response);
            if (json.RootElement.TryGetProperty("lat", out var latElement) &&
                json.RootElement.TryGetProperty("lon", out var lonElement))
            {
                return (latElement.GetDouble(), lonElement.GetDouble());
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetTimeZoneByLocationAsync()
    {
        try
        {
            var location = await GetLocationAsync();
            if (location != null)
            {
                var timeZoneId = TimeZoneInfo.Local.Id;
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var url = $"http://ip-api.com/json/{location.Value.Latitude},{location.Value.Longitude}?fields=timezone";
                    var response = await client.GetStringAsync(url);
                    var json = System.Text.Json.JsonDocument.Parse(response);
                    if (json.RootElement.TryGetProperty("timezone", out var tzElement))
                    {
                        var tz = tzElement.GetString();
                        if (!string.IsNullOrEmpty(tz))
                        {
                            try
                            {
                                TimeZoneInfo.FindSystemTimeZoneById(tz);
                                return tz;
                            }
                            catch { }
                        }
                    }
                }
                catch { }
                return timeZoneId;
            }
            return TimeZoneInfo.Local.Id;
        }
        catch
        {
            return null;
        }
    }

    

    private void OnPluginSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginSettings.LongitudeDisplayMode))
        {
            UpdateCoordinateDisplay();
        }
    }

    private void UpdateCoordinateDisplay()
    {
        if (_pluginSettings == null)
            return;

        var isDms = _pluginSettings.LongitudeDisplayMode == LongitudeDisplayMode.Dms;

        _longitudeTextBox.IsVisible = !isDms;
        _longitudeDmsPanel.IsVisible = isDms;
        _latitudeTextBox.IsVisible = !isDms;
        _latitudeDmsPanel.IsVisible = isDms;

        if (isDms)
        {
            UpdateLongitudeDmsFromValue();
            UpdateLatitudeDmsFromValue();
        }
        else
        {
            _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(Settings.Longitude);
            _latitudeTextBox.Text = LatitudeConverter.ToDecimalString(Settings.Latitude);
        }
    }
}

public static class LatitudeConverter
{
    public static bool TryParseDecimal(string input, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (double.TryParse(input.Trim(), out double value))
        {
            result = Math.Max(-90, Math.Min(90, value));
            return true;
        }
        return false;
    }

    public static bool TryParseDms(int degrees, int minutes, double seconds, bool isNorth, out double result)
    {
        result = 0;
        if (!ValidateDms(degrees, minutes, seconds))
            return false;

        var sign = isNorth ? 1 : -1;
        result = sign * (degrees + minutes / 60.0 + seconds / 3600.0);
        result = Math.Max(-90, Math.Min(90, result));
        return true;
    }

    public static string ToDecimalString(double latitude)
    {
        latitude = Math.Max(-90, Math.Min(90, latitude));
        return latitude.ToString("F4");
    }

    public static string ToDmsString(double latitude)
    {
        latitude = Math.Max(-90, Math.Min(90, latitude));

        var sign = latitude >= 0 ? "N" : "S";
        var absLatitude = Math.Abs(latitude);

        var degrees = (int)Math.Floor(absLatitude);
        var remaining = absLatitude - degrees;

        var minutes = (int)Math.Floor(remaining * 60);
        remaining -= minutes / 60.0;

        var seconds = remaining * 3600;

        return $"{degrees}°{minutes}'{seconds:F2}\"{sign}";
    }

    public static void DecomposeDms(double latitude, out int degrees, out int minutes, out double seconds, out bool isNorth)
    {
        latitude = Math.Max(-90, Math.Min(90, latitude));

        isNorth = latitude >= 0;
        var absLatitude = Math.Abs(latitude);

        degrees = (int)Math.Floor(absLatitude);
        var remaining = absLatitude - degrees;

        minutes = (int)Math.Floor(remaining * 60);
        remaining -= minutes / 60.0;

        seconds = Math.Round(remaining * 3600, 2);

        if (seconds >= 60)
        {
            seconds = 0;
            minutes++;
        }
        if (minutes >= 60)
        {
            minutes = 0;
            degrees++;
        }
    }

    public static bool ValidateDms(int degrees, int minutes, double seconds)
    {
        return degrees >= 0 && degrees <= 90 &&
               minutes >= 0 && minutes < 60 &&
               seconds >= 0 && seconds < 60;
    }
}
