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

public partial class LocalSolarTimeSettingsControl : ComponentBase<LocalSolarTimeSettings>
{
    private TextBox _longitudeTextBox = null!;
    private bool _initCompleted;
    private TextBox _longitudeDmsDegreesTextBox = null!;
    private TextBox _longitudeDmsMinutesTextBox = null!;
    private TextBox _longitudeDmsSecondsTextBox = null!;
    private ComboBox _longitudeDmsDirectionComboBox = null!;
    private StackPanel _longitudeDmsPanel = null!;
    private WpfColorPicker _colorPicker = null!;
    private WpfNumericUpDown _fontSizeNumericUpDown = null!;
    private CheckBox _enableCustomFontSizeToggle = null!;
    private CheckBox _enableCustomFontColorToggle = null!;
    private CheckBox _enableCustomFontFamilyToggle = null!;
    private CheckBox _enableCustomFontWeightToggle = null!;
    private ComboBox _fontFamilyComboBox = null!;
    private ComboBox _fontWeightComboBox = null!;
    private Button _getLocationButton = null!;
    private TextBlock _statusText = null!;
    private readonly PluginSettings? _pluginSettings;

    public LocalSolarTimeSettingsControl() : this(null)
    {
    }

    public LocalSolarTimeSettingsControl(PluginSettings? pluginSettings = null)
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

        _getLocationButton = (Button)((StackPanel)GetLocationItem.Switcher).Children[0];

        _fontSizeNumericUpDown = (WpfNumericUpDown)((StackPanel)FontSizeItem.Switcher).Children[0];
        _enableCustomFontSizeToggle = (CheckBox)((StackPanel)FontSizeItem.Switcher).Children[1];

        _colorPicker = (WpfColorPicker)((StackPanel)ColorItem.Switcher).Children[0];
        _enableCustomFontColorToggle = (CheckBox)((StackPanel)ColorItem.Switcher).Children[1];

        _fontFamilyComboBox = (ComboBox)((StackPanel)FontFamilyItem.Switcher).Children[0];
        _enableCustomFontFamilyToggle = (CheckBox)((StackPanel)FontFamilyItem.Switcher).Children[1];

        _fontWeightComboBox = (ComboBox)((StackPanel)FontWeightItem.Switcher).Children[0];
        _enableCustomFontWeightToggle = (CheckBox)((StackPanel)FontWeightItem.Switcher).Children[1];

        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            _fontFamilyComboBox.Items.Add(font);
        }
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            _fontWeightComboBox.Items.Add(weight);
        }

        _longitudeDmsDirectionComboBox.Items.Add("东经");
        _longitudeDmsDirectionComboBox.Items.Add("西经");

        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeTextBox, OnLongitudeLostFocus);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsDegreesTextBox, OnLongitudeDmsValueChanged);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsMinutesTextBox, OnLongitudeDmsValueChanged);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_longitudeDmsSecondsTextBox, OnLongitudeDmsValueChanged);
        _longitudeDmsDirectionComboBox.SelectionChanged += OnLongitudeDmsValueChanged;
    }

    private void OnEnableCustomFontSizeChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontSize = _enableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontColor = _enableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontFamilyChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontFamily = _enableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontWeightChanged(object? sender, RoutedEventArgs e)
    {
        Settings.EnableCustomFontWeight = _enableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_fontFamilyComboBox.SelectedItem != null)
        {
            Settings.FontFamily = _fontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnFontWeightChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_fontWeightComboBox.SelectedItem != null)
        {
            Settings.FontWeight = _fontWeightComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void UpdateControlsEnabled()
    {
        var fontSizeEnabled = Settings.EnableCustomFontSize;
        var fontColorEnabled = Settings.EnableCustomFontColor;
        var fontFamilyEnabled = Settings.EnableCustomFontFamily;
        var fontWeightEnabled = Settings.EnableCustomFontWeight;
        _colorPicker.IsEnabled = fontColorEnabled;
        _fontSizeNumericUpDown.IsEnabled = fontSizeEnabled;
        _fontFamilyComboBox.IsEnabled = fontFamilyEnabled;
        _fontWeightComboBox.IsEnabled = fontWeightEnabled;
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
        UpdateDmsFromLongitude();
        if (_pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms)
        {
            _longitudeTextBox.Visibility = Visibility.Collapsed;
            _longitudeDmsPanel.Visibility = Visibility.Visible;
        }
        else
        {
            _longitudeTextBox.Visibility = Visibility.Visible;
            _longitudeDmsPanel.Visibility = Visibility.Collapsed;
        }
        _enableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        _enableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
        _enableCustomFontFamilyToggle.IsChecked = Settings.EnableCustomFontFamily;
        _enableCustomFontWeightToggle.IsChecked = Settings.EnableCustomFontWeight;
        UpdateControlsEnabled();
        _colorPicker.Color = ParseColor(Settings.FontColor);
        _fontSizeNumericUpDown.Value = (decimal)Settings.TextFontSize;
        _fontFamilyComboBox.SelectedItem = Settings.FontFamily;
        _fontWeightComboBox.SelectedItem = Settings.FontWeight;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (_pluginSettings != null)
        {
            _pluginSettings.PropertyChanged -= OnPluginSettingsPropertyChanged;
        }
    }

    private void UpdateDmsFromLongitude()
    {
        LongitudeConverter.DecomposeDms(Settings.Longitude, out int d, out int m, out double s, out bool isEast);
        _longitudeDmsDegreesTextBox.Text = d.ToString();
        _longitudeDmsMinutesTextBox.Text = m.ToString();
        _longitudeDmsSecondsTextBox.Text = s.ToString("F2");
        _longitudeDmsDirectionComboBox.SelectedIndex = isEast ? 0 : 1;
    }

    private void OnLongitudeLostFocus(object? sender, RoutedEventArgs e)
    {
        if (LongitudeConverter.TryParseDecimal(_longitudeTextBox.Text, out double lon))
        {
            Settings.Longitude = lon;
            _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(lon);
            UpdateDmsFromLongitude();
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
            UpdateDmsFromLongitude();
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
                var longitude = Math.Round(location.Value, 4);
                Settings.Longitude = longitude;
                if (_pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms)
                {
                    UpdateDmsFromLongitude();
                    _statusText.Text = $"已获取位置：经度 {LongitudeConverter.ToDmsString(longitude)}";
                }
                else
                {
                    _longitudeTextBox.Text = longitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
                    _statusText.Text = $"已获取位置：经度 {longitude:F4}°";
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

    private async Task<double?> GetLocationAsync()
    {
        return await GetLocationByIpAsync();
    }

    private async Task<double?> GetLocationByIpAsync()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetStringAsync("http://ip-api.com/json/?fields=lon");

            var json = System.Text.Json.JsonDocument.Parse(response);
            if (json.RootElement.TryGetProperty("lon", out var lonElement))
            {
                return lonElement.GetDouble();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void OnColorChanged(object? sender, ColorChangedEventArgs e)
    {
        Settings.FontColor = _colorPicker.Color.ToString();
    }

    private Color ParseColor(string colorString)
    {
        try
        {
            return ThemeHelper.ParseColor(colorString);
        }
        catch
        {
            return ThemeHelper.ParseColor(ThemeHelper.GetTextColorHex());
        }
    }

    private void OnFontSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_fontSizeNumericUpDown.Value.HasValue)
        {
            Settings.TextFontSize = (double)_fontSizeNumericUpDown.Value.Value;
        }
    }

    private void OnPluginSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginSettings.LongitudeDisplayMode))
        {
            UpdateLongitudeDisplay();
        }
    }

    private void UpdateLongitudeDisplay()
    {
        if (_pluginSettings == null)
            return;

        _longitudeTextBox.Visibility = _pluginSettings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal ? Visibility.Visible : Visibility.Collapsed;
        _longitudeDmsPanel.Visibility = _pluginSettings.LongitudeDisplayMode == LongitudeDisplayMode.Dms ? Visibility.Visible : Visibility.Collapsed;

        if (_pluginSettings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal)
        {
            _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(Settings.Longitude);
        }
        else
        {
            UpdateDmsFromLongitude();
        }
    }
}
