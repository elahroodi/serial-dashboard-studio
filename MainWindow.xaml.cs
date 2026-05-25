using Microsoft.Win32;
using SerialDebugPanel.Core;
using SerialDebugPanel.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;


namespace SerialDebugPanel
{
    public partial class MainWindow : Window
    {
        private readonly SerialManager _serialManager = new();
        private readonly Dictionary<string, IWidget> _widgetRegistry = new();
        private const string RegistryPath = @"Software\SerialDebugPanel\Settings";
        private string _currentConfigPath;

        public MainWindow()
        {
            InitializeComponent();
            SetupSerialControls();

            // ۱. ابتدا پروژه توسط کاربر انتخاب می‌شود
            if (!OpenProjectSelector())
            {
                // اگر کاربر پنجره انتخاب پروژه را بست یا کنسل کرد، نرم‌افزار بسته می‌شود
                Application.Current.Shutdown();
                return;
            }

            _serialManager.DataReceived += OnSerialDataReceived;
            _serialManager.LogOccurred += OnSerialLogOccurred;

            // ذخیره تنظیمات هنگام بستن نرم‌افزار و قطع اتصال پورت
            this.Closing += (s, e) => SaveSettings();
            this.Closed += (s, e) => _serialManager.Disconnect();
        }

        private bool OpenProjectSelector()
        {
            var selector = new ProjectManagerWindow();
            if (selector.ShowDialog() == true)
            {
                _currentConfigPath = selector.SelectedProjectPath;

                // نمایش نام فایل انتخاب شده روی دکمه هدر بدون پسوند .json
                ActiveProfileBtn.Content = System.IO.Path.GetFileNameWithoutExtension(_currentConfigPath);

                // لود کردن ویجت‌ها با پاس دادن مسیر فایل کانفیگ
                LoadWidgets(_currentConfigPath);
                return true;
            }
            return false;
        }

        private void ActiveProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            // باز کردن دوباره دیالوگ انتخاب پروژه برای سوییچ سریع در حین کارکرد برنامه
            OpenProjectSelector();
        }

        private void LoadWidgets(string configPath)
        {
            try
            {
                // پاکسازی UI قبلی برای سوییچ داینامیک و بدون تداخل ویجت‌ها
                ControlsPanel.Children.Clear();
                MonitorsPanel.Children.Clear();
                _widgetRegistry.Clear();

                // ارسال مسیر فایل به فکتوری جهت پردازش
                var (controls, monitors) = WidgetFactory.CreateWidgets(configPath);

                if (controls != null)
                {
                    foreach (var widget in controls)
                    {
                        if (!string.IsNullOrEmpty(widget.Key))
                        {
                            _widgetRegistry[widget.Key] = widget;
                        }

                        ControlsPanel.Children.Add(widget.GetControl());
                        widget.ValueChanged += OnWidgetValueChanged;
                    }
                }

                if (monitors != null)
                {
                    foreach (var widget in monitors)
                    {
                        if (!string.IsNullOrEmpty(widget.Key))
                        {
                            _widgetRegistry[widget.Key] = widget;
                        }

                        MonitorsPanel.Children.Add(widget.GetControl());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading widgets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupSerialControls()
        {
            // ۱. بارگذاری پورت‌های سریال فیزیکی موجود در سیستم
            var availablePorts = _serialManager.GetAvailablePorts();
            PortComboBox.ItemsSource = availablePorts;

            // ۲. پر کردن لیست بادریت‌های رایج برای STM32/Arduino
            var baudRates = new int[] { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
            BaudComboBox.ItemsSource = baudRates;

            // ۳. بازیابی هوشمند آخرین وضعیت ذخیره شده
            RestoreSettings(availablePorts);
        }

        private void RestoreSettings(IList<string> availablePorts)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    string savedPort = key?.GetValue("LastPort", "") as string;
                    string savedBaudStr = key?.GetValue("LastBaud", "115200") as string;

                    // بازیابی پورت: چک کردن اتصال فیزیکی پورت ذخیره شده
                    if (!string.IsNullOrEmpty(savedPort) && availablePorts.Contains(savedPort))
                    {
                        PortComboBox.SelectedItem = savedPort;
                    }
                    else if (PortComboBox.Items.Count > 0)
                    {
                        // اگر پورت قبلی قطع بود، بدون کرش کردن اولین پورت در دسترس را انتخاب کن
                        PortComboBox.SelectedIndex = 0;
                    }

                    // بازیابی بادریت
                    if (int.TryParse(savedBaudStr, out int savedBaud))
                    {
                        BaudComboBox.SelectedItem = savedBaud;
                    }
                    else
                    {
                        BaudComboBox.SelectedItem = 115200; // پیش‌فرض ایمن
                    }
                }
            }
            catch
            {
                // در صورت بروز هرگونه خطای رجیستری، مقادیر پیش‌فرض ست می‌شوند
                if (PortComboBox.Items.Count > 0) PortComboBox.SelectedIndex = 0;
                BaudComboBox.SelectedItem = 115200;
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        if (PortComboBox.SelectedItem != null)
                        {
                            key.SetValue("LastPort", PortComboBox.SelectedItem.ToString()!);
                        }
                        if (BaudComboBox.SelectedItem != null)
                        {
                            key.SetValue("LastBaud", BaudComboBox.SelectedItem.ToString()!);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private void LoadWidgets()
        {
            try
            {
                var (controls, monitors) = WidgetFactory.CreateWidgets();

                if (controls != null)
                {
                    foreach (var widget in controls)
                    {
                        if (!string.IsNullOrEmpty(widget.Key))
                        {
                            _widgetRegistry[widget.Key] = widget;
                        }

                        ControlsPanel.Children.Add(widget.GetControl());
                        widget.ValueChanged += OnWidgetValueChanged;
                    }
                }

                if (monitors != null)
                {
                    foreach (var widget in monitors)
                    {
                        if (!string.IsNullOrEmpty(widget.Key))
                        {
                            _widgetRegistry[widget.Key] = widget;
                        }

                        MonitorsPanel.Children.Add(widget.GetControl());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading widgets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnWidgetValueChanged(object sender, string message)
        {
            if (_serialManager.IsOpen)
            {
                _serialManager.SendData(message);
            }
        }

        private void OnSerialDataReceived(string key, string value)
        {
            Dispatcher.Invoke(() =>
            {
                if (_widgetRegistry.TryGetValue(key, out var widget))
                {
                    widget.UpdateValue(value);
                }
            });
        }

        private void OnSerialLogOccurred(string log)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Log: {log}";
            });
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_serialManager.IsOpen)
            {
                if (PortComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a COM Port.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string port = PortComboBox.SelectedItem.ToString()!;
                int baud = (int)BaudComboBox.SelectedItem;

                try
                {
                    _serialManager.Connect(port, baud);
                    ConnectButton.Content = "Disconnect";
                    ConnectButton.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // قرمز مدرن لایت (#EF4444)

                    // استایل نوار وضعیت سبز ملایم در حالت اتصال
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    StatusText.Text = $"Status: Connected to {port}";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                    BottomStatusBar.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
                    BottomStatusBar.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 252, 231));

                    SaveSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _serialManager.Disconnect();
                ConnectButton.Content = "Connect";
                ConnectButton.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // آبی برند (#2563EB)

                // بازگشت نوار وضعیت به تم خاکستری پیش‌فرض
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                StatusText.Text = "Status: Disconnected";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                BottomStatusBar.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                BottomStatusBar.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
            }
        }


        private void GeneratorButton_Click(object sender, RoutedEventArgs e)
        {
            // ساخت و باز کردن پنجره پیکربندی به عنوان ساب‌ویندو پنجره اصلی
            ConfiguratorWindow configWin = new ConfiguratorWindow();
            configWin.Owner = this;
            configWin.ShowDialog(); // باز شدن به صورت مودال (کاربر را تا بستن پنجره بلاک می‌کند)
        }


    }


}