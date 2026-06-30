using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevHub.Models;

namespace RevHub.ViewModels;

/// <summary>
/// 游戏卡片 ViewModel
/// 包装 GameConfig 用于游戏列表显示
/// </summary>
public partial class GameCardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _gameId = string.Empty;

    [ObservableProperty]
    private string _gameName = string.Empty;

    [ObservableProperty]
    private string _gameIcon = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TransportType _transportType = TransportType.Udp;

    [ObservableProperty]
    private int _udpPort;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// 传输类型描述
    /// </summary>
    public string TransportTypeDisplay => TransportType switch
    {
        TransportType.Udp => "UDP",
        TransportType.SharedMemory => "共享内存",
        _ => "未知"
    };

    /// <summary>
    /// 是否使用共享内存
    /// </summary>
    public bool IsSharedMemory => TransportType == TransportType.SharedMemory;

    /// <summary>
    /// 使用说明
    /// </summary>
    public string SetupInstructions => GameId switch
    {
        "euro-truck-simulator-2" => "安装说明：\n1. 将 SHETS2Telemetry_x64.dll 复制到 ETS2 安装目录的 bin\\win_x64\\plugins\\ 文件夹\n2. 启动 ETS2 游戏\n3. 首次启动时会提示 SDK 已激活，点击确定\n4. 进入游戏后点击\"测试连接\"",
        "american-truck-simulator" => "安装说明：\n1. 将 SHETS2Telemetry_x64.dll 复制到 ATS 安装目录的 bin\\win_x64\\plugins\\ 文件夹\n2. 启动 ATS 游戏\n3. 首次启动时会提示 SDK 已激活，点击确定\n4. 进入游戏后点击\"测试连接\"",
        "assetto-corsa" => "使用说明：\n1. 启动 Assetto Corsa 游戏\n2. 进入比赛\n3. 点击\"测试连接\"",
        "assetto-corsa-competizione" => "使用说明：\n1. 启动 ACC 游戏\n2. 进入比赛\n3. 点击\"测试连接\"",
        "forza-horizon-5" => "使用说明：\n1. 启动 Forza Horizon 5\n2. 在设置中开启 UDP 输出（端口 21337）\n3. 进入比赛\n4. 点击\"测试连接\"",
        "forza-horizon-6" => "使用说明：\n1. 启动 Forza Horizon 6\n2. 在设置中开启 UDP 输出（端口 27875）\n3. 进入比赛\n4. 点击\"测试连接\"",
        "f1-2020" => "使用说明：\n1. 启动 F1 2020\n2. 游戏设置 → 遥测设置\n3. 开启 UDP Telemetry\n4. UDP Format 选择 2020\n5. UDP Send Rate: 60Hz\n6. 进入比赛\n7. 点击\"测试连接\"",
        _ => ""
    };

    /// <summary>
    /// 是否有使用说明
    /// </summary>
    public bool HasSetupInstructions => !string.IsNullOrEmpty(SetupInstructions);

    /// <summary>
    /// 插件文件路径
    /// </summary>
    public string PluginFilePath => GameId switch
    {
        "euro-truck-simulator-2" => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "ETS2", "SHETS2Telemetry_x64.dll"),
        "american-truck-simulator" => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "ETS2", "SHETS2Telemetry_x64.dll"),
        _ => ""
    };

    /// <summary>
    /// 是否有插件文件
    /// </summary>
    public bool HasPluginFile => !string.IsNullOrEmpty(PluginFilePath) && File.Exists(PluginFilePath);

    /// <summary>
    /// 打开插件文件所在文件夹
    /// </summary>
    [RelayCommand]
    private void OpenPluginFolder()
    {
        if (!string.IsNullOrEmpty(PluginFilePath))
        {
            var directory = Path.GetDirectoryName(PluginFilePath);
            if (Directory.Exists(directory))
            {
                Process.Start("explorer.exe", directory);
            }
        }
    }

    public GameCardViewModel()
    {
    }

    public GameCardViewModel(GameConfig config)
    {
        GameId = config.GameId;
        GameName = config.GameName;
        GameIcon = config.GameIcon;
        Description = config.Description;
        TransportType = config.TransportType;
        UdpPort = config.UdpPort;
        IsEnabled = config.IsEnabled;
    }

    /// <summary>
    /// 转换回 GameConfig 用于持久化
    /// </summary>
    public GameConfig ToConfig()
    {
        return new GameConfig
        {
            GameId = GameId,
            GameName = GameName,
            GameIcon = GameIcon,
            Description = Description,
            TransportType = TransportType,
            UdpPort = UdpPort,
            IsEnabled = IsEnabled
        };
    }
}
