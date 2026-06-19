﻿using System;
using AdvancedTimeIsland.ViewModels.Main;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 日期/地方时/区时显示控件
/// 使用纯 C# 代码构建 UI
/// </summary>
public class AdvancedDateControl : UserControl
{
    private readonly AdvancedDateViewModel _viewModel;
    private TextBlock? _exactTimeTextBlock;
    private TextBlock? _dateTextBlock;
    private TextBlock? _timeTextBlock;

    public AdvancedDateControl(AdvancedDateViewModel viewModel)
    {
        _viewModel = viewModel;
        
        // 初始化组件
        InitializeComponent();
        
        // 设置数据上下文
        DataContext = _viewModel;
    }

    /// <summary>
    /// 初始化 UI 组件
    /// </summary>
    private void InitializeComponent()
    {
        // 创建主容器
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8
        };

        // 创建精确时间显示
        _exactTimeTextBlock = new TextBlock
        {
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.White,
            Text = "精确时间加载中..."
        };

        // 创建日期显示
        _dateTextBlock = new TextBlock
        {
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.LightGray,
            Text = ""
        };

        // 创建时间显示
        _timeTextBlock = new TextBlock
        {
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.White,
            Text = ""
        };

        // 添加到主容器
        mainPanel.Children.Add(_exactTimeTextBlock);
        mainPanel.Children.Add(_dateTextBlock);
        mainPanel.Children.Add(_timeTextBlock);

        // 设置控件内容
        Content = mainPanel;

        // 绑定数据
        SetupBindings();
    }

    /// <summary>
    /// 设置数据绑定
    /// </summary>
    private void SetupBindings()
    {
        // 手动绑定（不使用 XAML 绑定）
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    /// <summary>
    /// 视图模型属性变更处理
    /// </summary>
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AdvancedDateViewModel.ExactTime):
                _exactTimeTextBlock!.Text = $"精确时间: {_viewModel.ExactTime}";
                break;
            case nameof(AdvancedDateViewModel.DateDisplay):
                _dateTextBlock!.Text = _viewModel.DateDisplay;
                break;
            case nameof(AdvancedDateViewModel.TimeDisplay):
                _timeTextBlock!.Text = _viewModel.TimeDisplay;
                break;
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        if (_viewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
