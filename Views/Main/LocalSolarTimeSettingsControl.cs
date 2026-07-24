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
    private ToggleSwitch _enableCustomFontSizeToggle;
    private ToggleSwitch _enableCustomFontColorToggle;
    private Button _getLocationButton;
    private TextBlock _statusText;
    private readonly PluginSettings? _pluginSettings;

    private TextBlock _titleTextBlock;
    private TextBlock _longitudeLabelTextBlock;
    private TextBlock _dmsDegreeSymbol;
    private TextBlock _dmsMinuteSymbol;
    private TextBlock _dmsSecondSymbol;
    private TextBlock _hintTextBlock;
    private TextBlock _styleTitleTextBlock;
    private TextBlock _colorLabelTextBlock;
    private TextBlock _fontSizeLabelTextBlock;

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

        _titleTextBlock = new TextBlock { Text = "经度设置", FontSize = 14, FontWeight = FontWeight.Bold };
        sp.Children.Add(_titleTextBlock);

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

        _statusText = new TextBlock { Text = "", FontSize = 11, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_statusText);

        _hintTextBlock = new TextBlock { Text = "取值范围为(-180到180]，单位为度，正数为东经，负数为西经", FontSize = 11, TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_hintTextBlock);

        _enableCustomFontSizeToggle = new ToggleSwitch { Content = "启用自定义字体大小", Margin = new Thickness(0, 10, 0, 0) };
        _enableCustomFontSizeToggle.IsCheckedChanged += OnEnableCustomFontSizeChanged;
        sp.Children.Add(_enableCustomFontSizeToggle);

        _enableCustomFontColorToggle = new ToggleSwitch { Content = "启用自定义字体颜色", Margin = new Thickness(0, 4, 0, 0) };
        _enableCustomFontColorToggle.IsCheckedChanged += OnEnableCustomFontColorChanged;
        sp.Children.Add(_enableCustomFontColorToggle);

        _styleTitleTextBlock = new TextBlock { Text = "字体样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_styleTitleTextBlock);

        var colorRow = new Grid();
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        colorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _colorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_colorLabelTextBlock, 0);
        colorRow.Children.Add(_colorLabelTextBlock);

        _colorTextBox = new TextBox { Width = 120, Watermark = "#FFFFFF" };
        Grid.SetColumn(_colorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_colorTextBox, OnColorLostFocus);
        colorRow.Children.Add(_colorTextBox);
        sp.Children.Add(colorRow);

        var fontSizeRow = new Grid();
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _fontSizeLabelTextBlock = new TextBlock { Text = "文本大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_fontSizeLabelTextBlock, 0);
        fontSizeRow.Children.Add(_fontSizeLabelTextBlock);

        _fontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_fontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_fontSizeTextBox, OnFontSizeLostFocus);
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

    private void UpdateThemeColors()
    {
        _titleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _longitudeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _dmsDegreeSymbol.Foreground = ThemeHelper.GetTextBrush();
        _dmsMinuteSymbol.Foreground = ThemeHelper.GetTextBrush();
        _dmsSecondSymbol.Foreground = ThemeHelper.GetTextBrush();
        _statusText.Foreground = ThemeHelper.GetGrayBrush();
        _hintTextBlock.Foreground = ThemeHelper.GetGrayBrush();
        _enableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _enableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _styleTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _colorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _fontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void OnEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontSize = _enableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontColor = _enableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void UpdateControlsEnabled()
    {
        var fontSizeEnabled = Settings.EnableCustomFontSize;
        var fontColorEnabled = Settings.EnableCustomFontColor;
        _colorTextBox.IsEnabled = fontColorEnabled;
        _fontSizeTextBox.IsEnabled = fontSizeEnabled;
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
        _enableCustomFontSizeToggle.IsChecked = Settings.EnableCustomFontSize;
        _enableCustomFontColorToggle.IsChecked = Settings.EnableCustomFontColor;
        UpdateControlsEnabled();
        _colorTextBox.Text = Settings.FontColor;
        _fontSizeTextBox.Text = Settings.TextFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
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
        // 直接使用基于 IP 的定位，避免引入 WinRT/Windows SDK 依赖（可显著减小安装包体积）
        // 精度为城市级，对本地真太阳时计算已足够
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


