using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Win32;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class LogWidget : IWidget
    {
        private readonly WidgetConfig _config;
        private readonly Border _borderContainer;
        private readonly TextBox _logBox;
        private readonly StringBuilder _allLogs = new(); // نگهداری لاگ‌ها برای فیلتر کردن راحت‌تر

        // کنترل وضعیت‌ها
        private bool _isPaused = false;
        private string _currentFilter = string.Empty;
        private const int MaxLogLines = 1000; // محدودیت حجم لاگ جهت جلوگیری از کندی سیستم

        public string Key => _config.variable ?? string.Empty;
        public event EventHandler<string>? ValueChanged;

        public LogWidget(WidgetConfig config)
        {
            _config = config;

            var mainStack = new StackPanel();

            // ۱. هدر چندمنظوره (عنوان در چپ، دکمه‌ها در راست)
            var headerDock = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 0, 0, 6)
            };

            // عنوان ویجت
            var labelText = new TextBlock
            {
                Text = _config.label,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(labelText, Dock.Left);
            headerDock.Children.Add(labelText);

            // کانتینر دکمه‌ها و ابزارها در سمت راست هدر
            var controlsStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(controlsStack, Dock.Right);
            headerDock.Children.Add(controlsStack);

            // فیلتر جستجو لحظه‌ای (Search Box)
            var searchBox = new TextBox
            {
                Width = 80,
                Height = 20,
                FontSize = 10,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };
            // اضافه کردن Placeholder به باکس جستجو با ترفند ساده
            searchBox.Text = "Filter...";
            searchBox.Foreground = Brushes.Gray;
            searchBox.GotFocus += (s, e) => { if (searchBox.Text == "Filter...") { searchBox.Text = ""; searchBox.Foreground = Brushes.Black; } };
            searchBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(searchBox.Text)) { searchBox.Text = "Filter..."; searchBox.Foreground = Brushes.Gray; } };
            searchBox.TextChanged += (s, e) =>
            {
                _currentFilter = searchBox.Text == "Filter..." ? string.Empty : searchBox.Text.Trim();
                ApplyFilter();
            };
            controlsStack.Children.Add(searchBox);

            // دکمه Pause / Resume
            var pauseButton = new ToggleButton
            {
                Content = "Pause",
                Height = 22,
                Width = 48,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(2, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            StyleToggleButton(pauseButton);
            pauseButton.Checked += (s, e) => { _isPaused = true; pauseButton.Content = "Resume"; };
            pauseButton.Unchecked += (s, e) => { _isPaused = false; pauseButton.Content = "Pause"; };
            controlsStack.Children.Add(pauseButton);

            // دکمه Export
            var exportButton = new Button
            {
                Content = "Export",
                Height = 22,
                Width = 48,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            StyleStandardButton(exportButton, Color.FromRgb(34, 197, 94), Color.FromRgb(22, 163, 74)); // تِم سبز
            exportButton.Click += ExportButton_Click;
            controlsStack.Children.Add(exportButton);

            // دکمه Clear
            var clearButton = new Button
            {
                Content = "Clear",
                Height = 22,
                Width = 48,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            StyleStandardButton(clearButton, Color.FromRgb(239, 68, 68), Color.FromRgb(220, 38, 38)); // تِم قرمز
            clearButton.Click += (s, e) =>
            {
                _allLogs.Clear();
                _logBox.Clear();
            };
            controlsStack.Children.Add(clearButton);

            mainStack.Children.Add(headerDock);

            // ۲. خط جداکننده افقی زیر عنوان
            var horizontalSeparator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۳. باکس نمایش لاگ‌ها
            _logBox = new TextBox
            {
                Height = 150,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128)), // Green 400
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate 700
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                VerticalContentAlignment = VerticalAlignment.Top
            };

            var logBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                ClipToBounds = true,
                Child = _logBox
            };

            mainStack.Children.Add(logBorder);

            // ۴. کادر دور تا دور کل ویجت
            _borderContainer = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12),
                Child = mainStack
            };
        }

        public FrameworkElement GetControl() => _borderContainer;

        public void UpdateValue(string value)
        {
            if (_isPaused) return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logLine = $"[{timestamp}] {value}";

            Application.Current.Dispatcher.Invoke(() =>
            {
                // ۱. اضافه کردن به دیتابیس لوکال لاگ‌ها
                _allLogs.AppendLine(logLine);

                // کنترل سایز بافر متنی برای جلوگیری از سنگین شدن رم
                if (_allLogs.Length > 100000) // مکانیزم حفاظتی هرس متن
                {
                    _allLogs.Remove(0, 20000);
                }

                // ۲. درج زنده در باکس نمایشی (در صورت تطابق با فیلتر فعال)
                if (string.IsNullOrEmpty(_currentFilter) || logLine.IndexOf(_currentFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logBox.AppendText(logLine + "\r\n");

                    // محدود کردن تعداد خطوط نمایشی در کنترل باکس به حداکثر مجاز
                    if (_logBox.LineCount > MaxLogLines)
                    {
                        var text = _logBox.Text;
                        int firstLineBreak = text.IndexOf(Environment.NewLine);
                        if (firstLineBreak >= 0)
                        {
                            _logBox.Text = text.Substring(firstLineBreak + Environment.NewLine.Length);
                        }
                    }

                    _logBox.ScrollToEnd();
                }
            });
        }

        // سیستم بازنویسی متن بر اساس فیلتر جستجو
        private void ApplyFilter()
        {
            if (string.IsNullOrEmpty(_currentFilter))
            {
                _logBox.Text = _allLogs.ToString();
            }
            else
            {
                var filtered = new StringBuilder();
                using (var reader = new StringReader(_allLogs.ToString()))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.IndexOf(_currentFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            filtered.AppendLine(line);
                        }
                    }
                }
                _logBox.Text = filtered.ToString();
            }
            _logBox.ScrollToEnd();
        }

        // برون‌سپاری لاگ‌ها به فایل متنی متناسب
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_allLogs.ToString()))
            {
                MessageBox.Show("لاگی برای ذخیره‌سازی وجود ندارد.", "هشدار", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt",
                FileName = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.log",
                Title = "ذخیره فایل لاگ ترمینال"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, _allLogs.ToString(), Encoding.UTF8);
                    MessageBox.Show("فایل لاگ با موفقیت ذخیره شد!", "موفقیت", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در ذخیره فایل: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // متدهای اختصاصی استایل‌دهی دکمه‌ها
        private void StyleStandardButton(Button button, Color hoverBg, Color hoverBorder)
        {
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));

            borderFactory.Name = "BtnBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(226, 232, 240)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            var triggerHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            triggerHover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverBg), "BtnBorder"));
            triggerHover.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(hoverBorder), "BtnBorder"));
            triggerHover.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));

            template.Triggers.Add(triggerHover);
            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            style.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(100, 116, 139))));

            button.Style = style;
        }

        private void StyleToggleButton(ToggleButton button)
        {
            var style = new Style(typeof(ToggleButton));
            var template = new ControlTemplate(typeof(ToggleButton));
            var borderFactory = new FrameworkElementFactory(typeof(Border));

            borderFactory.Name = "BtnBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(226, 232, 240)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            var triggerChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            triggerChecked.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(249, 115, 22)), "BtnBorder"));
            triggerChecked.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(234, 88, 12)), "BtnBorder"));
            triggerChecked.Setters.Add(new Setter(ToggleButton.ForegroundProperty, Brushes.White));

            template.Triggers.Add(triggerChecked);
            style.Setters.Add(new Setter(ToggleButton.TemplateProperty, template));
            style.Setters.Add(new Setter(ToggleButton.ForegroundProperty, new SolidColorBrush(Color.FromRgb(100, 116, 139))));

            button.Style = style;
        }
    }
}
