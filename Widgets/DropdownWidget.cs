using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class DropdownWidget : BaseWidget
    {
        private readonly ComboBox _comboBox;
        private readonly Border _widgetBorder;
        private readonly Border _accentBar;
        private bool _internalUpdate;

        public override string Key => Config.variable ?? Config.command ?? base.Key;

        public DropdownWidget(WidgetConfig config) : base(config)
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

            // ۴. کامبوباکس با ظاهری مدرن، فلت و خوانا
            _comboBox = new ComboBox
            {
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(10, 0, 24, 0),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), // Slate 50
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Slate 300
                BorderThickness = new Thickness(1),
                Focusable = false,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) // Slate 800
            };

            // شخصی‌سازی ComboBox با قالب حاشیه‌ای ملایم و گوشه‌های گرد
            var comboBoxTemplate = new ControlTemplate(typeof(ComboBox));
            var cbBorderFactory = new FrameworkElementFactory(typeof(Border));
            cbBorderFactory.Name = "CbBorder";
            cbBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            cbBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ComboBox.BackgroundProperty));
            cbBorderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(ComboBox.BorderBrushProperty));
            cbBorderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(ComboBox.BorderThicknessProperty));

            // استفاده از ContentPresenter پیش‌فرض ویندوز برای دکمه کشویی بدون شکستن پوسته کنترلر
            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(ComboBox.PaddingProperty));

            cbBorderFactory.AppendChild(contentPresenterFactory);
            comboBoxTemplate.VisualTree = cbBorderFactory;

            // اعمال کنترل بصری بر تغییرات فوکوس و هاور روی درگاه کشویی
            _comboBox.DropDownOpened += (s, e) => TriggerGlowEffect(true);
            _comboBox.DropDownClosed += (s, e) => TriggerGlowEffect(false);

            // ۵. پر کردن گزینه‌ها در کامبوباکس از ساختار Config
            if (config.options != null)
            {
                foreach (DropdownOption option in config.options)
                {
                    var item = new ComboBoxItem
                    {
                        Content = option.label,
                        Tag = option.value,
                        Height = 28,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(10, 0, 10, 0),
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
                    };
                    _comboBox.Items.Add(item);

                    if (option.value == config.defaultOption || option.label == config.defaultOption)
                    {
                        _comboBox.SelectedItem = item;
                    }
                }
            }

            if (_comboBox.SelectedItem == null && _comboBox.Items.Count > 0)
            {
                _comboBox.SelectedIndex = 0;
            }

            // مدیریت تغییر انتخاب بدون تداخل با درگاه ارتباطی ورودی
            _comboBox.SelectionChanged += (s, e) =>
            {
                if (_internalUpdate) return;

                if (_comboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string? value = selectedItem.Tag?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        RaiseValueChanged(BuildMessage(value));
                        TriggerSelectionPulse();
                    }
                }
            };

            mainStack.Children.Add(_comboBox);

            // ۶. نوار عمودی سمت چپ (Accent Bar 8px) - بنفش متالیک (Violet 500)
            _accentBar = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)), // Violet 500 (#8B5CF6)
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // گرید اصلی برای متمایز کردن موقعیت نوار با بخش کنترلر
            var masterGrid = new Grid();
            masterGrid.Children.Add(mainStack);
            masterGrid.Children.Add(_accentBar);

            mainStack.Margin = new Thickness(16, 0, 0, 0);

            // ۷. کادر دور تا دور کل ویجت با هاله پس‌زمینه ۳٪ بنفش
            _widgetBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(8, 139, 92, 246)), // هاله بسیار ملایم بنفش در بک‌گراند
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Child = masterGrid
            };

            Container.Children.Add(_widgetBorder);
        }

        // ایجاد انیمیشن درخشش ملایم لحظه‌ای به محض تغییر گزینه در منوی کشویی
        private void TriggerSelectionPulse()
        {
            var glow = new DropShadowEffect
            {
                Color = Color.FromRgb(139, 92, 246),
                Direction = 0,
                ShadowDepth = 0,
                Opacity = 0.6,
                BlurRadius = 12
            };

            _widgetBorder.Effect = glow;

            var fadeAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(100)
            };

            fadeAnimation.Completed += (s, e) => _widgetBorder.Effect = null;
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, fadeAnimation);
        }

        // مدیریت رنگ حاشیه کل ویجت هنگام فعال بودن منوی بازشو
        private void TriggerGlowEffect(bool isOpen)
        {
            if (isOpen)
            {
                _widgetBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246));
                _widgetBorder.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(139, 92, 246),
                    Direction = 0,
                    ShadowDepth = 0,
                    Opacity = 0.3,
                    BlurRadius = 6
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
            _internalUpdate = true;
            foreach (ComboBoxItem item in _comboBox.Items)
            {
                if (item.Tag?.ToString() == value)
                {
                    _comboBox.SelectedItem = item;
                    break;
                }
            }
            _internalUpdate = false;
        }
    }
}
