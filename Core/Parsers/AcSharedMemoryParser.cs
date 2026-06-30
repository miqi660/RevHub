using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RevHub.Core.Models;

namespace RevHub.Core.Parsers
{
    /// <summary>
    /// Assetto Corsa 共享内存解析器
    /// AC 通过共享内存输出实时遥测数据
    /// </summary>
    public class AcSharedMemoryParser : IDisposable
    {
        // AC 共享内存名称
        private const string PHYSICS_MEMORY_NAME = "acpmf_physics";
        private const string GRAPHICS_MEMORY_NAME = "acpmf_graphics";
        private const string STATIC_MEMORY_NAME = "acpmf_static";

        private MemoryMappedFile? _physicsMmf;
        private MemoryMappedViewAccessor? _physicsAccessor;
        private CancellationTokenSource? _cts;
        private Task? _readTask;
        private bool _disposed;

        public string Name => "AC Shared Memory Parser";
        public string GameId => "assetto-corsa";
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
                _physicsMmf = MemoryMappedFile.OpenExisting(PHYSICS_MEMORY_NAME, MemoryMappedFileRights.Read);
                _physicsAccessor = _physicsMmf.CreateViewAccessor(0, Marshal.SizeOf<AcPhysicsData>(), MemoryMappedFileAccess.Read);

                _cts = new CancellationTokenSource();
                IsRunning = true;
                _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"无法打开 AC 共享内存。请确保游戏正在运行。错误: {ex.Message}", ex);
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
            int lastPacketId = -1;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_physicsAccessor == null) break;

                    // 读取共享内存
                    AcPhysicsData physicsData = default;
                    _physicsAccessor.Read(0, out physicsData);

                    // 检查数据是否更新
                    if (physicsData.PacketId != lastPacketId)
                    {
                        lastPacketId = physicsData.PacketId;

                        // 转换为标准格式
                        var basicData = MapToBasic(physicsData);
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
        /// 将 AC 数据映射到标准格式
        /// </summary>
        private BasicTelemetryData MapToBasic(AcPhysicsData physics)
        {
            // AC 档位: 0=R, 1=N, 2=1档, 3=2档, ...
            string gearDisplay;
            if (physics.Gear == 0) gearDisplay = "R";
            else if (physics.Gear == 1) gearDisplay = "N";
            else gearDisplay = (physics.Gear - 1).ToString();

            return new BasicTelemetryData
            {
                // 转速
                Rpm = physics.Rpms,
                MaxRpm = physics.MaxRpms,

                // 速度 (m/s)
                Speed = physics.SpeedMs,

                // 档位
                Gear = physics.Gear,
                GearDisplay = gearDisplay,

                // 踏板 (0-1 -> 0-100%)
                Throttle = physics.Gas * 100f,
                Brake = physics.Brake * 100f,
                // AC 离合器: 0=踩下, 1=释放
                Clutch = (1f - physics.Clutch) * 100f,
                Handbrake = physics.Handbrake * 100f,

                // 方向盘角度
                Steer = physics.SteerAngle * 100f
            };
        }

        /// <summary>
        /// 同步读取一次（IBasicTelemetryParser 接口要求）
        /// </summary>
        public bool TryParse(byte[] rawData, out BasicTelemetryData? result)
        {
            result = null;
            // AC 不使用 UDP，此方法不适用
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
            _physicsAccessor?.Dispose();
            _physicsMmf?.Dispose();
        }
    }

    /// <summary>
    /// AC 共享内存物理数据结构
    /// 参考: acpmf_physics
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AcPhysicsData
    {
        public int PacketId;
        public float Gas;
        public float Brake;
        public float Fuel;
        public int Gear;
        public int Rpms;
        public float SteerAngle;
        public float SpeedMs;
        public float VelocityX;
        public float VelocityY;
        public float VelocityZ;
        public float AccGX;
        public float AccGY;
        public float AccGZ;
        public float WheelSlipFL;
        public float WheelSlipFR;
        public float WheelSlipRL;
        public float WheelSlipRR;
        public float WheelLoadFL;
        public float WheelLoadFR;
        public float WheelLoadRL;
        public float WheelLoadRR;
        public float WheelsPressureFL;
        public float WheelsPressureFR;
        public float WheelsPressureRL;
        public float WheelsPressureRR;
        public float WheelAngularSpeedFL;
        public float WheelAngularSpeedFR;
        public float WheelAngularSpeedRL;
        public float WheelAngularSpeedRR;
        public float TyreWearFL;
        public float TyreWearFR;
        public float TyreWearRL;
        public float TyreWearRR;
        public float TyreDirtyLevelFL;
        public float TyreDirtyLevelFR;
        public float TyreDirtyLevelRL;
        public float TyreDirtyLevelRR;
        public float TyreCoreTempFL;
        public float TyreCoreTempFR;
        public float TyreCoreTempRL;
        public float TyreCoreTempRR;
        public float CamberRadFL;
        public float CamberRadFR;
        public float CamberRadRL;
        public float CamberRadRR;
        public float SuspensionTravelFL;
        public float SuspensionTravelFR;
        public float SuspensionTravelRL;
        public float SuspensionTravelRR;
        public float Drt;
        public float Heading;
        public float Pitch;
        public float Roll;
        public float CgHeight;
        public int LastTyreSlipTimestamp;
        public int LastTyreSlipStatus;
        public int LastTyreSlipSlip;
        public float Handbrake;
        public float MaxRpms;
        public float Clutch;
        // 更多字段...
    }
}
