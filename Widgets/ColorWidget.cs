using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class ColorWidget : BaseWidget
    {
        private readonly Border _widgetBorder;
        private readonly Border _accentBar;
        private readonly Border _colorPreview;
        private readonly Button _pickButton;

        public override string Key => Config.variable ?? Config.command ?? base.Key;

        public ColorWidget(WidgetConfig config) : base(config)
        {
            // ۱. پاکسازی کامل محتوای پیش‌فرض کانتینر
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

            // ۴. گرید محتوای تعاملی ویجت
            var actionGrid = new Grid();
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ۵. پیش‌نمایش دایره‌ای شکل رنگ با حاشیه ظریف
            _colorPreview = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                BorderThickness = new Thickness(1.5),
                Background = ParseColor(config.defaultColor ?? "#3B82F6"),
                Margin = new Thickness(0, 0, 12, 0)
            };

            Grid.SetColumn(_colorPreview, 0);
            actionGrid.Children.Add(_colorPreview);

            // ۶. دکمه شیک انتخاب رنگ با لبه‌های نرم و متناسب با تم خاکستری Slate
            _pickButton = new Button
            {
                Content = "Pick Color",
                Height = 32,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Focusable = false
            };

            var buttonStyle = new Style(typeof(Button));
            var buttonTemplate = new ControlTemplate(typeof(Button));
            var btnBorder = new FrameworkElementFactory(typeof(Border));
            btnBorder.Name = "BtnBorder";
            btnBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            btnBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249))); // Slate 100
            btnBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(226, 232, 240))); // Slate 200
            btnBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var btnContent = new FrameworkElementFactory(typeof(ContentPresenter));
            btnContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            btnContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            btnBorder.AppendChild(btnContent);
            buttonTemplate.VisualTree = btnBorder;

            var triggerHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(226, 232, 240)), "BtnBorder")); // Slate 200

            buttonTemplate.Triggers.Add(triggerHover);
            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));
            buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85)))); // Slate 700

            _pickButton.Style = buttonStyle;
            _pickButton.Click += (s, e) => ShowWpfColorPicker();

            Grid.SetColumn(_pickButton, 1);
            actionGrid.Children.Add(_pickButton);

            mainStack.Children.Add(actionGrid);

            // ۷. نوار عمودی سمت چپ (Accent Bar 8px) با رنگ شروع پویا
            _accentBar = new Border
            {
                Width = 8,
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // ۸. کادر بیرونی اصلی ویجت با قابلیت شخصی‌سازی بک‌گراند زنده
            _widgetBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid
            };

            Container.Children.Add(_widgetBorder);

            // همگام‌سازی رنگ آکسنت‌بار و هاله پس‌زمینه با رنگ پیش‌فرض اولیه
            SyncDynamicColors(((SolidColorBrush)_colorPreview.Background).Color);
        }

        private Brush ParseColor(string hex)
        {
            try
            {
                return (SolidColorBrush)(new BrushConverter().ConvertFromString(hex) ?? Brushes.CornflowerBlue);
            }
            catch
            {
                return Brushes.CornflowerBlue;
            }
        }

        // انیمیشن پویا برای همگام‌سازی رنگ‌های آکسنت‌بار و هاله پس‌زمینه ویجت با افکت انیمیت روان
        private void SyncDynamicColors(Color newColor)
        {
            _accentBar.Background = new SolidColorBrush(newColor);

            // رنگ‌آمیزی هاله پس‌زمینه کارت با شفافیت ۵٪ رنگ انتخابی
            var tintColor = Color.FromArgb(12, newColor.R, newColor.G, newColor.B);
            _widgetBorder.Background = new SolidColorBrush(tintColor);
        }

        private void TriggerPulseFeedback(Color pulseColor)
        {
            var glow = new DropShadowEffect
            {
                Color = pulseColor,
                Direction = 0,
                ShadowDepth = 0,
                Opacity = 0.6,
                BlurRadius = 12
            };

            _widgetBorder.Effect = glow;

            var fadeAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(100)
            };

            fadeAnimation.Completed += (s, e) => _widgetBorder.Effect = null;
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, fadeAnimation);
        }

        public override void UpdateValue(string value)
        {
            try
            {
                var brush = ParseColor(value);
                if (brush is SolidColorBrush solidBrush)
                {
                    _colorPreview.Background = solidBrush;
                    SyncDynamicColors(solidBrush.Color);
                }
            }
            catch
            {
                // نادیده گرفتن مقادیر نویز یا خراب دریافتی از پورت سریال
            }
        }

        // طراحی فوق لوکس و دارک متالیک دیالوگ انتخاب رنگ اختصاصی
        private void ShowWpfColorPicker()
        {
            var pickerWindow = new Window
            {
                Title = "Select Color",
                Width = 340,
                Height = 310,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false
            };

            var mainGrid = new Grid { Margin = new Thickness(20) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var contentStack = new StackPanel();

            // فریم بزرگ با گوشه‌های گرد و درخشش زنده و نئونی متناسب با رنگ انتخابی فعلی
            var liveIndicator = new Border
            {
                Height = 48,
                Margin = new Thickness(0, 0, 0, 20),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)) // Slate 700
            };

            var glowEffect = new DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.7
            };
            liveIndicator.Effect = glowEffect;

            Color currentColor = Colors.Blue;
            if (_colorPreview.Background is SolidColorBrush currentBrush)
            {
                currentColor = currentBrush.Color;
            }

            liveIndicator.Background = new SolidColorBrush(currentColor);
            glowEffect.Color = currentColor;

            // تعریف ۳ اسلایدر رنگی
            var rSlider = CreateSlider("R", currentColor.R, Color.FromRgb(239, 68, 68)); // Red 500
            var gSlider = CreateSlider("G", currentColor.G, Color.FromRgb(34, 197, 94)); // Green 500
            var bSlider = CreateSlider("B", currentColor.B, Color.FromRgb(59, 130, 246)); // Blue 500

            void UpdateLivePreview()
            {
                byte r = (byte)rSlider.slider.Value;
                byte g = (byte)gSlider.slider.Value;
                byte b = (byte)bSlider.slider.Value;
                var newColor = Color.FromRgb(r, g, b);

                liveIndicator.Background = new SolidColorBrush(newColor);
                glowEffect.Color = newColor;
            }

            rSlider.slider.ValueChanged += (s, e) => UpdateLivePreview();
            gSlider.slider.ValueChanged += (s, e) => UpdateLivePreview();
            bSlider.slider.ValueChanged += (s, e) => UpdateLivePreview();

            contentStack.Children.Add(liveIndicator);
            contentStack.Children.Add(rSlider.panel);
            contentStack.Children.Add(gSlider.panel);
            contentStack.Children.Add(bSlider.panel);

            mainGrid.Children.Add(contentStack);

            // دکمه APPLY نهایی با افکت آبی پررنگ
            var applyButton = new Button
            {
                Content = "APPLY COLOR",
                Height = 36,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Focusable = false,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var applyBtnStyle = new Style(typeof(Button));
            var applyBtnTemplate = new ControlTemplate(typeof(Button));
            var applyBorder = new FrameworkElementFactory(typeof(Border));
            applyBorder.Name = "ApplyBorder";
            applyBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            applyBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(59, 130, 246))); // Blue 500

            var applyContent = new FrameworkElementFactory(typeof(ContentPresenter));
            applyContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            applyContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            applyBorder.AppendChild(applyContent);
            applyBtnTemplate.VisualTree = applyBorder;

            var applyHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            applyHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(37, 99, 235)), "ApplyBorder")); // Blue 600

            applyBtnTemplate.Triggers.Add(applyHover);
            applyBtnStyle.Setters.Add(new Setter(Button.TemplateProperty, applyBtnTemplate));
            applyBtnStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));

            applyButton.Style = applyBtnStyle;
            applyButton.Click += (s, e) => { pickerWindow.DialogResult = true; };

            Grid.SetRow(applyButton, 1);
            mainGrid.Children.Add(applyButton);

            pickerWindow.Content = mainGrid;

            if (pickerWindow.ShowDialog() == true)
            {
                byte r = (byte)rSlider.slider.Value;
                byte g = (byte)gSlider.slider.Value;
                byte b = (byte)bSlider.slider.Value;

                var finalColor = Color.FromRgb(r, g, b);
                string hex = $"#{r:X2}{g:X2}{b:X2}";
                _colorPreview.Background = new SolidColorBrush(finalColor);

                SyncDynamicColors(finalColor);
                RaiseValueChanged(BuildMessage(hex));
                TriggerPulseFeedback(finalColor);
            }
        }

        private (StackPanel panel, Slider slider) CreateSlider(string label, byte initialValue, Color sliderThemeColor)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 6) };

            var text = new TextBlock
            {
                Text = label,
                Width = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            var slider = new Slider
            {
                Width = 220,
                Minimum = 0,
                Maximum = 255,
                Value = initialValue,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };

            // رنگ دکمه لغزنده به فرمت هگز مناسب برای قرارگیری در XAML
            string hexColor = $"#{sliderThemeColor.R:X2}{sliderThemeColor.G:X2}{sliderThemeColor.B:X2}";

            // ساخت تمپلت اسلایدر با XamlReader برای دور زدن باگ‌های کلاس FrameworkElementFactory
            string xamlTemplate = $@"
        <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
                         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' 
                         TargetType='Slider'>
            <Grid>
                <!-- ریل اسلایدر -->
                <Border Height='6' CornerRadius='3' Background='#1E293B' VerticalAlignment='Center'/>
                <!-- ردیاب کنترل برای لغزش دستیار -->
                <Track Name='PART_Track'>
                    <Track.Thumb>
                        <Thumb Width='14' Height='14' Cursor='Hand'>
                            <Thumb.Template>
                                <ControlTemplate>
                                    <Border Background='{hexColor}' BorderBrush='White' BorderThickness='1.5' CornerRadius='7'/>
                                </ControlTemplate>
                            </Thumb.Template>
                        </Thumb>
                    </Track.Thumb>
                </Track>
            </Grid>
        </ControlTemplate>";

            slider.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(xamlTemplate);


            var valueLabel = new TextBlock
            {
                Text = initialValue.ToString("D3"),
                Width = 30,
                Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)), // Slate 100
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };

            slider.ValueChanged += (s, e) =>
            {
                valueLabel.Text = ((int)slider.Value).ToString("D3");
            };

            panel.Children.Add(text);
            panel.Children.Add(slider);
            panel.Children.Add(valueLabel);

            return (panel, slider);
        }


    }
}
