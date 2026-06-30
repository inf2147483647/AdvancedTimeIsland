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
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;

namespace AdvancedTimeIsland.Views.Main;

public class LocalSolarTimeSettingsControl : ComponentBase<LocalSolarTimeSettings>
{
    private TextBox _longitudeTextBox;
    private TextBox _longitudeDmsDegreesTextBox;
    private TextBox _longitudeDmsMinutesTextBox;
    private TextBox _longitudeDmsSecondsTextBox;
    private ComboBox _longitudeDmsDirectionComboBox;
    private Panel _longitudeDmsPanel;
    private TextBox _colorTextBox;
    private TextBox _fontSizeTextBox;
    private Button _getLocationButton;
    private TextBlock _statusText;
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
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        // 经度设置
        var title = new TextBlock { Text = "经度设置", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
        sp.Children.Add(title);

        // 经度输入行：标签 + 输入框 + 获取位置按钮
        var longitudeRow = new Grid();
        longitudeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        longitudeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        longitudeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var longitudeLabel = new TextBlock { Text = "经度:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(longitudeLabel, 0);
        longitudeRow.Children.Add(longitudeLabel);

        var isDms = _pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms;

        _longitudeTextBox = new TextBox { Width = 120, HorizontalAlignment = HorizontalAlignment.Left, IsVisible = !isDms };
        Grid.SetColumn(_longitudeTextBox, 1);
        _longitudeTextBox.LostFocus += OnLongitudeLostFocus;
        longitudeRow.Children.Add(_longitudeTextBox);

        _longitudeDmsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Left, IsVisible = isDms };
        Grid.SetColumn(_longitudeDmsPanel, 1);

        _longitudeDmsDegreesTextBox = new TextBox { Width = 50, Watermark = "度" };
        _longitudeDmsDegreesTextBox.LostFocus += OnLongitudeDmsValueChanged;
        _longitudeDmsPanel.Children.Add(_longitudeDmsDegreesTextBox);
        _longitudeDmsPanel.Children.Add(new TextBlock { Text = "°", VerticalAlignment = VerticalAlignment.Center });

        _longitudeDmsMinutesTextBox = new TextBox { Width = 45, Watermark = "分" };
        _longitudeDmsMinutesTextBox.LostFocus += OnLongitudeDmsValueChanged;
        _longitudeDmsPanel.Children.Add(_longitudeDmsMinutesTextBox);
        _longitudeDmsPanel.Children.Add(new TextBlock { Text = "′", VerticalAlignment = VerticalAlignment.Center });

        _longitudeDmsSecondsTextBox = new TextBox { Width = 45, Watermark = "秒" };
        _longitudeDmsSecondsTextBox.LostFocus += OnLongitudeDmsValueChanged;
        _longitudeDmsPanel.Children.Add(_longitudeDmsSecondsTextBox);
        _longitudeDmsPanel.Children.Add(new TextBlock { Text = "″", VerticalAlignment = VerticalAlignment.Center });

        _longitudeDmsDirectionComboBox = new ComboBox { Width = 90 };
        _longitudeDmsDirectionComboBox.Items.Add("东经");
        _longitudeDmsDirectionComboBox.Items.Add("西经");
        _longitudeDmsDirectionComboBox.SelectedIndex = 0;
        _longitudeDmsDirectionComboBox.SelectionChanged += OnLongitudeDmsValueChanged;
        _longitudeDmsPanel.Children.Add(_longitudeDmsDirectionComboBox);

        longitudeRow.Children.Add(_longitudeDmsPanel);

        _getLocationButton = new Button
        {
            Content = "获取当前位置",
            Padding = new Thickness(8, 4),
            Margin = new Thickness(8, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(_getLocationButton, 2);
        _getLocationButton.Click += OnGetLocationClick;
        longitudeRow.Children.Add(_getLocationButton);
        sp.Children.Add(longitudeRow);

        // 状态提示
        _statusText = new TextBlock { Text = "", FontSize = 11, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_statusText);

        // 提示
        var hint = new TextBlock { Text = "取值范围为(-180到180]，单位为度，正数为东经，负数为西经", FontSize = 11, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(hint);

        // 字体样式设置
        var styleTitle = new TextBlock { Text = "字体样式", FontSize = 14, FontWeight = FontWeight.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(styleTitle);

        // 字体颜色设置
        var colorRow = new Grid();
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var colorLabel = new TextBlock { Text = "颜色:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(colorLabel, 0);
        colorRow.Children.Add(colorLabel);

        _colorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_colorTextBox, 1);
        _colorTextBox.LostFocus += OnColorLostFocus;
        colorRow.Children.Add(_colorTextBox);
        sp.Children.Add(colorRow);

        // 字体大小设置
        var fontSizeRow = new Grid();
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var fontSizeLabel = new TextBlock { Text = "文本大小:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(fontSizeLabel, 0);
        fontSizeRow.Children.Add(fontSizeLabel);

        _fontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_fontSizeTextBox, 1);
        _fontSizeTextBox.LostFocus += OnFontSizeLostFocus;
        fontSizeRow.Children.Add(_fontSizeTextBox);
        sp.Children.Add(fontSizeRow);

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _longitudeTextBox.Text = LongitudeConverter.ToDecimalString(Settings.Longitude);
        UpdateDmsFromLongitude();
        if (_pluginSettings?.LongitudeDisplayMode == LongitudeDisplayMode.Dms)
        {
            _longitudeTextBox.IsVisible = false;
            _longitudeDmsPanel.IsVisible = true;
        }
        else
        {
            _longitudeTextBox.IsVisible = true;
            _longitudeDmsPanel.IsVisible = false;
        }
        _colorTextBox.Text = Settings.FontColor;
        _fontSizeTextBox.Text = Settings.TextFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void UpdateDmsFromLongitude()
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

    private async void OnGetLocationClick(object? sender, EventArgs e)
    {
        _getLocationButton.IsEnabled = false;
        _getLocationButton.Content = "获取中...";
        _statusText.Text = "正在获取位置...";
        _statusText.Foreground = Brushes.Orange;

        try
        {
            var location = await GetLocationAsync();
            if (location != null)
            {
                // 精确到0.0001（保留4位小数）
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
                _statusText.Foreground = Brushes.LightGreen;
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
        try
        {
            // 使用 Windows.Devices.Geolocation 获取位置
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
                    return position.Coordinate.Point.Position.Longitude;
                }
            }
            
            return null;
        }
        catch (Exception)
        {
            // 如果 Windows API 失败，尝试备用方案
            return await GetLocationByIpAsync();
        }
    }

    /// <summary>
    /// 备用方案：通过 IP 定位服务获取位置
    /// </summary>
    private async Task<double?> GetLocationByIpAsync()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            // 使用免费的 IP 定位 API
            var response = await client.GetStringAsync("http://ip-api.com/json/?fields=lon");
            
            // 解析 JSON 响应
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

    private void OnColorLostFocus(object? sender, EventArgs e)
    {
        var color = _colorTextBox.Text ?? "#FFFFFF";
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try
            {
                Avalonia.Media.Color.Parse(color);
                Settings.FontColor = color;
            }
            catch
            {
                _colorTextBox.Text = Settings.FontColor;
            }
        }
        else
        {
            _colorTextBox.Text = Settings.FontColor;
        }
    }

    private void OnFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_fontSizeTextBox.Text, out double size))
        {
            Settings.TextFontSize = size;
            _fontSizeTextBox.Text = Settings.TextFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            _fontSizeTextBox.Text = Settings.TextFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

        _longitudeTextBox.IsVisible = _pluginSettings.LongitudeDisplayMode == LongitudeDisplayMode.Decimal;
        _longitudeDmsPanel.IsVisible = _pluginSettings.LongitudeDisplayMode == LongitudeDisplayMode.Dms;

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



