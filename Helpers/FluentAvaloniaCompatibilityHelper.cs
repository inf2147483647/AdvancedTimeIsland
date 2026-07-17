using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AdvancedTimeIsland.Helpers;

public static class FluentAvaloniaCompatibilityHelper
{
    private static bool? _isV3;

    public static bool IsV3
    {
        get
        {
            if (_isV3.HasValue)
                return _isV3.Value;

            _isV3 = CheckIsV3();
            return _isV3.Value;
        }
    }

    private static bool CheckIsV3()
    {
        try
        {
            var infoBarType = Type.GetType("FluentAvalonia.UI.Controls.FAInfoBar, FluentAvalonia");
            if (infoBarType != null)
                return true;

            var contentDialogType = Type.GetType("FluentAvalonia.UI.Controls.FAContentDialog, FluentAvalonia");
            if (contentDialogType != null)
                return true;
        }
        catch
        {
        }

        return false;
    }

    public static Control CreateInfoBar()
    {
        if (IsV3)
        {
            return CreateV3InfoBar();
        }
        else
        {
            return CreateV2InfoBar();
        }
    }

    private static Control CreateV3InfoBar()
    {
        var type = Type.GetType("FluentAvalonia.UI.Controls.FAInfoBar, FluentAvalonia");
        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        return CreateV2InfoBar();
    }

    private static Control CreateV2InfoBar()
    {
        var type = Type.GetType("FluentAvalonia.UI.Controls.InfoBar, FluentAvalonia");
        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        return new Border();
    }

    public static void SetInfoBarProperty(Control infoBar, string propertyName, object value)
    {
        if (infoBar == null)
            return;

        var type = infoBar.GetType();
        var property = type.GetProperty(propertyName);
        if (property != null)
        {
            property.SetValue(infoBar, value);
        }
    }

    public static object GetInfoBarProperty(Control infoBar, string propertyName)
    {
        if (infoBar == null)
            return null!;

        var type = infoBar.GetType();
        var property = type.GetProperty(propertyName);
        if (property != null)
        {
            return property.GetValue(infoBar)!;
        }
        return null!;
    }

    public static void AddInfoBarClosedHandler(Control infoBar, EventHandler handler)
    {
        if (infoBar == null)
            return;

        var type = infoBar.GetType();
        var eventInfo = type.GetEvent("Closed");
        if (eventInfo != null)
        {
            var delegateType = eventInfo.EventHandlerType;
            var convertedHandler = ConvertEventHandler(handler, delegateType);
            eventInfo.AddEventHandler(infoBar, convertedHandler);
        }
    }

    public static object CreateContentDialog()
    {
        if (IsV3)
        {
            return CreateV3ContentDialog();
        }
        else
        {
            return CreateV2ContentDialog();
        }
    }

    private static object CreateV3ContentDialog()
    {
        var type = Type.GetType("FluentAvalonia.UI.Controls.FAContentDialog, FluentAvalonia");
        if (type != null)
        {
            return Activator.CreateInstance(type)!;
        }
        return CreateV2ContentDialog();
    }

    private static object CreateV2ContentDialog()
    {
        var type = Type.GetType("FluentAvalonia.UI.Controls.ContentDialog, FluentAvalonia");
        if (type != null)
        {
            return Activator.CreateInstance(type)!;
        }
        return new object();
    }

    public static void SetContentDialogProperty(object dialog, string propertyName, object value)
    {
        if (dialog == null)
            return;

        var type = dialog.GetType();
        var property = type.GetProperty(propertyName);
        if (property != null)
        {
            property.SetValue(dialog, value);
        }
    }

    public static object GetContentDialogProperty(object dialog, string propertyName)
    {
        if (dialog == null)
            return null!;

        var type = dialog.GetType();
        var property = type.GetProperty(propertyName);
        if (property != null)
        {
            return property.GetValue(dialog)!;
        }
        return null!;
    }

    public static void AddContentDialogButtonClickHandler(object dialog, string eventName, EventHandler handler)
    {
        if (dialog == null)
            return;

        var type = dialog.GetType();
        var eventInfo = type.GetEvent(eventName);
        if (eventInfo != null)
        {
            var delegateType = eventInfo.EventHandlerType;
            var convertedHandler = ConvertEventHandler(handler, delegateType);
            eventInfo.AddEventHandler(dialog, convertedHandler);
        }
    }

    private static Delegate ConvertEventHandler(EventHandler handler, Type targetDelegateType)
    {
        if (handler == null || targetDelegateType == null)
            return handler;

        if (targetDelegateType == typeof(EventHandler))
            return handler;

        var invokeMethod = targetDelegateType.GetMethod("Invoke");
        if (invokeMethod == null)
            return handler;

        var parameters = invokeMethod.GetParameters();
        if (parameters.Length != 2)
            return handler;

        var senderType = parameters[0].ParameterType;
        var argsType = parameters[1].ParameterType;

        var genericType = typeof(EventHandlerAdapter<,>).MakeGenericType(senderType, argsType);
        var adapter = Activator.CreateInstance(genericType, handler);
        var adapterMethod = genericType.GetMethod("Invoke")!;

        return Delegate.CreateDelegate(targetDelegateType, adapter, adapterMethod);
    }

    private class EventHandlerAdapter<TSender, TArgs>
    {
        private readonly EventHandler _handler;

        public EventHandlerAdapter(EventHandler handler)
        {
            _handler = handler;
        }

        public void Invoke(TSender sender, TArgs args)
        {
            _handler?.Invoke(sender, args as EventArgs ?? EventArgs.Empty);
        }
    }

    public static void AddLostFocusHandler(InputElement element, EventHandler<RoutedEventArgs> handler)
    {
        if (element == null)
            return;

        var type = element.GetType();
        var eventInfo = type.GetEvent("LostFocus");
        if (eventInfo != null)
        {
            var delegateType = eventInfo.EventHandlerType;
            var convertedHandler = ConvertRoutedEventHandler(handler, delegateType);
            eventInfo.AddEventHandler(element, convertedHandler);
        }
    }

    private static Delegate ConvertRoutedEventHandler(EventHandler<RoutedEventArgs> handler, Type targetDelegateType)
    {
        if (handler == null || targetDelegateType == null)
            return handler;

        if (targetDelegateType == typeof(EventHandler<RoutedEventArgs>))
            return handler;

        var invokeMethod = targetDelegateType.GetMethod("Invoke");
        if (invokeMethod == null)
            return handler;

        var parameters = invokeMethod.GetParameters();
        if (parameters.Length != 2)
            return handler;

        var senderType = parameters[0].ParameterType;
        var argsType = parameters[1].ParameterType;

        var genericType = typeof(RoutedEventHandlerAdapter<,>).MakeGenericType(senderType, argsType);
        var adapter = Activator.CreateInstance(genericType, handler);
        var adapterMethod = genericType.GetMethod("Invoke")!;

        return Delegate.CreateDelegate(targetDelegateType, adapter, adapterMethod);
    }

    private class RoutedEventHandlerAdapter<TSender, TArgs>
    {
        private readonly EventHandler<RoutedEventArgs> _handler;

        public RoutedEventHandlerAdapter(EventHandler<RoutedEventArgs> handler)
        {
            _handler = handler;
        }

        public void Invoke(TSender sender, TArgs args)
        {
            _handler?.Invoke(sender, args as RoutedEventArgs ?? new RoutedEventArgs());
        }
    }

    public static void AddCheckedHandler(ToggleButton toggleButton, EventHandler<RoutedEventArgs> handler)
    {
        if (toggleButton == null)
            return;

        var type = toggleButton.GetType();
        var eventInfo = type.GetEvent("Checked");
        if (eventInfo != null)
        {
            var delegateType = eventInfo.EventHandlerType;
            var convertedHandler = ConvertRoutedEventHandler(handler, delegateType);
            eventInfo.AddEventHandler(toggleButton, convertedHandler);
            return;
        }

        var isCheckedChangedEvent = type.GetEvent("IsCheckedChanged");
        if (isCheckedChangedEvent != null)
        {
            EventHandler<RoutedEventArgs> wrapper = (sender, args) =>
            {
                var isCheckedProp = type.GetProperty("IsChecked");
                var isChecked = isCheckedProp?.GetValue(toggleButton) as bool?;
                if (isChecked == true)
                {
                    handler?.Invoke(sender, args);
                }
            };
            var delegateType = isCheckedChangedEvent.EventHandlerType;
            var convertedHandler = ConvertRoutedEventHandler(wrapper, delegateType);
            isCheckedChangedEvent.AddEventHandler(toggleButton, convertedHandler);
        }
    }

    public static void AddUncheckedHandler(ToggleButton toggleButton, EventHandler<RoutedEventArgs> handler)
    {
        if (toggleButton == null)
            return;

        var type = toggleButton.GetType();
        var eventInfo = type.GetEvent("Unchecked");
        if (eventInfo != null)
        {
            var delegateType = eventInfo.EventHandlerType;
            var convertedHandler = ConvertRoutedEventHandler(handler, delegateType);
            eventInfo.AddEventHandler(toggleButton, convertedHandler);
            return;
        }

        var isCheckedChangedEvent = type.GetEvent("IsCheckedChanged");
        if (isCheckedChangedEvent != null)
        {
            EventHandler<RoutedEventArgs> wrapper = (sender, args) =>
            {
                var isCheckedProp = type.GetProperty("IsChecked");
                var isChecked = isCheckedProp?.GetValue(toggleButton) as bool?;
                if (isChecked != true)
                {
                    handler?.Invoke(sender, args);
                }
            };
            var delegateType = isCheckedChangedEvent.EventHandlerType;
            var convertedHandler = ConvertRoutedEventHandler(wrapper, delegateType);
            isCheckedChangedEvent.AddEventHandler(toggleButton, convertedHandler);
        }
    }

    public static async System.Threading.Tasks.Task<object> ShowContentDialogAsync(object dialog, TopLevel topLevel)
    {
        if (dialog == null)
            return null!;

        var type = dialog.GetType();
        var method = type.GetMethod("ShowAsync", new[] { typeof(TopLevel) });
        if (method != null)
        {
            var task = method.Invoke(dialog, new object[] { topLevel }) as System.Threading.Tasks.Task;
            if (task != null)
            {
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    return resultProperty.GetValue(task)!;
                }
            }
            return null!;
        }

        method = type.GetMethod("ShowAsync", Type.EmptyTypes);
        if (method != null)
        {
            var task = method.Invoke(dialog, null) as System.Threading.Tasks.Task;
            if (task != null)
            {
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    return resultProperty.GetValue(task)!;
                }
            }
            return null!;
        }

        return null!;
    }

    public static bool IsContentDialogResultPrimary(object result)
    {
        if (result == null)
            return false;

        var type = result.GetType();
        var name = type.Name;

        if (name == "FAContentDialogResult" || name == "ContentDialogResult")
        {
            var valueField = type.GetField("Primary", BindingFlags.Public | BindingFlags.Static);
            if (valueField != null)
            {
                var primaryValue = valueField.GetValue(null);
                return result.Equals(primaryValue);
            }
        }

        return false;
    }

    public static bool IsContentDialogResultSecondary(object result)
    {
        if (result == null)
            return false;

        var type = result.GetType();
        var name = type.Name;

        if (name == "FAContentDialogResult" || name == "ContentDialogResult")
        {
            var valueField = type.GetField("Secondary", BindingFlags.Public | BindingFlags.Static);
            if (valueField != null)
            {
                var secondaryValue = valueField.GetValue(null);
                return result.Equals(secondaryValue);
            }
        }

        return false;
    }

    public static object GetContentDialogResultPrimary()
    {
        if (IsV3)
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.FAContentDialogResult, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Primary", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }
        else
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.ContentDialogResult, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Primary", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }

        return null!;
    }

    public static object GetContentDialogButtonPrimary()
    {
        if (IsV3)
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.FAContentDialogButton, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Primary", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }
        else
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.ContentDialogButton, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Primary", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }

        return null!;
    }

    public static object GetInfoBarSeverityWarning()
    {
        if (IsV3)
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.FAInfoBarSeverity, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Warning", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }
        else
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.InfoBarSeverity, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Warning", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }

        return 1;
    }

    public static object GetInfoBarSeverityInformational()
    {
        if (IsV3)
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.FAInfoBarSeverity, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Informational", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }
        else
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.InfoBarSeverity, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Informational", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }

        return 0;
    }

    public static object GetInfoBarSeverityError()
    {
        if (IsV3)
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.FAInfoBarSeverity, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Error", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }
        else
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.InfoBarSeverity, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Error", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }

        return 2;
    }

    public static object GetInfoBarSeveritySuccess()
    {
        if (IsV3)
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.FAInfoBarSeverity, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Success", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }
        else
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.InfoBarSeverity, FluentAvalonia");
            if (type != null)
            {
                var field = type.GetField("Success", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null)!;
            }
        }

        return 3;
    }

    public static Control CreateSettingsExpander()
    {
        if (IsV3)
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.FASettingsExpander, FluentAvalonia");
            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
        }
        else
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.SettingsExpander, FluentAvalonia");
            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
        }

        return new Border();
    }

    public static Control CreateSettingsExpanderItem()
    {
        if (IsV3)
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.FASettingsExpanderItem, FluentAvalonia");
            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
        }
        else
        {
            var type = Type.GetType("FluentAvalonia.UI.Controls.SettingsExpanderItem, FluentAvalonia");
            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
        }

        return new Border();
    }

    public static void NavigateBack(Control control)
    {
        if (control == null)
            return;

        try
        {
            var frameType = Type.GetType("FluentAvalonia.UI.Controls.Frame, FluentAvalonia");
            if (frameType != null)
            {
                var parent = FindVisualParent(control);
                while (parent != null)
                {
                    if (frameType.IsAssignableFrom(parent.GetType()))
                    {
                        var canGoBackProp = frameType.GetProperty("CanGoBack");
                        var canGoBack = (bool?)canGoBackProp?.GetValue(parent) ?? false;
                        if (canGoBack)
                        {
                            var goBackMethod = frameType.GetMethod("GoBack");
                            goBackMethod?.Invoke(parent, null);
                        }
                        return;
                    }

                    parent = FindVisualParent(parent);
                }
            }
            else
            {
                var parent = FindVisualParent(control);
                while (parent != null)
                {
                    var goBackMethod = parent.GetType().GetMethod("GoBack", Type.EmptyTypes);
                    if (goBackMethod != null)
                    {
                        var canGoBackProp = parent.GetType().GetProperty("CanGoBack");
                        var canGoBack = (bool?)canGoBackProp?.GetValue(parent) ?? true;
                        if (canGoBack)
                        {
                            goBackMethod.Invoke(parent, null);
                        }
                        return;
                    }

                    parent = FindVisualParent(parent);
                }
            }
        }
        catch
        {
        }
    }

    private static Avalonia.Visual? FindVisualParent(Avalonia.Visual visual)
    {
        if (visual == null)
            return null;

        var visualParentProperty = visual.GetType().GetProperty("VisualParent");
        if (visualParentProperty != null)
        {
            return visualParentProperty.GetValue(visual) as Avalonia.Visual;
        }

        if (visual is Avalonia.Controls.Control ctrl)
        {
            return ctrl.Parent as Avalonia.Visual;
        }

        return null;
    }
}