using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class TextWidget : BaseWidget
    {
        private readonly TextBox _textBox;
        private readonly Button _sendButton;
        private readonly Border _widgetBorder;
        private readonly Border _accentBar;
        private readonly Grid _inputGrid;

        public override string Key => Config.variable ?? Config.command ?? base.Key;

        public TextWidget(WidgetConfig config) : base(config)
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

            // ۴. گرید برای چیدمان افقی تکست‌باکس و دکمه ارسال
            _inputGrid = new Grid();
            _inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // ۵. طراحی تکست‌باکس مدرن با لبه‌های گرد و پدینگ داخلی مناسب
            _textBox = new TextBox
            {
                Text = config.defaultText ?? "",
                Height = 32,
                Padding = new Thickness(10, 0, 10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), // Slate 50
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Slate 300
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) // Slate 800
            };

            // قالب‌دهی تکست‌باکس برای لبه‌های گرد مدرن
            var textBoxTemplate = new ControlTemplate(typeof(TextBox));
            var tbBorderFactory = new FrameworkElementFactory(typeof(Border));
            tbBorderFactory.Name = "TbBorder";
            tbBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            tbBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            tbBorderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            tbBorderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));

            var scrollViewerFactory = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewerFactory.Name = "PART_ContentHost";
            tbBorderFactory.AppendChild(scrollViewerFactory);
            textBoxTemplate.VisualTree = tbBorderFactory;
            _textBox.Template = textBoxTemplate;

            // رویداد فوکوس برای درخشان شدن کل کارت
            _textBox.GotFocus += (s, e) => TriggerFocusGlow(true);
            _textBox.LostFocus += (s, e) => TriggerFocusGlow(false);

            // ارسال با کلید اینتر
            _textBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    SendValue();
                }
            };
            Grid.SetColumn(_textBox, 0);
            _inputGrid.Children.Add(_textBox);

            // ۶. دکمه ارسال مدرن با رنگ آسمانی (Sky) متمایز کننده متون
            _sendButton = new Button
            {
                Content = "Send",
                Height = 32,
                Width = 65,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Focusable = false,
                Background = new SolidColorBrush(Color.FromRgb(14, 165, 233)) // Sky 500 (#0EA5E9)
            };

            var buttonStyle = new Style(typeof(Button));
            var buttonTemplate = new ControlTemplate(typeof(Button));
            var btnBorder = new FrameworkElementFactory(typeof(Border));
            btnBorder.Name = "BtnBorder";
            btnBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            btnBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));

            var btnContent = new FrameworkElementFactory(typeof(ContentPresenter));
            btnContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            btnContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            btnBorder.AppendChild(btnContent);
            buttonTemplate.VisualTree = btnBorder;

            var triggerHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(3, 105, 161)))); // Sky 700

            var triggerPressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
            triggerPressed.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(7, 89, 133)))); // Sky 800

            buttonTemplate.Triggers.Add(triggerHover);
            buttonTemplate.Triggers.Add(triggerPressed);

            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));

            _sendButton.Style = buttonStyle;
            _sendButton.Click += (s, e) => SendValue();

            Grid.SetColumn(_sendButton, 1);
            _inputGrid.Children.Add(_sendButton);

            mainStack.Children.Add(_inputGrid);

            // ۷. نوار عمودی سمت چپ (Accent Bar 8px) با رنگ آبی آسمانی متالیک
            _accentBar = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromRgb(14, 165, 233)), // Sky 500
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // گرید اصلی برای تراز کردن محتوا و نوار چپ
            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // ۸. کادر بیرونی کل ویجت با هاله نوری بسیار کمرنگ و تراز استاندارد فاصله بالا
            _widgetBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(5, 14, 165, 233)), // هاله کم رنگ آبی آسمانی در پس‌زمینه (۲٪)
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10), // تراز فاصله با استاندارد کل ویجت‌ها برای انسجام
                Child = masterGrid
            };

            Container.Children.Add(_widgetBorder);
        }

        private void SendValue()
        {
            string text = _textBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                RaiseValueChanged(BuildMessage(text));
                TriggerSendFeedback();
            }
        }

        // ایجاد انیمیشن و فیدبک گرافیکی بسیار باحال در زمان ارسال موفق اطلاعات
        private void TriggerSendFeedback()
        {
            // افکت لرزش افقی جزئی کارت (Micro-Interaction) برای نمایش فرآیند ارسال داده
            var doubleAnimation = new DoubleAnimation
            {
                From = 0,
                To = 4,
                Duration = TimeSpan.FromMilliseconds(40),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            };

            var translateTransform = new TranslateTransform();
            _inputGrid.RenderTransform = translateTransform;
            translateTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
        }

        // مدیریت افکت فوکوس روی تکست‌باکس و اعمال سایه درخشان دور تا دور کل کارت ویجت
        private void TriggerFocusGlow(bool hasFocus)
        {
            if (hasFocus)
            {
                _widgetBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(14, 165, 233)); // لبه فیروزه‌ای هنگام فوکوس

                var shadow = new DropShadowEffect
                {
                    Color = Color.FromRgb(14, 165, 233),
                    Direction = 0,
                    ShadowDepth = 0,
                    Opacity = 0.4,
                    BlurRadius = 8
                };
                _widgetBorder.Effect = shadow;
            }
            else
            {
                _widgetBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)); // بازگشت به رنگ خاکستری ملایم
                _widgetBorder.Effect = null;
            }
        }

        public override void UpdateValue(string value)
        {
            _textBox.Text = value;
        }
    }
}
