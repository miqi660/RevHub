# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

多游戏实时遥测 HUD 覆盖层，基于 WPF (.NET 8.0) 构建。通过 UDP 或共享内存接收赛车游戏遥测数据，以 60 FPS 刷新率驱动六个可视化控件。

**支持的游戏**: Forza Horizon 5, Forza Horizon 6, Forza Motorsport, F1 2020, iRacing, Assetto Corsa, Assetto Corsa Competizione, Euro Truck Simulator 2

## 构建与运行

```bash
# 构建
dotnet build

# 运行（启动 LauncherWindow 游戏选择界面）
dotnet run

# 发布
dotnet publish -c Release
```

无 `.sln` 文件，直接操作 `.csproj`。依赖 .NET 8.0 BCL、WPF 框架和 CommunityToolkit.Mvvm 8.4.0。

## 架构

数据流：`游戏 (UDP/共享内存) → UdpReceiver/SharedMemoryReader → MainWindow (60FPS timer) → Controls`

### 双层架构（新旧并存）

项目存在两套并行的数据层：

**旧层 (`Data/`)** — 原始 WinForms 移植版本：
- `ForzaTelemetryData` — 324 字节 blittable struct，`[StructLayout(Sequential, Pack=1)]` 直接映射 Forza UDP 包格式
- `UdpReceiver` — 简化版接收器，使用旧接口 `ITelemetryParser`
- `ITelemetryParser` — 返回 `ForzaTelemetryData` 的旧接口

**新层 (`Core/`)** — 多游戏支持重构：
- `UdpReceiver` — 增强版接收器，注入 `Core.Interfaces.ITelemetryParser`，返回 `StandardTelemetryData`
- `ITelemetryParser` — 标准化接口，`TryParse` 输出 `StandardTelemetryData`
- `BasicUdpReceiver` / `SafeUdpReceiver` — 简化/安全版本
- `SharedMemoryReader` — 共享内存读取器（非 UDP 游戏）
- `Parsers/` — 多游戏解析器工厂（Forza、AC、ACC、ETS2）

**当前 MainWindow 使用旧层**，启动器 (`LauncherWindow`) 使用新层。

### 启动流程

```
App.OnStartup → LauncherWindow（游戏选择界面，MVVM）
                    ↓ 用户选择游戏
               MainWindow(gameConfig, settings)（HUD 覆盖层，Code-Behind）
```

### 控件层 (`Controls/`)

六个自定义 UserControl，通过依赖属性从 MainWindow 接收数据：

| 控件 | 渲染方式 |
|---|---|
| ChartControl | `OnRender` + `StreamGeometry` 绘制油门/刹车/离合曲线 |
| PedalsControl | XAML `ScaleTransform` 动画驱动的竖条仪表 |
| GearControl | TextBlock 显示档位 (R/N/1-16) |
| SpeedControl | TextBlock 显示速度 + "km/h" |
| RpmLedControl | `OnRender` 绘制 7 个圆形 LED，85%+ RPM 闪烁 |
| SteeringControl | `OnRender` 从 SVG 路径数据绘制矢量方向盘，旋转响应转向 |

## 关键设计决策

- **MVVM 混合模式** — 启动器使用 MVVM（CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]`），HUD 使用 Code-Behind 直接推送数据到子控件依赖属性（性能优先）
- **策略模式解析器** — `ITelemetryParser` 接口支持多游戏扩展，`GameRegistry` 通过字典工厂管理解析器创建
- **自定义渲染** — ChartControl、RpmLedControl、SteeringControl 使用 `OnRender` + `DrawingContext` 直接绘制，使用预冻结的 `SolidColorBrush` 避免每帧分配
- **线程安全** — `lock(_dataLock)` 保护 UDP 接收线程与 UI timer 之间的共享遥测状态
- **无边框透明覆盖层** — `WindowStyle="None"`, `AllowsTransparency="True"`, `Topmost="True"`
- **Viewbox 等比缩放** — `Scale = Window.Height / InnerGrid.LogicalHeight`，基于高度计算消除累积误差

## 添加新游戏支持

**重要**：添加新游戏前，必须先读取以下两个模板文件获取完整步骤和代码骨架：
- `Core/Example/AddNewSharedMemoryParser.cs` — 共享内存游戏模板（AC/ACC/ETS2 同类）
- `Core/Example/AddNewUdpParser.cs` — UDP 游戏模板（Forza 同类）

### 步骤

1. **确定传输方式**：UDP 还是共享内存
   - UDP：游戏通过网络发送固定格式二进制包
   -共享内存：游戏通过 MemoryMappedFile 输出数据

2. **创建解析器**（按模板文件中的指引）：
   - 共享内存：在 `Core/Parsers/` 创建，实现 `IDisposable`，通过 `DataUpdated` 事件推送 `BasicTelemetryData`
   - UDP：在 `Core/Parsers/` 创建，实现 `Data.Parsers.ITelemetryParser` 接口

3. **注册并集成**：
   - UDP 游戏：在 `GameRegistry._parserFactories` 字典中注册
   - 共享内存游戏：在 `MainWindow.xaml.cs` 构造函数中添加分支
   - 在 `LauncherViewModel.TestConnectionAsync()` 中添加测试分支

### 参考实现

- **共享内存游戏**：`Core/Parsers/AcSharedMemoryParser.cs`、`AccSharedMemoryParser.cs`、`Ets2SharedMemoryParser.cs`
- **UDP 游戏**：`Data/Parsers/ForzaParser.cs`

### SimHub 游戏配置参考

配置方案详情参考 `d:\SimHub\_Addons\GamePlugins\` 目录。

#### UDP 游戏配置

| 游戏 | 端口 | 配置位置 |
|---|---|---|
| Forza Horizon 4/5 | 21337 | 默认开启，无需配置 |
| Forza Horizon 6 | 27875 | 默认开启 |
| Forza Motorsport 7/8 | 21337 | 默认开启 |
| ACC | 9000 | `Documents\config\broadcasting.json` → `updListenerPort` |
| F1 2020 | 20777 | 游戏内设置: UDP Telemetry ON, Format 2020, 60Hz |
| F1 2022-2025 | 20777 | 游戏内设置: UDP Telemetry ON, 对应年份 Format, 60Hz |
| Codemasters (DiRT/Dirt3) | 20777 | `hardware_settings_config.xml` → `<udp>` |
| EA WRC 23 | 29888 | SimHub 专用，`wrc_simhub` 协议 |
| WRC Generations | 22888 | `Documents\My Games\WRCG\UserSetting.cfg` |
| RBR NGP | 6776 | `RichardBurnsRally.ini` → `udpTelemetryPort` |
| iRacing | 7890 | 默认开启 |
| Assetto Corsa | 9996 | 默认开启 |
| LFS (OutGauge) | 63392 | `LFS\cfg.txt` |
| DCS World | 10025 | Lua 脚本 `SHTelemetry.lua` |
| IL-2 Sturmovik | 24321 | `startup.cfg` |

#### 共享内存游戏配置

| 游戏 | 共享内存名/方式 | 配置文件 |
|---|---|---|
| Assetto Corsa | `acpmf_simhub_v2` | Python 插件 `simhub_shared_mem.py` |
| ACC | `accMap` | `broadcasting.json` |
| rFactor 1 | `rFactorSharedMemoryMap.dll` | 复制 DLL 到游戏目录 |
| rFactor 2 | `rFactor2SharedMemoryMapPlugin64.dll` | 复制到 `Bin64\Plugins\` |
| Automobilista | `SHAMSSharedMemory.dll` | 复制 DLL 到游戏目录 |
| Project Cars 2/3 | 共享内存模式 | 游戏内设置 |
| Automobilista 2 | 共享内存模式 | 游戏内设置 |
| ETS2/ATS | SDK 插件 | `ets2-sdk-plugin` 放入 bin 目录 |
| Elite Dangerous | 进程内存指针 | `Offsets\EliteDangerous64_Offsets.xml` |
| GTR2 | `.plr` 文件 | `Write Shared Memory="1"` |

#### 其他协议类型

| 游戏 | 协议 | 说明 |
|---|---|---|
| CS:GO | HTTP GSI | `localhost:3051`，Valve Game State Integration |
| Dota 2 | HTTP GSI | `localhost:3050` |
| Flight Simulator | 独立程序 | `SimHubFS.exe` 桥接 |
| OMSI 2 | DLL 插件 | `SimhubOmsi.dll` 复制到游戏 plugins 目录 |
| Farming Simulator | Mod 包 | `SHTelemetry.zip` 复制到 Mods 目录 |
| BeamNg | OutGauge | 游戏内 Hardware 设置 |

#### 端口快速参考

| 端口 | 游戏 |
|---|---|
| 21337 | Forza Horizon 4/5, Motorsport 7/8 |
| 27875 | Forza Horizon 6 |
| 20777 | F1 系列、Codemasters 系列 |
| 9000 | ACC |
| 9996 | Assetto Corsa |
| 7890 | iRacing |
| 29888 | EA WRC 23 |
| 22888 | WRC Generations, TDU SC |
| 6776 | RBR NGP |
| 10025 | DCS World |

**注意：** 部分游戏（如 Forza、F1、RBR）有 PDF 格式的配置指南在 SimHub 目录中，添加这些游戏时需参考对应 PDF。

---

## 同系列游戏解析器兼容性

**适配同系列游戏前，必须先确认数据格式是否相同。**

### 可共用解析器的系列

| 系列 | 可共用游戏 | 端口 | 解析器 |
|---|---|---|---|
| **Forza** | FH4, FH5, FM7, FM8 | 21337 | `ForzaParser` |
| **Forza** | FH6 | 27875 | `ForzaParser`（需修改端口） |

**Forza 系列说明**：
- FH4/FH5/FM7/FM8：端口 21337，324 字节，完全相同的 UDP 格式
- FH6：端口 27875，324 字节，基础字段相同，额外有 3 个字段（`CarGroup`, `SmashableVelDiff`, `SmashableMass`）
- 所有 Forza 游戏共用同一个 `ForzaParser`，仅端口不同

### 不能共用解析器的系列

| 系列 | 差异原因 | 解决方案 |
|---|---|---|
| **F1 系列** | 每年 UDP 格式版本不同（2022/2023/2024/2025） | 为每年创建独立解析器 |
| **WRC 系列** | 三种完全不同的协议（Dirt Rally 兼容/SimHub JSON/原生 UDP） | 为每个子系列创建独立解析器 |
| **AC vs ACC** | 共享内存名不同、速度单位不同（m/s vs km/h）、ACC 有额外字段 | 已有独立解析器 `AcSharedMemoryParser` 和 `AccSharedMemoryParser` |
| **rFactor 1 vs 2** | 不同的 DLL 插件、完全不同的共享内存结构 | 需为每个版本创建独立解析器 |

### 添加同系列新游戏的检查清单

1. **确认端口**：查看 SimHub 目录中的 `HowTo.txt` 或 `HowTo.pdf`
2. **对比数据包大小**：与现有解析器的 `ExpectedPacketSize` 对比
3. **对比字段布局**：检查偏移量和数据类型是否一致
4. **测试共用性**：先尝试用现有解析器解析新游戏数据，验证字段是否正确

## 注意事项

- **添加新游戏后必须删除配置文件**：每次添加新游戏配置后，需删除 `%AppData%/RevHub/settings.json` 再重新编译运行，否则新游戏不会出现在列表中（程序优先读取已有配置，不会自动合并新增的默认游戏）
- 修改遥测数据结构时，`ForzaTelemetryData` 的字段顺序和 `StructLayout` 必须与 Forza UDP 协议严格匹配，否则反序列化会错位
- `OnRender` 中的画笔对象必须调用 `Freeze()` 以避免跨线程访问异常和每帧 GC 压力
- 端口 21337 是 Forza 系列游戏的标准遥测端口，不要随意更改默认值
- 新增游戏支持时，在 `GameRegistry._parserFactories` 字典中注册解析器工厂即可
- 项目由 WinForms 版本移植而来，参考实现在 `c:\Users\Administrator\Desktop\ui资料\MainForm.cs`
