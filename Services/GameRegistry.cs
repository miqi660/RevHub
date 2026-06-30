using System;
using System.Collections.Generic;
using System.Linq;
using RevHub.Data.Parsers;
using RevHub.Models;

#nullable enable

namespace RevHub.Services;

/// <summary>
/// 游戏注册表实现
/// 使用字典工厂模式管理游戏配置和解析器创建
/// </summary>
public class GameRegistry : IGameRegistry
{
    private readonly List<GameConfig> _games = new();

    /// <summary>
    /// UDP 解析器工厂字典：ParserType 字符串 → 解析器实例工厂
    /// 共享内存游戏 (AC/ACC/ETS2) 在 MainWindow 中直接处理，不经过此工厂
    /// </summary>
    private readonly Dictionary<string, Func<ITelemetryParser>> _parserFactories = new()
    {
        ["RevHub.Data.Parsers.ForzaParser"] = () => new ForzaParser(),
        ["RevHub.Data.Parsers.F12020Parser"] = () => new F12020Parser(),
    };

    public IReadOnlyList<GameConfig> RegisteredGames => _games.AsReadOnly();

    public GameConfig? GetGame(string gameId)
    {
        return _games.FirstOrDefault(g => g.GameId == gameId);
    }

    public ITelemetryParser CreateParser(string gameId)
    {
        var game = GetGame(gameId)
            ?? throw new ArgumentException($"未找到游戏配置: {gameId}", nameof(gameId));

        if (_parserFactories.TryGetValue(game.ParserType, out var factory))
        {
            return factory();
        }

        throw new InvalidOperationException($"未注册的解析器类型: {game.ParserType}");
    }

    public void RegisterGame(GameConfig config)
    {
        var existing = _games.FirstOrDefault(g => g.GameId == config.GameId);
        if (existing != null)
        {
            _games.Remove(existing);
        }
        _games.Add(config);
    }

    /// <summary>
    /// 从 AppSettings 加载游戏列表
    /// </summary>
    public void LoadFromSettings(AppSettings settings)
    {
        _games.Clear();
        foreach (var game in settings.Games)
        {
            _games.Add(game);
        }
    }
}
