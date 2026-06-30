using CommunityToolkit.Mvvm.ComponentModel;
using RevHub.Models;

namespace RevHub.ViewModels;

/// <summary>
/// HUD 设置面板 ViewModel
/// 管理透明度、踏板模式、方向盘圈数等配置项
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _windowOpacity = 1.0;

    [ObservableProperty]
    private bool _showChart = true;

    [ObservableProperty]
    private string _pedalMode = "Clutch";

    [ObservableProperty]
    private double _steeringTurns = 0.5;

    [ObservableProperty]
    private int _udpPort = 21337;

    [ObservableProperty]
    private int _packetSize = 324;

    /// <summary>
    /// 从 AppSettings 加载配置
    /// </summary>
    public void LoadFromSettings(AppSettings settings)
    {
        WindowOpacity = settings.WindowOpacity;
        ShowChart = settings.ShowChart;
        PedalMode = settings.PedalMode;
        SteeringTurns = settings.SteeringTurns;
    }

    /// <summary>
    /// 将当前值应用到 AppSettings
    /// </summary>
    public void ApplyToSettings(AppSettings settings)
    {
        settings.WindowOpacity = WindowOpacity;
        settings.ShowChart = ShowChart;
        settings.PedalMode = PedalMode;
        settings.SteeringTurns = SteeringTurns;
    }
}
