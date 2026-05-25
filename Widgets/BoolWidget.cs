using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class BoolWidget : BaseWidget
    {
        private readonly Border _borderContainer;
        private readonly Border _accentBar;
        private readonly CheckBox _checkBox;
        private readonly Border _toggleBackground;
        private readonly Ellipse _toggleThumb;
        private readonly TextBlock _statusText;
        private bool _internalUpdate;

        public override string Key => Config.command ?? base.Key;

        public BoolWidget(WidgetConfig config) : base(config)
        {
            // ۱. پاک کردن محتوای پیش‌فرض کانتینر بیس و حذف مارجین اضافی
            Container.Children.Clear();
            Container.Margin = new Thickness(0);

            var mainStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // ۲. عنوان ویجت (بالاترین بخش)
            var labelText = new TextBlock
            {
                Text = config.label,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)), // Slate 600
                Margin = new Thickness(0, 0, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            mainStack.Children.Add(labelText);

            // ۳. خط جداکننده افقی ظریف
            var horizontalSeparator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۴. گرید ردیف سوییچ با ارتفاع استاندارد ۳۶ پیکسل
            var controlGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 36
            };
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // سوییچ (چپ)
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // فاصله خالی کشسانی وسط
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // متن وضعیت (ON/OFF - راست)

            // دکمه سفارشی سوییچ کشویی (Toggle Control)
            var toggleCanvas = new Canvas { Width = 44, Height = 24, VerticalAlignment = VerticalAlignment.Center };

            _toggleBackground = new Border
            {
                Width = 44,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // خاکستری پیش‌فرض خاموش
                BorderThickness = new Thickness(0)
            };
            toggleCanvas.Children.Add(_toggleBackground);

            _toggleThumb = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Margin = new Thickness(3, 3, 0, 0) // موقعیت اولیه خاموش (چپ)
            };
            toggleCanvas.Children.Add(_toggleThumb);

            // ساخت چک‌باکس نامرئی روی لایه بالایی جهت دریافت کلیک‌ها
            _checkBox = new CheckBox
            {
                Opacity = 0,
                Width = 44,
                Height = 24,
                Cursor = System.Windows.Input.Cursors.Hand,
                IsChecked = config.defaultBool ?? false,
                VerticalAlignment = VerticalAlignment.Center
            };

            var switchContainer = new Grid { VerticalAlignment = VerticalAlignment.Center };
            switchContainer.Children.Add(toggleCanvas);
            switchContainer.Children.Add(_checkBox);

            Grid.SetColumn(switchContainer, 0);
            controlGrid.Children.Add(switchContainer);

            // ۵. لایبل وضعیت متنی در سمت راست (روشن / خاموش) با قلم بسیار ضخیم
            _statusText = new TextBlock
            {
                Text = (config.defaultBool ?? false) ? "ON" : "OFF",
                FontWeight = FontWeights.ExtraBold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_statusText, 2);
            controlGrid.Children.Add(_statusText);

            mainStack.Children.Add(controlGrid);

            // ۶. نوار عمودی ضخیم در سمت چپ (عرض ۸ پیکسل برای هماهنگی کامل با پلتفرم شما)
            _accentBar = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // رنگ پیشفرض خنثی
                CornerRadius = new CornerRadius(6, 0, 0, 6), // کاملا هماهنگ با گوشه کادر اصلی
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // گرید نهایی برای ترکیب لایه‌ها
            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            // تنظیم فاصله داخلی محتوا از نوار سمت چپ
            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // ۷. کادر دور تا دور کل ویجت با قابلیت پذیرش بک‌گراند داینامیک
            _borderContainer = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 8),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid
            };

            // افزودن لایه نهایی به کانتینر پایه
            Container.Children.Add(_borderContainer);

            // ۸. منطق رویدادها
            _checkBox.Checked += (_, _) =>
            {
                ApplyVisualState(true);
                if (_internalUpdate) return;
                RaiseValueChanged(BuildMessage("1"));
            };

            _checkBox.Unchecked += (_, _) =>
            {
                ApplyVisualState(false);
                if (_internalUpdate) return;
                RaiseValueChanged(BuildMessage("0"));
            };

            // اعمال وضعیت اولیه بصری بر اساس دیتای پیش‌فرض کانفیگ
            ApplyVisualState(config.defaultBool ?? false);
        }

        private void ApplyVisualState(bool isChecked)
        {
            if (isChecked)
            {
                var greenAccent = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // سبز نئونی (Success)
                var greenBg = new SolidColorBrush(Color.FromArgb(15, 34, 197, 94)); // سبز ملایم پس‌زمینه (شفافیت ~۶٪)
                var greenBorder = new SolidColorBrush(Color.FromArgb(60, 34, 197, 94));

                _toggleBackground.Background = greenAccent;
                _toggleThumb.Margin = new Thickness(23, 3, 0, 0); // پرش کلید تاشو به راست

                if (_statusText != null)
                {
                    _statusText.Text = "ON";
                    _statusText.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                }

                if (_accentBar != null) _accentBar.Background = greenAccent;
                if (_borderContainer != null)
                {
                    _borderContainer.Background = greenBg;
                    _borderContainer.BorderBrush = greenBorder;
                }
            }
            else
            {
                var grayAccent = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // Slate 300
                _toggleBackground.Background = grayAccent;
                _toggleThumb.Margin = new Thickness(3, 3, 0, 0); // پرش به چپ

                if (_statusText != null)
                {
                    _statusText.Text = "OFF";
                    _statusText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate 400
                }

                if (_accentBar != null) _accentBar.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                if (_borderContainer != null)
                {
                    _borderContainer.Background = Brushes.White;
                    _borderContainer.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                }
            }
        }

        public override void UpdateValue(string value)
        {
            _internalUpdate = true;
            bool isTrue = value == "1" || value.ToLower() == "true" || value.ToLower() == "on";
            _checkBox.IsChecked = isTrue;
            ApplyVisualState(isTrue);
            _internalUpdate = false;
        }
    }
}
