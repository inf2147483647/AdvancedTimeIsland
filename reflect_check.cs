using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        var assembly = Assembly.LoadFrom(@"C:\Users\seewo\.nuget\packages\classisland.core\1.7.106.2-dev-v2\lib\net8.0\ClassIsland.Core.dll");
        
        var type = assembly.GetType("ClassIsland.Core.Abstractions.Services.NotificationProviders.NotificationProviderBase");
        if (type == null)
        {
            Console.WriteLine("Type not found");
            return;
        }

        Console.WriteLine("=== Fields ===");
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            Console.WriteLine($"  {field.FieldType.Name} {field.Name}");
        }

        Console.WriteLine("\n=== Properties ===");
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            Console.WriteLine($"  {prop.PropertyType.Name} {prop.Name}");
        }

        Console.WriteLine("\n=== Methods ===");
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({parameters})");
        }

        Console.WriteLine("\n=== Constructors ===");
        foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var parameters = string.Join(", ", ctor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            Console.WriteLine($"  .ctor({parameters})");
        }
    }
}