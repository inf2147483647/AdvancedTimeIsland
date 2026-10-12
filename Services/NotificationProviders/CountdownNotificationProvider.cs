using System;
using System.Collections.Generic;
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

    // 原先用静态 event 在构造函数里 += 实例方法且从不退订，会经静态委托链永久 root 住提供者实例。
    // 改为弱引用集合：实例可被 GC 回收，Notify 时只通知存活实例并顺带清理死引用。
    private static readonly object NotifyLock = new();
    private static readonly List<WeakReference<CountdownNotificationProvider>> Providers = new();

    public CountdownNotificationProvider()
    {
        lock (NotifyLock)
        {
            Providers.Add(new WeakReference<CountdownNotificationProvider>(this));
        }
    }

    public static void Notify(string maskText, int maskDurationSeconds, string overlayText, int overlayDurationSeconds)
    {
        List<CountdownNotificationProvider> alive;
        lock (NotifyLock)
        {
            alive = new List<CountdownNotificationProvider>();
            for (var i = Providers.Count - 1; i >= 0; i--)
            {
                if (Providers[i].TryGetTarget(out var provider))
                {
                    alive.Add(provider);
                }
                else
                {
                    Providers.RemoveAt(i);
                }
            }
        }

        foreach (var provider in alive)
        {
            provider.DoNotify(maskText, maskDurationSeconds, overlayText, overlayDurationSeconds);
        }
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



