using System;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTimeIsland.Shared;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;

namespace AdvancedTimeIsland.Automation.Triggers;

public abstract class TimeTriggerBase<T> : TriggerBase<T> where T : class
{
    private CancellationTokenSource? _stopCancellationTokenSource;
    private DateTime _lastTriggeredSecond = DateTime.MinValue;

    protected IExactTimeService ExactTimeService => GlobalConstants.HostInterfaces.ExactTimeService!;

    public override void Loaded()
    {
        _stopCancellationTokenSource = new CancellationTokenSource();
        _ = Task.Factory.StartNew(TriggerWorker, TaskCreationOptions.LongRunning);
    }

    private async Task TriggerWorker()
    {
        while (_stopCancellationTokenSource?.IsCancellationRequested == false)
        {
            try
            {
                var now = GetCurrentTime();
                
                if (now.Second != _lastTriggeredSecond.Second ||
                    now.Minute != _lastTriggeredSecond.Minute ||
                    now.Hour != _lastTriggeredSecond.Hour ||
                    now.Day != _lastTriggeredSecond.Day ||
                    now.Month != _lastTriggeredSecond.Month ||
                    now.Year != _lastTriggeredSecond.Year)
                {
                    if (CheckTrigger(now))
                    {
                        Dispatcher.UIThread.Invoke(Trigger);
                        _lastTriggeredSecond = now;
                    }
                }

                var delay = GetNextCheckDelay(now);
                await Task.Delay(delay, _stopCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(1000, _stopCancellationTokenSource.Token);
            }
        }
    }

    protected virtual DateTime GetCurrentTime()
    {
        return ExactTimeService.GetCurrentLocalDateTime();
    }

    protected abstract bool CheckTrigger(DateTime now);

    protected virtual TimeSpan GetNextCheckDelay(DateTime now)
    {
        var nextSecond = now.AddSeconds(1).AddMilliseconds(-now.Millisecond);
        var delay = nextSecond - now;
        if (delay < TimeSpan.FromMilliseconds(100))
        {
            delay = TimeSpan.FromMilliseconds(100);
        }
        return delay;
    }

    public override void UnLoaded()
    {
        _stopCancellationTokenSource?.Cancel();
    }
}
