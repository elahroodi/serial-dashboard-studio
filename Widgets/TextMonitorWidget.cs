using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class TextMonitorWidget : BaseWidget
    {
        private readonly Border _borderContainer;
        private readonly Border _accentBar;
        private readonly TextBlock _valueText;
        private readonly TextBlock _unitText;
        private readonly DispatcherTimer _copyFeedbackTimer;

        public override string Key => Config.variable ?? base.Key;

        public TextMonitorWidget(WidgetConfig config) : base(config)
        {
            // تایمر برای بازگرداندن متن بعد از کپی شدن در کلیپ‌بورد
            _copyFeedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _copyFeedbackTimer.Tick += (s, e) => {
                _copyFeedbackTimer.Stop();
                _valueText.Opacity = 1.0;
                _valueText.ToolTip = "Double-click to copy";
            };

            // کانتینر اصلی عمودی
            var mainStack = new StackPanel();

            // ۱. هدر شیک و جمع‌وجور
            var labelText = new TextBlock
            {
                Text = config.label,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            mainStack.Children.Add(labelText);

            // ۲. خط جداکننده افقی ظریف
            var horizontalSeparator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۳. گرید اصلی سه ستونه برای نمایش داده‌ها
            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // الف) بلاک متنی نمایش مقدار با قابلیت Trimming و کپی با دابل کلیک
            _valueText = new TextBlock
            {
                Text = "--",
                FontSize = 22,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Cursor = Cursors.Hand,
                ToolTip = "Double-click to copy",
                Margin = new Thickness(4, 0, 8, 0)
            };
            _valueText.MouseLeftButtonDown += OnValueDoubleClicked;

            Grid.SetColumn(_valueText, 0);
            contentGrid.Children.Add(_valueText);

            // ب) خط جداکننده عمودی فشرده
            var verticalSeparator = new Border
            {
                Width = 1,
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(4, 2, 8, 2)
            };
            Grid.SetColumn(verticalSeparator, 1);
            contentGrid.Children.Add(verticalSeparator);

            // ج) بلاک متنی واحد با رنگ مشکی تیره شیک
            _unitText = new TextBlock
            {
                Text = config.unit ?? string.Empty,
                FontSize = 12,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900 (مشکی تیره)
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
            Grid.SetColumn(_unitText, 2);
            contentGrid.Children.Add(_unitText);

            mainStack.Children.Add(contentGrid);

            // ۴. نوار عمودی ضخیم در سمت چپ (عرض ۸ پیکسل برای وضوح فوق‌العاده در هشدارها)
            _accentBar = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // رنگ پیشفرض خنثی
                CornerRadius = new CornerRadius(6, 0, 0, 6), // کاملا هماهنگ با گوشه بیرونی کادر اصلی
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // گرید نهایی برای ترکیب لایه‌ها
            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            // تنظیم فاصله داخلی متن‌ها از نوار سمت چپ
            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // کادر دور تا دور کل ویجت
            _borderContainer = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 8),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid
            };

            // لود پیشفرض در صورت وجود داده اولیه
            if (config.defaultFloat.HasValue)
            {
                UpdateValue(config.defaultFloat.Value.ToString("F1"));
            }
        }

        public override FrameworkElement GetControl() => _borderContainer;

        public override void UpdateValue(string value)
        {
            _valueText.Text = value;

            if (double.TryParse(value, out double numVal))
            {
                ApplyDynamicColoring(numVal);
            }
            else
            {
                ResetColoring();
            }
        }

        private void OnValueDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && _valueText.Text != "--")
            {
                Clipboard.SetText(_valueText.Text);

                _valueText.Opacity = 0.3;
                _valueText.ToolTip = "Copied!";

                _copyFeedbackTimer.Stop();
                _copyFeedbackTimer.Start();
            }
        }

        private void ApplyDynamicColoring(double value)
        {
            if (Config.criticalThreshold.HasValue && value >= Config.criticalThreshold.Value)
            {
                var redAccent = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // قرمز پررنگ
                var redBg = new SolidColorBrush(Color.FromArgb(20, 239, 68, 68)); // قرمز بسیار ملایم (شفافیت ~8%)

                _valueText.Foreground = redAccent;
                _accentBar.Background = redAccent;
                _borderContainer.Background = redBg;
                _borderContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 239, 68, 68));
            }
            else if (Config.warningThreshold.HasValue && value >= Config.warningThreshold.Value)
            {
                var orangeAccent = new SolidColorBrush(Color.FromRgb(249, 115, 22)); // نارنجی
                var orangeBg = new SolidColorBrush(Color.FromArgb(20, 249, 115, 22)); // نارنجی بسیار ملایم

                _valueText.Foreground = orangeAccent;
                _accentBar.Background = orangeAccent;
                _borderContainer.Background = orangeBg;
                _borderContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 249, 115, 22));
            }
            else
            {
                // وضعیت نرمال (سبز خوش‌رنگ با هاله پس‌زمینه سبز بسیار ملایم و شیک)
                var greenAccent = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                var greenBg = new SolidColorBrush(Color.FromArgb(15, 34, 197, 94)); // سبز فوق‌العاده ملایم (شفافیت ~6%)

                _valueText.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Slate 900
                _accentBar.Background = greenAccent;
                _borderContainer.Background = greenBg;
                _borderContainer.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 34, 197, 94));
            }
        }

        private void ResetColoring()
        {
            // بازگشت به حالت خنثی (بدون هاله رنگی فعال)
            _valueText.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            _accentBar.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
            _borderContainer.Background = Brushes.White;
            _borderContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
        }


    
    }
}
