// 子进程管道客户端：连接 ClassIsland 进程内的插件管道服务器（固定名）。
// 断线后 1s×∞ 重连——"跟随启停=关"时宿主重启（ClassIsland 重新启动、插件重建管道服务器）
// 即可自动 adopt 回数据推送，恢复前窗口保持冻结显示。
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdvancedTimeIsland.Shared.FloatingSchedule;

namespace AdvancedTimeIsland.FloatScheduleChild;

internal sealed class FloatSchedulePipeClient
{
    readonly object _writeLock = new();
    StreamWriter? _writer;
    volatile bool _connected;

    public bool Connected => _connected;
    public event Action<FloatScheduleIpcMsgType, System.Text.Json.JsonElement>? MessageReceived;
    public event Action? ConnectedChanged;

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            NamedPipeClientStream? client = null;
            try
            {
                client = new NamedPipeClientStream(".", Program.PipeName,
                    PipeDirection.InOut, PipeOptions.Asynchronous);
                using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    timeoutCts.CancelAfter(3000);
                    await client.ConnectAsync(timeoutCts.Token).ConfigureAwait(false);
                }

                var reader = new StreamReader(client, Encoding.UTF8);
                lock (_writeLock)
                {
                    _writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = false };
                }
                _connected = true;
                ConnectedChanged?.Invoke();

                string? line;
                while (!ct.IsCancellationRequested &&
                       (line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
                {
                    if (FloatScheduleIpc.TryDecode(line, out var type, out var data))
                    {
                        try { MessageReceived?.Invoke(type, data); } catch { }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // 连接失败/断线：走统一重连节奏
            }
            finally
            {
                var wasConnected = _connected;
                _connected = false;
                lock (_writeLock)
                {
                    try { _writer?.Dispose(); } catch { }
                    _writer = null;
                }
                try { client?.Dispose(); } catch { }
                if (wasConnected)
                {
                    try { ConnectedChanged?.Invoke(); } catch { }
                }
            }
            if (ct.IsCancellationRequested) break;
            try { await Task.Delay(1000, ct).ConfigureAwait(false); } catch { break; }
        }
    }

    public void Send(FloatScheduleIpcMsgType type, object? payload)
    {
        if (!_connected) return;
        try
        {
            var line = FloatScheduleIpc.Encode(type, payload);
            lock (_writeLock)
            {
                _writer?.WriteLine(line);
                _writer?.Flush();
            }
        }
        catch
        {
            // 写失败视为断线，读循环/重连逻辑会接管
        }
    }
}
