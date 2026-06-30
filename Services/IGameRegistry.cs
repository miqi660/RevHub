using System.Collections.Generic;
using RevHub.Data.Parsers;
using RevHub.Models;

#nullable enable

namespace RevHub.Services;

/// <summary>
/// 游戏注册表接口
/// 管理可用的游戏配置和对应的解析器
/// </summary>
public interface IGameRegistry
{
    /// <summary>
    /// 已注册的游戏列表
    /// </summary>
    IReadOnlyList<GameConfig> RegisteredGames { get; }

    /// <summary>
    /// 根据游戏 ID 获取配置
    /// </summary>
    GameConfig? GetGame(string gameId);

    /// <summary>
    /// 为指定游戏创建遥测数据解析器
    /// </summary>
    ITelemetryParser CreateParser(string gameId);

    /// <summary>
    /// 注册新游戏配置
    /// </summary>
    void RegisterGame(GameConfig config);

    /// <summary>
    /// 从 AppSettings 加载游戏列表
    /// </summary>
    void LoadFromSettings(AppSettings settings);
}
