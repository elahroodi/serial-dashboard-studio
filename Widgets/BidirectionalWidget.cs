using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class BidirectionalWidget : BaseWidget
    {
        private readonly Border _widgetBorder;
        private readonly Border _accentBar;
        private readonly TextBox _textBox;
        private readonly Button _sendButton;
        private bool _suppressChange = false;

        private static readonly Regex NumericRegex = new Regex(@"^-?\d*\.?\d*$");
        private readonly Color _themeColor = Color.FromRgb(20, 184, 166); // Teal 500 (#14B8A6)

        public override string Key => Config.variable ?? Config.command ?? base.Key;

        public BidirectionalWidget(WidgetConfig config) : base(config)
        {
            // ۱. پاکسازی و آماده‌سازی کانتینر اصلی
            Container.Children.Clear();
            Container.Margin = new Thickness(0);

            var mainStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // ۲. عنوان ویجت با رنگ تیره Slate
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
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), // Slate 100
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۴. گرید توزیع فضا برای تکست‌باکس و دکمه ارسال
            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // ۵. تکست‌باکس با استایل مدرن و لبه‌های گرد شده سفارشی
            _textBox = new TextBox
            {
                Height = 32,
                Text = config.defaultText ?? "",
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(10, 0, 10, 0),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), // Slate 50
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(0, 0, 8, 0)
            };

            // قالب گرافیکی اختصاصی برای دور زدن لبه‌های تیز تکست‌باکس و افزودن ترنزیشن فوکوس
            var textBoxTemplate = new ControlTemplate(typeof(TextBox));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "Border";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));

            var scrollViewerFactory = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewerFactory.Name = "PART_ContentHost";
            borderFactory.AppendChild(scrollViewerFactory);
            textBoxTemplate.VisualTree = borderFactory;
            _textBox.Template = textBoxTemplate;

            // رویدادهای ایجاد افکت Glow پویا در زمان فوکوس
            _textBox.GotFocus += (s, e) => ApplyTextBoxFocusStyle(true);
            _textBox.LostFocus += (s, e) => ApplyTextBoxFocusStyle(false);

            Grid.SetColumn(_textBox, 0);
            inputGrid.Children.Add(_textBox);

            // ۶. دکمه ارسال (Send Button) با استایل مدرن فیروزه‌ای و انیمیشن کلیک
            _sendButton = new Button
            {
                Content = "Send",
                Height = 32,
                Width = 70,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Focusable = false
            };

            var buttonStyle = new Style(typeof(Button));
            var buttonTemplate = new ControlTemplate(typeof(Button));
            var btnBorder = new FrameworkElementFactory(typeof(Border));
            btnBorder.Name = "BtnBorder";
            btnBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            btnBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(_themeColor));

            var btnContent = new FrameworkElementFactory(typeof(ContentPresenter));
            btnContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            btnContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            btnBorder.AppendChild(btnContent);
            buttonTemplate.VisualTree = btnBorder;

            // افکت hover برای تغییر ملایم رنگ به رنگ تیره‌تر فیروزه‌ای
            var triggerHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            triggerHover.SetSets(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(13, 148, 136)), "BtnBorder")); // Teal 600

            buttonTemplate.Triggers.Add(triggerHover);
            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));

            _sendButton.Style = buttonStyle;

            // هندلر کلیک انیمیشنی (Scale Down جزئی دکمه برای حس بهتر کلیک)
            _sendButton.PreviewMouseDown += (s, e) => ApplyClickAnimation(_sendButton, true);
            _sendButton.PreviewMouseUp += (s, e) => ApplyClickAnimation(_sendButton, false);
            _sendButton.MouseLeave += (s, e) => ApplyClickAnimation(_sendButton, false);

            Grid.SetColumn(_sendButton, 1);
            inputGrid.Children.Add(_sendButton);

            mainStack.Children.Add(inputGrid);

            // ۷. نوار عمودی سمت چپ (Accent Bar)
            _accentBar = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(_themeColor),
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // ۸. کادر محافظ اصلی بیرونی با هاله پس‌زمینه فیروزه‌ای فوق‌العاده کمرنگ (۳٪)
            _widgetBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(8, 20, 184, 166)), // 3% Teal Tint
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid
            };

            Container.Children.Add(_widgetBorder);

            // ۹. منطق رویدادها و ارسال اطلاعات
            _sendButton.Click += (s, e) => SendCurrentValue();

            _textBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    e.Handled = true;
                    SendCurrentValue();
                }
            };

            SetupNumericValidation();
        }

        private void ApplyTextBoxFocusStyle(bool isFocused)
        {
            if (isFocused)
            {
                _textBox.BorderBrush = new SolidColorBrush(_themeColor);
                _textBox.Background = Brushes.White;
                _textBox.Effect = new DropShadowEffect
                {
                    Color = _themeColor,
                    Direction = 0,
                    ShadowDepth = 0,
                    Opacity = 0.25,
                    BlurRadius = 6
                };
            }
            else
            {
                _textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                _textBox.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                _textBox.Effect = null;
            }
        }

        private void ApplyClickAnimation(FrameworkElement element, bool isPressed)
        {
            var scale = isPressed ? 0.95 : 1.0;
            var transform = new ScaleTransform(scale, scale, element.ActualWidth / 2, element.ActualHeight / 2);
            element.RenderTransform = transform;
        }

        private void SendCurrentValue()
        {
            if (string.IsNullOrEmpty(Config.command)) return;

            string textToSend = _textBox.Text.Trim();
            if (string.IsNullOrEmpty(textToSend)) return;

            bool isModified = false;

            if (textToSend.EndsWith("."))
            {
                textToSend = textToSend.TrimEnd('.');
                if (string.IsNullOrEmpty(textToSend) || textToSend == "-")
                {
                    textToSend = "0";
                }
                _textBox.Text = textToSend;
                isModified = true;
            }
            else if (textToSend == "-")
            {
                textToSend = "0";
                _textBox.Text = textToSend;
                isModified = true;
            }

            if (isModified)
            {
                _textBox.Focus();
                _textBox.CaretIndex = _textBox.Text.Length;
            }

            // ارسال پیام به بستر سریال از طریق ساختار پایه BaseWidget
            RaiseValueChanged(BuildMessage(textToSend));
            TriggerSuccessPulse();
        }

        private void TriggerSuccessPulse()
        {
            var glow = new DropShadowEffect
            {
                Color = _themeColor,
                Direction = 0,
                ShadowDepth = 0,
                Opacity = 0.5,
                BlurRadius = 10
            };

            _widgetBorder.Effect = glow;

            var fade = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(100)
            };
            fade.Completed += (s, e) => _widgetBorder.Effect = null;
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, fade);
        }

        private void SetupNumericValidation()
        {
            _textBox.PreviewTextInput += (s, e) =>
            {
                var textBox = (TextBox)s;
                string proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                                                 .Insert(textBox.SelectionStart, e.Text);

                bool isInvalid = !IsValidInput(proposedText);
                e.Handled = isInvalid;

                if (isInvalid)
                {
                    TriggerValidationWarning();
                }
            };

            _textBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Space)
                {
                    e.Handled = true;
                    TriggerValidationWarning();
                }
            };

            DataObject.AddPastingHandler(_textBox, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    var pasteText = (string)e.DataObject.GetData(DataFormats.Text);
                    var textBox = (TextBox)s;
                    string proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                                                     .Insert(textBox.SelectionStart, pasteText);

                    if (!IsValidInput(proposedText))
                    {
                        e.CancelCommand();
                        TriggerValidationWarning();
                    }
                }
                else
                {
                    e.CancelCommand();
                }
            });
        }

        private void TriggerValidationWarning()
        {
            // فلش آنی حاشیه تکست‌باکس با رنگ نارنجی هشدار
            var warningBrush = new SolidColorBrush(Color.FromRgb(249, 115, 22)); // Orange 500
            _textBox.BorderBrush = warningBrush;

            var colorAnimation = new ColorAnimation
            {
                To = _textBox.IsFocused ? _themeColor : Color.FromRgb(226, 232, 240),
                Duration = TimeSpan.FromMilliseconds(300)
            };

            _textBox.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private bool IsValidInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return true;
            return NumericRegex.IsMatch(input);
        }

        public override void UpdateValue(string value)
        {
            _suppressChange = true;
            if (IsValidInput(value))
            {
                _textBox.Text = value;
            }
            _suppressChange = false;
        }
    }

    // متد کمکی برای مقداردهی تریگرها در FrameworkElementFactory
    public static class TriggerExtensions
    {
        public static void SetSets(this Trigger trigger, Setter setter, string targetName = null)
        {
            if (targetName != null) setter.TargetName = targetName;
            trigger.Setters.Add(setter);
        }
    }
}
