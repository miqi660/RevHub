using System;
using System.Runtime.InteropServices;

namespace RevHub.Data
{
    /// <summary>
    /// F1 2020 UDP 遥测数据结构
    /// 基于 Codemasters F1 2020 UDP Specification
    /// 端口: 20777, UDP Format: 2020
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct F12020PacketHeader
    {
        public ushort PacketFormat;        // 2020
        public byte GameMajorVersion;      // 游戏主版本号
        public byte GameMinorVersion;      // 游戏次版本号
        public byte PacketVersion;         // 包版本
        public byte PacketId;              // 包类型 ID
        public ulong SessionUID;           // 会话唯一标识
        public float SessionTime;          // 会话时间戳
        public uint FrameIdentifier;       // 帧标识
        public byte PlayerCarIndex;        // 玩家车辆索引
        public sbyte SecondaryPlayerCarIndex; // 第二玩家车辆索引 (-1=无)
    }

    /// <summary>
    /// F1 2020 Car Telemetry 数据 (Packet ID: 6)
    /// 包含速度、转速、踏板、轮胎温度等实时数据
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct F12020CarTelemetryData
    {
        public ushort Speed;                    // 速度 (km/h)
        public float Throttle;                  // 油门 (0.0-1.0)
        public float Steer;                     // 转向 (-1.0 到 1.0)
        public float Brake;                     // 刹车 (0.0-1.0)
        public byte Clutch;                     // 离合器 (0-100)
        public sbyte Gear;                      // 档位 (-1=R, 0=N, 1-8=前进档)
        public ushort EngineRPM;                // 发动机转速
        public byte DRS;                        // DRS 状态 (0=关, 1=开)
        public byte RevLightsPercent;           // 转速灯百分比

        // 刹车温度 (°C)
        public ushort BrakeTemperatureRL;
        public ushort BrakeTemperatureRR;
        public ushort BrakeTemperatureFL;
        public ushort BrakeTemperatureFR;

        // 轮胎表面温度 (°C)
        public byte TyreSurfaceTemperatureRL;
        public byte TyreSurfaceTemperatureRR;
        public byte TyreSurfaceTemperatureFL;
        public byte TyreSurfaceTemperatureFR;

        // 轮胎内部温度 (°C)
        public byte TyreInnerTemperatureRL;
        public byte TyreInnerTemperatureRR;
        public byte TyreInnerTemperatureFL;
        public byte TyreInnerTemperatureFR;

        public ushort EngineTemperature;        // 发动机温度 (°C)

        // 轮胎压力 (PSI)
        public float TyrePressureRL;
        public float TyrePressureRR;
        public float TyrePressureFL;
        public float TyrePressureFR;

        // 路面类型 (每轮)
        public byte SurfaceTypeRL;
        public byte SurfaceTypeRR;
        public byte SurfaceTypeFL;
        public byte SurfaceTypeFR;
    }

    /// <summary>
    /// F1 2020 Car Status 数据 (Packet ID: 7)
    /// 包含燃油、轮胎磨损、损坏等状态信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct F12020CarStatusData
    {
        public byte TractionControl;            // 牵引力控制 (0-2)
        public byte AntiLockBrakes;             // ABS (0=关, 1=开)
        public byte FuelMix;                    // 燃油混合 (0=Lean, 1=Standard, 2=Rich, 3=Max)
        public byte FrontBrakeBias;             // 前刹车偏置 (%)
        public byte PitLimiterStatus;           // 维修区限速器 (0=关, 1=开)
        public float FuelInTank;                // 当前燃油量 (kg)
        public float FuelCapacity;              // 油箱容量 (kg)
        public float FuelRemainingLaps;         // 剩余圈数燃油
        public ushort MaxRPM;                   // 最大转速
        public ushort IdleRPM;                  // 怠速转速
        public byte MaxGears;                   // 最大档位数
        public byte DRSAllowed;                 // DRS 是否可用 (0=否, 1=是)

        // 轮胎磨损 (%)
        public byte TyreWearRL;
        public byte TyreWearRR;
        public byte TyreWearFL;
        public byte TyreWearFR;

        // 实际轮胎化合物
        public byte ActualTyreCompound;         // 轮胎类型 (16=超软, 17=软, 18=中, 19=硬, 7=中性雨胎, 8=全雨胎)
        public byte VisualTyreCompound;         // 视觉轮胎类型

        public byte TyresAgeLaps;               // 轮胎已用圈数

        public byte VehicleFiaFlags;            // FIA 旗帜 (-1=无效, 0=无, 1=绿, 2=蓝, 3=黄, 4=红)

        public float ERSStoreEnergy;            // ERS 储存能量 (J)
        public byte ERSDeployMode;              // ERS 部署模式 (0=无, 1=低, 2=中, 3=高, 4=超车, 5=热身圈)
        public float ERSHarvestedThisLapMGUH;   // 本圈 MGU-H 回收能量
        public float ERSHarvestedThisLapMGUK;   // 本圈 MGU-K 回收能量
        public float ERSDeployedThisLap;        // 本圈 ERS 部署能量

        public byte NetworkPaused;              // 网络暂停 (0=正常, 1=暂停)
    }

    /// <summary>
    /// F1 2020 Lap Data (Packet ID: 2)
    /// 包含圈速、赛道位置等信息
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct F12020LapData
    {
        public uint LastLapTime;                // 上一圈时间 (ms)
        public uint CurrentLapTime;             // 当前圈时间 (ms)
        public ushort Sector1TimeInMS;          // S1 时间 (ms)
        public ushort Sector2TimeInMS;          // S2 时间 (ms)
        public float BestLapTime;               // 最佳圈速 (s)
        public float BestLapOverallTime;        // 整体最佳圈速 (s)
        public byte BestLapSector1TimeInMS;     // 最佳圈 S1
        public byte BestLapSector2TimeInMS;     // 最佳圈 S2
        public byte BestLapSector3TimeInMS;     // 最佳圈 S3
        public float BestOverallSector1Time;    // 整体最佳 S1
        public ushort BestOverallSector1LapNum; // 整体最佳 S1 圈数
        public float BestOverallSector2Time;    // 整体最佳 S2
        public ushort BestOverallSector2LapNum; // 整体最佳 S2 圈数
        public float BestOverallSector3Time;    // 整体最佳 S3
        public ushort BestOverallSector3LapNum; // 整体最佳 S3 圈数

        public float LapDistance;               // 本圈行驶距离 (m)
        public float TotalDistance;             // 总行驶距离 (m)
        public float SafetyCarDelta;            // 安全车 delta (s)
        public byte CarPosition;                // 赛车位置
        public byte CurrentLapNum;              // 当前圈数
        public byte PitStatus;                  // 维修区状态 (0=无, 1=进站中, 2=出站中)
        public byte Sector;                     // 当前扇区 (0, 1, 2)
        public byte CurrentLapInvalid;          // 当前圈无效 (0=有效, 1=无效)
        public byte Penalties;                  // 罚时秒数
        public byte GridPosition;               // 发车位
        public byte DriverStatus;               // 驾驶状态 (0=在赛道, 1=在维修区, 2=退赛, 3=未准备好)
        public byte ResultStatus;               // 结果状态 (0=无效, 1=无效, 2=有效, 3=完赛, 4=未完赛, 5=退赛, 6=未分类, 7=失格)
    }

    /// <summary>
    /// F1 2020 遥测数据解析结果
    /// 整合多个包类型的有用数据，用于 HUD 显示
    /// </summary>
    public struct F12020TelemetryData
    {
        // 来自 Car Telemetry (Packet ID: 6)
        public ushort Speed;                // km/h
        public float Throttle;              // 0.0-1.0
        public float Brake;                 // 0.0-1.0
        public float Steer;                 // -1.0 到 1.0
        public byte Clutch;                 // 0-100
        public sbyte Gear;                  // -1=R, 0=N, 1-8
        public ushort EngineRPM;
        public ushort MaxRPM;
        public ushort IdleRPM;
        public byte DRS;
        public byte RevLightsPercent;
        public ushort EngineTemperature;

        // 轮胎温度
        public byte TyreSurfaceTempFL;
        public byte TyreSurfaceTempFR;
        public byte TyreSurfaceTempRL;
        public byte TyreSurfaceTempRR;

        // 刹车温度
        public ushort BrakeTempFL;
        public ushort BrakeTempFR;
        public ushort BrakeTempRL;
        public ushort BrakeTempRR;

        // 轮胎压力
        public float TyrePressureFL;
        public float TyrePressureFR;
        public float TyrePressureRL;
        public float TyrePressureRR;

        // 来自 Car Status (Packet ID: 7)
        public float FuelInTank;
        public float FuelCapacity;
        public float FuelRemainingLaps;
        public byte FuelMix;
        public byte FrontBrakeBias;
        public byte TractionControl;
        public byte AntiLockBrakes;

        // 轮胎磨损
        public byte TyreWearFL;
        public byte TyreWearFR;
        public byte TyreWearRL;
        public byte TyreWearRR;

        // ERS
        public float ERSStoreEnergy;
        public byte ERSDeployMode;

        // 来自 Lap Data (Packet ID: 2)
        public uint LastLapTimeMs;
        public uint CurrentLapTimeMs;
        public float BestLapTime;
        public ushort Sector1TimeMs;
        public ushort Sector2TimeMs;
        public byte CurrentLapNum;
        public byte CarPosition;
        public float LapDistance;
        public float TotalDistance;
        public byte PitStatus;
        public byte CurrentLapInvalid;

        // 辅助属性
        public float ThrottlePercent => Throttle * 100f;
        public float BrakePercent => Brake * 100f;
        public float ClutchPercent => Clutch;
        public float SteerPercent => Steer * 100f;
        public float FuelPercent => FuelCapacity > 0 ? (FuelInTank / FuelCapacity * 100f) : 0f;

        public string GearString
        {
            get
            {
                if (Gear == -1) return "R";
                if (Gear == 0) return "N";
                if (Gear >= 1 && Gear <= 8) return Gear.ToString();
                return "N";
            }
        }

        public string CurrentLapTimeString => TimeSpan.FromMilliseconds(CurrentLapTimeMs).ToString(@"mm\:ss\.fff");
        public string LastLapTimeString => LastLapTimeMs > 0 ? TimeSpan.FromMilliseconds(LastLapTimeMs).ToString(@"mm\:ss\.fff") : "--:--.---";
        public string BestLapTimeString => BestLapTime > 0 ? TimeSpan.FromSeconds(BestLapTime).ToString(@"mm\:ss\.fff") : "--:--.---";
    }
}
