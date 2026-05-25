using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class GaugeWidget : BaseWidget
    {
        private readonly Border _borderContainer;
        private readonly Path _bgArcPath;
        private readonly Path _arcPath;
        private readonly TextBlock _valueText;
        private readonly double _minValue;
        private readonly double _maxValue;
        private readonly LinearGradientBrush _gradientBrush;

        // زوایای استاندارد برای یک گیج حلقه باز مدرن (۲۷۰ درجه)
        private const double StartAngle = 135;
        private const double EndAngle = 405;
        private const double TotalAngle = EndAngle - StartAngle;

        // ابعاد فشرده و بهینه‌سازی شده
        private const double CenterX = 60;
        private const double CenterY = 50;
        private const double Radius = 42;

        public override string Key => Config.variable ?? base.Key;

        public GaugeWidget(WidgetConfig config) : base(config)
        {
            _minValue = config.minY ?? 0;
            _maxValue = config.maxY ?? 100;

            var mainStack = new StackPanel();

            // ۱. هدر فوق‌العاده جمع و جور
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            var labelText = new TextBlock
            {
                Text = config.label,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                VerticalAlignment = VerticalAlignment.Center
            };
            headerGrid.Children.Add(labelText);
            mainStack.Children.Add(headerGrid);

            // جداکننده افقی بسیار نازک و ملایم
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(separator);

            // ۲. کانتینر فشرده گیج (کاهش ارتفاع به ۹۵ برای حذف فضاهای مرده)
            var gaugeContainer = new Grid
            {
                Width = 120,
                Height = 95,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // گرادیان رنگی متالیک
            _gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 1),
                EndPoint = new Point(1, 0)
            };
            _gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(59, 130, 246), 0.0));
            _gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(139, 92, 246), 1.0));

            _bgArcPath = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                StrokeThickness = 8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Fill = Brushes.Transparent
            };

            _arcPath = new Path
            {
                Stroke = _gradientBrush,
                StrokeThickness = 8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Fill = Brushes.Transparent,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(139, 92, 246),
                    BlurRadius = 6,
                    ShadowDepth = 0,
                    Opacity = 0.4
                }
            };

            gaugeContainer.Children.Add(_bgArcPath);
            gaugeContainer.Children.Add(_arcPath);

            // ۳. متن مرکز گیج (تراز شده دقیق با ابعاد جدید)
            var textContainer = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            _valueText = new TextBlock
            {
                Text = "--",
                FontSize = 20,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var unitText = new TextBlock
            {
                Text = config.unit ?? string.Empty,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0)
            };

            textContainer.Children.Add(_valueText);
            textContainer.Children.Add(unitText);
            gaugeContainer.Children.Add(textContainer);

            mainStack.Children.Add(gaugeContainer);

            // کادر کلی با پدینگ کمتر برای ذخیره فضا
            _borderContainer = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 6),
                Margin = new Thickness(0, 0, 0, 10),
                Child = mainStack
            };

            DrawBackgroundArc();

            double initialValue = config.defaultFloat ?? _minValue;
            UpdateArc(initialValue);
        }

        public override FrameworkElement GetControl() => _borderContainer;

        public override void UpdateValue(string value)
        {
            if (double.TryParse(value, out double val))
            {
                val = Math.Clamp(val, _minValue, _maxValue);
                _valueText.Text = $"{val:F1}";
                UpdateArc(val);
                ApplyDynamicColoring(val);
            }
        }

        private void DrawBackgroundArc()
        {
            Point startPoint = GetPointOnCircle(CenterX, CenterY, Radius, StartAngle);
            Point endPoint = GetPointOnCircle(CenterX, CenterY, Radius, EndAngle);

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = startPoint };
            var arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(Radius, Radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = true
            };

            pathFigure.Segments.Add(arcSegment);
            pathGeometry.Figures.Add(pathFigure);
            _bgArcPath.Data = pathGeometry;
        }

        private void UpdateArc(double value)
        {
            double percentage = (value - _minValue) / (_maxValue - _minValue);
            if (double.IsNaN(percentage) || double.IsInfinity(percentage)) percentage = 0;

            double currentAngle = StartAngle + (percentage * TotalAngle);

            Point startPoint = GetPointOnCircle(CenterX, CenterY, Radius, StartAngle);
            Point endPoint = GetPointOnCircle(CenterX, CenterY, Radius, currentAngle);

            if (percentage < 0.005)
            {
                _arcPath.Visibility = Visibility.Collapsed;
                return;
            }
            _arcPath.Visibility = Visibility.Visible;

            bool isLargeArc = (percentage * TotalAngle) > 180;

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = startPoint };
            var arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(Radius, Radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };

            pathFigure.Segments.Add(arcSegment);
            pathGeometry.Figures.Add(pathFigure);
            _arcPath.Data = pathGeometry;
        }

        private void ApplyDynamicColoring(double value)
        {
            Color primaryColor;
            Color secondaryColor;

            if (Config.criticalThreshold.HasValue && value >= Config.criticalThreshold.Value)
            {
                primaryColor = Color.FromRgb(239, 68, 68);
                secondaryColor = Color.FromRgb(185, 28, 28);
            }
            else if (Config.warningThreshold.HasValue && value >= Config.warningThreshold.Value)
            {
                primaryColor = Color.FromRgb(249, 115, 22);
                secondaryColor = Color.FromRgb(194, 65, 12);
            }
            else
            {
                primaryColor = Color.FromRgb(59, 130, 246);
                secondaryColor = Color.FromRgb(139, 92, 246);
            }

            _gradientBrush.GradientStops[0].Color = primaryColor;
            _gradientBrush.GradientStops[1].Color = secondaryColor;

            if (_arcPath.Effect is DropShadowEffect shadow)
            {
                shadow.Color = primaryColor;
            }
        }

        private Point GetPointOnCircle(double centerX, double centerY, double radius, double angleDegrees)
        {
            double angleRadians = angleDegrees * Math.PI / 180.0;
            return new Point(
                centerX + radius * Math.Cos(angleRadians),
                centerY + radius * Math.Sin(angleRadians)
            );
        }
    }
}
