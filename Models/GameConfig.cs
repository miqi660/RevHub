using System.Text.Json.Serialization;

namespace RevHub.Models;

/// <summary>
/// 数据传输方式
/// </summary>
public enum TransportType
{
    /// <summary>
    /// UDP 网络传输
    /// </summary>
    Udp,

    /// <summary>
    /// 共享内存
    /// </summary>
    SharedMemory
}

/// <summary>
/// 游戏配置模型
/// </summary>
public class GameConfig
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("gameName")]
    public string GameName { get; set; } = string.Empty;

    [JsonPropertyName("gameIcon")]
    public string GameIcon { get; set; } = string.Empty;

    [JsonPropertyName("transportType")]
    public TransportType TransportType { get; set; } = TransportType.Udp;

    [JsonPropertyName("udpPort")]
    public int UdpPort { get; set; } = 21337;

    [JsonPropertyName("packetSize")]
    public int PacketSize { get; set; } = 324;

    /// <summary>
    /// 共享内存名称（仅 SharedMemory 类型使用）
    /// </summary>
    [JsonPropertyName("sharedMemoryName")]
    public string SharedMemoryName { get; set; } = string.Empty;

    /// <summary>
    /// 解析器类型的完全限定名，用于 GameRegistry 反射创建解析器实例
    /// </summary>
    [JsonPropertyName("parserType")]
    public string ParserType { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}
