using SerialDebugPanel.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace SerialDebugPanel.Widgets
{
    public class LedIndicatorWidget : BaseWidget
    {
        private readonly Border _borderContainer;
        private readonly Border _accentBar;
        private readonly UniformGrid _ledGrid;

        private readonly List<LedElement> _ledElements = new();
        private readonly int _ledCount; // تعداد LEDها به صورت داینامیک محاسبه می‌شود

        public override string Key => Config.variable ?? base.Key;

        public LedIndicatorWidget(WidgetConfig config) : base(config)
        {
            // ۱. تعیین تعداد داینامیک LEDها بر اساس بخش names در کانفیگ
            // اگر در کانفیگ "names" تعریف شده باشد، تعداد کلیدهای آن ملاک است؛ در غیر این صورت پیش‌فرض ۸ است.
            _ledCount = config.names != null && config.names.Count > 0
                ? config.names.Count
                : 8;

            // کانتینر اصلی عمودی
            var mainStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // ۲. هدر ظریف
            var labelText = new TextBlock
            {
                Text = config.label ?? "Status Indicators",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)), // Slate 600
                Margin = new Thickness(0, 0, 0, 4)
            };
            mainStack.Children.Add(labelText);

            // ۳. خط جداکننده افقی
            var horizontalSeparator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۴. گرید یکنواخت با تعداد ستون داینامیک
            _ledGrid = new UniformGrid
            {
                Rows = 1,
                Columns = _ledCount,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 36
            };

            // ساخت و مقداردهی اولیه LEDها بر اساس تعداد داینامیک جدید
            InitializeLeds(config);
            mainStack.Children.Add(_ledGrid);

            // ۵. نوار عمودی سمت چپ
            _accentBar = new Border
            {
                Width = 6,
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                CornerRadius = new CornerRadius(4, 0, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // گرید نهایی لایه‌ها
            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            mainStack.Margin = new Thickness(14, 0, 0, 0);

            _borderContainer = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 8, 10, 6),
                Margin = new Thickness(0, 0, 0, 8),
                Child = masterGrid
            };
        }

        private void InitializeLeds(WidgetConfig config)
        {
            _ledElements.Clear();
            _ledGrid.Children.Clear();
            _ledGrid.Columns = _ledCount;

            // استخراج کلیدهای بخش names برای نگاشت دقیق ایندکس‌ها
            // اگر از شیوه "1": "ttt" استفاده می‌کنید، کلیدها را به ترتیب عددی سورز می‌کنیم
            List<string> sortedKeys = new List<string>();
            if (config.names != null)
            {
                sortedKeys = config.names.Keys.OrderBy(k => int.TryParse(k, out int val) ? val : int.MaxValue).ToList();
            }

            for (int i = 0; i < _ledCount; i++)
            {
                string ledLabel = $"D{i}";

                // دریافت نام از بخش names بر اساس ساختار تعریف شده
                if (config.names != null)
                {
                    // تلاش برای خواندن بر اساس ایندکس رشته‌ای عددی (مثلاً "1") یا اولویت لیست مرتب شده
                    string keyToLook = i.ToString();
                    if (config.names.TryGetValue(keyToLook, out var customLabel))
                    {
                        ledLabel = customLabel;
                    }
                    else if (i < sortedKeys.Count && config.names.TryGetValue(sortedKeys[i], out var fallbackLabel))
                    {
                        ledLabel = fallbackLabel;
                    }
                }

                var ledContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var txtLabel = new TextBlock
                {
                    Text = ledLabel,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 3)
                };

                var glow = new DropShadowEffect
                {
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0,
                    Color = Color.FromRgb(34, 197, 94) // سبز
                };

                var ell = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200 خاموش
                    Stroke = new SolidColorBrush(Color.FromRgb(186, 200, 218)),
                    StrokeThickness = 1,
                    Effect = glow
                };

                ledContainer.Children.Add(txtLabel);
                ledContainer.Children.Add(ell);
                _ledGrid.Children.Add(ledContainer);

                _ledElements.Add(new LedElement
                {
                    Ellipse = ell,
                    LabelText = txtLabel,
                    GlowEffect = glow
                });
            }
        }

        public override FrameworkElement GetControl() => _borderContainer;

        public override void UpdateValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            bool[] states = new bool[_ledCount];

            try
            {
                // حالت ۱: ورودی به صورت لیست کاما سپریتور (مانند: 1,0)
                if (value.Contains(","))
                {
                    var parts = value.Split(',');
                    for (int i = 0; i < Math.Min(parts.Length, _ledCount); i++)
                    {
                        states[i] = parts[i].Trim() == "1" || parts[i].Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                }
                // حالت ۲: ورودی یک عدد دهدهی بیت‌ماسک (مانند: 3)
                else if (int.TryParse(value, out int bitmask))
                {
                    for (int i = 0; i < _ledCount; i++)
                    {
                        states[i] = (bitmask & (1 << i)) != 0;
                    }
                }
                // حالت ۳: ورودی یک رشته باینری بدون فاصله (مانند: 10)
                else
                {
                    var cleanVal = value.Trim();
                    for (int i = 0; i < Math.Min(cleanVal.Length, _ledCount); i++)
                    {
                        states[i] = cleanVal[i] == '1';
                    }
                }
            }
            catch
            {
                return;
            }

            // اعمال وضعیت‌ها روی المان‌های گرافیکی به صورت داینامیک بر اساس تعداد جدید
            bool anyActive = false;
            for (int i = 0; i < _ledElements.Count; i++)
            {
                var element = _ledElements[i];
                bool isActive = states[i];

                if (isActive)
                {
                    anyActive = true;
                    element.Ellipse.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // سبز نئونی زنده
                    element.Ellipse.Stroke = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                    element.LabelText.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74)); // متن سبز تیره خوانا
                    element.GlowEffect.Opacity = 0.8;
                }
                else
                {
                    element.Ellipse.Fill = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // Slate 200 خاموش
                    element.Ellipse.Stroke = new SolidColorBrush(Color.FromRgb(186, 200, 218));
                    element.LabelText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate 400
                    element.GlowEffect.Opacity = 0;
                }
            }

            if (anyActive)
            {
                _accentBar.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                _borderContainer.Background = new SolidColorBrush(Color.FromArgb(10, 34, 197, 94));
                _borderContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 34, 197, 94));
            }
            else
            {
                _accentBar.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                _borderContainer.Background = Brushes.White;
                _borderContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
            }
        }

        private class LedElement
        {
            public required Ellipse Ellipse { get; init; }
            public required TextBlock LabelText { get; init; }
            public required DropShadowEffect GlowEffect { get; init; }
        }
    }
}
