using System;
using System.ComponentModel;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
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

    private TextBlock _coordTitleTextBlock;

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

    private TextBlock _timeZoneTitleTextBlock;
    private ComboBox _timeZoneComboBox;
    private Button _getTimeZoneButton;
    private TextBlock _timeZoneLabelTextBlock;

    private ToggleSwitch? _sunriseLabelEnableCustomFontSizeToggle;
    private ToggleSwitch? _sunriseLabelEnableCustomFontColorToggle;
    private ToggleSwitch? _sunriseTimeEnableCustomFontSizeToggle;
    private ToggleSwitch? _sunriseTimeEnableCustomFontColorToggle;
    private ToggleSwitch? _sunsetLabelEnableCustomFontSizeToggle;
    private ToggleSwitch? _sunsetLabelEnableCustomFontColorToggle;
    private ToggleSwitch? _sunsetTimeEnableCustomFontSizeToggle;
    private ToggleSwitch? _sunsetTimeEnableCustomFontColorToggle;

    private TextBlock _styleTitleTextBlock;

    private TextBlock _sunriseLabelLabel;
    private ColorPicker _sunriseLabelColorPicker;
    private NumericUpDown _sunriseLabelSizeNumericUpDown;

    private TextBlock _sunriseTimeLabel;
    private ColorPicker _sunriseTimeColorPicker;
    private NumericUpDown _sunriseTimeSizeNumericUpDown;

    private TextBlock _sunsetLabelLabel;
    private ColorPicker _sunsetLabelColorPicker;
    private NumericUpDown _sunsetLabelSizeNumericUpDown;

    private TextBlock _sunsetTimeLabel;
    private ColorPicker _sunsetTimeColorPicker;
    private NumericUpDown _sunsetTimeSizeNumericUpDown;

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

        _coordTitleTextBlock = new TextBlock { Text = "经纬度设置", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_coordTitleTextBlock);

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
        sp.Children.Add(longitudeRow);

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
        sp.Children.Add(latitudeRow);

        _getLocationButton = new Button
        {
            Content = "获取当前位置",
            Padding = new Thickness(8, 4),
            Margin = new Thickness(0, 4, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _getLocationButton.Click += OnGetLocationClick;
        sp.Children.Add(_getLocationButton);

        _statusText = new TextBlock { Text = "", FontSize = 11, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_statusText);

        _timeZoneTitleTextBlock = new TextBlock { Text = "时区设置", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_timeZoneTitleTextBlock);

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

        sp.Children.Add(timeZoneRow);

        _styleTitleTextBlock = new TextBlock { Text = "字体样式", FontSize = 14, FontWeight = FontWeight.Bold };
        var styleTitleRow = CreateTitleRow(_styleTitleTextBlock, out _, out _, out _, out _, null, null, null, null, null, null, null, null);
        styleTitleRow.Margin = new Thickness(0, 10, 0, 0);
        sp.Children.Add(styleTitleRow);

        var sunriseLabelTitle = new TextBlock { Text = "日出标签样式", FontSize = 12 };
        var sunriseLabelTitleRow = CreateTitleRow(sunriseLabelTitle, out _sunriseLabelEnableCustomFontSizeToggle, out _sunriseLabelEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnSunriseLabelEnableCustomFontSizeChanged, OnSunriseLabelEnableCustomFontColorChanged, null, null);
        sp.Children.Add(sunriseLabelTitleRow);
        sp.Children.Add(CreateFontSizeRow("大小", out _sunriseLabelLabel, out _sunriseLabelSizeNumericUpDown, OnSunriseLabelFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色", out _, out _sunriseLabelColorPicker, OnSunriseLabelColorChanged));

        var sunriseTimeTitle = new TextBlock { Text = "日出时间样式", FontSize = 12 };
        var sunriseTimeTitleRow = CreateTitleRow(sunriseTimeTitle, out _sunriseTimeEnableCustomFontSizeToggle, out _sunriseTimeEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnSunriseTimeEnableCustomFontSizeChanged, OnSunriseTimeEnableCustomFontColorChanged, null, null);
        sp.Children.Add(sunriseTimeTitleRow);
        sp.Children.Add(CreateFontSizeRow("大小", out _sunriseTimeLabel, out _sunriseTimeSizeNumericUpDown, OnSunriseTimeFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色", out _, out _sunriseTimeColorPicker, OnSunriseTimeColorChanged));

        var sunsetLabelTitle = new TextBlock { Text = "日落标签样式", FontSize = 12 };
        var sunsetLabelTitleRow = CreateTitleRow(sunsetLabelTitle, out _sunsetLabelEnableCustomFontSizeToggle, out _sunsetLabelEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnSunsetLabelEnableCustomFontSizeChanged, OnSunsetLabelEnableCustomFontColorChanged, null, null);
        sp.Children.Add(sunsetLabelTitleRow);
        sp.Children.Add(CreateFontSizeRow("大小", out _sunsetLabelLabel, out _sunsetLabelSizeNumericUpDown, OnSunsetLabelFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色", out _, out _sunsetLabelColorPicker, OnSunsetLabelColorChanged));

        var sunsetTimeTitle = new TextBlock { Text = "日落时间样式", FontSize = 12 };
        var sunsetTimeTitleRow = CreateTitleRow(sunsetTimeTitle, out _sunsetTimeEnableCustomFontSizeToggle, out _sunsetTimeEnableCustomFontColorToggle, out _, out _,
            "启用自定义大小", "启用自定义颜色", null, null,
            OnSunsetTimeEnableCustomFontSizeChanged, OnSunsetTimeEnableCustomFontColorChanged, null, null);
        sp.Children.Add(sunsetTimeTitleRow);
        sp.Children.Add(CreateFontSizeRow("大小", out _sunsetTimeLabel, out _sunsetTimeSizeNumericUpDown, OnSunsetTimeFontSizeChanged));
        sp.Children.Add(CreateColorRow("颜色", out _, out _sunsetTimeColorPicker, OnSunsetTimeColorChanged));

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
        _coordTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _longitudeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _latitudeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dmsDegreeSymbol.Foreground = ThemeHelper.GetTextBrush();
        _dmsMinuteSymbol.Foreground = ThemeHelper.GetTextBrush();
        _dmsSecondSymbol.Foreground = ThemeHelper.GetTextBrush();
        _latDmsDegreeSymbol.Foreground = ThemeHelper.GetTextBrush();
        _latDmsMinuteSymbol.Foreground = ThemeHelper.GetTextBrush();
        _latDmsSecondSymbol.Foreground = ThemeHelper.GetTextBrush();
        _statusText.Foreground = ThemeHelper.GetGrayBrush();
        _timeZoneTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _timeZoneLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _sunriseLabelEnableCustomFontSizeToggle?.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _sunriseLabelEnableCustomFontColorToggle?.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _sunriseTimeEnableCustomFontSizeToggle?.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _sunriseTimeEnableCustomFontColorToggle?.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _sunsetLabelEnableCustomFontSizeToggle?.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _sunsetLabelEnableCustomFontColorToggle?.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _sunsetTimeEnableCustomFontSizeToggle?.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _sunsetTimeEnableCustomFontColorToggle?.SetValue(ForegroundProperty, ThemeHelper.GetTextBrush());
        _styleTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _sunriseLabelLabel.Foreground = ThemeHelper.GetTextBrush();
        _sunriseTimeLabel.Foreground = ThemeHelper.GetTextBrush();
        _sunsetLabelLabel.Foreground = ThemeHelper.GetTextBrush();
        _sunsetTimeLabel.Foreground = ThemeHelper.GetTextBrush();
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

    private Grid CreateTitleRow(TextBlock title, out ToggleSwitch? toggle1, out ToggleSwitch? toggle2, out ToggleSwitch? toggle3, out ToggleSwitch? toggle4,
        string? content1, string? content2, string? content3, string? content4,
        EventHandler<RoutedEventArgs>? handler1, EventHandler<RoutedEventArgs>? handler2, EventHandler<RoutedEventArgs>? handler3, EventHandler<RoutedEventArgs>? handler4)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(title, 0);
        row.Children.Add(title);

        int col = 1;

        if (content1 != null)
        {
            toggle1 = new ToggleSwitch { Content = content1, VerticalAlignment = VerticalAlignment.Center };
            if (handler1 != null)
                toggle1.IsCheckedChanged += handler1;
            Grid.SetColumn(toggle1, col++);
            row.Children.Add(toggle1);
        }
        else
        {
            toggle1 = null;
        }

        if (content2 != null)
        {
            toggle2 = new ToggleSwitch { Content = content2, VerticalAlignment = VerticalAlignment.Center };
            if (handler2 != null)
                toggle2.IsCheckedChanged += handler2;
            Grid.SetColumn(toggle2, col++);
            row.Children.Add(toggle2);
        }
        else
        {
            toggle2 = null;
        }

        if (content3 != null)
        {
            toggle3 = new ToggleSwitch { Content = content3, VerticalAlignment = VerticalAlignment.Center };
            if (handler3 != null)
                toggle3.IsCheckedChanged += handler3;
            Grid.SetColumn(toggle3, col++);
            row.Children.Add(toggle3);
        }
        else
        {
            toggle3 = null;
        }

        if (content4 != null)
        {
            toggle4 = new ToggleSwitch { Content = content4, VerticalAlignment = VerticalAlignment.Center };
            if (handler4 != null)
                toggle4.IsCheckedChanged += handler4;
            Grid.SetColumn(toggle4, col);
            row.Children.Add(toggle4);
        }
        else
        {
            toggle4 = null;
        }

        return row;
    }

    private Grid CreateFontSizeRow(string labelText, out TextBlock label, out NumericUpDown numericUpDown,
        EventHandler<NumericUpDownValueChangedEventArgs> valueChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        numericUpDown = new NumericUpDown
        {
            Width = 155,
            Minimum = 1,
            Maximum = 100,
            Increment = 1m,
            FormatString = "0.00",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        numericUpDown.ValueChanged += valueChangedHandler;
        Grid.SetColumn(numericUpDown, 1);
        row.Children.Add(numericUpDown);

        return row;
    }

    private Grid CreateColorRow(string labelText, out TextBlock label, out ColorPicker colorPicker,
        EventHandler<ColorChangedEventArgs> colorChangedHandler)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        label = new TextBlock { Text = labelText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        colorPicker = new ColorPicker { Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        colorPicker.ColorChanged += colorChangedHandler;
        Grid.SetColumn(colorPicker, 1);
        row.Children.Add(colorPicker);

        return row;
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

        _sunriseLabelEnableCustomFontSizeToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.SunriseLabelEnableCustomFontSize);
        _sunriseLabelEnableCustomFontColorToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.SunriseLabelEnableCustomFontColor);
        _sunriseTimeEnableCustomFontSizeToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.SunriseTimeEnableCustomFontSize);
        _sunriseTimeEnableCustomFontColorToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.SunriseTimeEnableCustomFontColor);
        _sunsetLabelEnableCustomFontSizeToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.SunsetLabelEnableCustomFontSize);
        _sunsetLabelEnableCustomFontColorToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.SunsetLabelEnableCustomFontColor);
        _sunsetTimeEnableCustomFontSizeToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.SunsetTimeEnableCustomFontSize);
        _sunsetTimeEnableCustomFontColorToggle?.SetValue(ToggleSwitch.IsCheckedProperty, Settings.SunsetTimeEnableCustomFontColor);
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
