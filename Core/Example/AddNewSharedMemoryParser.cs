// ═══════════════════════════════════════════════════════════
//  添加新游戏的共享内存解析器 — 模板
// ═══════════════════════════════════════════════════════════
//
//  步骤:
//  1. 复制本文件，重命名为 YourGameParser.cs
//  2. 定义共享内存结构体 (参考游戏 SDK 文档)
//  3. 实现 MapToBasic() 映射到 BasicTelemetryData
//  4. 在 MainWindow.xaml.cs 的构造函数中添加分支
//  5. 在 LauncherViewModel.TestConnectionAsync() 中添加测试分支
//  6. 在 AppSettings.games 中添加游戏配置
//
//  参考实现:
//  - AcSharedMemoryParser.cs   (Assetto Corsa)
//  - AccSharedMemoryParser.cs  (Assetto Corsa Competizione)
//  - Ets2SharedMemoryParser.cs (Euro Truck Simulator 2)
//
// ═══════════════════════════════════════════════════════════

using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RevHub.Core.Models;

namespace RevHub.Core.Parsers.Example;

/// <summary>
/// 新游戏共享内存解析器模板
/// 替换 MEMORY_NAME 和 YourGamePhysicsData 为实际值
/// </summary>
public class YourGameSharedMemoryParser : IDisposable
{
    // ① 共享内存名称 — 从游戏 SDK 文档获取
    private const string MEMORY_NAME = "Local\\YourGameTelemetry";

    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _accessor;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private bool _disposed;

    /// <summary>
    /// 数据更新事件 — MainWindow 订阅此事件
    /// </summary>
    public event EventHandler<BasicTelemetryData>? DataUpdated;

    public bool IsRunning { get; private set; }
    public int UpdateRate { get; set; } = 60;

    public void Start()
    {
        if (IsRunning) return;

        try
        {
            _mmf = MemoryMappedFile.OpenExisting(MEMORY_NAME, MemoryMappedFileRights.Read);
            _accessor = _mmf.CreateViewAccessor(
                0,
                Marshal.SizeOf<YourGamePhysicsData>(),
                MemoryMappedFileAccess.Read
            );

            _cts = new CancellationTokenSource();
            IsRunning = true;
            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"无法打开共享内存。请确保游戏正在运行。错误: {ex.Message}", ex);
        }
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        IsRunning = false;
        try { _readTask?.Wait(TimeSpan.FromSeconds(2)); }
        catch (AggregateException) { }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        var interval = TimeSpan.FromMilliseconds(1000.0 / UpdateRate);
        int lastPacketId = -1;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_accessor == null) break;

                YourGamePhysicsData raw = default;
                _accessor.Read(0, out raw);

                if (raw.PacketId != lastPacketId)
                {
                    lastPacketId = raw.PacketId;
                    DataUpdated?.Invoke(this, MapToBasic(raw));
                }

                await Task.Delay(interval, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { /* 忽略读取错误 */ }
        }
    }

    /// <summary>
    /// ② 核心映射 — 将游戏原始数据转为 BasicTelemetryData
    /// 注意:
    ///   - Speed 单位必须是 m/s
    ///   - Throttle/Brake/Clutch/Handbrake 范围 0-100%
    ///   - Steer 范围 -100 到 100
    ///   - Gear: -1=R, 0=N, 1+=前进档
    /// </summary>
    private BasicTelemetryData MapToBasic(YourGamePhysicsData raw)
    {
        return new BasicTelemetryData
        {
            Rpm = raw.Rpm,
            MaxRpm = raw.MaxRpm > 0 ? raw.MaxRpm : 8000f,
            Speed = raw.SpeedMs,                  // 必须是 m/s
            Gear = raw.Gear,                      // -1=R, 0=N, 1+=前进
            GearDisplay = raw.Gear < 0 ? "R"
                        : raw.Gear == 0 ? "N"
                        : raw.Gear.ToString(),
            Throttle = raw.Throttle * 100f,       // 0-1 → 0-100%
            Brake = raw.Brake * 100f,
            Clutch = raw.Clutch * 100f,
            Handbrake = raw.Handbrake * 100f,
            Steer = raw.SteerAngle * 100f         // -1~1 → -100~100
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _accessor?.Dispose();
        _mmf?.Dispose();
    }
}

/// <summary>
/// ③ 共享内存结构体 — 字段顺序必须与游戏 SDK 完全一致
/// 使用 [StructLayout(Sequential)] 确保内存布局匹配
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct YourGamePhysicsData
{
    public int PacketId;       // 帧序号，用于检测数据更新
    public float Rpm;
    public float MaxRpm;
    public float SpeedMs;      // m/s
    public int Gear;           // -1=R, 0=N, 1+=前进
    public float Throttle;     // 0-1
    public float Brake;        // 0-1
    public float Clutch;       // 0-1
    public float Handbrake;    // 0-1
    public float SteerAngle;   // -1 ~ 1
    // ... 按 SDK 文档补充更多字段
}
