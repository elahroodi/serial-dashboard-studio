using System.Collections.Generic;

namespace SerialDebugPanel.Core
{
    public class DropdownOption
    {
        public string label { get; set; } = string.Empty;
        public string value { get; set; } = string.Empty;
    }

    public class ConfigRoot
    {
        public List<WidgetConfig> controls { get; set; } = new();
        public List<WidgetConfig> monitors { get; set; } = new();
    }

    public class WidgetConfig
    {
        public string type { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public string? command { get; set; }
        public string? variable { get; set; }
        public string? unit { get; set; }
        public bool? defaultBool { get; set; }
        public int? defaultInt { get; set; }
        public int? min { get; set; }
        public int? max { get; set; }
        public double? defaultFloat { get; set; }
        public double? minFloat { get; set; }
        public double? maxFloat { get; set; }
        public double? step { get; set; }
        public int? decimals { get; set; }
        public string? defaultText { get; set; }
        public List<DropdownOption>? options { get; set; }
        public string? defaultOption { get; set; }
        public string? defaultColor { get; set; }
        public string? value { get; set; }
        public string? buttonText { get; set; }
        public int? historySize { get; set; }
        public int? maxHistory { get; set; }
        public double? minY { get; set; }
        public double? maxY { get; set; }
        public double? warningThreshold { get; set; }
        public double? criticalThreshold { get; set; }
        public List<string>? columns { get; set; }
        public List<string>? variables { get; set; }

        // حفظ پروپرتی قدیمی برای بخش‌های دیگر پروژه
        public Dictionary<string, string>? alarms { get; set; }

        // پروپرتی جدید برای نام‌گذاری دلخواه LEDها در LedIndicatorWidget
        public Dictionary<string, string>? names { get; set; }
    }
}
