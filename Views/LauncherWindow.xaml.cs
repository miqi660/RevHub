using System.Windows;
using System.Windows.Input;
using RevHub.Models;
using RevHub.Services;
using RevHub.ViewModels;

namespace RevHub.Views;

/// <summary>
/// 启动界面主窗口
/// 极简 Code-Behind：仅负责 DI、窗口事件和窗口切换
/// </summary>
public partial class LauncherWindow : Window
{
    private readonly LauncherViewModel _viewModel;

    public LauncherWindow()
    {
        InitializeComponent();

        // 手动依赖注入（项目规模不需要 DI 容器）
        var configService = new JsonConfigService();
        var gameRegistry = new GameRegistry();
        _viewModel = new LauncherViewModel(configService, gameRegistry);

        DataContext = _viewModel;
        Loaded += OnWindowLoaded;
        _viewModel.LaunchRequested += OnLaunchRequested;
    }

    /// <summary>
    /// 窗口加载完成后初始化 ViewModel
    /// </summary>
    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    /// <summary>
    /// 启动 HUD 请求：隐藏启动器 → 显示 HUD → HUD 关闭时恢复启动器
    /// </summary>
    private void OnLaunchRequested(GameConfig game, AppSettings settings)
    {
        var hudWindow = new MainWindow(game, settings);
        hudWindow.Show();
        this.Hide();

        // HUD 关闭后重新显示启动器
        hudWindow.Closed += (s, args) =>
        {
            this.Show();
            this.Activate();
        };
    }

    #region 窗口交互

    /// <summary>
    /// 标题栏拖拽移动
    /// </summary>
    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    /// <summary>
    /// 最小化窗口
    /// </summary>
    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion
}
