using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RevHub.Models;

namespace RevHub.Services;

/// <summary>
/// 基于 JSON 文件的配置持久化服务
/// 配置文件路径: %AppData%/RevHub/settings.json
/// </summary>
public class JsonConfigService : IConfigService
{
    private static readonly string ConfigDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevHub");

    private static readonly string ConfigPath =
        Path.Combine(ConfigDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings Settings { get; private set; } = new();

    public async Task LoadAsync()
    {
        if (!File.Exists(ConfigPath))
        {
            // 首次运行，使用默认配置
            Settings = CreateDefaultSettings();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(ConfigPath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? CreateDefaultSettings();
        }
        catch (Exception)
        {
            // 配置文件损坏，使用默认配置
            Settings = CreateDefaultSettings();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
            var json = JsonSerializer.Serialize(Settings, JsonOptions);
            await File.WriteAllTextAsync(ConfigPath, json);
        }
        catch (Exception)
        {
            // 保存失败时静默处理，不影响用户体验
        }
    }

    /// <summary>
    /// 创建默认配置（首次运行时使用）
    /// </summary>
    private static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            LastSelectedGame = "forza-horizon-6",
            WindowOpacity = 1.0,
            ShowChart = true,
            PedalMode = "Clutch",
            SteeringTurns = 0.5,
            Games = GetDefaultGames()
        };
    }

    /// <summary>
    /// 内置默认游戏列表
    /// </summary>
    private static List<GameConfig> GetDefaultGames()
    {
        return new List<GameConfig>
        {
            new()
            {
                GameId = "forza-horizon-5",
                GameName = "Forza Horizon 5",
                GameIcon = "🏎️",
                TransportType = TransportType.Udp,
                UdpPort = 21337,
                PacketSize = 324,
                ParserType = "RevHub.Data.Parsers.ForzaParser",
                Description = "Forza Horizon 5 实时遥测数据 (UDP)",
                IsEnabled = true
            },
            new()
            {
                GameId = "forza-horizon-6",
                GameName = "Forza Horizon 6",
                GameIcon = "🏎️",
                TransportType = TransportType.Udp,
                UdpPort = 27875,
                PacketSize = 324,
                ParserType = "RevHub.Data.Parsers.ForzaParser",
                Description = "Forza Horizon 6 实时遥测数据 (UDP)",
                IsEnabled = true
            },
            new()
            {
                GameId = "assetto-corsa",
                GameName = "Assetto Corsa",
                GameIcon = "🏎️",
                TransportType = TransportType.SharedMemory,
                UdpPort = 0,
                PacketSize = 0,
                ParserType = "RevHub.Core.Parsers.AcSharedMemoryParserAdapter",
                SharedMemoryName = "acpmf_physics",
                Description = "AC 共享内存遥测数据 (无需端口)",
                IsEnabled = true
            },
            new()
            {
                GameId = "assetto-corsa-competizione",
                GameName = "Assetto Corsa Competizione",
                GameIcon = "🏁",
                TransportType = TransportType.SharedMemory,
                UdpPort = 0,
                PacketSize = 0,
                ParserType = "RevHub.Core.Parsers.AccSharedMemoryParserAdapter",
                SharedMemoryName = "Local\\acpmf_physics",
                Description = "ACC 共享内存遥测数据 (无需端口)",
                IsEnabled = true
            },
            new()
            {
                GameId = "euro-truck-simulator-2",
                GameName = "Euro Truck Simulator 2",
                GameIcon = "🚛",
                TransportType = TransportType.SharedMemory,
                UdpPort = 0,
                PacketSize = 0,
                ParserType = "RevHub.Core.Parsers.Ets2SharedMemoryParserAdapter",
                SharedMemoryName = "Local\\SCSTelemetry",
                Description = "ETS2 共享内存遥测数据 (需安装 SDK 插件)",
                IsEnabled = true
            },
            new()
            {
                GameId = "f1-2020",
                GameName = "F1 2020",
                GameIcon = "🏎️",
                TransportType = TransportType.Udp,
                UdpPort = 20777,
                PacketSize = 1464,
                ParserType = "RevHub.Data.Parsers.F12020Parser",
                Description = "F1 2020 实时遥测数据 (UDP Format 2020)",
                IsEnabled = true
            }
        };
    }
}
