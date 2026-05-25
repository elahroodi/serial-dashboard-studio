using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public abstract class BaseWidget : IWidget
    {
        protected readonly WidgetConfig Config;
        protected readonly StackPanel Container;
        protected readonly TextBlock TitleBlock;
        public virtual string Key => Config.variable ?? Config.command ?? string.Empty;

        public event EventHandler<string> ValueChanged;

        protected BaseWidget(WidgetConfig config)
        {
            Config = config;

            Container = new StackPanel
            {
                Margin = new Thickness(8),
                Orientation = Orientation.Vertical
            };

            TitleBlock = new TextBlock
            {
                Text = config.label ?? config.command ?? config.variable ?? "Widget",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6),
                Foreground = Brushes.White
            };

            Container.Children.Add(TitleBlock);
        }

        //public virtual UIElement GetControl() => Container;
        public virtual FrameworkElement GetControl() => Container;

        public abstract void UpdateValue(string value);

        protected void RaiseValueChanged(string value)
        {
            ValueChanged?.Invoke(this, value);
        }

        protected string BuildMessage(string value)
        {
            return $"{Config.command}={value}";
        }
    }
}
