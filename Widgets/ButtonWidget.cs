using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class ButtonWidget : BaseWidget
    {
        private readonly Button _button;
        private readonly Border _widgetBorder;
        private readonly Border _accentBar;

        public override string Key => Config.command ?? base.Key;

        public ButtonWidget(WidgetConfig config) : base(config)
        {
            // ۱. پاکسازی کامل محتوای قبلی کانتینر بیس و حذف حاشیه‌های مزاحم
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

            // ۳. خط جداکننده افقی ظریف زیر عنوان
            var horizontalSeparator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), // Slate 100
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۴. دکمه سفارشی با افکت‌های مدرن و انتقال نرم حالتها
            _button = new Button
            {
                Content = string.IsNullOrWhiteSpace(config.buttonText) ? "Trigger" : config.buttonText,
                Height = 36,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderThickness = new Thickness(0),
                Focusable = false,
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)) // Blue 500
            };

            // ساخت استایل و تمپلت داینامیک برای دکمه با گوشه‌های گرد
            var buttonStyle = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "ButtonBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            // تریگرها برای تغییر رنگ پس‌زمینه در حالت Hover و Pressed
            var triggerHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(37, 99, 235)))); // Blue 600

            var triggerPressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
            triggerPressed.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(29, 78, 216)))); // Blue 700

            template.Triggers.Add(triggerHover);
            template.Triggers.Add(triggerPressed);

            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            _button.Style = buttonStyle;

            // انیمیشن کوچک مایکرو-اینتراکشن هنگام فشردن دکمه (Scale Down جزئی)
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            _button.RenderTransform = scaleTransform;
            _button.RenderTransformOrigin = new Point(0.5, 0.5);

            _button.PreviewMouseDown += (s, e) =>
            {
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, TimeSpan.FromMilliseconds(50)));
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, TimeSpan.FromMilliseconds(50)));
            };

            _button.PreviewMouseUp += (s, e) =>
            {
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(100)));
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(100)));
            };

            // رویداد کلیک و ارسال داده به سریال پورت
            _button.Click += (_, _) =>
            {
                string value = string.IsNullOrWhiteSpace(Config.value) ? "1" : Config.value;
                RaiseValueChanged(BuildMessage(value));
                TriggerClickGlowEffect();
            };

            mainStack.Children.Add(_button);

            // ۵. نوار عمودی سمت چپ (Accent Bar 8px) - یکپارچه با زبان طراحی بقیه ویجت‌ها
            _accentBar = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)), // Blue 500
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // گرید اصلی برای تراز کردن محتوا و Accent Bar
            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            // ایجاد فاصله افقی محتوا از نوار آبی سمت چپ
            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // ۶. کادر دور تا دور کل ویجت با یک بک‌گراند بسیار ملایم خاکستری/آبی
            _widgetBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(5, 59, 130, 246)), // ۳٪ هاله آبی ملایم پس‌زمینه
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid
            };

            Container.Children.Add(_widgetBorder);
        }

        // ایجاد یک فیدبک بصری جذاب (هاله نوری) موقت روی کل کارت هنگام کلیک روی دکمه
        private void TriggerClickGlowEffect()
        {
            var glow = new DropShadowEffect
            {
                Color = Color.FromRgb(59, 130, 246),
                Direction = 0,
                ShadowDepth = 0,
                Opacity = 0.8,
                BlurRadius = 15
            };

            _widgetBorder.Effect = glow;

            // انیمیشن محو شدن تدریجی هاله نوری پس از ۲۰۰ میلی‌ثانیه
            var fadeAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(250),
                BeginTime = TimeSpan.FromMilliseconds(50)
            };

            fadeAnimation.Completed += (s, e) =>
            {
                _widgetBorder.Effect = null;
            };

            glow.BeginAnimation(DropShadowEffect.OpacityProperty, fadeAnimation);
        }

        public override void UpdateValue(string value)
        {
            // دکمه‌ها نیازی به به روز رسانی مقدار ورودی ندارند.
        }
    }
}
