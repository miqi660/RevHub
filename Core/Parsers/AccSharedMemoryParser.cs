using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RevHub.Core.Models;

namespace RevHub.Core.Parsers
{
    /// <summary>
    /// Assetto Corsa Competizione 共享内存解析器
    /// ACC 通过共享内存输出实时遥测数据，不使用 UDP
    /// </summary>
    public class AccSharedMemoryParser : IDisposable
    {
        // ACC 共享内存名称
        private const string PHYSICS_MEMORY_NAME = "Local\\acpmf_physics";
        private const string GRAPHICS_MEMORY_NAME = "Local\\acpmf_graphics";
        private const string STATIC_MEMORY_NAME = "Local\\acpmf_static";

        private MemoryMappedFile? _physicsMmf;
        private MemoryMappedViewAccessor? _physicsAccessor;
        private CancellationTokenSource? _cts;
        private Task? _readTask;
        private bool _disposed;

        public string Name => "ACC Shared Memory Parser";
        public string GameId => "assetto-corsa-competizione";
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
                _physicsAccessor = _physicsMmf.CreateViewAccessor(0, Marshal.SizeOf<AccPhysicsData>(), MemoryMappedFileAccess.Read);

                _cts = new CancellationTokenSource();
                IsRunning = true;
                _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"无法打开 ACC 共享内存。请确保游戏正在运行。错误: {ex.Message}", ex);
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
                    AccPhysicsData physicsData = default;
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
        /// 将 ACC 数据映射到标准格式
        /// </summary>
        private BasicTelemetryData MapToBasic(AccPhysicsData physics)
        {
            // ACC 档位表示: 0=R, 1=N, 2=1档, 3=2档, ...
            // 需要减 1 转换为: -1=R, 0=N, 1=1档, 2=2档, ...
            int gear = physics.Gear - 1;

            string gearDisplay;
            if (gear < 0) gearDisplay = "R";
            else if (gear == 0) gearDisplay = "N";
            else gearDisplay = gear.ToString();

            // 调试输出
            Console.WriteLine($"[ACC] RawGear={physics.Gear}, ConvertedGear={gear}, GearDisplay={gearDisplay}");

            return new BasicTelemetryData
            {
                // 转速
                Rpm = physics.Rpms,
                MaxRpm = 8000f, // ACC 共享内存中没有最大转速，使用默认值

                // 速度 (km/h -> m/s)
                Speed = physics.SpeedKmh / 3.6f,

                // 档位
                Gear = gear,
                GearDisplay = gearDisplay,

                // 踏板 (0-1 -> 0-100%)
                Throttle = physics.Gas * 100f,
                Brake = physics.Brake * 100f,
                // ACC 离合器反转: 0=踩下, 1=释放
                Clutch = (1f - physics.Clutch) * 100f,
                Handbrake = 0f, // ACC 共享内存中没有手刹数据

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
            // ACC 不使用 UDP，此方法不适用
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
    /// ACC 共享内存物理数据结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AccPhysicsData
    {
        public int PacketId;
        public float Gas;
        public float Brake;
        public float Fuel;
        public int Gear;
        public int Rpms;
        public float SteerAngle;
        public float SpeedKmh;
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
        public float CarDamageFront;
        public float CarDamageRear;
        public float CarDamageLeft;
        public float CarDamageRight;
        public float CarDamageCentre;
        public int NumberOfTyresOut;
        public int IsAiControlled;
        public float TyreTempI;
        public float TyreTempM;
        public float TyreTempO;
        public int IsInPit;
        public int IsInPitLane;
        public float SurfaceGrip;
        public float MandatoryPitDone;
        public float WindSpeedX;
        public float WindSpeedY;
        public float WindDirectionX;
        public float WindDirectionY;
        public float Clutch;
        // 更多字段...
    }
}
