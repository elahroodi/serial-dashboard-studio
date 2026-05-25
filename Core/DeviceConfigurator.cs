using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeviceConfigurator.Models
{
    public class RootConfig
    {
        [JsonPropertyName("controls")]
        public List<ControlItem> Controls { get; set; } = new List<ControlItem>();

        [JsonPropertyName("monitors")]
        public List<MonitorItem> Monitors { get; set; } = new List<MonitorItem>();
    }

    public class ControlItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("default")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Default { get; set; }

        [JsonPropertyName("min")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Min { get; set; }

        [JsonPropertyName("max")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Max { get; set; }

        [JsonPropertyName("defaultInt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DefaultInt { get; set; }

        [JsonPropertyName("unit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Unit { get; set; }

        [JsonPropertyName("buttonText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ButtonText { get; set; }

        [JsonPropertyName("defaultText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string DefaultText { get; set; }

        [JsonPropertyName("options")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<DropdownOption> Options { get; set; }

        [JsonPropertyName("defaultOption")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string DefaultOption { get; set; }

        [JsonPropertyName("minFloat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? MinFloat { get; set; }

        [JsonPropertyName("maxFloat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? MaxFloat { get; set; }

        [JsonPropertyName("defaultFloat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? DefaultFloat { get; set; }

        [JsonPropertyName("step")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Step { get; set; }

        [JsonPropertyName("decimals")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Decimals { get; set; }

        [JsonPropertyName("defaultColor")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string DefaultColor { get; set; }

        [JsonPropertyName("variable")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Variable { get; set; }
    }

    public class DropdownOption
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class MonitorItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("variable")]
        public string Variable { get; set; }

        [JsonPropertyName("unit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Unit { get; set; }

        [JsonPropertyName("warningThreshold")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? WarningThreshold { get; set; }

        [JsonPropertyName("criticalThreshold")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? CriticalThreshold { get; set; }

        [JsonPropertyName("columns")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string> Columns { get; set; }

        // تغییر نام فیلد بر اساس نیاز جدید برای مپ شدن صحیح نام ال ای دی ها و آلارم ها
        [JsonPropertyName("names")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string> Names { get; set; }

        [JsonPropertyName("alarms")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string> Alarms { get; set; }
    }
}
