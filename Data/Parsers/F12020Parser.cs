using System;
using System.Runtime.InteropServices;

namespace RevHub.Data.Parsers
{
    /// <summary>
    /// F1 2020 UDP 遥测数据解析器
    /// 端口: 20777, UDP Format: 2020
    /// 处理 Car Telemetry (ID:6), Car Status (ID:7), Lap Data (ID:2)
    /// </summary>
    public class F12020Parser : ITelemetryParser
    {
        // 包头大小: 24 字节
        private const int HeaderSize = 24;

        // 包类型 ID
        private const byte PacketCarTelemetry = 6;
        private const byte PacketCarStatus = 7;
        private const byte PacketLapData = 2;

        // 缓存最新的状态数据
        private F12020CarStatusData _lastStatusData;
        private F12020LapData _lastLapData;
        private ushort _maxRPM = 12000;
        private ushort _idleRPM = 4000;

        // 最小包大小: 包头 + 最小数据
        public int ExpectedPacketSize => HeaderSize + 20;

        /// <summary>
        /// 解析 F1 2020 UDP 数据包
        /// 每个包可能包含不同类型的数据，需要根据 PacketId 分别处理
        /// </summary>
        public ForzaTelemetryData Parse(byte[] data)
        {
            if (data == null || data.Length < HeaderSize)
            {
                throw new ArgumentException($"数据长度不足，需要至少 {HeaderSize} 字节");
            }

            // 解析包头
            var header = ParseHeader(data);

            // 根据包类型处理
            switch (header.PacketId)
            {
                case PacketCarTelemetry:
                    return ParseCarTelemetry(data, header);
                case PacketCarStatus:
                    return ParseCarStatus(data, header);
                case PacketLapData:
                    return ParseLapData(data, header);
                default:
                    // 其他包类型，返回缓存的数据
                    return ConvertToForzaFormat();
            }
        }

        public bool IsValidPacketSize(int length)
        {
            return length >= ExpectedPacketSize;
        }

        private F12020PacketHeader ParseHeader(byte[] data)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<F12020PacketHeader>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        private ForzaTelemetryData ParseCarTelemetry(byte[] data, F12020PacketHeader header)
        {
            if (data.Length < HeaderSize + Marshal.SizeOf<F12020CarTelemetryData>())
            {
                return ConvertToForzaFormat();
            }

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var telemetry = Marshal.PtrToStructure<F12020CarTelemetryData>(
                    handle.AddrOfPinnedObject() + HeaderSize);

                // 获取玩家车辆数据
                var playerData = GetPlayerCarTelemetry(data, header.PlayerCarIndex);

                // 更新 RPM 信息
                if (playerData.EngineRPM > 0)
                {
                    _maxRPM = (ushort)Math.Max(_maxRPM, playerData.EngineRPM);
                }

                return ConvertToForzaFormat(playerData);
            }
            finally
            {
                handle.Free();
            }
        }

        private F12020CarTelemetryData GetPlayerCarTelemetry(byte[] data, byte playerCarIndex)
        {
            int carDataSize = Marshal.SizeOf<F12020CarTelemetryData>();
            int offset = HeaderSize + (playerCarIndex * carDataSize);

            if (data.Length < offset + carDataSize)
            {
                return default;
            }

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<F12020CarTelemetryData>(
                    handle.AddrOfPinnedObject() + offset);
            }
            finally
            {
                handle.Free();
            }
        }

        private ForzaTelemetryData ParseCarStatus(byte[] data, F12020PacketHeader header)
        {
            int carStatusSize = Marshal.SizeOf<F12020CarStatusData>();
            int offset = HeaderSize + (header.PlayerCarIndex * carStatusSize);

            if (data.Length < offset + carStatusSize)
            {
                return ConvertToForzaFormat();
            }

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                _lastStatusData = Marshal.PtrToStructure<F12020CarStatusData>(
                    handle.AddrOfPinnedObject() + offset);

                _maxRPM = _lastStatusData.MaxRPM;
                _idleRPM = _lastStatusData.IdleRPM;

                return ConvertToForzaFormat();
            }
            finally
            {
                handle.Free();
            }
        }

        private ForzaTelemetryData ParseLapData(byte[] data, F12020PacketHeader header)
        {
            int lapDataSize = Marshal.SizeOf<F12020LapData>();
            int offset = HeaderSize + (header.PlayerCarIndex * lapDataSize);

            if (data.Length < offset + lapDataSize)
            {
                return ConvertToForzaFormat();
            }

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                _lastLapData = Marshal.PtrToStructure<F12020LapData>(
                    handle.AddrOfPinnedObject() + offset);

                return ConvertToForzaFormat();
            }
            finally
            {
                handle.Free();
            }
        }

        private ForzaTelemetryData ConvertToForzaFormat(F12020CarTelemetryData telemetry = default)
        {
            return new ForzaTelemetryData
            {
                IsRaceOn = 1,
                TimestampMS = (uint)Environment.TickCount,

                // 发动机转速
                EngineMaxRpm = _maxRPM,
                EngineIdleRpm = _idleRPM,
                CurrentEngineRpm = telemetry.EngineRPM,

                // 速度 (F1 用 km/h, Forza 用 m/s)
                Speed = telemetry.Speed / 3.6f,

                // 踏板输入 (F1: 0.0-1.0, Forza: 0-255)
                Accelerator = (byte)(telemetry.Throttle * 255f),
                Brake = (byte)(telemetry.Brake * 255f),
                Clutch = (byte)(telemetry.Clutch * 2.55f),

                // 转向 (F1: -1.0 到 1.0, Forza: -127 到 127)
                Steer = (sbyte)(telemetry.Steer * 127f),

                // 档位 (F1: -1=R, 0=N, 1-8, Forza: 0=R, 11=N, 1-16)
                Gear = ConvertGear(telemetry.Gear),

                // 轮胎温度 (简化，使用表面温度)
                TireTempFrontLeft = telemetry.TyreSurfaceTemperatureFL,
                TireTempFrontRight = telemetry.TyreSurfaceTemperatureFR,
                TireTempRearLeft = telemetry.TyreSurfaceTemperatureRL,
                TireTempRearRight = telemetry.TyreSurfaceTemperatureRR,

                // 燃油
                Fuel = _lastStatusData.FuelCapacity > 0
                    ? _lastStatusData.FuelInTank / _lastStatusData.FuelCapacity
                    : 0f,

                // 圈速
                BestLap = _lastLapData.BestLapTime,
                LastLap = _lastLapData.LastLapTime / 1000f,
                CurrentLap = _lastLapData.CurrentLapTime / 1000f,
                Lap = _lastLapData.CurrentLapNum,
                RacePosition = _lastLapData.CarPosition,
            };
        }

        private static byte ConvertGear(sbyte f1Gear)
        {
            // F1: -1=R, 0=N, 1-8
            // Forza: 0=R, 11=N, 1-16
            if (f1Gear == -1) return 0;   // R
            if (f1Gear == 0) return 11;   // N
            if (f1Gear >= 1 && f1Gear <= 8) return (byte)f1Gear;
            return 11; // N
        }
    }
}
