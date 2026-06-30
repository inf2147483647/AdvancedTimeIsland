using System;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;

namespace AdvancedTimeIsland.Services.NotificationProviders;

[NotificationProviderInfo("A1B2C3D4-E5F6-7890-ABCD-EF1234567890", "倒计时提醒", "Timer", "倒计时到达时发送提醒")]
[NotificationChannelInfo(ChannelMaskId, "倒计时遮罩", "Timer", "倒计时到达时发送遮罩提醒")]
[NotificationChannelInfo(ChannelOverlayId, "倒计时正文", "Timer", "倒计时到达时发送正文提醒")]
public class CountdownNotificationProvider : NotificationProviderBase
{
    public const string ChannelMaskId = "02021313-a1a3-bcbe-4f9a-214748364700";
    public const string ChannelOverlayId = "02021313-a1a3-bcbe-4f9a-214748364701";

    private static event Action<string, int, string, int>? OnNotify;

    public CountdownNotificationProvider()
    {
        OnNotify += DoNotify;
    }

    public static void Notify(string maskText, int maskDurationSeconds, string overlayText, int overlayDurationSeconds)
    {
        OnNotify?.Invoke(maskText, maskDurationSeconds, overlayText, overlayDurationSeconds);
    }

    private void DoNotify(string maskText, int maskDurationSeconds, string overlayText, int overlayDurationSeconds)
    {
        var request = new NotificationRequest
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(maskText, factory: x =>
            {
                x.Duration = TimeSpan.FromSeconds(maskDurationSeconds);
            }),
            OverlayContent = string.IsNullOrWhiteSpace(overlayText)
                ? null
                : NotificationContent.CreateSimpleTextContent(overlayText, factory: x =>
                {
                    x.Duration = TimeSpan.FromSeconds(overlayDurationSeconds);
                })
        };

        Channel(ChannelMaskId).ShowNotification(request);
    }
}



