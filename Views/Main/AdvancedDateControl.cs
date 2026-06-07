using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Data;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using AdvancedTimeIsland.ViewModels.Main;
using AdvancedTimeIsland.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AdvancedTimeIsland.Views.Main;

/// <summary>
/// 高级日期显示控件，支持自定义日期格式
/// </summary>
[ComponentInfo(
    "98765432-5678-5678-5678-567812345678",
    "高级日期显示",
    "\uE9B0",
    "支持显示自定义格式的日期，包括星期全称/简称和标准日期格式")]
public partial class AdvancedDateControl : ComponentBase<AdvancedDateViewModel>
{
    private readonly TextBlock _dateTextBlock;

    public AdvancedDateControl(IServiceProvider serviceProvider)
    {
        // 获取服务，使用注入的服务Provider，避免直接访问App
        var timeBaseService = serviceProvider.GetRequiredService<TimeBaseService>();
        ViewModel = new AdvancedDateViewModel(timeBaseService);
        
        // 纯C#构建UI，无XAML
        _dateTextBlock = new TextBlock
        {
            FontSize = 14,
            Foreground = Brushes.White,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        
        // 使用传统绑定，不依赖ReactiveUI，避免依赖缺失问题
        _dateTextBlock.Bind(
            TextBlock.TextProperty, 
            new Binding(nameof(ViewModel.FormattedDate))
        );
        
        // 订阅Unloaded事件，释放资源，避免重写方法的签名错误
        Unloaded += (s, e) =>
        {
            // 释放ViewModel资源，停止定时器
            ViewModel.Dispose();
        };
        
        // 设置控件内容
        Content = _dateTextBlock;
    }

    public AdvancedDateViewModel ViewModel { get; set; }
}