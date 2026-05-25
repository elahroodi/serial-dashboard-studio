using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class SliderWidget : BaseWidget
    {
        private readonly Border _borderContainer;
        private readonly Border _accentBar;
        private readonly Slider _slider;
        private readonly TextBlock _valueText;
        private readonly bool _isFloat;
        private readonly int _decimals;
        private readonly double _step;
        private readonly string _formatString;
        private bool _internalUpdate;

        public override string Key => Config.command ?? base.Key;

        public SliderWidget(WidgetConfig config) : base(config)
        {
            // ۱. تشخیص هوشمند نوع داده (Float یا Int) بر اساس مشخصات فنی کانفیگ
            // اگر decimals وجود داشته باشد یا گام حرکت (step) اعشاری باشد، اسلایدر خودکار Float می‌شود
            _decimals = config.decimals ?? (config.step.HasValue && config.step.Value.ToString(CultureInfo.InvariantCulture).Contains(".")
                ? config.step.Value.ToString(CultureInfo.InvariantCulture).Split('.')[1].Length
                : 0);

            // اگر کاربر decimals یا step اعشاری تعیین کرده باشد، یا فیلد پیش‌فرض اعشاری پر شده باشد:
            _isFloat = _decimals > 0 || (config.step.HasValue && config.step.Value % 1 != 0) || config.defaultFloat.HasValue;

            // تعیین پله‌های حرکت (Step)؛ برای عدد صحیح پیش‌فرض ۱ و برای اعشار پیش‌فرض ۰.۱ یا مقدار دلخواه کاربر
            _step = config.step ?? (_isFloat ? 0.1 : 1.0);

            // تعیین فرمت داینامیک نمایش مقدار عددی بر اساس تعداد اعشار واقعی
            _formatString = _isFloat ? $"F{_decimals}" : "F0";

            // ۲. پاک کردن محتوای پیش‌فرض کانتینر بیس و حذف مارجین اضافی
            Container.Children.Clear();
            Container.Margin = new Thickness(0);

            var mainStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // ۳. هدر بالایی شامل عنوان و مقدار زنده فعلی
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleLabel = new TextBlock
            {
                Text = config.label,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)), // Slate 600
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleLabel, 0);
            headerGrid.Children.Add(titleLabel);

            // دریافت مقادیر عددی پیش‌فرض با قابلیت پشتیبانی از اعشار
            double minVal = config.min ?? (double)(config.minFloat ?? 0.0);
            double maxVal = config.max ?? (double)(config.maxFloat ?? 100.0);

            // تخصیص هوشمند مقدار اولیه عددی
            double defaultVal = _isFloat
                ? (config.defaultFloat ?? (config.defaultInt ?? minVal))
                : (config.defaultInt ?? (int)minVal);

            string unitSuffix = string.IsNullOrEmpty(config.unit) ? "" : $" {config.unit}";

            _valueText = new TextBlock
            {
                Text = $"{defaultVal.ToString(_formatString, CultureInfo.InvariantCulture)}{unitSuffix}",
                FontWeight = FontWeights.ExtraBold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)), // آبی زنده متالیک (#3B82F6)
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_valueText, 1);
            headerGrid.Children.Add(_valueText);

            mainStack.Children.Add(headerGrid);

            // ۴. خط جداکننده افقی ظریف
            var horizontalSeparator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۵. ردیف اسلایدر به همراه نمایش محدوده بالا و پایین (Min/Max)
            var sliderLayout = new StackPanel { Orientation = Orientation.Vertical };

            _slider = new Slider
            {
                Minimum = minVal,
                Maximum = maxVal,
                Value = defaultVal,
                TickFrequency = _step,
                IsSnapToTickEnabled = true, // فعال بودن جفت شدن به گام‌ها به صورت پیش‌فرض برای تجربه کاربری عالی
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 4),
                Focusable = false
            };

            // تعریف محدوده کمینه و بیشینه در زیر اسلایدر
            var rangeLabelsGrid = new Grid { Margin = new Thickness(2, 0, 2, 0) };
            rangeLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rangeLabelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var minLabel = new TextBlock
            {
                Text = minVal.ToString(_formatString, CultureInfo.InvariantCulture),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(minLabel, 0);
            rangeLabelsGrid.Children.Add(minLabel);

            var maxLabel = new TextBlock
            {
                Text = maxVal.ToString(_formatString, CultureInfo.InvariantCulture),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(maxLabel, 1);
            rangeLabelsGrid.Children.Add(maxLabel);

            sliderLayout.Children.Add(_slider);
            sliderLayout.Children.Add(rangeLabelsGrid);
            mainStack.Children.Add(sliderLayout);

            // ۶. نوار عمودی ضخیم در سمت چپ (عرض ۸ پیکسل - آبی برای کنترلرهای آنالوگ)
            var blueAccent = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            _accentBar = new Border
            {
                Width = 8,
                Background = blueAccent,
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // گرید نهایی برای ترکیب لایه‌ها
            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            // تنظیم فاصله داخلی محتوای اصلی از نوار آبی چپ
            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // ۷. کادر دور تا دور کل ویجت با پس‌زمینه هاله ملایم آبی (۳٪ شفافیت) برای ایجاد تفکیک بصری شیک
            _borderContainer = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(8, 59, 130, 246)), // هاله بسیار کم‌رنگ آبی پس‌زمینه
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 59, 130, 246)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 8),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid
            };

            Container.Children.Add(_borderContainer);

            // ۸. مدیریت رویداد تغییر مقدار اسلایدر
            _slider.ValueChanged += (_, _) =>
            {
                double val = _slider.Value;

                // اعمال دقیق Step روی مقدار نهایی
                if (_step > 0)
                {
                    double snappedValue = Math.Round(val / _step) * _step;
                    if (Math.Abs(snappedValue - val) > double.Epsilon && snappedValue >= minVal && snappedValue <= maxVal)
                    {
                        val = snappedValue;
                    }
                }

                _valueText.Text = $"{val.ToString(_formatString, CultureInfo.InvariantCulture)}{unitSuffix}";

                if (_internalUpdate) return;

                string stringVal = _isFloat
                    ? val.ToString(_formatString, CultureInfo.InvariantCulture)
                    : ((int)Math.Round(val)).ToString(CultureInfo.InvariantCulture);

                RaiseValueChanged(BuildMessage(stringVal));
            };
        }

        public override void UpdateValue(string value)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
            {
                double minVal = Config.min ?? (double)(Config.minFloat ?? 0.0);
                double maxVal = Config.max ?? (double)(Config.maxFloat ?? 100.0);

                // محدودسازی مقدار دریافتی در بازه مجاز (Clamp)
                if (doubleValue < minVal) doubleValue = minVal;
                if (doubleValue > maxVal) doubleValue = maxVal;

                _internalUpdate = true;
                _slider.Value = doubleValue;

                string unitSuffix = string.IsNullOrEmpty(Config.unit) ? "" : $" {Config.unit}";
                _valueText.Text = $"{doubleValue.ToString(_formatString, CultureInfo.InvariantCulture)}{unitSuffix}";
                _internalUpdate = false;
            }
        }
    }
}
