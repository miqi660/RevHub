using System.Threading.Tasks;
using RevHub.Models;

namespace RevHub.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// 当前应用设置
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    /// 从持久化存储加载配置
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// 将当前配置保存到持久化存储
    /// </summary>
    Task SaveAsync();
}
