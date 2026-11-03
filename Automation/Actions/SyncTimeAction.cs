using System.Threading.Tasks;
using AdvancedTimeIsland.Shared;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;

namespace AdvancedTimeIsland.Automation.Actions;

[ActionInfo("advancedtimeisland.sync_classisland_time", "同步ClassIsland时间", "\uE161")]
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
