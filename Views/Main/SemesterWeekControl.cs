using System;
using AdvancedTimeIsland.Helpers;
using AdvancedTimeIsland.Models;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.ViewModels.Main;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Views.Main;

[ComponentInfo(
    "99aabbcc-0001-2233-4455-66778899aa02",
    "今日周数（学期）（ATI）",
    "\uE122",
    "显示当前周所处的学期周数（以ClassIsland学期开始日为第一周第一天）"
)]
public class SemesterWeekControl : ComponentBase<SemesterWeekSettings>
{
    private SemesterWeekViewModel vm;
    private TextBlock _textBlock;
    private readonly TimeBaseService _timeBaseService;

    public SemesterWeekControl(TimeBaseService tbs)
    {
        _timeBaseService = tbs;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var border = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _textBlock = new TextBlock { Text = "..." };
        border.Child = _textBlock;
        Content = border;
    }

    private void UpdateFontColor(string colorStr)
    {
        _textBlock.Foreground = ThemeHelper.GetColorBrush(colorStr, Settings.EnableCustomFontColor);
    }

    private void UpdateFontSize(double fontSize)
    {
        if (fontSize > 0)
            _textBlock.FontSize = fontSize;
        else
            _textBlock.FontSize = FontFamilyHelper.GetBodyFontSize(_textBlock);
    }

    private void UpdateFontFamily()
    {
        if (Settings.EnableCustomFontFamily)
            _textBlock.FontFamily = FontFamilyHelper.GetFontFamilyOrDefault(Settings.FontFamily);
        else
            _textBlock.ClearValue(TextBlock.FontFamilyProperty);
    }

    private void UpdateFontWeight()
    {
        if (Settings.EnableCustomFontWeight)
            _textBlock.FontWeight = FontFamilyHelper.GetFontWeightFromString(Settings.FontWeight);
        else
            _textBlock.ClearValue(TextBlock.FontWeightProperty);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateFontColor(Settings.FontColor);
    }

    private void OnBodyFontSizeChanged(object? sender, EventArgs e)
    {
        UpdateFontSize(Settings.EnableCustomFontSize ? Settings.FontSize : 0);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged += OnBodyFontSizeChanged;
        vm = new SemesterWeekViewModel(_timeBaseService, Settings, UpdateFontColor, UpdateFontSize);
        DataContext = vm;
        _textBlock.Text = vm.DisplayText;
        vm.PropertyChanged += OnVmPropertyChanged;
        UpdateFontColor(Settings.FontColor);
        UpdateFontSize(Settings.EnableCustomFontSize ? Settings.FontSize : 0);
        UpdateFontFamily();
        UpdateFontWeight();
        Settings.PropertyChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.FontFamily) ||
            e.PropertyName == nameof(Settings.EnableCustomFontFamily))
        {
            UpdateFontFamily();
        }
        else if (e.PropertyName == nameof(Settings.FontWeight) ||
                 e.PropertyName == nameof(Settings.EnableCustomFontWeight))
        {
            UpdateFontWeight();
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(vm.DisplayText))
        {
            _textBlock.Text = vm.DisplayText;
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current != null)
        {
            Application.Current.ActualThemeVariantChanged -= OnThemeVariantChanged;
        }
        FontFamilyHelper.BodyFontSizeChanged -= OnBodyFontSizeChanged;
        Settings.PropertyChanged -= OnSettingsChanged;
        vm.PropertyChanged -= OnVmPropertyChanged;
        vm.Dispose();
    }
}