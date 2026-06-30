// ═══════════════════════════════════════════════════════════
//  添加新游戏的 UDP 解析器 — 模板
// ═══════════════════════════════════════════════════════════
//
//  步骤:
//  1. 复制本文件，重命名为 YourGameUdpParser.cs
//  2. 实现 Data.Parsers.ITelemetryParser 接口
//  3. 在 GameRegistry._parserFactories 中注册
//  4. 在 AppSettings.games 中添加游戏配置 (ParserType 指向此类全名)
//
//  参考实现:
//  - Data/Parsers/ForzaParser.cs (Forza Horizon 6 / Motorsport)
//
// ═══════════════════════════════════════════════════════════

using System;
using System.Runtime.InteropServices;
using RevHub.Data;
using RevHub.Data.Parsers;

namespace RevHub.Core.Parsers.Example;

/// <summary>
/// 新游戏 UDP 解析器模板
/// UDP 游戏通过网络发送固定格式的二进制数据包
/// </summary>
public class YourGameUdpParser : ITelemetryParser
{
    /// <summary>
    /// 期望的数据包大小 (字节)
    /// 从游戏遥测文档获取
    /// </summary>
    public int ExpectedPacketSize => 256; // 替换为实际值

    /// <summary>
    /// 解析 UDP 数据包 → ForzaTelemetryData
    ///
    /// 映射规则:
    ///   - Speed: 游戏单位 → m/s (ForzaTelemetryData.Speed 的单位)
    ///   - Accelerator/Brake/Clutch/Handbrake: 0-255 (byte)
    ///   - Steer: -127 到 127 (sbyte)
    ///   - Gear: 0=R, 11=N, 1-16=前进档
    ///   - CurrentEngineRpm / EngineMaxRpm: float
    /// </summary>
    public ForzaTelemetryData Parse(byte[] data)
    {
        // 方式 1: blittable struct 直接 Marshal (推荐，零拷贝)
        // if (data.Length < ExpectedPacketSize)
        //     throw new ArgumentException($"数据长度不足: {data.Length} < {ExpectedPacketSize}");
        // return Marshal.PtrToStructure<YourGamePacket>(
        //     GCHandle.Alloc(data, GCHandleType.Pinned).AddrOfPinnedObject()
        // );

        // 方式 2: 手动按偏移量读取 (灵活，支持非 blittable)
        var result = new ForzaTelemetryData
        {
            CurrentEngineRpm = BitConverter.ToSingle(data, 0),
            EngineMaxRpm = BitConverter.ToSingle(data, 4),
            Speed = BitConverter.ToSingle(data, 8),         // 必须是 m/s
            Accelerator = data[12],                          // 0-255
            Brake = data[13],
            Clutch = data[14],
            Handbrake = data[15],
            Gear = MapGear(data[16]),                        // 需要档位映射
            Steer = (sbyte)(BitConverter.ToSingle(data, 20) * 127f) // -1~1 → -127~127
        };
        return result;
    }

    /// <summary>
    /// 档位映射 — 游戏原始值 → Forza 格式 (0=R, 11=N, 1-16=前进)
    /// </summary>
    private static byte MapGear(byte rawGear)
    {
        // 示例: 游戏使用 0=N, 1=R, 2+=前进
        // if (rawGear == 1) return 0;       // R
        // if (rawGear == 0) return 11;      // N
        // return (byte)(rawGear - 1);       // 前进档

        return rawGear; // 按实际游戏格式替换
    }

    /// <summary>
    /// 验证数据包大小是否合法
    /// </summary>
    public bool IsValidPacketSize(int length)
    {
        return length >= ExpectedPacketSize;
    }
}

// ═══════════════════════════════════════════════════════════
//  注册方式 (在 GameRegistry.cs 中):
// ═══════════════════════════════════════════════════════════
//
//  private readonly Dictionary<string, Func<ITelemetryParser>> _parserFactories = new()
//  {
//      ["RevHub.Data.Parsers.ForzaParser"] = () => new ForzaParser(),
//      ["RevHub.Core.Parsers.Example.YourGameUdpParser"] = () => new YourGameUdpParser(),
//  };
//
//  然后在 AppSettings.games JSON 中:
//  {
//    "gameId": "your-game",
//    "gameName": "Your Game",
//    "parserType": "RevHub.Core.Parsers.Example.YourGameUdpParser",
//    "udpPort": 12345,
//    ...
//  }
//
// ═══════════════════════════════════════════════════════════
