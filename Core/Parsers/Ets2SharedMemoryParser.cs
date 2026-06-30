using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RevHub.Core.Models;

namespace RevHub.Core.Parsers
{
    /// <summary>
    /// Euro Truck Simulator 2 共享内存解析器
    /// ETS2 通过 SDK 插件输出实时遥测数据
    /// 共享内存名称: Local\SCSTelemetry
    /// </summary>
    public class Ets2SharedMemoryParser : IDisposable
    {
        // ETS2 共享内存名称
        private const string TELEMETRY_MEMORY_NAME = "Local\\SCSTelemetry";

        private MemoryMappedFile? _mmf;
        private MemoryMappedViewAccessor? _accessor;
        private CancellationTokenSource? _cts;
        private Task? _readTask;
        private bool _disposed;

        public string Name => "ETS2 Shared Memory Parser";
        public string GameId => "euro-truck-simulator-2";
        public int ExpectedPacketSize => 0; // 共享内存不需要包大小

        /// <summary>
        /// 数据更新事件
        /// </summary>
        public event EventHandler<BasicTelemetryData>? DataUpdated;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 更新频率 (Hz)
        /// </summary>
        public int UpdateRate { get; set; } = 60;

        /// <summary>
        /// 启动共享内存监听
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                return;

            try
            {
                _mmf = MemoryMappedFile.OpenExisting(TELEMETRY_MEMORY_NAME, MemoryMappedFileRights.Read);
                _accessor = _mmf.CreateViewAccessor(0, Marshal.SizeOf<Ets2TelemetryData>(), MemoryMappedFileAccess.Read);

                _cts = new CancellationTokenSource();
                IsRunning = true;
                _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"无法打开 ETS2 共享内存。请确保游戏正在运行且 SDK 插件已安装。错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            _cts?.Cancel();
            IsRunning = false;

            try
            {
                _readTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException) { }
        }

        /// <summary>
        /// 读取循环
        /// </summary>
        private async Task ReadLoopAsync(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromMilliseconds(1000.0 / UpdateRate);
            uint lastTime = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_accessor == null) break;

                    // 读取共享内存
                    Ets2TelemetryData telemetryData = default;
                    _accessor.Read(0, out telemetryData);

                    // 检查数据是否更新
                    if (telemetryData.Time != lastTime)
                    {
                        lastTime = telemetryData.Time;

                        // 转换为标准格式
                        var basicData = MapToBasic(telemetryData);
                        DataUpdated?.Invoke(this, basicData);
                    }

                    await Task.Delay(interval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // 忽略错误继续读取
                }
            }
        }

        /// <summary>
        /// 将 ETS2 数据映射到标准格式
        /// </summary>
        private BasicTelemetryData MapToBasic(Ets2TelemetryData telemetry)
        {
            // ETS2 档位: 0=N, 1-16=前进, -1=R
            // 需要转换为: -1=R, 0=N, 1+=前进
            int gear = telemetry.Gear;
            string gearDisplay;
            if (gear < 0) gearDisplay = "R";
            else if (gear == 0) gearDisplay = "N";
            else gearDisplay = gear.ToString();

            return new BasicTelemetryData
            {
                // 转速 (ETS2 RPM 需要从 Hz 转换)
                Rpm = telemetry.Rpm,
                MaxRpm = telemetry.RpmLimit > 0 ? telemetry.RpmLimit : 3000f,

                // 速度 (m/s)
                Speed = telemetry.Speed,

                // 档位
                Gear = gear,
                GearDisplay = gearDisplay,

                // 踏板 (0-1 -> 0-100%)
                Throttle = telemetry.Throttle * 100f,
                Brake = telemetry.Brake * 100f,
                Clutch = telemetry.Clutch * 100f,
                Handbrake = 0f,

                // 方向盘角度
                Steer = telemetry.Steer * 100f
            };
        }

        /// <summary>
        /// 同步读取一次（IBasicTelemetryParser 接口要求）
        /// </summary>
        public bool TryParse(byte[] rawData, out BasicTelemetryData? result)
        {
            result = null;
            // ETS2 不使用 UDP，此方法不适用
            return false;
        }

        public bool IsValidPacketSize(int packetSize)
        {
            return true; // 共享内存不需要验证包大小
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
            _accessor?.Dispose();
            _mmf?.Dispose();
        }
    }

    /// <summary>
    /// ETS2 共享内存遥测数据结构
    /// 参考: SCS Telemetry SDK
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Ets2TelemetryData
    {
        // 头部信息
        public uint Time;
        public uint Paused;

        // 卡车数据
        public float Speed;              // m/s
        public float AccelerationX;
        public float AccelerationY;
        public float AccelerationZ;
        public float CoordinateX;
        public float CoordinateY;
        public float CoordinateZ;
        public float RotationX;
        public float RotationY;
        public float RotationZ;

        // 发动机
        public float Rpm;
        public float RpmLimit;
        public int Gear;
        public int GearUp;
        public int GearDown;
        public int Selector;

        // 输入
        public float Throttle;
        public float Brake;
        public float Clutch;
        public float Steer;

        // 燃油
        public float Fuel;
        public float FuelCapacity;
        public float FuelConsumption;
        public float FuelRange;

        // 速度
        public float SpeedLimit;

        // 里程表
        public float Odometer;

        // 时间
        public float DrivingTime;
        public float RestTime;
        public float AwakeTime;

        // 更多字段...
    }
}
