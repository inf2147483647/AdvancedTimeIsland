using System;
using System.Globalization;
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

public class FpsMonitorSettingsControl : ComponentBase<FpsMonitorSettings>
{
    private TextBox _labelColorTextBox;
    private TextBox _labelFontSizeTextBox;
    private TextBox _valueColorTextBox;
    private TextBox _valueFontSizeTextBox;
    private ToggleSwitch _labelEnableCustomFontSizeToggle;
    private ToggleSwitch _labelEnableCustomFontColorToggle;
    private ToggleSwitch _valueEnableCustomFontSizeToggle;
    private ToggleSwitch _valueEnableCustomFontColorToggle;
    private ToggleSwitch _enableComponentToggle;

    private TextBlock _labelTitleTextBlock;
    private TextBlock _labelColorLabelTextBlock;
    private TextBlock _labelFontSizeLabelTextBlock;
    private TextBlock _valueTitleTextBlock;
    private TextBlock _valueColorNoteTextBlock;
    private TextBlock _valueColorLabelTextBlock;
    private TextBlock _valueFontSizeLabelTextBlock;

    private bool _isInDialogFlow;

    public FpsMonitorSettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var sp = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        _enableComponentToggle = new ToggleSwitch { Content = "启用此组件", Margin = new Thickness(0, 10, 0, 0) };
        _enableComponentToggle.IsCheckedChanged += OnEnableComponentToggleChanged;
        sp.Children.Add(_enableComponentToggle);

        _labelTitleTextBlock = new TextBlock { Text = "标签样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_labelTitleTextBlock);

        var labelColorRow = new Grid();
        labelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _labelColorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_labelColorLabelTextBlock, 0);
        labelColorRow.Children.Add(_labelColorLabelTextBlock);

        _labelColorTextBox = new TextBox { Width = 120, Watermark = ThemeHelper.GetTextColorHex() };
        Grid.SetColumn(_labelColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_labelColorTextBox, OnLabelColorLostFocus);
        labelColorRow.Children.Add(_labelColorTextBox);
        sp.Children.Add(labelColorRow);

        _labelEnableCustomFontColorToggle = new ToggleSwitch { Content = "使用自定义颜色" };
        _labelEnableCustomFontColorToggle.IsCheckedChanged += OnLabelEnableCustomFontColorChanged;
        sp.Children.Add(_labelEnableCustomFontColorToggle);

        var labelFontSizeRow = new Grid();
        labelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        labelFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _labelFontSizeLabelTextBlock = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_labelFontSizeLabelTextBlock, 0);
        labelFontSizeRow.Children.Add(_labelFontSizeLabelTextBlock);

        _labelFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_labelFontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_labelFontSizeTextBox, OnLabelFontSizeLostFocus);
        labelFontSizeRow.Children.Add(_labelFontSizeTextBox);
        sp.Children.Add(labelFontSizeRow);

        _labelEnableCustomFontSizeToggle = new ToggleSwitch { Content = "使用自定义大小" };
        _labelEnableCustomFontSizeToggle.IsCheckedChanged += OnLabelEnableCustomFontSizeChanged;
        sp.Children.Add(_labelEnableCustomFontSizeToggle);

        _valueTitleTextBlock = new TextBlock { Text = "值样式", FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) };
        sp.Children.Add(_valueTitleTextBlock);

        _valueColorNoteTextBlock = new TextBlock { Text = "默认颜色根据FPS自动变化（>=30绿色，20-30黄色，<20红色），启用自定义颜色后将使用固定颜色", FontSize = 12, Foreground = ThemeHelper.GetSubTextBrush(), TextWrapping = TextWrapping.Wrap };
        sp.Children.Add(_valueColorNoteTextBlock);

        var valueColorRow = new Grid();
        valueColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueColorRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _valueColorLabelTextBlock = new TextBlock { Text = "颜色:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_valueColorLabelTextBlock, 0);
        valueColorRow.Children.Add(_valueColorLabelTextBlock);

        _valueColorTextBox = new TextBox { Width = 120, Watermark = ThemeHelper.GetTextColorHex() };
        Grid.SetColumn(_valueColorTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_valueColorTextBox, OnValueColorLostFocus);
        valueColorRow.Children.Add(_valueColorTextBox);
        sp.Children.Add(valueColorRow);

        _valueEnableCustomFontColorToggle = new ToggleSwitch { Content = "使用自定义颜色" };
        _valueEnableCustomFontColorToggle.IsCheckedChanged += OnValueEnableCustomFontColorChanged;
        sp.Children.Add(_valueEnableCustomFontColorToggle);

        var valueFontSizeRow = new Grid();
        valueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        valueFontSizeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _valueFontSizeLabelTextBlock = new TextBlock { Text = "字体大小:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(_valueFontSizeLabelTextBlock, 0);
        valueFontSizeRow.Children.Add(_valueFontSizeLabelTextBlock);

        _valueFontSizeTextBox = new TextBox { Width = 80, Watermark = "14" };
        Grid.SetColumn(_valueFontSizeTextBox, 1);
        FluentAvaloniaCompatibilityHelper.AddLostFocusHandler(_valueFontSizeTextBox, OnValueFontSizeLostFocus);
        valueFontSizeRow.Children.Add(_valueFontSizeTextBox);
        sp.Children.Add(valueFontSizeRow);

        _valueEnableCustomFontSizeToggle = new ToggleSwitch { Content = "使用自定义大小" };
        _valueEnableCustomFontSizeToggle.IsCheckedChanged += OnValueEnableCustomFontSizeChanged;
        sp.Children.Add(_valueEnableCustomFontSizeToggle);

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
        _enableComponentToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontSizeToggle.Foreground = ThemeHelper.GetTextBrush();
        _valueEnableCustomFontColorToggle.Foreground = ThemeHelper.GetTextBrush();
        _labelTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _labelColorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _labelFontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueTitleTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueColorNoteTextBlock.Foreground = ThemeHelper.GetSubTextBrush();
        _valueColorLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
        _valueFontSizeLabelTextBlock.Foreground = ThemeHelper.GetTextBrush();
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeColors();
    }

    private void OnLabelEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontSize = _labelEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnLabelEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.LabelEnableCustomFontColor = _labelEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontSizeChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontSize = _valueEnableCustomFontSizeToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private void OnValueEnableCustomFontColorChanged(object? sender, EventArgs e)
    {
        Settings.ValueEnableCustomFontColor = _valueEnableCustomFontColorToggle.IsChecked ?? false;
        UpdateControlsEnabled();
    }

    private async void OnEnableComponentToggleChanged(object? sender, EventArgs e)
    {
        if (_enableComponentToggle.IsChecked ?? false)
        {
            if (Settings.EnableComponent)
            {
                return;
            }
            await StartEnableFlow();
        }
        else
        {
            Settings.EnableComponent = false;
        }
    }

    private async Task<bool> ShowEpilepsyWarningDialogAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return false;

        var contentPanel = new StackPanel();

        var warningTextBlock = new TextBlock
        {
            Text = "有极少数的人在观看一些视觉影像时可能会突然癫痫发作，这些影像包括快速改变的数字或图形。在使用此组件时，这些人可能会出现癫痫症状。甚至连不具有癫痫史的人，也可能在查看此组件时出现类似癫痫症状。\n\n" +
                   "如果您或您的家人有癫痫史，请在添加此组件之前先与医生咨询。如果您在使用此组件时出现以下症状，包括眼睛疼痛、视觉异常、偏头痛、痉挛或意识障碍（诸如昏迷）等，请立即中止使用，并且请您于再次使用此组件之前咨询您的医生。\n\n" +
                   "除上述症状外，当您感到头痛、头晕眼花、恶心想吐或类似晕车症状时，以及当身体的某些部位感到不舒服或疼痛时，请立即中止使用。若在中止使用后，症状仍没有减退，请立即寻求医生的诊疗。",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(12)
        };
        contentPanel.Children.Add(warningTextBlock);

        var countDownTextBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 8, 0, 0),
            Text = "请阅读以上内容，确定按钮将在15秒后可用..."
        };
        contentPanel.Children.Add(countDownTextBlock);

        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "警告：使用前详阅");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", contentPanel);
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定（15）");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "CloseButtonText", "取消");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "IsPrimaryButtonEnabled", false);

        _ = Task.Run(async () =>
        {
            for (int i = 15; i >= 0; i--)
            {
                await Task.Delay(1000);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (i > 0)
                    {
                        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", $"确定（{i}）");
                    }
                    else
                    {
                        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
                        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "IsPrimaryButtonEnabled", true);
                    }
                });
            }
        });

        var result = await FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, topLevel);
        return FluentAvaloniaCompatibilityHelper.IsContentDialogResultPrimary(result);
    }

    private async Task<bool> ShowDebugWarningDialogAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return false;

        var dialog = FluentAvaloniaCompatibilityHelper.CreateContentDialog();
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Title", "警告");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "Content", new TextBlock
        {
            Text = "此组件仅供调试，严禁用于教学环境！！！",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(12)
        });
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "PrimaryButtonText", "确定");
        FluentAvaloniaCompatibilityHelper.SetContentDialogProperty(dialog, "CloseButtonText", "取消");

        var result = await FluentAvaloniaCompatibilityHelper.ShowContentDialogAsync(dialog, topLevel);
        return FluentAvaloniaCompatibilityHelper.IsContentDialogResultPrimary(result);
    }

    private async Task StartEnableFlow()
    {
        if (_isInDialogFlow) return;
        _isInDialogFlow = true;

        try
        {
            bool epilepsyAccepted = await ShowEpilepsyWarningDialogAsync();
            if (!epilepsyAccepted)
            {
                Settings.EnableComponent = false;
                _enableComponentToggle.IsChecked = false;
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                bool debugAccepted = await ShowDebugWarningDialogAsync();
                if (!debugAccepted)
                {
                    Settings.EnableComponent = false;
                    _enableComponentToggle.IsChecked = false;
                    return;
                }
            }

            Settings.EnableComponent = true;
        }
        finally
        {
            _isInDialogFlow = false;
        }
    }

    private void UpdateControlsEnabled()
    {
        _labelColorTextBox.IsEnabled = Settings.LabelEnableCustomFontColor;
        _labelFontSizeTextBox.IsEnabled = Settings.LabelEnableCustomFontSize;
        _valueColorTextBox.IsEnabled = Settings.ValueEnableCustomFontColor;
        _valueFontSizeTextBox.IsEnabled = Settings.ValueEnableCustomFontSize;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        UpdateThemeColors();
        _enableComponentToggle.IsChecked = Settings.EnableComponent;
        _labelEnableCustomFontSizeToggle.IsChecked = Settings.LabelEnableCustomFontSize;
        _labelEnableCustomFontColorToggle.IsChecked = Settings.LabelEnableCustomFontColor;
        _valueEnableCustomFontSizeToggle.IsChecked = Settings.ValueEnableCustomFontSize;
        _valueEnableCustomFontColorToggle.IsChecked = Settings.ValueEnableCustomFontColor;
        UpdateControlsEnabled();
        _labelColorTextBox.Text = Settings.LabelFontColor;
        _labelFontSizeTextBox.Text = Settings.LabelFontSize.ToString(CultureInfo.InvariantCulture);
        _valueColorTextBox.Text = Settings.ValueFontColor;
        _valueFontSizeTextBox.Text = Settings.ValueFontSize.ToString(CultureInfo.InvariantCulture);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
    }

    private void OnLabelColorLostFocus(object? sender, EventArgs e)
    {
        var color = _labelColorTextBox.Text ?? ThemeHelper.GetTextColorHex();
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try
            {
                Avalonia.Media.Color.Parse(color);
                Settings.LabelFontColor = color;
            }
            catch
            {
                _labelColorTextBox.Text = Settings.LabelFontColor;
            }
        }
        else
        {
            _labelColorTextBox.Text = Settings.LabelFontColor;
        }
    }

    private void OnLabelFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_labelFontSizeTextBox.Text, out double size))
        {
            Settings.LabelFontSize = size;
            _labelFontSizeTextBox.Text = Settings.LabelFontSize.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            _labelFontSizeTextBox.Text = Settings.LabelFontSize.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void OnValueFontSizeLostFocus(object? sender, EventArgs e)
    {
        if (double.TryParse(_valueFontSizeTextBox.Text, out double size))
        {
            Settings.ValueFontSize = size;
            _valueFontSizeTextBox.Text = Settings.ValueFontSize.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            _valueFontSizeTextBox.Text = Settings.ValueFontSize.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void OnValueColorLostFocus(object? sender, EventArgs e)
    {
        var color = _valueColorTextBox.Text ?? ThemeHelper.GetTextColorHex();
        if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
        {
            try
            {
                Avalonia.Media.Color.Parse(color);
                Settings.ValueFontColor = color;
            }
            catch
            {
                _valueColorTextBox.Text = Settings.ValueFontColor;
            }
        }
        else
        {
            _valueColorTextBox.Text = Settings.ValueFontColor;
        }
    }
}
