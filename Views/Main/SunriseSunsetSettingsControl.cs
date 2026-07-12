using System;
using System.ComponentModel;
using System.Threading.Tasks;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

    private ToggleSwitch _enableCustomToggle;

    private TextBlock _styleTitleTextBlock;

    private TextBlock _sunriseLabelLabel;
    private TextBox _sunriseLabelColorTextBox;
    private TextBox _sunriseLabelSizeTextBox;

    private TextBlock _sunriseTimeLabel;
    private TextBox _sunriseTimeColorTextBox;
    private TextBox _sunriseTimeSizeTextBox;

    private TextBlock _sunsetLabelLabel;
    private TextBox _sunsetLabelColorTextBox;
    private TextBox _sunsetLabelSizeTextBox;

    private TextBlock _sunsetTimeLabel;
    private TextBox _sunsetTimeColorTextBox;
    private TextBox _sunsetTimeSizeTextBox;

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

        _enableCustomToggle = new ToggleSwitch { Content = "启用自定义颜色与字体", Margin = new Thickness(0, 10, 0, 0) };
        _enableCustomToggle.IsCheckedChanged += OnEnableCustomToggleChanged;
        sp.Children.Add(_enableCustomToggle);

        _styleTitleTextBlock = new TextBlock { Text = "字体样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_styleTitleTextBlock);

        var sunriseLabelRow = new Grid();
        sunriseLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sunriseLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sunriseLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sunriseLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _sunriseLabelLabel = new TextBlock { Text = "日出标签:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_sunriseLabelLabel, 0);
        sunriseLabelRow.Children.Add(_sunriseLabelLabel);

        _sunriseLabelColorTextBox = new TextBox { Width = 100, Watermark = "#FFFFFF" };
        Grid.SetColumn(_sunriseLabelColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_sunriseLabelColorTextBox, (s, e) => OnColorLostFocus(_sunriseLabelColorTextBox, nameof(Settings.SunriseLabelFontColor)));
        sunriseLabelRow.Children.Add(_sunriseLabelColorTextBox);

        _sunriseLabelSizeTextBox = new TextBox { Width = 60, Watermark = "14" };
        Grid.SetColumn(_sunriseLabelSizeTextBox, 3);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_sunriseLabelSizeTextBox, (s, e) => OnFontSizeLostFocus(_sunriseLabelSizeTextBox, nameof(Settings.SunriseLabelFontSize)));
        sunriseLabelRow.Children.Add(_sunriseLabelSizeTextBox);
        sp.Children.Add(sunriseLabelRow);

        var sunriseTimeRow = new Grid();
        sunriseTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sunriseTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sunriseTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sunriseTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _sunriseTimeLabel = new TextBlock { Text = "日出时间:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_sunriseTimeLabel, 0);
        sunriseTimeRow.Children.Add(_sunriseTimeLabel);

        _sunriseTimeColorTextBox = new TextBox { Width = 100, Watermark = "#FFFFFF" };
        Grid.SetColumn(_sunriseTimeColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_sunriseTimeColorTextBox, (s, e) => OnColorLostFocus(_sunriseTimeColorTextBox, nameof(Settings.SunriseTimeFontColor)));
        sunriseTimeRow.Children.Add(_sunriseTimeColorTextBox);

        _sunriseTimeSizeTextBox = new TextBox { Width = 60, Watermark = "14" };
        Grid.SetColumn(_sunriseTimeSizeTextBox, 3);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_sunriseTimeSizeTextBox, (s, e) => OnFontSizeLostFocus(_sunriseTimeSizeTextBox, nameof(Settings.SunriseTimeFontSize)));
        sunriseTimeRow.Children.Add(_sunriseTimeSizeTextBox);
        sp.Children.Add(sunriseTimeRow);

        var sunsetLabelRow = new Grid();
        sunsetLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sunsetLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sunsetLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sunsetLabelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _sunsetLabelLabel = new TextBlock { Text = "日落标签:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_sunsetLabelLabel, 0);
        sunsetLabelRow.Children.Add(_sunsetLabelLabel);

        _sunsetLabelColorTextBox = new TextBox { Width = 100, Watermark = "#FFFFFF" };
        Grid.SetColumn(_sunsetLabelColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_sunsetLabelColorTextBox, (s, e) => OnColorLostFocus(_sunsetLabelColorTextBox, nameof(Settings.SunsetLabelFontColor)));
        sunsetLabelRow.Children.Add(_sunsetLabelColorTextBox);

        _sunsetLabelSizeTextBox = new TextBox { Width = 60, Watermark = "14" };
        Grid.SetColumn(_sunsetLabelSizeTextBox, 3);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_sunsetLabelSizeTextBox, (s, e) => OnFontSizeLostFocus(_sunsetLabelSizeTextBox, nameof(Settings.SunsetLabelFontSize)));
        sunsetLabelRow.Children.Add(_sunsetLabelSizeTextBox);
        sp.Children.Add(sunsetLabelRow);

        var sunsetTimeRow = new Grid();
        sunsetTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sunsetTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sunsetTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sunsetTimeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _sunsetTimeLabel = new TextBlock { Text = "日落时间:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_sunsetTimeLabel, 0);
        sunsetTimeRow.Children.Add(_sunsetTimeLabel);

        _sunsetTimeColorTextBox = new TextBox { Width = 100, Watermark = "#FFFFFF" };
        Grid.SetColumn(_sunsetTimeColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_sunsetTimeColorTextBox, (s, e) => OnColorLostFocus(_sunsetTimeColorTextBox, nameof(Settings.SunsetTimeFontColor)));
        sunsetTimeRow.Children.Add(_sunsetTimeColorTextBox);

        _sunsetTimeSizeTextBox = new TextBox { Width = 60, Watermark = "14" };
        Grid.SetColumn(_sunsetTimeSizeTextBox, 3);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_sunsetTimeSizeTextBox, (s, e) => OnFontSizeLostFocus(_sunsetTimeSizeTextBox, nameof(Settings.SunsetTimeFontSize)));
        sunsetTimeRow.Children.Add(_sunsetTimeSizeTextBox);
        sp.Children.Add(sunsetTimeRow);

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
        _enableCustomToggle.Foreground = ThemeHelper.GetTextBrush();
        _styleTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _sunriseLabelLabel.Foreground = ThemeHelper.GetTextBrush();
        _sunriseTimeLabel.Foreground = ThemeHelper.GetTextBrush();
        _sunsetLabelLabel.Foreground = ThemeHelper.GetTextBrush();
        _sunsetTimeLabel.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
        if (Settings.EnableCustomColorAndFont)
        {
            UpdateFontColorsForTheme();
        }
    }

    private void UpdateFontColorsForTheme()
    {
        var newColor = ThemeHelper.GetSmartContrastColor(Settings.SunriseLabelFontColor);
        Settings.SunriseLabelFontColor = newColor;
        _sunriseLabelColorTextBox.Text = newColor;

        newColor = ThemeHelper.GetSmartContrastColor(Settings.SunriseTimeFontColor);
        Settings.SunriseTimeFontColor = newColor;
        _sunriseTimeColorTextBox.Text = newColor;

        newColor = ThemeHelper.GetSmartContrastColor(Settings.SunsetLabelFontColor);
        Settings.SunsetLabelFontColor = newColor;
        _sunsetLabelColorTextBox.Text = newColor;

        newColor = ThemeHelper.GetSmartContrastColor(Settings.SunsetTimeFontColor);
        Settings.SunsetTimeFontColor = newColor;
        _sunsetTimeColorTextBox.Text = newColor;
    }

    private void OnEnableCustomToggleChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomColorAndFont = _enableCustomToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        var enabled = Settings.EnableCustomColorAndFont;
        _sunriseLabelColorTextBox.IsEnabled = enabled;
        _sunriseLabelSizeTextBox.IsEnabled = enabled;
        _sunriseTimeColorTextBox.IsEnabled = enabled;
        _sunriseTimeSizeTextBox.IsEnabled = enabled;
        _sunsetLabelColorTextBox.IsEnabled = enabled;
        _sunsetLabelSizeTextBox.IsEnabled = enabled;
        _sunsetTimeColorTextBox.IsEnabled = enabled;
        _sunsetTimeSizeTextBox.IsEnabled = enabled;
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

        _enableCustomToggle.IsChecked = Settings.EnableCustomColorAndFont;
        UpdateControlsEnabled();

        _sunriseLabelColorTextBox.Text = Settings.SunriseLabelFontColor;
        _sunriseLabelSizeTextBox.Text = Settings.SunriseLabelFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _sunriseTimeColorTextBox.Text = Settings.SunriseTimeFontColor;
        _sunriseTimeSizeTextBox.Text = Settings.SunriseTimeFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _sunsetLabelColorTextBox.Text = Settings.SunsetLabelFontColor;
        _sunsetLabelSizeTextBox.Text = Settings.SunsetLabelFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _sunsetTimeColorTextBox.Text = Settings.SunsetTimeFontColor;
        _sunsetTimeSizeTextBox.Text = Settings.SunsetTimeFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
        try
        {
            var accessStatus = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();

            if (accessStatus == Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed)
            {
                var geolocator = new Windows.Devices.Geolocation.Geolocator
                {
                    DesiredAccuracy = Windows.Devices.Geolocation.PositionAccuracy.Default
                };

                var position = await geolocator.GetGeopositionAsync();

                if (position?.Coordinate?.Point?.Position != null)
                {
                    return (position.Coordinate.Point.Position.Latitude, position.Coordinate.Point.Position.Longitude);
                }
            }

            return await GetLocationByIpAsync();
        }
        catch (Exception)
        {
            return await GetLocationByIpAsync();
        }
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

    private void OnColorLostFocus(TextBox textBox, string propertyName)
    {
        var color = textBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try
            {
                Avalonia.Media.Color.Parse(color);
                switch (propertyName)
                {
                    case nameof(Settings.SunriseLabelFontColor):
                        Settings.SunriseLabelFontColor = color;
                        break;
                    case nameof(Settings.SunriseTimeFontColor):
                        Settings.SunriseTimeFontColor = color;
                        break;
                    case nameof(Settings.SunsetLabelFontColor):
                        Settings.SunsetLabelFontColor = color;
                        break;
                    case nameof(Settings.SunsetTimeFontColor):
                        Settings.SunsetTimeFontColor = color;
                        break;
                }
            }
            catch
            {
                textBox.Text = GetColorPropertyValue(propertyName);
            }
        }
        else
        {
            textBox.Text = GetColorPropertyValue(propertyName);
        }
    }

    private string GetColorPropertyValue(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Settings.SunriseLabelFontColor):
                return Settings.SunriseLabelFontColor;
            case nameof(Settings.SunriseTimeFontColor):
                return Settings.SunriseTimeFontColor;
            case nameof(Settings.SunsetLabelFontColor):
                return Settings.SunsetLabelFontColor;
            case nameof(Settings.SunsetTimeFontColor):
                return Settings.SunsetTimeFontColor;
            default:
                return "#FFFFFF";
        }
    }

    private void OnFontSizeLostFocus(TextBox textBox, string propertyName)
    {
        if (double.TryParse(textBox.Text, out double size))
        {
            switch (propertyName)
            {
                case nameof(Settings.SunriseLabelFontSize):
                    Settings.SunriseLabelFontSize = size;
                    textBox.Text = Settings.SunriseLabelFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case nameof(Settings.SunriseTimeFontSize):
                    Settings.SunriseTimeFontSize = size;
                    textBox.Text = Settings.SunriseTimeFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case nameof(Settings.SunsetLabelFontSize):
                    Settings.SunsetLabelFontSize = size;
                    textBox.Text = Settings.SunsetLabelFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case nameof(Settings.SunsetTimeFontSize):
                    Settings.SunsetTimeFontSize = size;
                    textBox.Text = Settings.SunsetTimeFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
            }
        }
        else
        {
            textBox.Text = GetFontSizePropertyValue(propertyName);
        }
    }

    private string GetFontSizePropertyValue(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Settings.SunriseLabelFontSize):
                return Settings.SunriseLabelFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case nameof(Settings.SunriseTimeFontSize):
                return Settings.SunriseTimeFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case nameof(Settings.SunsetLabelFontSize):
                return Settings.SunsetLabelFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case nameof(Settings.SunsetTimeFontSize):
                return Settings.SunsetTimeFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            default:
                return "14";
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
