// 液态玻璃背景捕获后端：DXGI Desktop Duplication（共享层，纯 System + Vortice）。
//
// 【为什么需要它】抓"窗口背后的桌面像素"原先只能用 GDI BitBlt(GetDC(NULL))，
//   实测单次固定耗时 ~24ms（与捕获面积几乎无关：32×32 也是 23.9ms），且是同步阻塞 ——
//   60Hz 采样根本跑不动（CPU/GPU 只有 6%，因为线程在等）。
//   DXGI Desktop Duplication 是"帧到达即回调"的模型：桌面无变化时 AcquireNextFrame 立即返回超时，
//   有变化时只做一次 GPU 内 CopyResource + Map，成本从"固定 24ms 阻塞"降到亚毫秒级。
//
// 【本文件的设计约束】
//   1. 可同时编入主项目与独立子进程（Shared\FloatingSchedule 通配），因此只依赖 System + Vortice。
//   2. 全部 COM 对象（device / context / duplication / staging texture）**一次创建长期复用**，
//      绝不逐帧新建（逐帧新建 device/duplication 会带来几十毫秒开销）。
//   3. 任何异常 / HRESULT 失败一律吞掉并返回 false，绝不抛给调用方（抓屏失败只应退化为"没有背景"）。
//   4. 抓帧无新帧（桌面无变化）不是错误：返回 false，但把 LastFrameValid 置为 true，
//      表示"上一次抓取的内容仍然代表当前桌面"（调用方可直接复用上一次的缓冲）。
//
// 【调用方契约】
//   TryCapture(screenX, screenY, w, h, dst)
//     · 返回 true  → dst 已被写入**新的一帧**内容（BGRA，top-down，stride = w*4）。
//     · 返回 false → dst **未被触碰**，其内容仍是上一次成功抓取的结果。
//         - 若 LastFrameValid == true ：桌面自上次抓取以来没有变化，上一次内容仍有效，可继续使用；
//         - 若 LastAcquireTimedOut == true 且 LastFrameValid == false ：
//             目标区域变过（例如窗口移动）但桌面无新帧 —— 本次无可用数据，调用方应跳过本次采样或回退 GDI；
//         - 其余情况为硬失败（初始化失败 / 被其他进程占用 / AccessLost / 旋转输出等），
//             调用方应记为一次失败并在阈值后回退 GDI（并可稍后重试本后端）。
using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpGen.Runtime;
using Vortice;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

/// <summary>
/// DXGI Desktop Duplication 抓帧后端（线程安全；失败一律返回 false，不抛）。
/// 注意：本类型**只允许**放在 Shared\FloatingSchedule 下（WPF 侧不包含该目录，见各 csproj），
/// 绝不能写进 FloatScheduleIpc / FloatScheduleRenderModel / FloatScheduleWindowSettings 这三个 WPF 也编译的文件。
/// </summary>
public sealed class FloatScheduleDxgiCapture : IDisposable
{
    // 从高到低尝试的 D3D 特性级别（桌面复制在 9.1 以上都可，但取高者兼容性最好）
    static readonly FeatureLevel[] FeatureLevels =
    {
        FeatureLevel.Level_11_1,
        FeatureLevel.Level_11_0,
        FeatureLevel.Level_10_1,
        FeatureLevel.Level_10_0,
        FeatureLevel.Level_9_3,
        FeatureLevel.Level_9_2,
        FeatureLevel.Level_9_1,
    };

    readonly object _gate = new();

    ID3D11Device? _device;
    ID3D11DeviceContext? _context;
    IDXGIDevice? _dxgiDevice;
    IDXGIAdapter? _adapter;
    IDXGIOutput? _output;
    IDXGIOutput1? _output1;
    IDXGIOutputDuplication? _duplication;
    ID3D11Texture2D? _staging;

    RawRect _outputRect;                  // 当前 output 在虚拟桌面中的坐标（用来把屏幕坐标换算成 output 内偏移）
    bool _rotationSupported;              // output 旋转必须是 Identity，否则放弃本后端
    bool _rotationUnsupported;            // 已确认该 output 带旋转（竖屏等）→ 不再重复建 device 尝试（Reset 后清除）
    int _stageWidth, _stageHeight;        // staging texture 尺寸（与 acquired texture 的 description 保持一致）
    Format _stageFormat = Format.Unknown;

    // 上一次成功抓取所对应的目标区域；区域变化时"上一帧"不再代表该区域 → LastFrameValid 失效
    int _lastX = int.MinValue, _lastY = int.MinValue, _lastW, _lastH;

    /// <summary>当前是否已成功初始化（device + duplication 就绪）。</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>上一次成功抓取的帧是否仍代表"当前请求区域"的桌面内容（桌面无变化时为 true）。</summary>
    public bool LastFrameValid { get; private set; }

    /// <summary>上一次 TryCapture 是否因"桌面无新帧"（AcquireNextFrame 超时）而返回 false。</summary>
    public bool LastAcquireTimedOut { get; private set; }

    /// <summary>当前绑定的 output 序号（-1 表示未初始化）。</summary>
    public int OutputIndex { get; private set; } = -1;

    /// <summary>
    /// 抓取屏幕上 (screenX, screenY, w, h) 这块区域到 dst（BGRA，top-down，stride = w*4，长度需 ≥ w*h*4）。
    /// 语义见文件头注释：true = dst 已写入新帧；false = dst 未被触碰。
    /// </summary>
    public bool TryCapture(int screenX, int screenY, int w, int h, byte[] dst)
    {
        if (w <= 0 || h <= 0 || dst.Length < w * h * 4) return false;
        lock (_gate)
        {
            try
            {
                LastAcquireTimedOut = false;
                if (!EnsureInitialized(screenX, screenY, w, h))
                {
                    LastFrameValid = false;
                    return false;
                }
                // 目标区域变了 → 上一次的帧不再代表这块区域
                if (_lastX != screenX || _lastY != screenY || _lastW != w || _lastH != h) LastFrameValid = false;

                // 最多两轮：第二轮用于"无新帧 + 目标区域已变"时重建 Duplication 拿初始整帧
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    // 常规用 0（不阻塞）；重建后的那一轮给一点等待时间拿初始帧，
                    // 但**不能给太长**——该调用在采样路径上同步执行，超时越长越会把一次采样拖慢
                    // （曾经 100ms，叠加采样间隔后会造成明显显示延迟），15ms 足够拿到重建后的初始帧。
                    uint timeoutMs = attempt == 0 ? 0u : 15u;
                    Result result;
                    IDXGIResource? desktopResource = null;
                    try
                    {
                        result = _duplication!.AcquireNextFrame(timeoutMs, out _, out desktopResource);
                    }
                    catch
                    {
                        // AcquireNextFrame 自身抛异常（极罕见）→ 按硬失败处理，重建后重试一次
                        TearDownDuplication();
                        if (attempt == 0 && EnsureInitialized(screenX, screenY, w, h)) continue;
                        LastFrameValid = false;
                        return false;
                    }

                    if (result.Success && desktopResource != null)
                    {
                        bool copied = false;
                        try
                        {
                            CopyFrame(desktopResource, screenX, screenY, w, h, dst);
                            copied = true;
                        }
                        catch
                        {
                            copied = false;
                        }
                        finally
                        {
                            // 【必须】无论成功失败都要释放资源并 ReleaseFrame，否则下一次 AcquireNextFrame 会持续失败
                            SafeDispose(desktopResource);
                            try { _duplication!.ReleaseFrame(); } catch { }
                        }
                        if (!copied)
                        {
                            LastFrameValid = false;
                            return false;
                        }
                        _lastX = screenX; _lastY = screenY; _lastW = w; _lastH = h;
                        LastFrameValid = true;
                        return true;
                    }

                    // 失败：先释放可能拿到的资源（超时时 desktopResource 为 null，安全）
                    SafeDispose(desktopResource);

                    if (result.Code == Vortice.DXGI.ResultCode.WaitTimeout.Code)
                    {
                        // 桌面无变化：不是错误。若上一帧不适用于当前区域 → 重建 Duplication 取初始整帧再试一次
                        LastAcquireTimedOut = true;
                        if (!LastFrameValid && attempt == 0 && TryRecreateDuplicationOnly()) continue;
                        return false;
                    }

                    // 其他 HRESULT 失败（AccessLost / 被 OBS 等独占 / 设备移除）→ 整段重建后重试一次
                    TearDownDuplication();
                    if (attempt == 0 && EnsureInitialized(screenX, screenY, w, h)) continue;
                    LastFrameValid = false;
                    return false;
                }
                LastFrameValid = false;
                return false;
            }
            catch
            {
                // 兜底：任何异常都不能抛给调用方
                LastFrameValid = false;
                IsInitialized = false;
                TearDownDuplication();
                return false;
            }
        }
    }

    /// <summary>主动释放全部 COM 对象并允许下次调用重新初始化（供调用方在运行期"重试恢复 DXGI"时使用）。</summary>
    public void Reset()
    {
        lock (_gate)
        {
            TearDownAll();
            _rotationUnsupported = false;   // 用户可能已变更显示器旋转 → 允许重新探测
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            TearDownAll();
        }
    }

    // ===================== 初始化 =====================

    /// <summary>
    /// 惰性初始化（可重试）：未初始化、或目标区域中心已不在当前 output 内（窗口移到别的显示器）时重建。
    /// 失败返回 false，绝不抛。
    /// </summary>
    bool EnsureInitialized(int screenX, int screenY, int w, int h)
    {
        try
        {
            int centerX = screenX + w / 2, centerY = screenY + h / 2;
            if (_rotationUnsupported) return false;   // 已知该输出带旋转 → 直接放弃，避免每次采样都重建 device
            if (_duplication != null && _rotationSupported && ContainsPoint(_outputRect, centerX, centerY)) return true;
            TearDownAll();

            // 创建 D3D11 设备（硬件；必须带 BgraSupport —— 桌面复制输出就是 BGRA）
            var create = D3D11.D3D11CreateDevice(
                (IDXGIAdapter?)null, DriverType.Hardware, DeviceCreationFlags.BgraSupport,
                FeatureLevels, out ID3D11Device? device, out ID3D11DeviceContext? context);
            if (create.Failure || device == null || context == null) return false;
            _device = device;
            _context = context;

            _dxgiDevice = _device.QueryInterface<IDXGIDevice>();
            _adapter = _dxgiDevice.GetAdapter();

            // 选"包含目标区域中心点"的那个 output（多显示器必须选对，否则拿到的帧是另一块屏）
            IDXGIOutput? chosen = null;
            RawRect chosenRect = default;
            int chosenIndex = -1;
            for (uint i = 0; i < 64; i++)   // 上限保护：正常显示器数量远小于 64
            {
                var er = _adapter.EnumOutputs(i, out IDXGIOutput? outObj);
                if (er.Failure || outObj == null) break;
                var desc = outObj.Description;
                var rect = desc.DesktopCoordinates;
                if (desc.AttachedToDesktop && ContainsPoint(rect, centerX, centerY))
                {
                    chosen = outObj;
                    chosenRect = rect;
                    chosenIndex = (int)i;
                    break;
                }
                SafeDispose(outObj);
            }
            if (chosen == null) return false;

            _output = chosen;
            _outputRect = chosenRect;
            OutputIndex = chosenIndex;

            // 旋转输出（竖屏等）不做处理：直接放弃本后端，由调用方回退 GDI（并记住该状态，避免重复尝试）
            if (_output.Description.Rotation != ModeRotation.Identity)
            {
                _rotationSupported = false;
                _rotationUnsupported = true;
                return false;
            }
            _rotationSupported = true;

            _output1 = _output.QueryInterface<IDXGIOutput1>();
            _duplication = _output1.DuplicateOutput(_device);   // 失败（被 OBS 等独占）会抛 → 由外层 catch 吞掉
            IsInitialized = true;
            LastFrameValid = false;
            return true;
        }
        catch
        {
            TearDownAll();
            return false;
        }
    }

    static bool ContainsPoint(RawRect r, int x, int y)
        => x >= r.Left && x < r.Right && y >= r.Top && y < r.Bottom;

    // ===================== 单帧拷贝 =====================

    /// <summary>把 acquired 的桌面纹理拷到 staging 纹理后映射到 CPU，再按屏幕坐标取出目标子矩形写进 dst。</summary>
    void CopyFrame(IDXGIResource resource, int screenX, int screenY, int w, int h, byte[] dst)
    {
        using var desktopTexture = resource.QueryInterface<ID3D11Texture2D>();
        var desc = desktopTexture.Description;

        // 桌面复制输出正常恒为 B8G8R8A8_UNorm（与现有 BGRA 折射缓冲一致）。
        // 若出现其它格式（极罕见），直接判为失败 → 调用方回退 GDI，避免把非 BGRA 数据当 BGRA 渲染出错误颜色。
        if (desc.Format != Format.B8G8R8A8_UNorm && desc.Format != Format.B8G8R8A8_UNorm_SRgb)
            throw new InvalidOperationException("unexpected duplication format: " + desc.Format);

        // 每帧的 staging texture 尺寸/格式都要与 acquired texture 一致（否则 CopyResource 会失败）
        EnsureStaging(desc);

        _context!.CopyResource(_staging!, desktopTexture);
        var mapped = _context.Map(_staging!, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None, out MappedSubresource sub);
        if (mapped.Failure || sub.DataPointer == IntPtr.Zero) throw new InvalidOperationException("Map failed");
        try
        {
            int outW = (int)desc.Width, outH = (int)desc.Height;
            // 屏幕坐标 → output 内偏移（左/上可能为负：多显示器时虚拟桌面原点可能不为 0）
            int sx = screenX - _outputRect.Left;
            int sy = screenY - _outputRect.Top;
            // 只拷贝落在 output 内的部分（理论上调用方已按显示器边界 clamp，这里再兜一层）
            int cx0 = Math.Max(0, sx), cy0 = Math.Max(0, sy);
            int cx1 = Math.Min(outW, sx + w), cy1 = Math.Min(outH, sy + h);
            if (cx1 <= cx0 || cy1 <= cy0) throw new InvalidOperationException("region outside output");

            int rowBytes = (cx1 - cx0) * 4;
            int dstColOffset = cx0 - sx;   // 该行在 dst 里的列偏移
            int rowPitch = (int)sub.RowPitch;
            for (int y = cy0; y < cy1; y++)
            {
                IntPtr rowSrc = sub.DataPointer + (nint)y * rowPitch + (nint)cx0 * 4;
                int dstOffset = ((y - sy) * w + dstColOffset) * 4;
                Marshal.Copy(rowSrc, dst, dstOffset, rowBytes);
            }
        }
        finally
        {
            try { _context.Unmap(_staging!, 0); } catch { }
        }
    }

    /// <summary>按 acquired 纹理的 description 创建/复用 staging 纹理（尺寸或格式变化才重建）。</summary>
    void EnsureStaging(in Texture2DDescription desc)
    {
        if (_staging != null && _stageWidth == (int)desc.Width && _stageHeight == (int)desc.Height && _stageFormat == desc.Format)
            return;
        SafeDispose(_staging);
        _staging = null;
        var stagingDesc = new Texture2DDescription
        {
            Width = desc.Width,
            Height = desc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = desc.Format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None,
        };
        _staging = _device!.CreateTexture2D(stagingDesc);
        _stageWidth = (int)desc.Width;
        _stageHeight = (int)desc.Height;
        _stageFormat = desc.Format;
    }

    /// <summary>只重建 Duplication（保留 device/context）：用于"桌面无新帧但目标区域已变"时重新取初始整帧。</summary>
    void TearDownDuplication()
    {
        SafeDispose(_duplication);
        _duplication = null;
        IsInitialized = false;
        if (_device != null && _output1 != null)
        {
            try
            {
                _duplication = _output1.DuplicateOutput(_device);
                IsInitialized = true;
            }
            catch
            {
                _duplication = null;
                IsInitialized = false;
            }
        }
    }

    /// <summary>
    /// 重建 Duplication 只为拿到"初始帧"：新建的 Duplication 会把**当前桌面**作为第一个可 Acquire 的帧交付，
    /// 于是"桌面内容未变化（无新帧）但抓取区域变了"的情况也能立刻取到正确内容。
    /// 相比整段重建（设备 + 适配器 + output 全来一遍，几十毫秒），这里只重建 Duplication（毫秒级）。
    /// </summary>
    bool TryRecreateDuplicationOnly()
    {
        SafeDispose(_duplication);
        _duplication = null;
        IsInitialized = false;
        if (_device == null || _output1 == null) return false;
        try
        {
            _duplication = _output1.DuplicateOutput(_device);
            IsInitialized = true;
            return true;
        }
        catch
        {
            _duplication = null;
            IsInitialized = false;
            return false;
        }
    }

    void TearDownAll()
    {
        SafeDispose(_staging);
        _staging = null;
        _stageWidth = _stageHeight = 0;
        _stageFormat = Format.Unknown;
        SafeDispose(_duplication);
        _duplication = null;
        SafeDispose(_output1);
        _output1 = null;
        SafeDispose(_output);
        _output = null;
        SafeDispose(_adapter);
        _adapter = null;
        SafeDispose(_dxgiDevice);
        _dxgiDevice = null;
        SafeDispose(_context);
        _context = null;
        SafeDispose(_device);
        _device = null;
        IsInitialized = false;
        LastFrameValid = false;
        OutputIndex = -1;
        _rotationSupported = false;
    }

    static void SafeDispose(IDisposable? d)
    {
        try { d?.Dispose(); } catch { }
    }
}
