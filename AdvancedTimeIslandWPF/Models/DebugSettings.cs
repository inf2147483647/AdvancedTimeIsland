using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdvancedTimeIsland.Models;

/// <summary>
/// 调试页面设置
/// </summary>
public class DebugSettings : INotifyPropertyChanged
{
    private bool _enableThrowExceptionTest = false;
    private bool _enableForceCrashTest = false;
    private bool _enableSelfDestructTest = false;

    /// <summary>
    /// 是否启用抛出异常测试
    /// </summary>
    public bool EnableThrowExceptionTest
    {
        get => _enableThrowExceptionTest;
        set
        {
            if (_enableThrowExceptionTest != value)
            {
                _enableThrowExceptionTest = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用强制崩溃测试
    /// </summary>
    public bool EnableForceCrashTest
    {
        get => _enableForceCrashTest;
        set
        {
            if (_enableForceCrashTest != value)
            {
                _enableForceCrashTest = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否启用自毁测试
    /// </summary>
    public bool EnableSelfDestructTest
    {
        get => _enableSelfDestructTest;
        set
        {
            if (_enableSelfDestructTest != value)
            {
                _enableSelfDestructTest = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}



