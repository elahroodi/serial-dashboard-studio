using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SerialDebugPanel.Widgets;

namespace SerialDebugPanel.Core
{
    public static class WidgetFactory
    {
        // ۱. اورلود بدون پارامتر برای سازگاری با کدهای قدیمی
        public static (List<IWidget> controls, List<IWidget> monitors) CreateWidgets()
        {
            // مسیر پیش‌فرض: فایل widgets.json در ریشه برنامه
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "widgets.json");

            if (!File.Exists(defaultPath))
            {
                // جستجو در پوشه جدید Configs به جای Config/Profiles
                string configsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");
                if (Directory.Exists(configsFolder))
                {
                    var files = Directory.GetFiles(configsFolder, "*.json");
                    if (files.Length > 0)
                    {
                        defaultPath = files[0]; // اولین فایل کانفیگ پیدا شده را به عنوان پیش‌فرض در نظر می‌گیرد
                    }
                }
            }

            return CreateWidgets(defaultPath);
        }

        // ۲. متد اصلی با دریافت مسیر فایل به صورت داینامیک
        public static (List<IWidget> controls, List<IWidget> monitors) CreateWidgets(string configPath)
        {
            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Configuration file not found: {configPath}");

            string json = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<ConfigRoot>(json);

            if (config == null)
                throw new Exception("Failed to deserialize configuration file.");

            var controls = new List<IWidget>();
            var monitors = new List<IWidget>();

            if (config.controls != null)
            {
                foreach (var item in config.controls)
                {
                    var widget = CreateControlWidget(item);
                    if (widget != null)
                        controls.Add(widget);
                }
            }

            if (config.monitors != null)
            {
                foreach (var item in config.monitors)
                {
                    var widget = CreateMonitorWidget(item);
                    if (widget != null)
                        monitors.Add(widget);
                }
            }

            return (controls, monitors);
        }

        private static IWidget CreateControlWidget(WidgetConfig config)
        {
            string type = config.type?.Trim().ToLowerInvariant();

            return type switch
            {
                "toggle" => new BoolWidget(config),
                "slider" => new SliderWidget(config),
                "button" => new ButtonWidget(config),
                "input" => new TextWidget(config),
                "select" => new DropdownWidget(config),
                "number" => new NumericWidget(config),
                "color" => new ColorWidget(config),
                "sync" => new BidirectionalWidget(config),
                _ => throw new NotSupportedException($"Unknown control widget type: {config.type}")
            };
        }

        private static IWidget CreateMonitorWidget(WidgetConfig config)
        {
            string type = config.type?.Trim().ToLowerInvariant();

            return type switch
            {
                "text" => new TextMonitorWidget(config),
                "led" => new LedIndicatorWidget(config),
                "gauge" => new GaugeWidget(config),
                "chart" => new GraphWidget(config),
                "log" => new LogWidget(config),
                "table" => new TableWidget(config),
                "alarm" => new AlarmWidget(config),
                _ => throw new NotSupportedException($"Unknown monitor widget type: {config.type}")
            };
        }
    }
}
