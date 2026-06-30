using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RevHub.Models;

/// <summary>
/// 应用全局设置，支持 JSON 持久化
/// </summary>
public class AppSettings
{
    [JsonPropertyName("lastSelectedGame")]
    public string LastSelectedGame { get; set; } = "forza-horizon-6";

    [JsonPropertyName("windowOpacity")]
    public double WindowOpacity { get; set; } = 1.0;

    [JsonPropertyName("showChart")]
    public bool ShowChart { get; set; } = true;

    [JsonPropertyName("pedalMode")]
    public string PedalMode { get; set; } = "Clutch";

    [JsonPropertyName("steeringTurns")]
    public double SteeringTurns { get; set; } = 0.5;

    [JsonPropertyName("games")]
    public List<GameConfig> Games { get; set; } = new();
}
