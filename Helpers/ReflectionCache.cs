using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq;

namespace AdvancedTimeIsland.Helpers;

/// <summary>
/// 宿主模型类型的反射元数据缓存。
/// 悬浮窗 500ms Tick / 5s 全量重建、在校统计逐日扫描都会反复对同一批 ClassIsland 类型
/// 调用 GetProperty / GetMethods（内部为线性扫描 + 分配），这里按 (类型, 名称) 记忆化，
/// 跨悬浮窗服务与在校日历服务复用，避免每次查询都重新反射。
/// </summary>
public static class ReflectionCache
{
    private const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;

    private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> Properties = new();
    private static readonly ConcurrentDictionary<(Type Type, string Name), MethodInfo[]> Methods = new();

    /// <summary>获取实例公共属性（找不到返回 null），结果按 (类型, 名称) 缓存。</summary>
    public static PropertyInfo? GetProperty(Type type, string propertyName)
    {
        return Properties.GetOrAdd((type, propertyName), static key =>
            key.Type.GetProperty(key.Name, InstancePublic));
    }

    /// <summary>读取目标对象的实例公共属性值；target 为 null 或属性不存在时返回 null。</summary>
    public static object? GetPropertyValue(object? target, string propertyName)
    {
        if (target == null)
        {
            return null;
        }

        return GetProperty(target.GetType(), propertyName)?.GetValue(target);
    }

    /// <summary>获取类型指定名称的全部实例公共方法（已过滤名称），结果按 (类型, 名称) 缓存。</summary>
    public static MethodInfo[] GetMethods(Type type, string methodName)
    {
        return Methods.GetOrAdd((type, methodName), static key =>
            key.Type.GetMethods(InstancePublic)
                   .Where(m => m.Name == key.Name)
                   .ToArray());
    }
}
