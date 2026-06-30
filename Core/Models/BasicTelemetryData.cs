namespace RevHub.Core.Models
{
    /// <summary>
    /// 精简遥测数据模型
    /// 只包含仪表盘核心显示所需的 8 个数据点
    /// </summary>
    public class BasicTelemetryData
    {
        // ═══════════════════════════════════════════════════════════
        // 踏板开度 (0-100%)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 油门开度 (0-100%)
        /// </summary>
        public float Throttle { get; set; }

        /// <summary>
        /// 刹车开度 (0-100%)
        /// </summary>
        public float Brake { get; set; }

        /// <summary>
        /// 离合器开度 (0-100%)
        /// </summary>
        public float Clutch { get; set; }

        /// <summary>
        /// 手刹开度 (0-100%)
        /// </summary>
        public float Handbrake { get; set; }

        // ═══════════════════════════════════════════════════════════
        // 发动机与传动
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 当前发动机转速 (RPM)
        /// </summary>
        public float Rpm { get; set; }

        /// <summary>
        /// 发动机最大转速 (RPM)
        /// </summary>
        public float MaxRpm { get; set; }

        /// <summary>
        /// 当前档位
        /// </summary>
        public int Gear { get; set; }

        /// <summary>
        /// 档位显示文本 (R, N, 1, 2, ...)
        /// </summary>
        public string GearDisplay { get; set; } = "N";

        // ═══════════════════════════════════════════════════════════
        // 速度
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 车辆速度 (米/秒)
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// 速度 (km/h)
        /// </summary>
        public float SpeedKmh => Speed * 3.6f;

        // ═══════════════════════════════════════════════════════════
        // 转向
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 方向盘角度 (-100 到 100)
        /// 负值 = 左转，正值 = 右转
        /// </summary>
        public float Steer { get; set; }
    }
}
