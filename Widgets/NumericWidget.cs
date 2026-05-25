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
    public class NumericWidget : BaseWidget
    {
        private readonly TextBox _textBox;
        private readonly Button _sendButton;
        private readonly TextBlock? _unitLabel;
        private readonly Border _widgetBorder;
        private readonly Border _accentBar;
        private readonly Grid _inputGrid;

        private static readonly Regex NumericRegex = new Regex(@"^-?\d*\.?\d*$");

        public override string Key => Config.variable ?? Config.command ?? base.Key;

        public NumericWidget(WidgetConfig config) : base(config)
        {
            // ۱. پاکسازی کامل کانتینر بیس و حذف حاشیه‌های پیش‌فرض
            Container.Children.Clear();
            Container.Margin = new Thickness(0);

            var mainStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // ۲. عنوان ویجت
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

            // ۴. گرید برای چیدمان TextBox، واحد و دکمه ارسال
            _inputGrid = new Grid();
            _inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // ۵. تکست‌باکس با لبه‌های گرد مدرن
            _textBox = new TextBox
            {
                Text = config.defaultFloat?.ToString() ?? "0",
                Height = 32,
                Padding = new Thickness(10, 0, 10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), // Slate 50
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Slate 300
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 6, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) // Slate 800
            };

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

            // اعتبارسنجی ورودی کیبورد
            _textBox.PreviewTextInput += (s, e) =>
            {
                var textBox = (TextBox)s;
                string proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                                                 .Insert(textBox.SelectionStart, e.Text);
                e.Handled = !IsValidInput(proposedText);
            };

            

            _textBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Space)
                {
                    e.Handled = true;
                }
            };

            _textBox.GotFocus += (s, e) => TriggerFocusGlow(true);
            _textBox.LostFocus += (s, e) => TriggerFocusGlow(false);

            _textBox.KeyDown += (s, e) =>
            {
                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                    e.Handled = true;
                    SendValue();
                }
            };

            DataObject.AddPastingHandler(_textBox, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    string pasteText = (string)e.DataObject.GetData(DataFormats.Text);
                    var textBox = (TextBox)s;
                    string proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                                                     .Insert(textBox.SelectionStart, pasteText);
                    if (!IsValidInput(proposedText)) e.CancelCommand();
                }
                else
                {
                    e.CancelCommand();
                }
            });

            Grid.SetColumn(_textBox, 0);
            _inputGrid.Children.Add(_textBox);

            // ۶. واحد اندازه‌گیری ثابت (Unit)
            if (!string.IsNullOrEmpty(config.unit))
            {
                _unitLabel = new TextBlock
                {
                    Text = config.unit,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)), // Slate 500
                    Margin = new Thickness(4, 0, 8, 0)
                };
                Grid.SetColumn(_unitLabel, 1);
                _inputGrid.Children.Add(_unitLabel);
            }

            // ۷. دکمه ارسال با استایل متمایز نارنجی
            _sendButton = new Button
            {
                Content = "Send",
                Height = 32,
                Width = 65,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Focusable = false,
                Background = new SolidColorBrush(Color.FromRgb(249, 115, 22)) // Orange 500 (#F97316)
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
            triggerHover.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(234, 88, 12)))); // Orange 600

            var triggerPressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
            triggerPressed.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(194, 65, 12)))); // Orange 700

            buttonTemplate.Triggers.Add(triggerHover);
            buttonTemplate.Triggers.Add(triggerPressed);

            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));

            _sendButton.Style = buttonStyle;
            _sendButton.Click += (s, e) => SendValue();

            Grid.SetColumn(_sendButton, 2);
            _inputGrid.Children.Add(_sendButton);

            mainStack.Children.Add(_inputGrid);

            // ۸. نمایش بازه مجاز (Min/Max) با ظاهر ظریف Slate
            if (config.min.HasValue && config.max.HasValue)
            {
                var rangeLabel = new TextBlock
                {
                    Text = $"Range: {config.min} ~ {config.max}",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                    Margin = new Thickness(2, 6, 0, 0)
                };
                mainStack.Children.Add(rangeLabel);
            }

            // ۹. نوار عمودی سمت چپ (Accent Bar 8px) - نارنجی پویا
            _accentBar = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromRgb(249, 115, 22)), // Orange 500
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // ۱۰. کادر اصلی ویجت با هاله ۲٪ نارنجی در بک‌گراند
            _widgetBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(5, 249, 115, 22)), // هاله نارنجی بسیار کمرنگ
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid
            };

            Container.Children.Add(_widgetBorder);
        }

        private bool IsValidInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return true;
            return NumericRegex.IsMatch(input);
        }

        private void SendValue()
        {
            string rawText = _textBox.Text.Trim();
            if (string.IsNullOrEmpty(rawText)) return;

            bool isModified = false;
            bool isOutOfRange = false;

            if (rawText.EndsWith("."))
            {
                rawText = rawText.TrimEnd('.');
                if (string.IsNullOrEmpty(rawText) || rawText == "-") rawText = "0";
                _textBox.Text = rawText;
                isModified = true;
            }
            else if (rawText == "-")
            {
                rawText = "0";
                _textBox.Text = rawText;
                isModified = true;
            }

            if (double.TryParse(rawText, out double val))
            {
                if (Config.min.HasValue && val < Config.min)
                {
                    val = (double)Config.min;
                    _textBox.Text = val.ToString();
                    isModified = true;
                    isOutOfRange = true;
                }
                if (Config.max.HasValue && val > Config.max)
                {
                    val = (double)Config.max;
                    _textBox.Text = val.ToString();
                    isModified = true;
                    isOutOfRange = true;
                }

                if (isModified)
                {
                    _textBox.Focus();
                    _textBox.CaretIndex = _textBox.Text.Length;
                }

                RaiseValueChanged(BuildMessage(val.ToString()));

                if (isOutOfRange)
                {
                    TriggerErrorFeedback();
                }
                else
                {
                    TriggerSendFeedback();
                }
            }
        }

        // افکت لرزش گرافیکی برای فیدبک ارسال عادی
        private void TriggerSendFeedback()
        {
            var shakeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 3,
                Duration = TimeSpan.FromMilliseconds(40),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            };

            var translateTransform = new TranslateTransform();
            _inputGrid.RenderTransform = translateTransform;
            translateTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
        }

        // انیمیشن خطا (لرزش و قرمز شدن لحظه‌ای کادر دور ویجت در صورت تجاوز از محدوده)
        private void TriggerErrorFeedback()
        {
            _widgetBorder.BorderBrush = Brushes.Red;
            var errorGlow = new DropShadowEffect
            {
                Color = Colors.Red,
                Direction = 0,
                ShadowDepth = 0,
                Opacity = 0.6,
                BlurRadius = 10
            };
            _widgetBorder.Effect = errorGlow;

            var shakeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 8,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };

            var translateTransform = new TranslateTransform();
            _inputGrid.RenderTransform = translateTransform;
            translateTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);

            var fadeAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(200)
            };

            fadeAnimation.Completed += (s, e) =>
            {
                _widgetBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                _widgetBorder.Effect = null;
            };
            errorGlow.BeginAnimation(DropShadowEffect.OpacityProperty, fadeAnimation);
        }

        // مدیریت افکت سایه و درخشش فوکوس روی فیلد متنی
        private void TriggerFocusGlow(bool hasFocus)
        {
            if (hasFocus)
            {
                _widgetBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(249, 115, 22));
                _widgetBorder.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(249, 115, 22),
                    Direction = 0,
                    ShadowDepth = 0,
                    Opacity = 0.4,
                    BlurRadius = 8
                };
            }
            else
            {
                _widgetBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                _widgetBorder.Effect = null;
            }
        }

        public override void UpdateValue(string value)
        {
            if (IsValidInput(value))
            {
                _textBox.Text = value;
            }
        }
    }
}
