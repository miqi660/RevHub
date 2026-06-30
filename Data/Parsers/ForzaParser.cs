namespace RevHub.Data.Parsers;

/// <summary>
/// Forza 系列游戏遥测数据解析器
/// 包装现有的 ForzaTelemetryData.FromBytes 方法
/// </summary>
public class ForzaParser : ITelemetryParser
{
    public int ExpectedPacketSize => ForzaTelemetryData.Size; // 324 字节

    public ForzaTelemetryData Parse(byte[] data)
    {
        return ForzaTelemetryData.FromBytes(data);
    }

    public bool IsValidPacketSize(int length)
    {
        return length >= ExpectedPacketSize;
    }
}
