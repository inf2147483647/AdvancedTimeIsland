using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace AdvancedTimeIsland.Shared;

public static class GlobalConstants
{
    public static class HostInterfaces
    {
        public static IExactTimeService? ExactTimeService { get; set; }
        public static ILogger<Plugin>? PluginLogger { get; set; }
    }
}
