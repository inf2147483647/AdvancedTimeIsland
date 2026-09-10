using System;
using System.Collections.Generic;
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

public class LocalSolarTimeSettingsControl : ComponentBase<LocalSolarTimeSettings>
{
    private TextBox _longitudeTextBox;
    private TextBox _longitudeDmsDegreesTextBox;
    private TextBox _longitudeDmsMinutesTextBox;
    private TextBox _longitudeDmsSecondsTextBox;
    private ComboBox _longitudeDmsDirectionComboBox;
    private Panel _longitudeDmsPanel;
    private ColorPicker _colorPicker;
    private NumericUpDown _fontSizeNumericUpDown;
    private CheckBox _enableCustomFontSizeToggle;
    private CheckBox _enableCustomFontColorToggle;
    private CheckBox _enableCustomFontFamilyToggle;
    private CheckBox _enableCustomFontWeightToggle;
    private ComboBox _fontFamilyComboBox;
    private ComboBox _fontWeightComboBox;
    private Button _getLocationButton;
    private TextBlock _statusText;
    private readonly PluginSettings? _pluginSettings;

    private readonly List<TextBlock> _dynamicTextBlocks = new();
    private readonly List<Border> _tableCellBorders = new();

    private TextBlock _titleTextBlock;
    private TextBlock _longitudeLabelTextBlock;
    private TextBlock _dmsDegreeSymbol;
    private TextBlock _dmsMinuteSymbol;
    private TextBlock _dmsSecondSymbol;
    private TextBlock _hintTextBlock;

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

        // ==================== 位置设置 ====================
        var locationPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

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
        locationPanel.Children.Add(longitudeRow);

        _statusText = new TextBlock { Text = "", FontSize = 11, TextWrapping = TextWrapping.Wrap };
        locationPanel.Children.Add(_statusText);

        _hintTextBlock = new TextBlock { Text = "取值范围为(-180到180]，单位为度，正数为东经，负数为西经", FontSize = 11, TextWrapping = TextWrapping.Wrap };
        locationPanel.Children.Add(_hintTextBlock);

        sp.Children.Add(SettingsGroupFactory.Create("位置设置", locationPanel));

        // ==================== 文案设置 ====================
        var textPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

        var tableScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = CreateFontStyleTable()
        };
        textPanel.Children.Add(tableScroll);
        textPanel.Children.Add(CreateFontWeightHintTextBlock());

        sp.Children.Add(SettingsGroupFactory.Create("文案设置", textPanel));

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
        Content = scrollViewer;
    }

    // ==================== 字体样式表格 ====================

    private Grid CreateFontStyleTable()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }
        for (int i = 0; i < 2; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddTableHeader(grid, 0, 0, "样式");
        AddTableHeader(grid, 0, 1, "自定义大小");
        AddTableHeader(grid, 0, 2, "自定义颜色");
        AddTableHeader(grid, 0, 3, "自定义字体");
        AddTableHeader(grid, 0, 4, "自定义字重");

        AddTableRowLabel(grid, 1, "地方时");
        AddTableCell(grid, 1, 1, CreateSizeCell(out _fontSizeNumericUpDown, out _enableCustomFontSizeToggle, OnEnableCustomFontSizeChanged, OnFontSizeChanged));
        AddTableCell(grid, 1, 2, CreateColorCell(out _colorPicker, out _enableCustomFontColorToggle, OnEnableCustomFontColorChanged, OnColorChanged));
        AddTableCell(grid, 1, 3, CreateFamilyCell(out _fontFamilyComboBox, out _enableCustomFontFamilyToggle, OnEnableCustomFontFamilyChanged, OnFontFamilyChanged));
        AddTableCell(grid, 1, 4, CreateWeightCell(out _fontWeightComboBox, out _enableCustomFontWeightToggle, OnEnableCustomFontWeightChanged, OnFontWeightChanged));

        // 表格外框（上边与左边），单元格自带右边与下边线，拼合为完整网格
        var outerBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 0),
            BorderBrush = ThemeHelper.GetSeparatorBrush(),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(outerBorder, 2);
        Grid.SetColumnSpan(outerBorder, 5);
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

    private static StackPanel CreateSizeCell(out NumericUpDown numericUpDown, out CheckBox toggle,
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

    private static StackPanel CreateColorCell(out ColorPicker colorPicker, out CheckBox toggle,
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

    private static StackPanel CreateFamilyCell(out ComboBox comboBox, out CheckBox toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        comboBox = new ComboBox { Width = 150, VerticalAlignment = VerticalAlignment.Center };
        foreach (var font in FontFamilyHelper.GetSystemFontFamilies())
        {
            comboBox.Items.Add(font);
        }
        comboBox.SelectionChanged += selectionChangedHandler;
        panel.Children.Add(comboBox);
        return panel;
    }

    private static StackPanel CreateWeightCell(out ComboBox comboBox, out CheckBox toggle,
        EventHandler<RoutedEventArgs> toggleHandler, EventHandler<SelectionChangedEventArgs> selectionChangedHandler)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toggle = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
        toggle.IsCheckedChanged += toggleHandler;
        panel.Children.Add(toggle);
        comboBox = new ComboBox { Width = 110, VerticalAlignment = VerticalAlignment.Center };
        foreach (var weight in FontFamilyHelper.GetFontWeights())
        {
            comboBox.Items.Add(weight);
        }
        comboBox.SelectionChanged += selectionChangedHandler;
        panel.Children.Add(comboBox);
        return panel;
    }

    private TextBlock CreateFontWeightHintTextBlock()
    {
        return new TextBlock
        {
            Text = "需要对应字体支持所选字重",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.Orange,
            Margin = new Thickness(0, 2, 0, 0)
        };
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
        _enableCustomFontFamilyToggle.Foreground = ThemeHelper.GetTextBrush();
        _enableCustomFontWeightToggle.Foreground = ThemeHelper.GetTextBrush();

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

    private void OnEnableCustomFontFamilyChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontFamily = _enableCustomFontFamilyToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnEnableCustomFontWeightChanged(object? sender, EventArgs e)
    {
        Settings.EnableCustomFontWeight = _enableCustomFontWeightToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnFontFamilyChanged(object? sender, EventArgs e)
    {
        if (_fontFamilyComboBox.SelectedItem != null)
        {
            Settings.FontFamily = _fontFamilyComboBox.SelectedItem.ToString() ?? "";
        }
    }

    private void OnFontWeightChanged(object? sender, EventArgs e)
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
        _enableCustomFontFamilyToggle.IsChecked = Settings.EnableCustomFontFamily;
        _enableCustomFontWeightToggle.IsChecked = Settings.EnableCustomFontWeight;
        UpdateControlsEnabled();
        _colorPicker.Color = ParseColor(Settings.FontColor);
        _fontSizeNumericUpDown.Value = (decimal)Settings.TextFontSize;
        _fontFamilyComboBox.SelectedItem = Settings.FontFamily;
        _fontWeightComboBox.SelectedItem = Settings.FontWeight;
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
            return Color.Parse(colorString);
        }
        catch
        {
            return Color.Parse(ThemeHelper.GetTextColorHex());
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