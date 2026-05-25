using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SerialDebugPanel.Widgets
{
    public class AlarmDetailsDialog : Window
    {
        private readonly AlarmWidget _parentWidget;
        private readonly Dictionary<string, string> _definitions;
        private readonly StackPanel _alarmsListStack;
        private readonly TextBlock _headerTitle;

        public AlarmDetailsDialog(AlarmWidget parentWidget, List<string> initialActiveIds, Dictionary<string, string> definitions)
        {
            _parentWidget = parentWidget;
            _definitions = definitions;

            // تنظیمات پنجره دیالوگ
            Title = "Active Alarms";
            Width = 420;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            var rootBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20)
            };

            var mainLayout = new Grid();
            mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // هدر
            var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var warningIcon = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            _headerTitle = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
            };
            headerStack.Children.Add(warningIcon);
            headerStack.Children.Add(_headerTitle);
            Grid.SetRow(headerStack, 0);
            mainLayout.Children.Add(headerStack);

            // بخش اسکرول لیست
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 15)
            };
            _alarmsListStack = new StackPanel();
            scrollViewer.Content = _alarmsListStack;
            Grid.SetRow(scrollViewer, 1);
            mainLayout.Children.Add(scrollViewer);

            // دکمه Close
            var closeButton = new Button
            {
                Content = "Close",
                Width = 90,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Foreground = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeButton.Click += (s, e) => Close();
            Grid.SetRow(closeButton, 2);
            mainLayout.Children.Add(closeButton);

            rootBorder.Child = mainLayout;
            Content = rootBorder;

            // بارگذاری اولیه داده‌ها
            PopulateAlarms(initialActiveIds);

            // سابسکرایب به رویداد آپدیت زنده
            _parentWidget.AlarmsUpdated += OnAlarmsUpdated;

            // آن‌سابسکرایب هنگام بسته شدن برای جلوگیری از Memory Leak
            Closed += (s, e) => _parentWidget.AlarmsUpdated -= OnAlarmsUpdated;
        }

        private void OnAlarmsUpdated(List<string> activeIds)
        {
            // انتقال به ترد UI برای جلوگیری از کرش در کار با سریال پورت (Multi-threading)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PopulateAlarms(activeIds);
            }));
        }

        private void PopulateAlarms(List<string> activeIds)
        {
            _alarmsListStack.Children.Clear();
            _headerTitle.Text = activeIds.Count > 0 ? $"System Alarms ({activeIds.Count})" : "System Alarms";

            if (activeIds.Count == 0)
            {
                _alarmsListStack.Children.Add(new TextBlock
                {
                    Text = "✓ No active alarms detected.",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                    FontWeight = FontWeights.Medium,
                    Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            else
            {
                foreach (var id in activeIds)
                {
                    string name = _definitions.ContainsKey(id) ? _definitions[id] : $"Unknown Error (Code {id})";

                    var rowBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(15, 239, 68, 68)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 10, 12, 10),
                        Margin = new Thickness(0, 0, 0, 6)
                    };

                    var rowGrid = new Grid();
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var codeBlock = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 10, 0),
                        Child = new TextBlock
                        {
                            Text = $"ID {id}",
                            FontSize = 9.5,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White
                        }
                    };
                    Grid.SetColumn(codeBlock, 0);
                    rowGrid.Children.Add(codeBlock);

                    var alarmText = new TextBlock
                    {
                        Text = name,
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(alarmText, 1);
                    rowGrid.Children.Add(alarmText);

                    rowBorder.Child = rowGrid;
                    _alarmsListStack.Children.Add(rowBorder);
                }
            }
        }
    }
}
