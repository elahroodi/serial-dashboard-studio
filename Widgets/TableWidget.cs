using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using SerialDebugPanel.Core;

namespace SerialDebugPanel.Widgets
{
    public class TableWidget : IWidget
    {
        private readonly WidgetConfig _config;
        private readonly Border _borderContainer;
        private readonly DataGrid _dataGrid;
        private readonly System.Collections.ObjectModel.ObservableCollection<Dictionary<string, string>> _rows = new();

        // کنترل‌های وضعیت
        private bool _isPaused = false;
        private int _maxHistory = 50; // مقدار پیش‌فرض پیش از خواندن کانفیگ
        private readonly TextBox _limitInput;

        public string Key => _config.variable ?? string.Empty;
        public event EventHandler<string>? ValueChanged;

        public TableWidget(WidgetConfig config)
        {
            _config = config;

            // خواندن مقدار اولیه ظرفیت از کانفیگ (در صورت وجود)
            if (_config.maxHistory > 0)
            {
                
                _maxHistory = (int)_config.maxHistory;
            }

            var mainStack = new StackPanel();

            // ۱. هدر چندمنظوره و پیشرفته ویجت
            var headerDock = new DockPanel
            {
                LastChildFill = false,
                Margin = new Thickness(0, 0, 0, 6)
            };

            // عنوان سمت چپ
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

            // ایجاد پنل دکمه‌ها در سمت راست هدر
            var controlsStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(controlsStack, Dock.Right);
            headerDock.Children.Add(controlsStack);

            // بخش تنظیم سقف تاریخچه (Limit Input)
            var limitLabel = new TextBlock
            {
                Text = "Limit:",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 4, 0)
            };
            controlsStack.Children.Add(limitLabel);

            _limitInput = new TextBox
            {
                Text = _maxHistory.ToString(),
                Width = 35,
                Height = 20,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };
            _limitInput.TextChanged += LimitInput_TextChanged;
            controlsStack.Children.Add(_limitInput);

            // دکمه Pause / Resume
            var pauseButton = new ToggleButton
            {
                Content = "Pause",
                Height = 22,
                Width = 48,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(6, 0, 0, 0),
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
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            StyleStandardButton(exportButton, Color.FromRgb(34, 197, 94), Color.FromRgb(22, 163, 74)); // تِم سبز ملایم
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
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            StyleStandardButton(clearButton, Color.FromRgb(239, 68, 68), Color.FromRgb(220, 38, 38)); // تِم قرمز ملایم
            clearButton.Click += (s, e) => _rows.Clear();
            controlsStack.Children.Add(clearButton);

            mainStack.Children.Add(headerDock);

            // ۲. خط جداکننده افقی
            var horizontalSeparator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(horizontalSeparator);

            // ۳. جدول DataGrid اصلی
            _dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                Height = 160,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Background = Brushes.White,
                BorderThickness = new Thickness(0),
                RowHeight = 28,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserResizeRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            // تنظیم استایل هدر ستون‌ها
            var columnHeaderStyle = new Style(typeof(DataGridColumnHeader));
            columnHeaderStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(71, 85, 105))));
            columnHeaderStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            columnHeaderStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            columnHeaderStyle.Setters.Add(new Setter(Control.FontSizeProperty, 11.0));
            columnHeaderStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 6, 8, 6)));
            columnHeaderStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            _dataGrid.ColumnHeaderStyle = columnHeaderStyle;

            // تنظیم استایل سلول‌ها
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6)));
            cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            cellStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            cellStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85))));
            _dataGrid.CellStyle = cellStyle;

            if (_config.columns != null)
            {
                foreach (var colName in _config.columns)
                {
                    _dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = colName,
                        Binding = new Binding($"[{colName}]"),
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                        ElementStyle = new Style(typeof(TextBlock))
                        {
                            Setters = {
                                new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                                new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                            }
                        }
                    });
                }
            }

            _dataGrid.ItemsSource = _rows;

            var gridBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Child = _dataGrid
            };

            mainStack.Children.Add(gridBorder);

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
            // اگر بروزرسانی متوقف (Pause) شده باشد، داده جدید نادیده گرفته می‌شود
            if (_isPaused) return;

            var parts = value.Split(',');
            if (_config.columns != null && parts.Length >= _config.columns.Count)
            {
                var rowData = new Dictionary<string, string>();
                for (int i = 0; i < _config.columns.Count; i++)
                {
                    rowData[_config.columns[i]] = parts[i].Trim();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _rows.Add(rowData);

                    // اعمال محدودیت پویای سقف سطرها (N)
                    while (_rows.Count > _maxHistory)
                    {
                        _rows.RemoveAt(0);
                    }

                    //if (_rows.Count > 0)
                    //{
                    //    _dataGrid.ScrollIntoView(_rows[_rows.Count - 1]);
                    //}
                });
            }
        }

        // هندلر تغییر محدودیت سطرها به صورت لحظه‌ای
        private void LimitInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(_limitInput.Text, out int result) && result > 0)
            {
                _maxHistory = result;
                // حذف بلافاصله سطرهای مازاد در صورت کم شدن محدودیت
                while (_rows.Count > _maxHistory)
                {
                    _rows.RemoveAt(0);
                }
            }
        }

        // متد ذخیره فایل با فرمت استاندارد CSV
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rows.Count == 0)
            {
                MessageBox.Show("هیچ داده‌ای برای خروجی گرفتن وجود ندارد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"Exported_Data_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "ذخیره فایل اکسل/CSV"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csvContent = new StringBuilder();
                    var headers = _config.columns ?? new List<string>();

                    // نوشتن هدر ستون‌ها
                    csvContent.AppendLine(string.Join(",", headers));

                    // نوشتن داده‌ها
                    foreach (var row in _rows)
                    {
                        var lineValues = new List<string>();
                        foreach (var header in headers)
                        {
                            var cellVal = row.ContainsKey(header) ? row[header] : "";
                            // ایمن‌سازی مقادیری که حاوی کاما یا گیومه هستند
                            if (cellVal.Contains(",") || cellVal.Contains("\"") || cellVal.Contains("\n"))
                            {
                                cellVal = $"\"{cellVal.Replace("\"", "\"\"")}\"";
                            }
                            lineValues.Add(cellVal);
                        }
                        csvContent.AppendLine(string.Join(",", lineValues));
                    }

                    File.WriteAllText(saveFileDialog.FileName, csvContent.ToString(), Encoding.UTF8);
                    MessageBox.Show("فایل با موفقیت ذخیره شد!", "موفقیت", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در ذخیره فایل: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // متدهای استایل‌دهی دکمه‌ها
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

            // رنگ نارنجی زنده برای وضعیت فعال شدن Pause
            var triggerChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            triggerChecked.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(249, 115, 22)), "BtnBorder")); // Orange 500
            triggerChecked.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(234, 88, 12)), "BtnBorder")); // Orange 600
            triggerChecked.Setters.Add(new Setter(ToggleButton.ForegroundProperty, Brushes.White));

            template.Triggers.Add(triggerChecked);
            style.Setters.Add(new Setter(ToggleButton.TemplateProperty, template));
            style.Setters.Add(new Setter(ToggleButton.ForegroundProperty, new SolidColorBrush(Color.FromRgb(100, 116, 139))));

            button.Style = style;
        }
    }
}
