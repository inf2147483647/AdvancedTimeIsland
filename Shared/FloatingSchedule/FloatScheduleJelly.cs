// 果冻效果的共享数学（纯 System，不依赖任何 UI 框架 → 进程内窗口与独立子进程共用同一份实现）。
//
// 语义：拖动时按指针的**二维速度**做形变 —— 沿运动主轴拉伸、垂直方向压缩；松手后欠阻尼弹簧回弹。
// 只做数学、不做定时器/变换：时间片与 UI 变换由各宿主（DispatcherTimer / RenderTransform）提供。
//
// 【为什么要把"算速度"和"写 UI"分开】拖动时指针事件频率可达 240Hz，而窗口位置只在渲染帧/节流点提交。
//   若每个指针事件都改 ScaleTransform，内容在窗口不动的情况下反复变形 → 视觉抽搐。
//   因此宿主约定：PointerMoved 只调 OnDrag 累积速度/目标形变，真正写 ScaleTransform 与窗口位置
//   在同一帧提交（见各窗口的 ApplyDragFrame / 位置提交处）。
using System;

namespace AdvancedTimeIsland.Shared.FloatingSchedule;

public sealed class FloatScheduleJelly
{
    /// <summary>拖动中最大形变量（10%：横向速度拉满时沿主轴拉伸 10%，垂直方向压缩 7.2%）。</summary>
    public const double MaxStretch = 0.10;
    /// <summary>速度拉满阈值（px/ms）：达到该合速度即用满最大形变。</summary>
    const double FullSpeed = 3.0;
    /// <summary>跟手系数：每次采样向目标形变/目标主轴靠近的比例（过大抖动、过小迟滞）。</summary>
    const double FollowRate = 0.35;
    /// <summary>松手初速系数（px/ms → 变形量/秒）：甩得越快回弹越猛。</summary>
    const double ReleaseGain = 0.0045;
    /// <summary>弹簧角频率：越大回弹越快。</summary>
    const double Omega = 26.0;
    /// <summary>阻尼比 &lt;1 → 有 1~2 次可见过冲（"Q 弹"的来源）。</summary>
    const double Zeta = 0.32;
    /// <summary>垂直方向压缩相对主轴拉伸的比例（0.72 接近体积守恒）。</summary>
    const double SquashRatio = 0.72;

    double _d;        // 变形量（>0 表示沿主轴拉伸）
    double _v;        // 弹簧速度（变形量/秒）
    double _vx;       // 平滑后的水平速度（px/ms）
    double _vy;       // 平滑后的垂直速度（px/ms）
    double _ax = 0.5; // 主轴权重：1=纯横向、0=纯纵向、0.5=斜向（不形变）
    double _lastX;    // 上次指针屏幕 X（px）
    double _lastY;    // 上次指针屏幕 Y（px）
    bool _hasLast;    // 是否已有上一采样点（决定能否算速度）
    double _lastSpeed; // 松手时的合速度（px/ms）

    /// <summary>横向缩放：主轴为横向时拉伸，主轴为纵向时压缩（斜向自动过渡，无跳变）。</summary>
    public double ScaleX => 1.0 + _d * (2.0 * _ax - 1.0);
    /// <summary>纵向缩放：与横向相反（保持面积近似守恒）。</summary>
    public double ScaleY => 1.0 - _d * SquashRatio * (2.0 * _ax - 1.0);
    public double Stretch => _d;
    /// <summary>当前主轴权重（仅供调试/宿主判断）。</summary>
    public double Axis => _ax;
    public bool IsIdle => Math.Abs(_d) < 0.0008 && Math.Abs(_v) < 0.02;

    /// <summary>复位到无形变（拖动开始 / 关闭开关 / 切换材质时调用）。</summary>
    public void Reset()
    {
        _d = 0;
        _v = 0;
        _vx = 0;
        _vy = 0;
        _ax = 0.5;
        _lastX = 0;
        _lastY = 0;
        _hasLast = false;
        _lastSpeed = 0;
    }

    /// <summary>
    /// 拖动中每个指针移动事件调用一次（只累积速度与目标形变，**不**直接写 UI 变换）。
    /// </summary>
    /// <param name="screenX">指针的屏幕 X（物理像素）。</param>
    /// <param name="screenY">指针的屏幕 Y（物理像素）。</param>
    /// <param name="dtMs">距上一次调用的毫秒间隔（首次可传 0）。</param>
    public void OnDrag(double screenX, double screenY, double dtMs)
    {
        if (_hasLast && dtMs > 0.1)
        {
            double vx = (screenX - _lastX) / dtMs;                                  // px/ms
            double vy = (screenY - _lastY) / dtMs;
            _vx = _vx * 0.55 + vx * 0.45;                                          // 一阶低通，避免逐帧抖动
            _vy = _vy * 0.55 + vy * 0.45;
            double speed = Math.Sqrt(_vx * _vx + _vy * _vy);
            _lastSpeed = speed;
            double target = Math.Clamp(speed / FullSpeed, 0, 1) * MaxStretch;
            _d += (target - _d) * FollowRate;                                      // 跟手渐进，不硬跳
            // 主轴权重同样平滑：横向拖 → 1，纵向拖 → 0，斜向连续过渡（避免主轴在 45° 附近来回跳）
            double rawAx = Math.Abs(_vx) / (Math.Abs(_vx) + Math.Abs(_vy) + 1e-6);
            _ax += (rawAx - _ax) * FollowRate;
        }
        _lastX = screenX;
        _lastY = screenY;
        _hasLast = true;
    }

    /// <summary>
    /// 松手：给出回弹初速。返回 true 表示还需要跑弹簧动画，false 表示本来就几乎没形变。
    /// </summary>
    public bool BeginRelease()
    {
        _hasLast = false;
        if (IsIdle)
        {
            _d = 0;
            _v = 0;
            return false;
        }
        _v = -Math.Abs(_lastSpeed) * ReleaseGain;   // 朝"压回去"的方向给初速
        return true;
    }

    /// <summary>
    /// 弹簧步进（固定步长积分）。返回 true 表示仍在动画中，false 表示已收敛（调用方应停定时器）。
    /// </summary>
    public bool Step(double dtSeconds)
    {
        double accel = -Omega * Omega * _d - 2.0 * Zeta * Omega * _v;
        _v += accel * dtSeconds;
        _d += _v * dtSeconds;
        if (IsIdle)
        {
            _d = 0;
            _v = 0;
            return false;
        }
        return true;
    }
}
