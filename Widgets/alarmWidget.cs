using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class AlarmWidget : BaseWidget
    {
        private readonly Border _borderContainer;
        private readonly Ellipse _led;
        private readonly TextBlock _statusText;
        private readonly TextBlock _titleText;
        private readonly DropShadowEffect _glowEffect;
        private readonly Border _accentBar;

        private List<string> _activeAlarmIds = new List<string>();
        private readonly Dictionary<string, string> _alarmDefinitions;

        // رویداد برای مطلع کردن دیالوگ از تغییرات لحظه‌ای
        public event Action<List<string>>? AlarmsUpdated;

        public override string Key => Config.variable ?? base.Key;

        public AlarmWidget(WidgetConfig config) : base(config)
        {
            _alarmDefinitions = config.alarms ?? new Dictionary<string, string>();

            var mainStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };

            _titleText = new TextBlock
            {
                Text = config.label ?? "Alarms Monitor",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            mainStack.Children.Add(_titleText);

            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(separator);

            var contentGrid = new Grid { Height = 36 };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var ledPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _glowEffect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 0, Opacity = 0, Color = Color.FromRgb(239, 68, 68) };

            _led = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                Stroke = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
                StrokeThickness = 1.5,
                Effect = _glowEffect
            };

            _statusText = new TextBlock
            {
                Text = "ALL SYSTEMS NOMINAL",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            ledPanel.Children.Add(_led);
            ledPanel.Children.Add(_statusText);
            contentGrid.Children.Add(ledPanel);

            var actionText = new TextBlock
            {
                Text = "DETAILS ➔",
                FontSize = 10,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0),
                Cursor = Cursors.Hand
            };
            Grid.SetColumn(actionText, 1);
            contentGrid.Children.Add(actionText);

            mainStack.Children.Add(contentGrid);

            _accentBar = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);
            mainStack.Margin = new Thickness(16, 0, 0, 0);

            _borderContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(15, 34, 197, 94)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 34, 197, 94)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 8),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid,
                Cursor = Cursors.Hand
            };

            _borderContainer.MouseDown += BorderContainer_MouseDown;
        }

        private void BorderContainer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // ارسال رفرنس خود ویجت به دیالوگ برای سابسکرایب شدن به تغییرات
                var dialog = new AlarmDetailsDialog(this, _activeAlarmIds, _alarmDefinitions);
                dialog.Owner = Window.GetWindow(_borderContainer);
                dialog.ShowDialog();
            }
        }

        public override FrameworkElement GetControl() => _borderContainer;

        public override void UpdateValue(string value)
        {
            _activeAlarmIds = value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(p => p.Trim())
                                  .Where(id => id != "0")
                                  .ToList();

            if (_activeAlarmIds.Count > 0)
            {
                _led.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                _led.Stroke = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                _statusText.Text = $"{_activeAlarmIds.Count} ACTIVE ALARMS (CLICK TO VIEW)";
                _statusText.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                _accentBar.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                _borderContainer.Background = new SolidColorBrush(Color.FromArgb(15, 239, 68, 68));
                _borderContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 239, 68, 68));
                _glowEffect.Opacity = 0.8;
            }
            else
            {
                _led.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                _led.Stroke = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                _statusText.Text = "ALL SYSTEMS NOMINAL";
                _statusText.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                _accentBar.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                _borderContainer.Background = new SolidColorBrush(Color.FromArgb(15, 34, 197, 94));
                _borderContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 34, 197, 94));
                _glowEffect.Opacity = 0;
            }

            // فایر کردن رویداد برای تغییر آنلاین دیالوگ باز شده
            AlarmsUpdated?.Invoke(_activeAlarmIds);
        }
    }
}
