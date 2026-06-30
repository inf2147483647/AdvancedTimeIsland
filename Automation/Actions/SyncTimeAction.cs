using System;
using System.Threading.Tasks;
using AdvancedTimeIsland.Services;
using AdvancedTimeIsland.Shared;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Actions;

[ActionInfo("advancedtimeisland.sync_classisland_time", "同步ClassIsland时间", "\uecc8")]
public class SyncClassIslandTimeAction : ActionBase
{
    protected override async Task OnInvoke()
    {
        base.OnInvoke();
        await Task.Run(() =>
        {
            GlobalConstants.HostInterfaces.ExactTimeService?.Sync();
        });
    }
}

[ActionInfo("advancedtimeisland.sync_plugin_time", "同步AdvancedTimeIsland插件时间", "\uecc9")]
public class SyncPluginTimeAction : ActionBase
{
    private const int SyncTimeoutSeconds = 10;

    protected override async Task OnInvoke()
    {
        base.OnInvoke();
        if (TimeBaseService.Instance != null)
        {
            await TimeBaseService.Instance.SyncTimeNowAsync(TimeSpan.FromSeconds(SyncTimeoutSeconds));
        }
    }
}
