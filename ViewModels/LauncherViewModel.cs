using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevHub.Models;
using RevHub.Services;

#nullable enable

namespace RevHub.ViewModels;

/// <summary>
/// 启动界面主 ViewModel
/// 管理游戏列表、设置面板、连接测试和 HUD 启动
/// </summary>
public partial class LauncherViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IGameRegistry _gameRegistry;
    private DispatcherTimer? _debounceTimer;

    public ObservableCollection<GameCardViewModel> Games { get; } = new();

    [ObservableProperty]
    private GameCardViewModel? _selectedGame;

    [ObservableProperty]
    private SettingsViewModel _settings;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _connectionStateText = "未连接";

    [ObservableProperty]
    private bool _isTesting;

    /// <summary>
    /// 启动 HUD 请求事件，View 层订阅此事件执行窗口切换
    /// </summary>
    public event Action<GameConfig, AppSettings>? LaunchRequested;

    public LauncherViewModel(IConfigService configService, IGameRegistry gameRegistry)
    {
        _configService = configService;
        _gameRegistry = gameRegistry;
        _settings = new SettingsViewModel();
    }

    /// <summary>
    /// 初始化：加载配置并填充游戏列表
    /// </summary>
    public async Task InitializeAsync()
    {
        await _configService.LoadAsync();
        var settings = _configService.Settings;

        // 加载游戏列表
        _gameRegistry.LoadFromSettings(settings);
        foreach (var gameConfig in settings.Games.Where(g => g.IsEnabled))
        {
            var vm = new GameCardViewModel(gameConfig);
            Games.Add(vm);
        }

        // 恢复上次选择的游戏
        var lastGame = Games.FirstOrDefault(g => g.GameId == settings.LastSelectedGame);
        if (lastGame != null)
        {
            SelectedGame = lastGame;
        }
        else if (Games.Count > 0)
        {
            SelectedGame = Games[0];
        }

        // 加载设置
        Settings.LoadFromSettings(settings);
        StatusText = $"已加载 {Games.Count} 个游戏配置";
    }

    /// <summary>
    /// 选择游戏变更时自动更新端口号和状态文本
    /// </summary>
    partial void OnSelectedGameChanged(GameCardViewModel? value)
    {
        if (value != null)
        {
            _configService.Settings.LastSelectedGame = value.GameId;
            Settings.UdpPort = value.UdpPort;
            StatusText = $"已选择: {value.GameName}";
            DebouncedSave();
        }
    }

    /// <summary>
    /// 测试连接（支持 UDP 和共享内存）
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (SelectedGame == null) return;

        IsTesting = true;
        ConnectionStateText = "测试中...";

        try
        {
            if (SelectedGame.TransportType == TransportType.SharedMemory)
            {
                // 共享内存类型测试
                StatusText = "正在检测共享内存...";

                var game = _gameRegistry.GetGame(SelectedGame.GameId);
                if (game != null && game.GameId == "assetto-corsa")
                {
                    // AC 共享内存测试
                    using var acParser = new Core.Parsers.AcSharedMemoryParser();
                    try
                    {
                        acParser.Start();
                        await Task.Delay(1000); // 等待 1 秒读取数据

                        if (acParser.IsRunning)
                        {
                            StatusText = "AC 共享内存连接成功！游戏正在运行";
                            ConnectionStateText = "已连接";
                        }
                        else
                        {
                            StatusText = "AC 共享内存未找到，请确保游戏正在运行";
                            ConnectionStateText = "未找到";
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusText = $"AC 连接失败: {ex.Message}";
                        ConnectionStateText = "错误";
                    }
                    finally
                    {
                        acParser.Stop();
                    }
                }
                else if (game != null && game.GameId == "assetto-corsa-competizione")
                {
                    // ACC 共享内存测试
                    using var accParser = new Core.Parsers.AccSharedMemoryParser();
                    try
                    {
                        accParser.Start();
                        await Task.Delay(1000); // 等待 1 秒读取数据

                        if (accParser.IsRunning)
                        {
                            StatusText = "ACC 共享内存连接成功！游戏正在运行";
                            ConnectionStateText = "已连接";
                        }
                        else
                        {
                            StatusText = "ACC 共享内存未找到，请确保游戏正在运行";
                            ConnectionStateText = "未找到";
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusText = $"ACC 连接失败: {ex.Message}";
                        ConnectionStateText = "错误";
                    }
                    finally
                    {
                        accParser.Stop();
                    }
                }
                else if (game != null && game.GameId == "euro-truck-simulator-2")
                {
                    // ETS2 共享内存测试
                    using var ets2Parser = new Core.Parsers.Ets2SharedMemoryParser();
                    try
                    {
                        ets2Parser.Start();
                        await Task.Delay(1000); // 等待 1 秒读取数据

                        if (ets2Parser.IsRunning)
                        {
                            StatusText = "ETS2 共享内存连接成功！游戏正在运行";
                            ConnectionStateText = "已连接";
                        }
                        else
                        {
                            StatusText = "ETS2 共享内存未找到，请确保游戏正在运行且 SDK 插件已安装";
                            ConnectionStateText = "未找到";
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusText = $"ETS2 连接失败: {ex.Message}";
                        ConnectionStateText = "错误";
                    }
                    finally
                    {
                        ets2Parser.Stop();
                    }
                }
                else
                {
                    StatusText = "共享内存连接测试未实现";
                    ConnectionStateText = "未知";
                }
            }
            else
            {
                // UDP 类型测试
                StatusText = $"正在测试端口 {Settings.UdpPort}...";

                var parser = _gameRegistry.CreateParser(SelectedGame.GameId);
                using var testReceiver = new Data.UdpReceiver(Settings.UdpPort, parser);

                var tcs = new TaskCompletionSource<bool>();

                testReceiver.DataReceived += (s, data) =>
                {
                    tcs.TrySetResult(true);
                };
                testReceiver.ErrorOccurred += (s, ex) =>
                {
                    tcs.TrySetException(ex);
                };

                testReceiver.Start();

                var timeoutTask = Task.Delay(3000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                testReceiver.Stop();

                if (completedTask == timeoutTask)
                {
                    StatusText = $"端口 {Settings.UdpPort}: 3秒内未收到数据（游戏可能未运行）";
                    ConnectionStateText = "超时";
                }
                else
                {
                    StatusText = $"端口 {Settings.UdpPort}: 连接成功！已收到数据包";
                    ConnectionStateText = "已连接";
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"连接失败: {ex.Message}";
            ConnectionStateText = "错误";
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>
    /// 启动 HUD 悬浮窗
    /// </summary>
    [RelayCommand]
    private void LaunchHud()
    {
        if (SelectedGame == null) return;

        // 保存当前设置
        Settings.ApplyToSettings(_configService.Settings);
        var gameConfig = SelectedGame.ToConfig();
        gameConfig.UdpPort = Settings.UdpPort;

        // 更新游戏配置中的端口
        var existingGame = _configService.Settings.Games
            .FirstOrDefault(g => g.GameId == gameConfig.GameId);
        if (existingGame != null)
        {
            existingGame.UdpPort = Settings.UdpPort;
        }

        // 保存并触发启动事件
        _ = _configService.SaveAsync();
        LaunchRequested?.Invoke(gameConfig, _configService.Settings);
    }

    /// <summary>
    /// 防抖自动保存（500ms 延迟）
    /// </summary>
    private void DebouncedSave()
    {
        _debounceTimer?.Stop();
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += async (s, e) =>
        {
            _debounceTimer.Stop();
            Settings.ApplyToSettings(_configService.Settings);
            await _configService.SaveAsync();
        };
        _debounceTimer.Start();
    }
}
