using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class GraphWidget : IWidget
    {
        private readonly WidgetConfig _config;
        private readonly Border _borderContainer;
        private readonly Canvas _canvas;
        private readonly Polyline _polyline;
        private readonly Queue<double> _points = new();
        private readonly int _maxPoints;
        private readonly TextBlock _realtimeValueText;
        private readonly TextBlock _maxText;
        private readonly TextBlock _minText;

        // متغیرهای وضعیت لحظه‌ای نمودار
        private bool _isPaused = false;
        private bool _autoScale = false;
        private double _currentMinY;
        private double _currentMaxY;

        public string Key => _config.variable ?? string.Empty;
        public event EventHandler<string>? ValueChanged;

        public GraphWidget(WidgetConfig config)
        {
            _config = config;
            _maxPoints = _config.historySize ?? 100;
            _currentMinY = _config.minY ?? 0;
            _currentMaxY = _config.maxY ?? 100;

            var mainStack = new StackPanel();

            // ۱. هدر پیشرفته و منعطف ویجت
            var headerDock = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 0, 0, 6)
            };

            // بخش چپ: عنوان و مقدار زنده در یک پنل افقی
            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var titleLabel = new TextBlock
            {
                Text = _config.label,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStack.Children.Add(titleLabel);

            _realtimeValueText = new TextBlock
            {
                Text = $"-- {_config.unit ?? string.Empty}".Trim(),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStack.Children.Add(_realtimeValueText);

            DockPanel.SetDock(titleStack, Dock.Left);
            headerDock.Children.Add(titleStack);

            // بخش راست: دکمه‌های کنترلی (Autoscale, Pause, Clear)
            var controlsStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // دکمه Autoscale
            var autoscaleButton = new ToggleButton
            {
                Content = "Auto Y",
                Height = 22,
                Width = 50,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            StyleToggleButton(autoscaleButton, Color.FromRgb(59, 130, 246), Color.FromRgb(37, 99, 235)); // تم آبی متالیک
            autoscaleButton.Checked += (s, e) => { _autoScale = true; Redraw(); };
            autoscaleButton.Unchecked += (s, e) => { _autoScale = false; ResetYAxisLimits(); Redraw(); };
            controlsStack.Children.Add(autoscaleButton);

            // دکمه Pause
            var pauseButton = new ToggleButton
            {
                Content = "Pause",
                Height = 22,
                Width = 48,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            StyleToggleButton(pauseButton, Color.FromRgb(249, 115, 22), Color.FromRgb(234, 88, 12)); // تم نارنجی
            pauseButton.Checked += (s, e) => { _isPaused = true; pauseButton.Content = "Resume"; };
            pauseButton.Unchecked += (s, e) => { _isPaused = false; pauseButton.Content = "Pause"; };
            controlsStack.Children.Add(pauseButton);

            // دکمه Clear
            var clearButton = new Button
            {
                Content = "Clear",
                Height = 22,
                Width = 48,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            StyleStandardButton(clearButton, Color.FromRgb(239, 68, 68), Color.FromRgb(220, 38, 38)); // تم قرمز
            clearButton.Click += (s, e) => { _points.Clear(); Redraw(); };
            controlsStack.Children.Add(clearButton);

            DockPanel.SetDock(controlsStack, Dock.Right);
            headerDock.Children.Add(controlsStack);

            mainStack.Children.Add(headerDock);

            // ۲. خط جداکننده افقی
            var horizontalSeparator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۳. کانتینر لایه‌بندی شده نمودار
            var graphGrid = new Grid { Height = 140, HorizontalAlignment = HorizontalAlignment.Stretch };

            var canvasBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true
            };

            _canvas = new Canvas { ClipToBounds = true };

            _polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                StrokeThickness = 2.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            _canvas.Children.Add(_polyline);
            canvasBorder.Child = _canvas;
            graphGrid.Children.Add(canvasBorder);

            // ۴. لایبل‌های کمینه و بیشینه داینامیک گوشه چپ
            var labelLayer = new Grid { IsHitTestVisible = false };

            _maxText = new TextBlock
            {
                Text = $"{_currentMaxY} {_config.unit ?? string.Empty}".Trim(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(8, 6, 0, 0)
            };

            _minText = new TextBlock
            {
                Text = $"{_currentMinY} {_config.unit ?? string.Empty}".Trim(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(8, 0, 0, 6)
            };

            labelLayer.Children.Add(_maxText);
            labelLayer.Children.Add(_minText);
            graphGrid.Children.Add(labelLayer);

            mainStack.Children.Add(graphGrid);

            // ۵. کادر دور تا دور کل ویجت
            _borderContainer = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12),
                Child = mainStack
            };

            _canvas.SizeChanged += (s, e) => Redraw();
        }

        public FrameworkElement GetControl() => _borderContainer;

        public void UpdateValue(string value)
        {
            if (_isPaused) return;

            if (double.TryParse(value, out double val))
            {
                _points.Enqueue(val);
                while (_points.Count > _maxPoints)
                {
                    _points.Dequeue();
                }

                _realtimeValueText.Text = $"{val:F1} {_config.unit ?? string.Empty}".Trim();
                Redraw();
            }
        }

        private void ResetYAxisLimits()
        {
            _currentMinY = _config.minY ?? 0;
            _currentMaxY = _config.maxY ?? 100;
        }

        private void Redraw()
        {
            if (_canvas.ActualWidth == 0 || _canvas.ActualHeight == 0)
                return;

            // ۱. پاک کردن حتمی نقاط قدیمی خط برای اعمال Clear
            _polyline.Points.Clear();
            _canvas.Children.Clear();

            // ۲. در صورت فعال بودن Autoscale، دامنه زنده را استخراج می‌کنیم
            if (_autoScale && _points.Count > 0)
            {
                double min = _points.Min();
                double max = _points.Max();

                // اضافه کردن کمی حاشیه امنیت برای زیبایی نمایش
                double padding = (max - min) * 0.1;
                if (padding == 0) padding = 1.0; // پیشگیری از حالت خط صاف مطلق

                _currentMinY = min - padding;
                _currentMaxY = max + padding;
            }

            // به‌روزرسانی لایبل‌های متنی بالا و پایین نمودار
            _maxText.Text = $"{_currentMaxY:F1} {_config.unit ?? string.Empty}".Trim();
            _minText.Text = $"{_currentMinY:F1} {_config.unit ?? string.Empty}".Trim();

            // ۳. ترسیم خطوط پس‌زمینه (Grid Lines)
            DrawGridLines();

            // ۴. ترسیم خطوط حد مجاز هشدار و بحرانی (Thresholds)
            DrawThresholdLines();

            _canvas.Children.Add(_polyline);

            // ۵. اگر داده کافی برای رسم وجود ندارد، از متد خارج شو (بعد از پاکسازی بالا)
            if (_points.Count < 2) return;

            double stepX = _canvas.ActualWidth / (_maxPoints - 1);
            var list = _points.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                double normalizedY = (list[i] - _currentMinY) / (_currentMaxY - _currentMinY);
                normalizedY = Math.Clamp(normalizedY, 0, 1);

                double x = i * stepX;
                double y = _canvas.ActualHeight - (normalizedY * _canvas.ActualHeight);

                _polyline.Points.Add(new Point(x, y));
            }
        }


        private void DrawGridLines()
        {
            var gridBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            double w = _canvas.ActualWidth;
            double h = _canvas.ActualHeight;

            for (int i = 1; i <= 3; i++)
            {
                double y = (h / 4) * i;
                var line = new Line { X1 = 0, Y1 = y, X2 = w, Y2 = y, Stroke = gridBrush, StrokeThickness = 1 };
                _canvas.Children.Add(line);
            }

            for (int i = 1; i <= 5; i++)
            {
                double x = (w / 6) * i;
                var line = new Line { X1 = x, Y1 = 0, X2 = x, Y2 = h, Stroke = gridBrush, StrokeThickness = 1 };
                _canvas.Children.Add(line);
            }
        }

        // سیستم داینامیک ترسیم خط‌چین‌های هشدار و بحرانی
        private void DrawThresholdLines()
        {
            double h = _canvas.ActualHeight;
            double w = _canvas.ActualWidth;

            // ۱. خط آستانه هشدار (Warning) - نارنجی ملایم
            if (_config.warningThreshold.HasValue)
            {
                double warningVal = _config.warningThreshold.Value;
                if (warningVal >= _currentMinY && warningVal <= _currentMaxY)
                {
                    double normY = (warningVal - _currentMinY) / (_currentMaxY - _currentMinY);
                    double y = h - (normY * h);
                    DrawDashedLine(y, Color.FromRgb(249, 115, 22)); // Orange 500
                }
            }

            // ۲. خط آستانه بحرانی (Critical) - قرمز ملایم
            if (_config.criticalThreshold.HasValue)
            {
                double criticalVal = _config.criticalThreshold.Value;
                if (criticalVal >= _currentMinY && criticalVal <= _currentMaxY)
                {
                    double normY = (criticalVal - _currentMinY) / (_currentMaxY - _currentMinY);
                    double y = h - (normY * h);
                    DrawDashedLine(y, Color.FromRgb(239, 68, 68)); // Red 500
                }
            }
        }

        private void DrawDashedLine(double y, Color color)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = _canvas.ActualWidth,
                Y2 = y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1.2,
                StrokeDashArray = new DoubleCollection { 4, 3 } // الگو خط‌چین شیک
            };
            _canvas.Children.Add(line);
        }

        // متدهای اختصاصی استایل‌دهی دکمه‌ها
        private void StyleStandardButton(Button button, Color hoverBg, Color hoverBorder)
        {
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));

            borderFactory.Name = "BtnBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(226, 232, 240)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            var triggerHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverBg), "BtnBorder"));
            triggerHover.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(hoverBorder), "BtnBorder"));
            triggerHover.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));

            template.Triggers.Add(triggerHover);
            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            style.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(100, 116, 139))));

            button.Style = style;
        }

        private void StyleToggleButton(ToggleButton button, Color activeBg, Color activeBorder)
        {
            var style = new Style(typeof(ToggleButton));
            var template = new ControlTemplate(typeof(ToggleButton));
            var borderFactory = new FrameworkElementFactory(typeof(Border));

            borderFactory.Name = "BtnBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(226, 232, 240)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            var triggerChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            triggerChecked.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(activeBg), "BtnBorder"));
            triggerChecked.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(activeBorder), "BtnBorder"));
            triggerChecked.Setters.Add(new Setter(ToggleButton.ForegroundProperty, Brushes.White));

            template.Triggers.Add(triggerChecked);
            style.Setters.Add(new Setter(ToggleButton.TemplateProperty, template));
            style.Setters.Add(new Setter(ToggleButton.ForegroundProperty, new SolidColorBrush(Color.FromRgb(100, 116, 139))));

            button.Style = style;
        }
    }
}
