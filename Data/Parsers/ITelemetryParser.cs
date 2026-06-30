namespace RevHub.Data.Parsers;

/// <summary>
/// 遥测数据解析器接口
/// 不同赛车游戏实现各自的解析逻辑
/// </summary>
public interface ITelemetryParser
{
    /// <summary>
    /// 期望的数据包大小（字节）
    /// </summary>
    int ExpectedPacketSize { get; }

    /// <summary>
    /// 将原始 UDP 字节解析为 ForzaTelemetryData 结构体
    /// </summary>
    /// <param name="data">原始字节数据</param>
    /// <returns>解析后的遥测数据</returns>
    ForzaTelemetryData Parse(byte[] data);

    /// <summary>
    /// 验证给定的数据包长度是否对此解析器有效
    /// </summary>
    bool IsValidPacketSize(int length);
}
