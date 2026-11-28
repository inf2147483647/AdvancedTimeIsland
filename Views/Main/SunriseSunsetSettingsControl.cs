using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public partial class SunriseSunsetSettingsControl : ComponentBase<SunriseSunsetSettings>
{
    private readonly PluginSettings? _pluginSettings;
    private bool _initCompleted;

    private TextBox _longitudeTextBox = null!;
    private TextBox _longitudeDmsDegreesTextBox = null!;
    private TextBox _longitudeDmsMinutesTextBox = null!;
    private TextBox _longitudeDmsSecondsTextBox = null!;
    private ComboBox _longitudeDmsDirectionComboBox = null!;
    private StackPanel _longitudeDmsPanel = null!;

    private TextBox _latitudeTextBox = null!;
    private TextBox _latitudeDmsDegreesTextBox = null!;
    private TextBox _latitudeDmsMinutesTextBox = null!;
    private TextBox _latitudeDmsSecondsTextBox = null!;
    private ComboBox _latitudeDmsDirectionComboBox = null!;
    private StackPanel _latitudeDmsPanel = null!;

    private Button _getLocationButton = null!;
    private TextBlock _statusText = null!;

    private ComboBox _timeZoneComboBox = null!;
    private Button _getTimeZoneButton = null!;

    private CheckBox _sunriseLabelEnableCustomFontSizeToggle = null!;
    private CheckBox _sunriseLabelEnableCustomFontColorToggle = null!;
    private CheckBox _sunriseTimeEnableCustomFontSizeToggle = null!;
    private CheckBox _sunriseTimeEnableCustomFontColorToggle = null!;
    private CheckBox _sunsetLabelEnableCustomFontSizeToggle = null!;
    private CheckBox _sunsetLabelEnableCustomFontColorToggle = null!;
    private CheckBox _sunsetTimeEnableCustomFontSizeToggle = null!;
    private CheckBox _sunsetTimeEnableCustomFontColorToggle = null!;

    private WpfColorPicker _sunriseLabelColorPicker = null!;
    private WpfNumericUpDown _sunriseLabelSizeNumericUpDown = null!;

    private WpfColorPicker _sunriseTimeColorPicker = null!;
    private WpfNumericUpDown _sunriseTimeSizeNumericUpDown = null!;

    private WpfColorPicker _sunsetLabelColorPicker = null!;
    private WpfNumericUpDown _sunsetLabelSizeNumericUpDown = null!;

    private WpfColorPicker _sunsetTimeColorPicker = null!;
    private WpfNumericUpDown _sunsetTimeSizeNumericUpDown = null!;

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
        InitializeControlReferences();
    }

    /// <summary>
    /// 从 XAML 的 SettingsControl.Switcher 中提取控件引用。
    /// （SettingsControl 模板内元素无法使用 x:Name，需通过 Switcher 属性访问）
    /// </summary>
    private void InitializeControlReferences()
    {
        _statusText = StatusText;
        _longitudeTextBox = (TextBox)((StackPanel)LongitudeItem.Switcher).Children[0];
        _longitudeDmsPanel = (StackPanel)((StackPanel)LongitudeItem.Switcher).Children[1];
        _longitudeDmsDegreesTextBox = (TextBox)_longitudeDmsPanel.Children[0];
        _longitudeDmsMinutesTextBox = (TextBox)_longitudeDmsPanel.Children[2];
        _longitudeDmsSecondsTextBox = (TextBox)_longitudeDmsPanel.Children[4];
        _longitudeDmsDirectionComboBox = (ComboBox)_longitudeDmsPanel.Children[6];

        _latitudeTextBox = (TextBox)((StackPanel)LatitudeItem.Switcher).Children[0];
        _latitudeDmsPanel = (StackPanel)((StackPanel)LatitudeItem.Switcher).Children[1];
        _latitudeDmsDegreesTextBox = (TextBox)_latitudeDmsPanel.Children[0];
        _latitudeDmsMinutesTextBox = (TextBox)_latitudeDmsPanel.Children[2];
        _latitudeDmsSecondsTextBox = (TextBox)_latitudeDmsPanel.Children[4];
        _latitudeDmsDirectionComboBox = (ComboBox)_latitudeDmsPanel.Children[6];

        _getLocationButton = (Button)((StackPanel)GetLocationItem.Switcher).Children[0];
        _timeZoneComboBox = (ComboBox)((StackPanel)TimeZoneItem.Switcher).Children[0];
        _getTimeZoneButton = (Button)((StackPanel)GetTimeZoneItem.Switcher).Children[0];

        _sunriseLabelSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)SunriseLabelFontSizeItem.Switcher).Children[0];
        _sunriseLabelEnableCustomFontSizeToggle = (CheckBox)((StackPanel)SunriseLabelFontSizeItem.Switcher).Children[1];
        _sunriseLabelColorPicker = (WpfColorPicker)((StackPanel)SunriseLabelColorItem.Switcher).Children[0];
        _sunriseLabelEnableCustomFontColorToggle = (CheckBox)((StackPanel)SunriseLabelColorItem.Switcher).Children[1];

        _sunriseTimeSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)SunriseTimeFontSizeItem.Switcher).Children[0];
        _sunriseTimeEnableCustomFontSizeToggle = (CheckBox)((StackPanel)SunriseTimeFontSizeItem.Switcher).Children[1];
        _sunriseTimeColorPicker = (WpfColorPicker)((StackPanel)SunriseTimeColorItem.Switcher).Children[0];
        _sunriseTimeEnableCustomFontColorToggle = (CheckBox)((StackPanel)SunriseTimeColorItem.Switcher).Children[1];

        _sunsetLabelSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)SunsetLabelFontSizeItem.Switcher).Children[0];
        _sunsetLabelEnableCustomFontSizeToggle = (CheckBox)((StackPanel)SunsetLabelFontSizeItem.Switcher).Children[1];
        _sunsetLabelColorPicker = (WpfColorPicker)((StackPanel)SunsetLabelColorItem.Switcher).Children[0];
        _sunsetLabelEnableCustomFontColorToggle = (CheckBox)((StackPanel)SunsetLabelColorItem.Switcher).Children[1];

        _sunsetTimeSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)SunsetTimeFontSizeItem.Switcher).Children[0];
        _sunsetTimeEnableCustomFontSizeToggle = (CheckBox)((StackPanel)SunsetTimeFontSizeItem.Switcher).Children[1];
        _sunsetTimeColorPicker = (WpfColorPicker)((StackPanel)SunsetTimeColorItem.Switcher).Children[0];
        _sunsetTimeEnableCustomFontColorToggle = (CheckBox)((StackPanel)SunsetTimeColorItem.Switcher).Children[1];

        _longitudeDmsDirectionComboBox.Items.Add("东经");
        _longitudeDmsDirectionComboBox.Items.Add("西经");
        _latitudeDmsDirectionComboBox.Items.Add("北纬");
        _latitudeDmsDirectionComboBox.Items.Add("南纬");

        _timeZoneComboBox.Items.Add("跟随插件设置");
        foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
        {
            _timeZoneComboBox.Items.Add(tz);
        }

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeTextBox, OnLongitudeLostFocus);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsDegreesTextBox, OnLongitudeDmsValueChanged);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsMinutesTextBox, OnLongitudeDmsValueChanged);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsSecondsTextBox, OnLongitudeDmsValueChanged);
        _longitudeDmsDirectionComboBox.SelectionChanged += OnLongitudeDmsValueChanged;

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_latitudeTextBox, OnLatitudeLostFocus);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_latitudeDmsDegreesTextBox, OnLatitudeDmsValueChanged);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_latitudeDmsMinutesTextBox, OnLatitudeDmsValueChanged);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_latitudeDmsSecondsTextBox, OnLatitudeDmsValueChanged);
        _latitudeDmsDirectionComboBox.SelectionChanged += OnLatitudeDmsValueChanged;
    }

    private void OnSunriseLabelEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.SunriseLabelEnableCustomFontSize = _sunriseLabelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunriseLabelEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.SunriseLabelEnableCustomFontColor = _sunriseLabelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunriseTimeEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.SunriseTimeEnableCustomFontSize = _sunriseTimeEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunriseTimeEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.SunriseTimeEnableCustomFontColor = _sunriseTimeEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunsetLabelEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.SunsetLabelEnableCustomFontSize = _sunsetLabelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunsetLabelEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.SunsetLabelEnableCustomFontColor = _sunsetLabelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunsetTimeEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.SunsetTimeEnableCustomFontSize = _sunsetTimeEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnSunsetTimeEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.SunsetTimeEnableCustomFontColor = _sunsetTimeEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
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

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        RunInitWhenReady();
    }

    private void RunInitWhenReady()
    {
        if (_initCompleted) return;
        if (Settings == null)
        {
            // OnInitialized 可能在组件创建期间提前触发，此时 Settings 尚未注入，延迟到 Loaded 后再初始化
            Loaded += OnLoadedAfterSettingsReady;
            return;
        }
        _initCompleted = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoadedAfterSettingsReady(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAfterSettingsReady;
        RunInitWhenReady();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(Settings.Longitude);
        UpdateLongitudeDmsFromValue();
        _latitudeTextBox.Text = LatitudeConverter.ToDecimalString(Settings.Latitude);
        UpdateLatitudeDmsFromValue();

        var isDms = _pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms;
        _longitudeTextBox.Visibility = !isDms ? Visibility.Visible : Visibility.Collapsed;
        _longitudeDmsPanel.Visibility = isDms ? Visibility.Visible : Visibility.Collapsed;
        _latitudeTextBox.Visibility = !isDms ? Visibility.Visible : Visibility.Collapsed;
        _latitudeDmsPanel.Visibility = isDms ? Visibility.Visible : Visibility.Collapsed;

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
            return ThemeHelper.ParseColor(colorString);
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

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
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

    private async void OnGetLocationClick(object? sender, RoutedEventArgs e)
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

    private async void OnGetTimeZoneClick(object? sender, RoutedEventArgs e)
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

    private void OnTimeZoneChanged(object? sender, SelectionChangedEventArgs e)
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

        _longitudeTextBox.Visibility = !isDms ? Visibility.Visible : Visibility.Collapsed;
        _longitudeDmsPanel.Visibility = isDms ? Visibility.Visible : Visibility.Collapsed;
        _latitudeTextBox.Visibility = !isDms ? Visibility.Visible : Visibility.Collapsed;
        _latitudeDmsPanel.Visibility = isDms ? Visibility.Visible : Visibility.Collapsed;

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
