using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace RevHub.Converters
{
    /// <summary>
    /// 双精度浮点数范围验证转换器
    /// 用于方向盘圈数等需要小数输入的场景
    /// </summary>
    public class DoubleRangeValidationRule : ValidationRule
    {
        public double Min { get; set; } = 0.01;
        public double Max { get; set; } = 255.0;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return new ValidationResult(false, "值不能为空");
                }

                // 尝试解析为 double
                if (double.TryParse(text, NumberStyles.Any, cultureInfo, out double result))
                {
                    if (result < Min || result > Max)
                    {
                        return new ValidationResult(false, $"值必须在 {Min} 到 {Max} 之间");
                    }
                    return ValidationResult.ValidResult;
                }

                return new ValidationResult(false, "请输入有效的数字");
            }

            return new ValidationResult(false, "无效的输入");
        }
    }

    /// <summary>
    /// Double 到 String 的转换器，支持指定格式
    /// </summary>
    public class DoubleToStringConverter : IValueConverter
    {
        public string Format { get; set; } = "F2";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return d.ToString(Format, culture);
            }
            return "0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                if (double.TryParse(text, NumberStyles.Any, culture, out double result))
                {
                    return result;
                }
            }
            return 0.0;
        }
    }
}
